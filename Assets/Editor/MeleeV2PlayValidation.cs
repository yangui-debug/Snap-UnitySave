#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
/// <summary>
/// 显式菜单或 Temp/MeleeV2.request 单次请求才运行。实际目标场景播放20秒，用临时检测器接线。
/// 不保存场景；拒绝覆盖脏场景和接管用户的播放，结束恢复临时日志/后台设置并退出本次播放。
/// </summary>
[InitializeOnLoad]
public static class MeleeV2PlayValidation
{
    private const string Key = "MeleeV2PlayValidation.running", Request = "Temp/MeleeV2.request", Result = "Temp/MeleeV2-results.json";
    private const string Scene = "Assets/20261012-PVE守关敌万军/20261012-PVE收关敌万军.unity";
    private static double nextCheck, start, nextSample;
    private static Report report;
    private static BlueShieldAdvance blue;
    private static bool background;
    private static StackTraceLogType errorStack, warningStack;
    private static readonly HashSet<int> seenRed = new HashSet<int>(), seenBlue = new HashSet<int>();
    private static readonly HashSet<string> states = new HashSet<string>(), errors = new HashSet<string>();
    [Serializable] private sealed class Report
    {
        public string status;
        public float seconds, maxForward, maxBackward, timeScale;
        public bool resumedInitialTimeline;
        public int redUnits, blueUnits, boundaryViolations, leftOrRangedControlled, oldControllerActive, maxRedCount, maxBlueCount;
        public string[] assertions, observedStates, runtimeErrors;
        public string[] timelineSamples, shieldSamples;
        public string note = "蓝方检测器仅在本次播放中临时创建；正式场景仍等待用户拖入触发器。";
    }
    static MeleeV2PlayValidation()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += Changed;
        EditorApplication.delayCall += () => { if (SessionState.GetBool(Key, false) && EditorApplication.isPlaying) Begin(); };
    }
    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextCheck || EditorApplication.isCompiling) return;
        nextCheck = EditorApplication.timeSinceStartup + 1d;
        if (!SessionState.GetBool(Key, false) && File.Exists(Request)) { File.Delete(Request); Run(); }
    }
    [MenuItem("Tools/新版桥口近战/运行20秒场景验证")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) { Refuse("已有播放或编译，未接管。"); return; }
        if (UnityEngine.SceneManagement.SceneManager.sceneCount != 1 || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != Scene)
        { Refuse("目标场景未单独打开。"); return; }
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) { Refuse("存在未保存场景，未覆盖。"); return; }
        EditorSceneManager.OpenScene(Scene);
        SessionState.SetBool(Key, true); SessionState.SetBool(Key + ".background", Application.runInBackground);
        SessionState.SetInt(Key + ".error", (int)Application.GetStackTraceLogType(LogType.Error));
        SessionState.SetInt(Key + ".warning", (int)Application.GetStackTraceLogType(LogType.Warning));
        EditorApplication.isPlaying = true;
    }
    private static void Refuse(string reason) { File.WriteAllText(Result, JsonUtility.ToJson(new Report { status = "refused: " + reason }, true)); }
    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) Begin();
        if (state == PlayModeStateChange.ExitingPlayMode && report != null && report.status == "running") Finish("interrupted");
    }
    private static void Begin()
    {
        if (report != null && report.status == "running") return;
        report = new Report { status = "running" }; seenRed.Clear(); seenBlue.Clear(); states.Clear(); errors.Clear();
        background = SessionState.GetBool(Key + ".background", Application.runInBackground); Application.runInBackground = true;
        errorStack = (StackTraceLogType)SessionState.GetInt(Key + ".error", (int)StackTraceLogType.ScriptOnly); warningStack = (StackTraceLogType)SessionState.GetInt(Key + ".warning", (int)StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None); Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        captured = false; report.assertions = MeleeV2Assertions.Run();
        blue = UnityEngine.Object.FindObjectOfType<BlueShieldAdvance>();
        if (blue != null && blue.detectionTrigger == null)
        {
            var go = new GameObject("仅本次验收_临时右桥检测器"); go.transform.position = new Vector3(9, 1, -12);
            var box = go.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(9, 7, 11); blue.detectionTrigger = box;
        }
        start = EditorApplication.timeSinceStartup; nextSample = 0;
        Application.logMessageReceived += Log;
        EditorApplication.update -= Sample; EditorApplication.update += Sample; Write();
    }
    private static void Log(string message, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + (type == LogType.Exception ? "\n" + stack : "")); }
    private static bool captured;
    private static void Sample()
    {
        if (!EditorApplication.isPlaying) { Finish("interrupted"); return; }
        if (EditorApplication.isPaused) EditorApplication.isPaused = false;
        if (EditorApplication.timeSinceStartup - start > 180d || Time.time >= 20f)
        { Finish(Time.time >= 20f ? "completed" : "wall_time_limit"); EditorApplication.isPlaying = false; return; }
        if (EditorApplication.timeSinceStartup < nextSample) return;
        nextSample = EditorApplication.timeSinceStartup + .1d; report.seconds = Time.time; report.timeScale = Time.timeScale;
        // Recorder 的编辑器启动异常有时将自动播放的 Timeline 留在 0 秒。
        // 只恢复本次验收、本来勾选自动播放且从未开始的 Director；不修改资源、录制设置或正常受击暂停。
        if (!report.resumedInitialTimeline && EditorApplication.timeSinceStartup - start > 2d)
        {
            foreach (var d in UnityEngine.Object.FindObjectsOfType<UnityEngine.Playables.PlayableDirector>())
                if (d.playOnAwake && d.time < .0001d && d.state != UnityEngine.Playables.PlayState.Playing)
                { d.Play(); report.resumedInitialTimeline = true; }
        }
        if (!captured && Time.time >= 10f) { ScreenCapture.CaptureScreenshot("Temp/MeleeV2_10s.png"); captured = true; }
        foreach (var u in UnityEngine.Object.FindObjectsOfType<RedMeleeSelf>())
        {
            if (!u.Controlled) continue; seenRed.Add(u.GetInstanceID()); states.Add("红:" + u.State);
            var a = u.GetComponent<SoldierAnimationController>(); if (u.transform.position.x < 4f || a.是远程单位) report.leftOrRangedControlled++;
        }
        foreach (var m in UnityEngine.Object.FindObjectsOfType<BlueShieldMember>())
        {
            if (!m.Live || m.Owner != blue) continue; seenBlue.Add(m.GetInstanceID()); states.Add("蓝:" + m.State);
            float offset = Vector3.Dot(m.MotionRoot.position - m.Home, blue.Axis);
            report.maxForward = Mathf.Max(report.maxForward, offset); report.maxBackward = Mathf.Max(report.maxBackward, -offset);
            if (offset > blue.maxForward + .05f || offset < -blue.maxBackward - .05f) report.boundaryViolations++;
        }
        foreach (var old in UnityEngine.Object.FindObjectsOfType<RightBridgeMeleeDirector>()) if (old.Active) report.oldControllerActive++;
        if (blue != null) { report.maxRedCount = Mathf.Max(report.maxRedCount, blue.RedCount); report.maxBlueCount = Mathf.Max(report.maxBlueCount, blue.BlueCount); }
        report.redUnits = seenRed.Count; report.blueUnits = seenBlue.Count; Write();
        var timeline = new List<string>();
        foreach (var d in UnityEngine.Object.FindObjectsOfType<UnityEngine.Playables.PlayableDirector>())
            timeline.Add(d.name + " state=" + d.state + " time=" + d.time + " playOnAwake=" + d.playOnAwake);
        report.timelineSamples = timeline.ToArray();
        var shields = new List<string>();
        foreach (var m in UnityEngine.Object.FindObjectsOfType<BlueShieldMember>())
        {
            if (shields.Count >= 6 || !m.Live || m.Owner != blue) continue;
            shields.Add(m.name + " home=" + m.Home + " pos=" + m.transform.position + " root=" + m.MotionRoot.name + " first=" + m.IsFirstRow + " state=" + m.State + " region=" + blue.controlCenter + "/" + blue.controlSize);
        }
        report.shieldSamples = shields.ToArray();
    }
    private static void Write()
    { report.observedStates = new List<string>(states).ToArray(); report.runtimeErrors = new List<string>(errors).ToArray(); File.WriteAllText(Result, JsonUtility.ToJson(report, true)); }
    private static void Finish(string status)
    {
        EditorApplication.update -= Sample; Application.logMessageReceived -= Log;
        if (report != null) { report.status = status; report.seconds = Time.time; Write(); }
        Application.runInBackground = background;
        Application.SetStackTraceLogType(LogType.Error, errorStack); Application.SetStackTraceLogType(LogType.Warning, warningStack);
        SessionState.SetBool(Key, false);
    }
}
#endif
