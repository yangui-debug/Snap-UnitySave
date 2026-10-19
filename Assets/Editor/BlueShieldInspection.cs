#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>显式请求时只读导出当前蓝盾接线与运行状态；不切换播放、不保存或重新加载场景。</summary>
[InitializeOnLoad]
public static class BlueShieldInspection
{
    private const string Request = "Temp/BlueShieldInspect.request", Result = "Temp/BlueShieldInspect.json";
    private static double next;
    private const string ProbeKey = "BlueShieldInspection.probe";
    static BlueShieldInspection()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += state =>
        {
            // 用户若提前停止本次诊断播放，立即释放标记；不能影响用户下一次自行播放。
            if (state != PlayModeStateChange.ExitingPlayMode || !SessionState.GetBool(ProbeKey, false)) return;
            SessionState.SetBool(ProbeKey, false);
            Application.runInBackground = SessionState.GetBool(ProbeKey + ".background", false);
        };
    }
    [Serializable] private sealed class Snapshot
    {
        public bool playing, paused, dirty;
        public float time;
        public string[] entries;
    }
    private static string PathOf(Transform t)
    { return t == null ? "null" : t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name; }
    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling) return;
        next = EditorApplication.timeSinceStartup + .5;
        if (File.Exists("Temp/BlueShieldProbe.request"))
        {
            File.Delete("Temp/BlueShieldProbe.request");
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // 保留用户尚未保存的接线；直接使用当前场景进入播放，绝不 OpenScene/SaveScene。
                SessionState.SetBool(ProbeKey, true); SessionState.SetInt(ProbeKey + ".mask", 0);
                SessionState.SetBool(ProbeKey + ".assertions", false);
                SessionState.SetBool(ProbeKey + ".background", Application.runInBackground);
                SessionState.SetFloat(ProbeKey + ".wall", (float)EditorApplication.timeSinceStartup);
                EditorApplication.isPlaying = true;
            }
        }
        if (SessionState.GetBool(ProbeKey, false) && EditorApplication.isPlaying)
        {
            Application.runInBackground = true;
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
            if (!SessionState.GetBool(ProbeKey + ".assertions", false))
            {
                SessionState.SetBool(ProbeKey + ".assertions", true);
                File.WriteAllText("Temp/BlueShieldProbe-assertions.json", JsonUtility.ToJson(new Snapshot { entries = MeleeV2Assertions.Run() }, true));
            }
            if (Time.time > 1f)
                foreach (var d in UnityEngine.Object.FindObjectsOfType<UnityEngine.Playables.PlayableDirector>())
                    if (d.playOnAwake && d.time < .0001 && d.state != UnityEngine.Playables.PlayState.Playing) d.Play();
            int mask = SessionState.GetInt(ProbeKey + ".mask", 0);
            for (int i = 0; i < 6; i++)
            {
                int second = 4 + i * 2;
                if (Time.time < second || (mask & (1 << i)) != 0) continue;
                Inspect(); File.Copy(Result, "Temp/BlueShieldProbe-" + second + ".json", true);
                mask |= 1 << i; SessionState.SetInt(ProbeKey + ".mask", mask);
            }
            if (Time.time >= 14f || EditorApplication.timeSinceStartup - SessionState.GetFloat(ProbeKey + ".wall", 0) > 150)
            {
                SessionState.SetBool(ProbeKey, false);
                Application.runInBackground = SessionState.GetBool(ProbeKey + ".background", false);
                EditorApplication.isPlaying = false;
            }
        }
        if (!File.Exists(Request)) return;
        File.Delete(Request); Inspect();
    }
    [MenuItem("Tools/新版桥口近战/只读导出当前盾兵状态")]
    public static void Inspect()
    {
        var entries = new List<string>();
        foreach (var owner in UnityEngine.Object.FindObjectsOfType<BlueShieldAdvance>())
        {
            entries.Add("manager=" + PathOf(owner.transform) + " active=" + owner.Active + " trigger=" + (owner.detectionTrigger != null ? owner.detectionTrigger.bounds.ToString() : "null") +
                " axis=" + owner.Axis + " control=" + owner.controlCenter + "/" + owner.controlSize + " red/blue=" + owner.RedCount + "/" + owner.BlueCount + " maxForward=" + owner.maxForward + " maxLateral=" + owner.maxLateral);
            for (int i = 0; i < owner.firstRow.Length; i++)
            {
                var a = owner.firstRow[i];
                if (a == null) { entries.Add("manual[" + i + "]=null"); continue; }
                entries.Add("manual[" + i + "]=" + PathOf(a.transform) + " id=" + a.GetInstanceID() + " enabled=" + a.isActiveAndEnabled + " faction=" + a.myFaction + " ranged=" + a.是远程单位 + " underOwner=" + a.transform.IsChildOf(owner.transform));
            }
            foreach (var a in owner.GetComponentsInChildren<SoldierAnimationController>(true))
            {
                if (!a.isActiveAndEnabled) continue;
                var m = a.GetComponent<BlueShieldMember>(); var ai = a.GetComponent<SoldierAIMovement>();
                string line = "unit=" + PathOf(a.transform) + " id=" + a.GetInstanceID() + " manual=" + Array.IndexOf(owner.firstRow, a) + " ai=" + (ai != null && ai.isActiveAndEnabled) + " pos=" + a.transform.position;
                if (m != null && m.Owner != null && m.MotionRoot != null)
                {
                    Vector3 offset = m.MotionRoot.position - m.Home;
                    line += " first=" + m.IsFirstRow + " live=" + m.Live + " state=" + m.State + " home=" + m.Home + " scope=" + owner.ContainsHome(m.Home) + " root=" + PathOf(m.MotionRoot) + " offset=" + offset;
                    line += " target=" + (m.Target != null ? PathOf(m.Target.transform) + " pos=" + m.Target.transform.position : "null");
                    if (Application.isPlaying && owner.Active)
                    {
                        Vector3 d = m.Target != null ? Vector3.ProjectOnPlane(m.Target.transform.position - a.transform.position, Vector3.up).normalized : owner.Axis;
                        Vector3 safe = (Vector3)typeof(BlueShieldMember).GetMethod("Limited", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(m, new object[] { d * owner.advanceSpeed });
                        line += " desired=" + d * owner.advanceSpeed + " allowed=" + safe;
                    }
                }
                else line += " member=unbound";
                entries.Add(line);
            }
        }
        File.WriteAllText(Result, JsonUtility.ToJson(new Snapshot { playing = EditorApplication.isPlaying, paused = EditorApplication.isPaused, time = Time.time,
            dirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, entries = entries.ToArray() }, true));
    }
}
#endif
