using UnityEngine;
using RayFire;

public class RayfireAbsoluteBoom : MonoBehaviour
{
    /// <summary>
    /// 【神级公开函数】专供你的 Timeline 信号点（Signal）在几秒时调用！
    /// </summary>
    public void ForceBoomAtCurrentPlace()
    {
        RayfireRigid rigid = GetComponent<RayfireRigid>();
        if (rigid != null)
        {
            // 1. 强行初始化，把当前的、你肉眼看到的这个位置录入它的物理系统
            rigid.Initialize();

            // 2. 核心：强制把它的记录坐标更新为当前的绝对世界坐标
            rigid.physics.position = transform.position;
            rigid.physics.rotation = transform.rotation;

            // 3. 彻底清空可能存在的各种残留速度，防止它被物理引擎甩飞
            // 【已修复】：改回了你当前 Unity 版本最兼容的经典 .velocity 变量
            if (rigid.physics.rigidBody != null)
            {
                rigid.physics.rigidBody.velocity = Vector3.zero;
                rigid.physics.rigidBody.angularVelocity = Vector3.zero;
            }

            // 4. 狠狠踹开爆炸引擎，这样它就绝对只能在当前的绝对坐标上原地炸裂！
            rigid.Demolish();
        }
    }
}