#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 右桥实际场景验收工具。菜单手动运行；自动运行仅在 Temp/RightBridgeValidation.request 存在时触发一次。
/// 不保存场景、不更改场景资源。拒绝覆盖未保存场景或接管用户已开始的播放。
/// 结果输出到 Temp/RightBridgeValidation.json；运行后恢复原 runInBackground 配置并退出自己启动的播放。
/// </summary>
[InitializeOnLoad]
public static class RightBridgeMeleeValidation
{
    private const string ScenePath = "Assets/20261012-PVE守关敌万军/20261012-PVE收关敌万军.unity";
    private const string Request = "Temp/RightBridgeValidation.request";
    private const string Result = "Temp/RightBridgeValidation.json";
    private const string RunningKey = "RightBridgeMeleeValidation.Running";
    private static double start;
    private static double nextSample;
    private static bool oldBackground;
    private static Report report;
    private static int nextCaptureSecond;
    private static float nextDuration = 45f;
    private static StackTraceLogType oldErrorStack;
    private static StackTraceLogType oldWarningStack;
    private static readonly HashSet<int> seenRed = new HashSet<int>();
    private static readonly HashSet<int> seenShield = new HashSet<int>();
    private static readonly Dictionary<int, Vector3> previousPositions = new Dictionary<int, Vector3>();
    private static readonly HashSet<int> hitUnits = new HashSet<int>();
    private static readonly HashSet<int> waitedUnits = new HashSet<int>();
    private static readonly HashSet<int> advancedUnits = new HashSet<int>();
    private static readonly List<string> errors = new List<string>();

    [Serializable]
    private sealed class Report
    {
        public string status;
        public string scene;
        public string note;
        public float simulatedSeconds;
        public float requestedSeconds;
        public int samples;
        public bool editorPaused;
        public float timeScale;
        public int shieldUnits;
        public int redUnits;
        public int unitsObservedHit;
        public int unitsObservedWaiting;
        public float maxShieldAdvance;
        public float maxShieldRetreat;
        public float largestObservedHitStep;
        public int shieldBoundaryViolations;
        public int leftSideUnitsIncorrectlyControlled;
        public int rangedUnitsIncorrectlyControlled;
        public string[] runtimeErrors;
        public string[] movementDecisions;
        public string[] isolatedAssertions;
        public string[] shieldSamples;
        public int shieldUnitsObservedFollowing;
        public float cumulativeShieldFollowDistance;
    }

    static RightBridgeMeleeValidation()
    {
        EditorApplication.delayCall += Initialize;
        EditorApplication.update += CheckRequest;
    }

    private static double nextRequestCheck;
    private static void CheckRequest()
    {
        if (EditorApplication.timeSinceStartup < nextRequestCheck || EditorApplication.isCompiling || SessionState.GetBool(RunningKey, false)) return;
        nextRequestCheck = EditorApplication.timeSinceStartup + 1d;
        if (!File.Exists(Request)) return;
        if (!float.TryParse(File.ReadAllText(Request).Trim(), out nextDuration)) nextDuration = 45f;
        nextDuration = Mathf.Clamp(nextDuration, 5f, 60f);
        File.Delete(Request);
        Run();
    }

    private static void Initialize()
    {
        // 请求文件在编译前写入时，本回调在域重载后只消费一次，不在正常编辑时反复启动播放。
        EditorApplication.playModeStateChanged -= OnPlayState;
        EditorApplication.playModeStateChanged += OnPlayState;
        if (SessionState.GetBool(RunningKey, false))
        {
            if (EditorApplication.isPlaying) BeginSampling();
            else if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged += OnPlayState;
                EditorApplication.update += AwaitPlay;
                start = EditorApplication.timeSinceStartup;
            }
            else
            {
                // 用户停止播放或脚本重载中断了验收时，不留下永久 Running 锁。
                SessionState.SetBool(RunningKey, false);
                if (File.Exists(Result))
                {
                    report = JsonUtility.FromJson<Report>(File.ReadAllText(Result));
                    report.status = "interrupted";
                    File.WriteAllText(Result, JsonUtility.ToJson(report, true));
                }
            }
        }
        else if (File.Exists(Request))
        {
            CheckRequest(); // 仅消费本验收工具的一次性请求文件。
        }
    }

    [MenuItem("Tools/右桥近战/运行45秒实际场景验收")]
    public static void Run()
    {
        report = new Report { scene = ScenePath };
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        { WriteRefusal("Unity 已在播放或编译，未接管现有会话。"); return; }
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
            { WriteRefusal("存在未保存的场景修改，未覆盖或保存用户内容。"); return; }
        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (active.path != ScenePath || UnityEngine.SceneManagement.SceneManager.sceneCount != 1)
        { WriteRefusal("请先单独打开目标场景；未切换其他场景。"); return; }
        // 重新读取已保存的目标场景，使磁盘上新增的管理器确定参与本次验收。
        EditorSceneManager.OpenScene(ScenePath);
        SessionState.SetBool(RunningKey, true);
        SessionState.SetFloat(RunningKey + ".Duration", nextDuration);
        nextDuration = 45f;
        SessionState.SetBool(RunningKey + ".Background", Application.runInBackground);
        SessionState.SetInt(RunningKey + ".ErrorStack", (int)Application.GetStackTraceLogType(LogType.Error));
        SessionState.SetInt(RunningKey + ".WarningStack", (int)Application.GetStackTraceLogType(LogType.Warning));
        EditorApplication.playModeStateChanged += OnPlayState;
        start = EditorApplication.timeSinceStartup;
        EditorApplication.update += AwaitPlay;
        EditorApplication.isPlaying = true;
        EditorApplication.isPaused = false;
    }

    private static void AwaitPlay()
    {
        if (EditorApplication.isPlaying) { EditorApplication.update -= AwaitPlay; BeginSampling(); }
        else if (EditorApplication.timeSinceStartup - start > 90d)
        { EditorApplication.update -= AwaitPlay; SessionState.SetBool(RunningKey, false); WriteRefusal("90秒内未能进入播放，请检查 Console。"); }
    }

    private static void OnPlayState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RunningKey, false)) BeginSampling();
        if (state == PlayModeStateChange.ExitingPlayMode && report != null && report.status == "running") Finish("interrupted");
    }

    private static void BeginSampling()
    {
        if (report != null && report.status == "running") return;
        report = new Report { status = "running", scene = ScenePath, requestedSeconds = SessionState.GetFloat(RunningKey + ".Duration", 45f) };
        report.isolatedAssertions = RightBridgeMeleeAssertions.Run();
        seenRed.Clear(); seenShield.Clear(); previousPositions.Clear(); hitUnits.Clear(); waitedUnits.Clear(); advancedUnits.Clear(); errors.Clear();
        oldBackground = SessionState.GetBool(RunningKey + ".Background", Application.runInBackground);
        Application.runInBackground = true;
        // 仅本次自动验收临时关闭重复日志堆栈，避免原场景的缺失标签刷屏拖慢模拟。
        // 错误消息仍全部采集；结束后恢复原日志配置，不写项目设置。
        oldErrorStack = (StackTraceLogType)SessionState.GetInt(RunningKey + ".ErrorStack", (int)StackTraceLogType.ScriptOnly);
        oldWarningStack = (StackTraceLogType)SessionState.GetInt(RunningKey + ".WarningStack", (int)StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        start = EditorApplication.timeSinceStartup;
        nextSample = 0d;
        nextCaptureSecond = 10;
        EditorApplication.update -= AwaitPlay;
        EditorApplication.update -= Sample;
        EditorApplication.update += Sample;
        Application.logMessageReceived += Log;
        File.WriteAllText(Result, JsonUtility.ToJson(report, true));
    }

    private static void Sample()
    {
        if (!EditorApplication.isPlaying) { Finish("interrupted"); return; }
        if (EditorApplication.timeSinceStartup - start > 240d) { Finish("wall_time_limit"); EditorApplication.isPlaying = false; return; }
        if (Time.time >= report.requestedSeconds) { Finish("completed"); EditorApplication.isPlaying = false; return; }
        if (EditorApplication.timeSinceStartup < nextSample) return;
        nextSample = EditorApplication.timeSinceStartup + 0.05d;
        report.samples++;
        report.simulatedSeconds = Time.time;
        report.editorPaused = EditorApplication.isPaused;
        report.timeScale = Time.timeScale;
        if (Time.time >= nextCaptureSecond && nextCaptureSecond <= 40)
        {
            // 捕获游戏渲染结果供视觉复核，不截取桌面或其他应用，不保存任何场景修改。
            ScreenCapture.CaptureScreenshot("Temp/RightBridgeMelee_" + nextCaptureSecond + "s.png");
            nextCaptureSecond += 10;
        }
        var decisions = new Dictionary<string, int>();
        var shieldSamples = new List<string>();
        // 本工具自己启动的会话允许从 Console Error Pause 恢复，防止首个已有错误使整场验收停在开局。
        if (EditorApplication.isPaused) EditorApplication.isPaused = false;
        foreach (var unit in UnityEngine.Object.FindObjectsOfType<RightBridgeMeleeUnit>())
        {
            if (!unit.Controlled) continue;
            string decision = (unit.IsShield ? "shield:" : "red:") + unit.LastMovementDecision;
            decisions.TryGetValue(decision, out int decisionCount); decisions[decision] = decisionCount + 1;
            int id = unit.GetInstanceID();
            if (unit.IsShield) seenShield.Add(id); else seenRed.Add(id);
            if (unit.IsShield && shieldSamples.Count < 12 && unit.Controller.CurrentTarget != null)
            {
                var rb = unit.GetComponent<Rigidbody>();
                var anim = unit.GetComponent<Animator>();
                Vector3 targetDelta = unit.Controller.CurrentTarget.position - unit.transform.position;
                shieldSamples.Add(unit.name + " gap=" + Vector3.Distance(unit.transform.position, unit.Controller.CurrentTarget.position).ToString("F2") +
                    " flat=" + Vector3.ProjectOnPlane(targetDelta, Vector3.up).magnitude.ToString("F2") +
                    " axial=" + Vector3.Dot(targetDelta, unit.Director.Axis).ToString("F2") +
                    " stop=" + unit.Director.shieldStopDistance.ToString("F2") + " decision=" + unit.LastMovementDecision +
                    " motionRoot=" + unit.MotionRoot.name + " ownLocal=" + unit.transform.localPosition +
                    " rb=" + (rb != null ? (rb.isKinematic + "/" + rb.constraints + "/" + rb.velocity) : "none") +
                    " rootMotion=" + (anim != null && anim.applyRootMotion));
            }
            if (!unit.IsShield && unit.transform.position.x < unit.Director.combatCenter.x - unit.Director.combatSize.x * 0.5f)
                report.leftSideUnitsIncorrectlyControlled++;
            if (unit.Controller.是远程单位) report.rangedUnitsIncorrectlyControlled++;
            if (unit.HitMovementActive)
            {
                hitUnits.Add(id);
                if (previousPositions.TryGetValue(id, out Vector3 before))
                    report.largestObservedHitStep = Mathf.Max(report.largestObservedHitStep, Vector3.Distance(before, unit.transform.position));
            }
            if (unit.WaitingForRefill) waitedUnits.Add(id);
            if (unit.IsShield && unit.HasFixedAnchor && unit.LastMovementDecision == "move" && previousPositions.TryGetValue(id, out Vector3 previous))
            {
                float forwardStep = Vector3.Dot(unit.transform.position - previous, unit.Director.Axis);
                if (forwardStep > 0.00001f) { advancedUnits.Add(id); report.cumulativeShieldFollowDistance += forwardStep; }
            }
            previousPositions[id] = unit.transform.position;
            if (unit.IsShield && unit.HasFixedAnchor)
            {
                float offset = Vector3.Dot(unit.transform.position - unit.FixedAnchor, unit.Director.Axis);
                report.maxShieldAdvance = Mathf.Max(report.maxShieldAdvance, offset);
                report.maxShieldRetreat = Mathf.Max(report.maxShieldRetreat, -offset);
                if (offset > unit.Director.maxAdvance + 0.05f || offset < -unit.Director.maxRetreat - 0.05f)
                    report.shieldBoundaryViolations++;
            }
        }
        report.shieldUnits = seenShield.Count; report.redUnits = seenRed.Count;
        report.unitsObservedHit = hitUnits.Count; report.unitsObservedWaiting = waitedUnits.Count;
        report.shieldUnitsObservedFollowing = advancedUnits.Count;
        report.runtimeErrors = errors.ToArray();
        var decisionLines = new List<string>();
        foreach (var pair in decisions) decisionLines.Add(pair.Key + " = " + pair.Value);
        report.movementDecisions = decisionLines.ToArray();
        report.shieldSamples = shieldSamples.ToArray();
        if (report.samples % 20 == 0) File.WriteAllText(Result, JsonUtility.ToJson(report, true));
    }

    private static void Log(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        string detail = message + "\n" + stack;
        if (errors.Count < 20 && !errors.Contains(detail)) errors.Add(detail);
    }

    private static void Finish(string status)
    {
        EditorApplication.update -= Sample;
        Application.logMessageReceived -= Log;
        Application.runInBackground = oldBackground;
        Application.SetStackTraceLogType(LogType.Error, oldErrorStack);
        Application.SetStackTraceLogType(LogType.Warning, oldWarningStack);
        SessionState.SetBool(RunningKey, false);
        report.status = status;
        report.runtimeErrors = errors.ToArray();
        report.note = "实际场景自动采样；未修改伤害或生成配置。数值检查不等于已完成人眼视觉验收。";
        File.WriteAllText(Result, JsonUtility.ToJson(report, true));
    }

    private static void WriteRefusal(string note)
    {
        if (report == null) report = new Report { scene = ScenePath };
        report.status = "not_run"; report.note = note;
        File.WriteAllText(Result, JsonUtility.ToJson(report, true));
    }
}
#endif
