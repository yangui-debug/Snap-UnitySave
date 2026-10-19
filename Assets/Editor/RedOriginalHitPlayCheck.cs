#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>在当前场景播放中通过真正的 SoldierSpawner 生成测试克隆，验证原生命脚本击退；不保存、重开场景或改蓝方设置。</summary>
public sealed class RedOriginalHitPlayCheck
{
    [Serializable] public sealed class Case
    {
        public string name, source;
        public float templateDistance, cloneDistance, actualDistance;
        public float disabledDrift;
        public bool inherited, movementPaused, lockReleased, passed;
    }
    [Serializable] public sealed class Report
    {
        public string status, template;
        public List<Case> cases = new List<Case>();
        public string[] assertions;
        public int actualRedHitUnits, actualRedWaitingUnits;
        public bool queueLeavesEnoughRoom, rearDidNotPush, sceneWasDirty;
    }
    private readonly List<GameObject> temporary = new List<GameObject>();
    private readonly HashSet<int> testIds = new HashSet<int>(), hitIds = new HashSet<int>(), waitingIds = new HashSet<int>();
    private readonly Dictionary<int, Vector3> previous = new Dictionary<int, Vector3>();
    private Report report;
    private SoldierSpawner testSpawner;
    private SoldierHealth templateHealth;
    private float originalTemplateDistance;
    private bool ownsPlay, oldBackground;
    private double wallStart;
    private Vector3 origin = new Vector3(1000f, 1000f, 1000f);
    private int sequence;
    private bool resumedInitialTimeline;
    private readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
    private float waitUntil;
    public bool IsRunning => report != null && report.status == "running";
    private void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        LateUpdate();
        if (Time.time < waitUntil) return;
        try
        {
            while (routines.Count > 0)
            {
                var top = routines.Peek();
                if (!top.MoveNext()) { routines.Pop(); continue; }
                if (top.Current is IEnumerator nested) { routines.Push(nested); continue; }
                if (top.Current is WaitForSeconds wait)
                    waitUntil = Time.time + (float)typeof(WaitForSeconds).GetField("m_Seconds", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(wait);
                return;
            }
        }
        catch (Exception ex) { report.template += "\n" + ex; Finish("validation_error"); }
    }
    public void Begin(bool own)
    {
        ownsPlay = own; oldBackground = Application.runInBackground; Application.runInBackground = true;
        wallStart = EditorApplication.timeSinceStartup;
        report = new Report { status = "running", sceneWasDirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty };
        Save();
        routines.Push(Checks()); EditorApplication.update += Tick;
    }
    private IEnumerator Checks()
    {
        // 等一帧让场景本来的 Start 全部执行完，不能在进入播放的域重载阶段启动测试生成器。
        yield return null;
        foreach (var spawner in UnityEngine.Object.FindObjectsOfType<SoldierSpawner>())
        {
            if (spawner.soldierPrefab == null || !spawner.soldierPrefab.name.Contains("红补兵")) continue;
            var h = spawner.soldierPrefab.GetComponent<SoldierHealth>();
            if (h == null || spawner.soldierPrefab.GetComponent<SoldierAIMovement>() == null) continue;
            templateHealth = h; report.template = spawner.soldierPrefab.name;
            var go = new GameObject("仅验收_临时红兵生成器"); temporary.Add(go);
            testSpawner = go.AddComponent<SoldierSpawner>(); testSpawner.enabled = false;
            testSpawner.soldierPrefab = spawner.soldierPrefab; testSpawner.spawnAreaSize = Vector3.zero;
            testSpawner.生成模式 = 生成模式类型.区域随机分布生成; testSpawner.生成形状 = 生成形状类型.方形;
            var setup = go.AddComponent<RedMeleeSpawnSetup>();
            setup.redSettings.center = origin; setup.redSettings.size = Vector3.one * 200f;
            setup.redSettings.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            break;
        }
        if (templateHealth == null) { Finish("no_original_red_template"); yield break; }
        originalTemplateDistance = templateHealth.击退距离;
        report.assertions = MeleeV2Assertions.Run();
        yield return RunCase("修改原模板为0.25米后新生成克隆", .25f, SoldierDamageSource.Melee);
        yield return RunCase("运行中改原模板为0.75米后再生成", .75f, SoldierDamageSource.Melee);
        yield return RunCase("远程共用同一个0.75米击退距离", .75f, SoldierDamageSource.Ranged);
        yield return RunCase("原模板0米关闭击退", 0f, SoldierDamageSource.Melee);
        yield return RunCase("击退时长短于一帧仍执行剩余路程", .75f, SoldierDamageSource.Melee, false, .001f);
        yield return RunCase("前方真实障碍限制击退但不穿透", .75f, SoldierDamageSource.Melee, true);
        var restarted = Spawn(.75f);
        if (restarted != null)
        {
            restarted.TakeDamage(1, Vector3.forward, SoldierDamageSource.Melee);
            yield return null;
            restarted.enabled = false;
            var stoppedBody = restarted.GetComponent<Rigidbody>();
            // 用刚体真实位置判断协程是否仍在位移，避免把 Rigidbody 插值的视觉追帧误判成继续击退。
            Vector3 stoppedAt = stoppedBody != null ? stoppedBody.position : restarted.transform.position;
            yield return new WaitForSeconds(.25f);
            float disabledDrift = Vector3.Distance(stoppedAt, stoppedBody != null ? stoppedBody.position : restarted.transform.position);
            bool stopped = !restarted.OriginalHitMovementActive && disabledDrift < .03f;
            restarted.enabled = true;
            Vector3 beforeRestart = restarted.transform.position;
            restarted.TakeDamage(1, Vector3.forward, SoldierDamageSource.Ranged);
            yield return new WaitForSeconds(.5f);
            float moved = Vector3.Dot(restarted.transform.position - beforeRestart, Vector3.forward);
            report.cases.Add(new Case { name = "禁用生命组件停止旧击退，重启后可正常再次受击", source = "Melee → Ranged", templateDistance = .75f,
                cloneDistance = restarted.击退距离, actualDistance = moved, inherited = Mathf.Approximately(restarted.击退距离, .75f),
                disabledDrift = disabledDrift,
                movementPaused = true, lockReleased = !restarted.OriginalHitMovementActive, passed = stopped && !restarted.OriginalHitMovementActive && Mathf.Abs(moved - .75f) < .035f });
            restarted.gameObject.SetActive(false);
        }
        // 前排按自己原生命脚本后退，后排 AI 保持启用，必须留下完整退路而不是一起向前推挤。
        var front = Spawn(2f);
        var rear = Spawn(2f);
        if (front == null || rear == null) { Finish("spawn_failed"); yield break; }
        Vector3 frontStart = front.transform.position;
        float radii = front.GetComponent<RedMeleeSelf>().Radius + rear.GetComponent<RedMeleeSelf>().Radius;
        rear.transform.position = frontStart + Vector3.forward * (2f + radii + .1f);
        Physics.SyncTransforms();
        Vector3 rearStart = rear.transform.position;
        Vector3 requested = rear.GetComponent<RedMeleeSelf>().FilterVelocity(Vector3.back * 7f, front.transform);
        report.queueLeavesEnoughRoom = requested.sqrMagnitude < .000001f;
        front.TakeDamage(1, Vector3.forward, SoldierDamageSource.Melee);
        yield return new WaitForSeconds(.5f);
        report.rearDidNotPush = Vector3.Distance(rearStart, rear.transform.position) < .03f &&
            Vector3.Dot(front.transform.position - frontStart, Vector3.forward) > 1.9f;
        front.gameObject.SetActive(false); rear.gameObject.SetActive(false);
        Save();
        // 同一真实场景继续播放，观察正常生成的红兵挨打与后排等候，不替换正式触发器。
        while (Time.time < 18f && EditorApplication.timeSinceStartup - wallStart < 160d) yield return null;
        bool pass = report.queueLeavesEnoughRoom && report.rearDidNotPush;
        foreach (var c in report.cases) pass &= c.passed;
        foreach (var a in report.assertions) pass &= a.StartsWith("PASS:");
        Finish(pass ? "completed_pass" : "completed_fail");
    }
    private SoldierHealth Spawn(float distance)
    {
        var before = new HashSet<SoldierHealth>(UnityEngine.Object.FindObjectsOfType<SoldierHealth>());
        testSpawner.transform.position = origin + Vector3.right * (++sequence * 10f);
        var random = UnityEngine.Random.state;
        try
        {
            // 模板只在同步调用生成器期间临时改值，实例化后立即恢复；不影响场景兵营的后续生成。
            templateHealth.击退距离 = distance;
            typeof(SoldierSpawner).GetMethod("SpawnSoldier", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(testSpawner, null);
        }
        finally { templateHealth.击退距离 = originalTemplateDistance; UnityEngine.Random.state = random; }
        foreach (var h in UnityEngine.Object.FindObjectsOfType<SoldierHealth>())
        {
            if (before.Contains(h)) continue;
            temporary.Add(h.gameObject); testIds.Add(h.GetInstanceID());
            var rb = h.GetComponent<Rigidbody>();
            if (rb != null) { rb.useGravity = false; rb.constraints = RigidbodyConstraints.FreezeRotation; rb.velocity = Vector3.zero; }
            h.击退轴向控制 = new Vector3(1, 0, 1); h.击退时长 = .15f; h.僵直停顿时间 = .2f;
            Physics.SyncTransforms(); return h;
        }
        return null;
    }
    private IEnumerator RunCase(string name, float distance, SoldierDamageSource source, bool blocked = false, float duration = .15f)
    {
        var h = Spawn(distance);
        if (h == null) { report.cases.Add(new Case { name = name, passed = false }); yield break; }
        h.击退时长 = duration;
        var c = new Case { name = name, source = source.ToString(), templateDistance = distance, cloneDistance = h.击退距离, inherited = Mathf.Approximately(h.击退距离, distance) };
        Vector3 start = h.transform.position;
        if (blocked)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); temporary.Add(wall);
            var ownCollider = h.GetComponent<Collider>();
            float forwardExtent = ownCollider != null ? ownCollider.bounds.max.z - start.z : .6f;
            // 让墙面与真实胶囊初始分开，避免测试夹具自带重叠引发物理分离位移。
            wall.transform.position = start + new Vector3(0f, .8f, forwardExtent + .2f); wall.transform.localScale = new Vector3(3f, 4f, .2f);
            Physics.SyncTransforms();
        }
        h.TakeDamage(1, Vector3.forward, source);
        c.movementPaused = h.OriginalHitMovementActive;
        yield return new WaitForSeconds(.5f);
        c.actualDistance = Vector3.Dot(h.transform.position - start, Vector3.forward);
        c.lockReleased = !h.OriginalHitMovementActive;
        c.passed = c.inherited && c.lockReleased && (blocked ? c.actualDistance < distance - .05f : Mathf.Abs(c.actualDistance - distance) < .035f);
        if (distance > 0f) c.passed &= c.movementPaused;
        report.cases.Add(c); h.gameObject.SetActive(false); Save();
    }
    void LateUpdate()
    {
        if (report == null || report.status != "running") return;
        if (ownsPlay && !resumedInitialTimeline && Time.time > 1f)
        {
            foreach (var d in UnityEngine.Object.FindObjectsOfType<UnityEngine.Playables.PlayableDirector>())
                if (d.playOnAwake && d.time < .0001 && d.state != UnityEngine.Playables.PlayState.Playing)
                { d.Play(); resumedInitialTimeline = true; }
        }
        foreach (var red in UnityEngine.Object.FindObjectsOfType<RedMeleeSelf>())
        {
            var h = red.GetComponent<SoldierHealth>();
            if (h == null || testIds.Contains(h.GetInstanceID()) || red.transform.position.x > 100f) continue;
            int id = h.GetInstanceID();
            if (h.OriginalKnockbackActive && previous.TryGetValue(id, out Vector3 old) && Vector3.Distance(old, h.transform.position) > .001f) hitIds.Add(id);
            if (red.State == "为前排留空间") waitingIds.Add(id);
            previous[id] = h.transform.position;
        }
        report.actualRedHitUnits = hitIds.Count; report.actualRedWaitingUnits = waitingIds.Count;
        if (EditorApplication.isPaused && ownsPlay) EditorApplication.isPaused = false;
    }
    private void Save() { File.WriteAllText("Temp/RedOriginalHit-results.json", JsonUtility.ToJson(report, true)); }
    private void Finish(string status)
    {
        EditorApplication.update -= Tick; report.status = status; Save();
        if (templateHealth != null) templateHealth.击退距离 = originalTemplateDistance;
        foreach (var go in temporary) if (go != null) UnityEngine.Object.Destroy(go);
        temporary.Clear(); Application.runInBackground = oldBackground;
        SessionState.SetBool("RedOriginalHitPlayCheck.own", false);
        if (ownsPlay) EditorApplication.isPlaying = false;
    }
    public void Abort()
    {
        EditorApplication.update -= Tick;
        if (templateHealth != null) templateHealth.击退距离 = originalTemplateDistance;
        foreach (var go in temporary) if (go != null) UnityEngine.Object.Destroy(go);
        temporary.Clear();
        if (report != null)
        {
            if (report.status == "running") { report.status = "interrupted"; Save(); }
            Application.runInBackground = oldBackground;
        }
    }
}

[InitializeOnLoad]
public static class RedOriginalHitCheckMenu
{
    private static double next;
    private static RedOriginalHitPlayCheck active;
    static RedOriginalHitCheckMenu()
    {
        EditorApplication.update += Poll;
        EditorApplication.delayCall += () =>
        { if (EditorApplication.isPlaying && SessionState.GetBool("RedOriginalHitPlayCheck.own", false)) Begin(true); };
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("RedOriginalHitPlayCheck.own", false)) Begin(true);
            if (s == PlayModeStateChange.ExitingPlayMode) { if (active != null) active.Abort(); SessionState.SetBool("RedOriginalHitPlayCheck.own", false); }
        };
    }
    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 1d;
        if (SessionState.GetBool("RedOriginalHitPlayCheck.own", false) && EditorApplication.isPlaying && EditorApplication.isPaused) EditorApplication.isPaused = false;
        if (SessionState.GetBool("RedOriginalHitPlayCheck.own", false) && EditorApplication.isPlaying) Begin(true);
        if (!File.Exists("Temp/RedOriginalHit.request")) return;
        File.Delete("Temp/RedOriginalHit.request"); Run();
    }
    [MenuItem("Tools/新版桥口近战/验证原红补兵击退与新克隆继承")]
    public static void Run()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "20261012-PVE收关敌万军") return;
        if (EditorApplication.isPlaying) { Begin(false); return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("RedOriginalHitPlayCheck.own", true); EditorApplication.isPlaying = true;
    }
    private static void Begin(bool own)
    {
        if (active != null && active.IsRunning) return;
        active = new RedOriginalHitPlayCheck(); active.Begin(own);
    }
}
#endif
