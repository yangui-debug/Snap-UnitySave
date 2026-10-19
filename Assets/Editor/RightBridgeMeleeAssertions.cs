#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>播放验收附带的隔离断言。在远离战场的临时对象上验证规则，执行后立即清理，不保存场景。</summary>
public static class RightBridgeMeleeAssertions
{
    public static string[] Run()
    {
        var results = new List<string>();
        var root = new GameObject("RightBridge_TransientAssertions");
        try
        {
            var director = root.AddComponent<RightBridgeMeleeDirector>();
            director.sceneName = root.scene.name;
            director.shieldRoot = root.transform;
            director.combatCenter = new Vector3(1000f, 1000f, 1000f);
            director.combatSize = Vector3.one * 100f;
            var front = Create(root, director, "front", new Vector3(1000f, 1000f, 1000f), false);
            var rear = Create(root, director, "rear", new Vector3(1000f, 1000f, 1001.5f), false);
            var target = NewController(root, "target", new Vector3(1000f, 1000f, 980f), FactionType.Blue);
            Check(results, "同列后排保留前排退路", director.IsQueueBlocked(rear, Vector3.back));
            Vector3 originalFront = front.transform.position;
            front.transform.position = rear.transform.position + Vector3.right * 0.9f + Vector3.back * 0.2f;
            Check(results, "斜列同排邻兵不误判成前排", !director.IsQueueBlocked(rear, Vector3.back));
            front.transform.position = originalFront;
            Vector3 queued = rear.FilterVelocity(Vector3.back, target.transform);
            Check(results, "阻挡时停止主动推进", queued == Vector3.zero);
            front.transform.position += Vector3.back * 10f;
            rear.FilterVelocity(Vector3.back, target.transform);
            Check(results, "前路开放后进入随机等待", rear.WaitingForRefill);
            Vector3 slot = new Vector3(1000f, 1000f, 970f);
            bool first = director.TryReserve(front, slot);
            bool second = director.TryReserve(rear, slot);
            Check(results, "同一空位只允许一人预留", first && !second);
            typeof(SoldierHealth).GetField("<isDead>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(front.GetComponent<SoldierHealth>(), true);
            Check(results, "死亡单位立即释放占位资格", director.TryReserve(rear, slot));
            rear.ReleaseControl();
            bool hit = rear.BeginHit(Vector3.forward, 2f, 0.15f, 0.2f, Vector3.one);
            Vector3 knockback = rear.FilterVelocity(Vector3.back * 7f, target.transform);
            Check(results, "击退优先于相反方向寻路", hit && Vector3.Dot(knockback, Vector3.forward) > 0f);
            rear.ReleaseControl();
            rear.BeginHit(Vector3.forward, 0f, 0f, 0f, Vector3.one);
            Check(results, "零击退零僵直不会刷新隐性移动锁", !rear.HitMovementActive);
            var shield = Create(root, director, "shield", new Vector3(1010f, 1000f, 1000f), true);
            var safe = typeof(RightBridgeMeleeUnit).GetMethod("SafeVelocity", BindingFlags.NonPublic | BindingFlags.Instance);
            Vector3 huge = Vector3.forward * (1000f / Time.fixedDeltaTime);
            Vector3 velocity = (Vector3)safe.Invoke(shield, new object[] { huge });
            shield.transform.position += velocity * Time.fixedDeltaTime;
            float advance = Vector3.Dot(shield.transform.position - shield.FixedAnchor, director.Axis);
            Vector3 blocked = (Vector3)safe.Invoke(shield, new object[] { Vector3.forward });
            Check(results, "固定前进边界且到界不能继续推进", Mathf.Abs(advance - director.maxAdvance) < 0.001f && blocked.sqrMagnitude < 0.000001f);
            velocity = (Vector3)safe.Invoke(shield, new object[] { -huge });
            shield.transform.position += velocity * Time.fixedDeltaTime;
            float retreat = Vector3.Dot(shield.transform.position - shield.FixedAnchor, director.Axis);
            Check(results, "固定后退边界不随补位重设", Mathf.Abs(retreat + director.maxRetreat) < 0.001f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "assertion_floor"; floor.transform.SetParent(root.transform);
            floor.transform.position = shield.transform.position + Vector3.down * 0.05f;
            floor.transform.localScale = new Vector3(10f, 0.2f, 10f);
            Physics.SyncTransforms();
            velocity = (Vector3)safe.Invoke(shield, new object[] { Vector3.right });
            Check(results, "脚底轻微接触地板仍可沿面移动", velocity.x > 0f);
            // 独立碰撞断言：墙前停止，不依靠大步位移穿过墙。
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "assertion_wall"; wall.transform.SetParent(root.transform);
            wall.transform.position = shield.transform.position + Vector3.forward + Vector3.up;
            wall.transform.localScale = new Vector3(2f, 4f, 0.2f);
            Physics.SyncTransforms();
            velocity = (Vector3)safe.Invoke(shield, new object[] { huge });
            Check(results, "碰撞扫掠限制大步位移", velocity.magnitude * Time.fixedDeltaTime < 0.8f);
            var touching = GameObject.CreatePrimitive(PrimitiveType.Cube);
            touching.name = "existing_overlap"; touching.transform.SetParent(root.transform);
            touching.transform.position = shield.transform.position + Vector3.back * 0.45f + Vector3.up;
            touching.transform.localScale = new Vector3(2f, 2f, 0.2f);
            Physics.SyncTransforms();
            velocity = (Vector3)safe.Invoke(shield, new object[] { Vector3.forward });
            Check(results, "允许离开原有重叠而非永久卡死", velocity.z > 0f);
            director.enablePushPull = false;
            Vector3 desired = new Vector3(1f, 0f, -3f);
            Check(results, "关闭总开关原样返回旧移动速度", rear.FilterVelocity(desired, target.transform) == desired);
            director.enablePushPull = true;
            target.arrowPrefab = root; target.firePoint = target.transform;
            Check(results, "远程不接入近战控制", RightBridgeMeleeDirector.FindOrAttach(target) == null);
            rear.Controller.enabled = false;
            Check(results, "未启用的旧控制器不重复占位", !rear.Controlled);
            rear.Controller.enabled = true;
            var physicalRoot = new GameObject("single_soldier_physical_root");
            physicalRoot.transform.SetParent(root.transform);
            physicalRoot.transform.position = new Vector3(1020f, 1000f, 1000f);
            physicalRoot.AddComponent<Rigidbody>();
            physicalRoot.AddComponent<SoldierAnimationController>().enabled = false;
            var modelController = NewController(physicalRoot, "active_model", physicalRoot.transform.position, FactionType.Blue);
            var attach = typeof(RightBridgeMeleeDirector).GetMethod("Attach", BindingFlags.NonPublic | BindingFlags.Instance);
            var modelUnit = (RightBridgeMeleeUnit)attach.Invoke(director, new object[] { modelController, true });
            Check(results, "模型脚本使用自己的父级单兵刚体", modelUnit.MotionRoot == physicalRoot.transform);
            NewController(physicalRoot, "another_active_soldier", physicalRoot.transform.position + Vector3.right * 2f, FactionType.Blue);
            modelUnit.Configure(director, modelController, true);
            Check(results, "禁止接管包含多名士兵的共享刚体", modelUnit.MotionRoot == modelController.transform);
            var row = new GameObject("timeline_row"); row.transform.SetParent(root.transform);
            var container = new GameObject("soldier_without_rigidbody"); container.transform.SetParent(row.transform);
            var animatedModel = NewController(container, "animated_model", new Vector3(1030f, 1000f, 1000f), FactionType.Blue);
            var animatedUnit = (RightBridgeMeleeUnit)attach.Invoke(director, new object[] { animatedModel, true });
            Check(results, "无刚体模型移动单兵容器且避开Timeline编组", animatedUnit.MotionRoot == container.transform);
            var followTarget = NewController(root, "follow_target", animatedModel.transform.position + Vector3.forward * 4f, FactionType.Red);
            typeof(SoldierAnimationController).GetField("currentTarget", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(animatedModel, followTarget.transform);
            Vector3 beforeFollow = animatedModel.transform.position;
            typeof(RightBridgeMeleeUnit).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(animatedUnit, null);
            Check(results, "有目标且前方有空间时单兵容器实际前进", Vector3.Dot(animatedModel.transform.position - beforeFollow, director.Axis) > 0.001f);
        }
        catch (Exception ex) { results.Add("FAIL: " + ex); }
        finally { UnityEngine.Object.DestroyImmediate(root); Physics.SyncTransforms(); }
        return results.ToArray();
    }

    private static SoldierAnimationController NewController(GameObject parent, string name, Vector3 position, FactionType faction)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform); go.transform.position = position;
        var health = go.AddComponent<SoldierHealth>(); health.enabled = false;
        var controller = go.AddComponent<SoldierAnimationController>();
        controller.myFaction = faction;
        return controller;
    }

    private static RightBridgeMeleeUnit Create(GameObject root, RightBridgeMeleeDirector director, string name, Vector3 position, bool shield)
    {
        var controller = NewController(root, name, position, shield ? FactionType.Blue : FactionType.Red);
        var capsule = controller.gameObject.AddComponent<CapsuleCollider>();
        capsule.center = Vector3.up; capsule.height = 2f; capsule.radius = 0.4f;
        var method = typeof(RightBridgeMeleeDirector).GetMethod("Attach", BindingFlags.NonPublic | BindingFlags.Instance);
        return (RightBridgeMeleeUnit)method.Invoke(director, new object[] { controller, shield });
    }

    private static void Check(List<string> results, string name, bool success) { results.Add((success ? "PASS: " : "FAIL: ") + name); }
}
#endif
