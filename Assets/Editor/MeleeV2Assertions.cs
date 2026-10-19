#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
/// <summary>新版战斗规则隔离验证。只创建播放期间临时对象，立即销毁，不保存任何资产或场景。</summary>
public static class MeleeV2Assertions
{
    private static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static void Set(object o, string name, object value) { o.GetType().GetField(name, Private).SetValue(o, value); }
    private static object Call(object o, string name, params object[] args) { return o.GetType().GetMethod(name, Private).Invoke(o, args); }
    private static void Check(List<string> r, string name, bool ok) { r.Add((ok ? "PASS: " : "FAIL: ") + name); }
    private static SoldierAnimationController Make(GameObject parent, Vector3 pos, FactionType faction)
    {
        var go = new GameObject("临时验证士兵"); go.transform.SetParent(parent.transform); go.transform.position = pos;
        go.AddComponent<SoldierHealth>(); var a = go.AddComponent<SoldierAnimationController>(); a.myFaction = faction;
        var c = go.AddComponent<CapsuleCollider>(); c.center = Vector3.up; c.height = 2f; c.radius = .4f;
        return a;
    }
    public static string[] Run()
    {
        var results = new List<string>(); var root = new GameObject("MeleeV2临时规则验证");
        var randomState = UnityEngine.Random.state;
        try
        {
            Vector3 origin = new Vector3(1000, 1000, 1000);
            var a = Make(root, origin, FactionType.Red); var b = Make(root, origin + Vector3.forward * 2f, FactionType.Red);
            var target = Make(root, origin + Vector3.back * 10f, FactionType.Blue);
            var settings = new RedMeleeSettings { sceneName = root.scene.name, center = origin, size = Vector3.one * 100f };
            var front = a.gameObject.AddComponent<RedMeleeSelf>(); front.Configure(settings);
            var rear = b.gameObject.AddComponent<RedMeleeSelf>(); rear.Configure(settings);
            Physics.SyncTransforms();
            Check(results, "红兵配置独立复制", !ReferenceEquals(front.settings, rear.settings));
            Check(results, "无蓝方管理器时红兵自行给前排留空间", rear.FilterVelocity(Vector3.back * 6f, target.transform) == Vector3.zero && rear.State == "为前排留空间");
            a.transform.position += Vector3.left * 10f; Physics.SyncTransforms();
            Check(results, "通路开放后红兵独立随机补位等待", rear.FilterVelocity(Vector3.back, target.transform) == Vector3.zero && rear.State == "补位随机等待");
            Vector3 slot = origin + Vector3.left * 5f;
            Check(results, "同一空位互斥", (bool)Call(front, "Reserve", slot) && !(bool)Call(rear, "Reserve", slot));
            Set(a.GetComponent<SoldierHealth>(), "<isDead>k__BackingField", true);
            Check(results, "死亡红兵释放空位", (bool)Call(rear, "Reserve", slot));
            var originalHealth = b.GetComponent<SoldierHealth>();
            Set(originalHealth, "originalHitUntil", Time.time + .2f); Set(originalHealth, "originalKnockbackUntil", Time.time + .15f);
            Check(results, "原生命脚本受击期间交战组件停止相反寻路输出", rear.FilterVelocity(Vector3.back * 7f, target.transform) == Vector3.zero && rear.State == "受击后退");
            Set(originalHealth, "originalHitUntil", 0f); Set(originalHealth, "originalKnockbackUntil", 0f);
            Check(results, "零击退零僵直不锁死", !rear.HitMovementActive);
            Check(results, "红兵交战组件不再有独立击退入口", typeof(RedMeleeSelf).GetMethod("BeginHit") == null);
            Set(originalHealth, "originalHitUntil", Time.time + .2f);
            rear.enabled = false; rear.enabled = true;
            Check(results, "重启交战组件不能提前解除原生命脚本受击状态", rear.HitMovementActive);
            Set(originalHealth, "originalHitUntil", 0f);
            float duration = .7f, strength = 3f; rear.Flash(SoldierDamageSource.Melee, ref duration, ref strength);
            Check(results, "红兵按近战来源独立减弱闪白", Mathf.Approximately(duration, .08f) && Mathf.Approximately(strength, .25f));
            duration = .7f; strength = 3f; rear.Flash(SoldierDamageSource.Unspecified, ref duration, ref strength);
            Check(results, "未指定来源保持旧闪白", duration == .7f && strength == 3f);
            rear.settings.enabled = false;
            Check(results, "关闭红兵总开关恢复原速度", rear.FilterVelocity(Vector3.back * 6f, target.transform) == Vector3.back * 6f);
            rear.settings.enabled = true;
            Set(b.GetComponent<SoldierHealth>(), "<isDead>k__BackingField", true);
            Check(results, "死亡优先于移动", rear.FilterVelocity(Vector3.forward, target.transform) == Vector3.zero);

            var blueRoot = new GameObject("临时蓝方父物体"); blueRoot.transform.SetParent(root.transform);
            var manager = blueRoot.AddComponent<BlueShieldAdvance>(); manager.sceneName = root.scene.name;
            manager.controlCenter = origin; manager.controlSize = Vector3.one * 100f;
            var triggerGO = new GameObject("临时检测体"); triggerGO.transform.SetParent(root.transform); triggerGO.transform.position = origin + Vector3.right * 20f;
            var trigger = triggerGO.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = Vector3.one * 20f; manager.detectionTrigger = trigger;
            var row = new GameObject("Timeline行"); row.transform.SetParent(blueRoot.transform);
            var container = new GameObject("单兵容器"); container.transform.SetParent(row.transform); container.transform.position = triggerGO.transform.position;
            var blue = Make(container, container.transform.position, FactionType.Blue); blue.gameObject.AddComponent<Animator>();
            var member = blue.gameObject.AddComponent<BlueShieldMember>(); member.Configure(manager, blue);
            var red = Make(root, blue.transform.position + Vector3.forward * 4f, FactionType.Red);
            Physics.SyncTransforms(); Call(manager, "Scan");
            Check(results, "触发器去重统计敌我人数", manager.RedCount == 1 && manager.BlueCount == 1);
            Vector3 home = member.Home; member.Tick(true, false); Physics.SyncTransforms();
            Check(results, "第一排实际向红兵推进且移动单兵容器", member.MotionRoot == container.transform && Vector3.Dot(container.transform.position - home, manager.Axis) > 0f);
            Vector3 previousHome = member.Home;
            for (int i = 0; i < 150; i++) { member.Tick(true, false); Physics.SyncTransforms(); }
            Check(results, "随机推进受固定上限约束且原位不刷新", container.transform.position.z - home.z <= manager.maxForward + .001f && member.Home == previousHome);
            member.Tick(true, true); float schedule = (float)typeof(BlueShieldMember).GetField("retreatAt", Private).GetValue(member);
            Check(results, "人数劣势先随机等待", member.State == "退守随机等待" && schedule >= Time.time + Mathf.Min(manager.retreatDelay.x, manager.retreatDelay.y) - .001f);
            Set(member, "retreatAt", Time.time); Vector3 before = container.transform.position; member.Tick(true, true);
            Check(results, "等待后慢速退回原位", Vector3.Distance(container.transform.position, home) < Vector3.Distance(before, home));
            member.BeginHit(Vector3.back, .5f, .2f, .3f, Vector3.one); member.Tick(true, false);
            Check(results, "蓝盾受击优先于推进", member.State == "受击优先");
            member.Release(); Set(red.GetComponent<SoldierHealth>(), "<isDead>k__BackingField", true); Physics.SyncTransforms(); Call(manager, "Scan"); before = container.transform.position; member.Tick(true, false);
            Check(results, "目标死亡且无新目标不盲目前进", container.transform.position == before && manager.RedCount == 0);
            var next = Make(root, blue.transform.position + Vector3.forward * 4f, FactionType.Red); Physics.SyncTransforms(); Call(manager, "Scan");
            container.transform.position = home + Vector3.forward * .3f; Physics.SyncTransforms(); before = container.transform.position; member.Tick(true, false);
            Check(results, "红方死亡后有新目标可有限补位", container.transform.position.z > before.z && member.Home == home);
            var second = Make(blueRoot, home + Vector3.right * 4f, FactionType.Blue);
            var secondMember = second.gameObject.AddComponent<BlueShieldMember>(); secondMember.Configure(manager, second);
            member.Release(); member.Tick(true, true); secondMember.Tick(true, true);
            float delay1 = (float)typeof(BlueShieldMember).GetField("retreatAt", Private).GetValue(member);
            float delay2 = (float)typeof(BlueShieldMember).GetField("retreatAt", Private).GetValue(secondMember);
            Check(results, "每名蓝盾独立随机退守时间", delay1 != delay2);
            var candidates = (List<BlueShieldMember>)typeof(BlueShieldAdvance).GetField("members", Private).GetValue(manager);
            candidates.Clear();
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = home + Vector3.right * (15f + i % 2 * 2f) + Vector3.forward * (i % 2 * 1.5f - (i >= 2 ? 2f : 0f));
                var ca = Make(blueRoot, p, FactionType.Blue); var cm = ca.gameObject.AddComponent<BlueShieldMember>(); cm.Configure(manager, ca); candidates.Add(cm);
            }
            Call(manager, "ChooseRow");
            Check(results, "斜列逐列识别前排且不误选后排", candidates[0].IsFirstRow && candidates[1].IsFirstRow && !candidates[2].IsFirstRow && !candidates[3].IsFirstRow);
            // 复现同排朝中间同一目标收拢：斜向移动会撞向左后邻兵，但正前方仍有空间。
            var jammed = Make(blueRoot, home + Vector3.right * 24f, FactionType.Blue);
            jammed.GetComponent<CapsuleCollider>().radius = .5f;
            var neighbour = Make(blueRoot, jammed.transform.position + new Vector3(-.95f, 0f, -.15f), FactionType.Blue);
            neighbour.GetComponent<CapsuleCollider>().radius = .5f;
            var neighbourMember = neighbour.gameObject.AddComponent<BlueShieldMember>(); neighbourMember.Configure(manager, neighbour);
            var laneMember = jammed.gameObject.AddComponent<BlueShieldMember>(); laneMember.Configure(manager, jammed);
            var laneTarget = Make(root, jammed.transform.position + new Vector3(-3f, 0f, 4f), FactionType.Red).GetComponent<SoldierHealth>();
            Set(laneMember, "target", laneTarget); Physics.SyncTransforms();
            Vector3 desire = new Vector3(-3f, 0f, 4f).normalized * manager.advanceSpeed;
            Vector3 diagonal = (Vector3)Call(laneMember, "Limited", desire);
            Vector3 sliding = (Vector3)Call(laneMember, "SafeAdvance", desire);
            Check(results, "同排斜向收拢受阻时沿列线继续前进", diagonal.sqrMagnitude < .000001f && sliding.z > .01f && Mathf.Abs(sliding.x) < .001f);
            neighbour.transform.position = jammed.transform.position + new Vector3(-.95f, 0f, .15f); Physics.SyncTransforms();
            sliding = (Vector3)Call(laneMember, "SafeAdvance", desire);
            Check(results, "斜排友军同时阻挡斜向和正前方时沿接触面避让", ((Vector3)Call(laneMember, "Limited", manager.Axis * manager.advanceSpeed)).sqrMagnitude < .000001f && sliding.z > .01f && sliding.x > 0f);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.SetParent(root.transform);
            wall.transform.position = jammed.transform.position + new Vector3(0f, 1f, .6f); wall.transform.localScale = new Vector3(1f, 2f, .2f);
            Physics.SyncTransforms(); sliding = (Vector3)Call(laneMember, "SafeAdvance", desire);
            Check(results, "列线备用推进仍被正前方真实障碍阻挡（前向步长=" + (Vector3.Dot(sliding, manager.Axis) * Time.fixedDeltaTime).ToString("F6") + "米）",
                Vector3.Dot(sliding, manager.Axis) * Time.fixedDeltaTime < .001f);
            wall.SetActive(false); manager.keepLaneWhenBlocked = false; Physics.SyncTransforms();
            Check(results, "关闭列线备用推进恢复原碰撞结果", ((Vector3)Call(laneMember, "SafeAdvance", desire) - diagonal).sqrMagnitude < .000001f);
            manager.keepLaneWhenBlocked = true;
            manager.controlCenter = Vector3.zero; before = container.transform.position; member.Tick(true, false);
            Check(results, "Timeline调往其他桥口后停止蓝盾接管", member.State == "离开右桥交还原控制" && before == container.transform.position);
            manager.detectionTrigger = null; Check(results, "未拖检测器时蓝方不接管", !manager.Active);
            Check(results, "蓝方物体没有安装红方行为组件", blueRoot.GetComponentsInChildren<RedMeleeSelf>(true).Length == 0 && blueRoot.GetComponentsInChildren<RedMeleeSpawnSetup>(true).Length == 0);
        }
        catch (Exception ex) { results.Add("FAIL: " + ex); }
        finally { UnityEngine.Object.DestroyImmediate(root); Physics.SyncTransforms(); UnityEngine.Random.state = randomState; }
        return results.ToArray();
    }
}
#endif
