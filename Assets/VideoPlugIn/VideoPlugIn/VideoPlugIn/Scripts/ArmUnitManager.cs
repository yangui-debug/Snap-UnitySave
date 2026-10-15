using System;
using System.Collections.Generic;
using UnityEngine;
using UnityTimer;

namespace VideoPlugIn.Scripts
{
    public class ArmUnitManager: MonoBehaviour
    {
        [Space, Header("时间参数:")]
        [Header("动画延迟时间(启动后多少秒开始移动)")]
        [SerializeField] float animatorDelay;
        [Header("停止武器生成时间")]
        [SerializeField] float stopSpawnArmUnitsTime;
        [Header("游戏结束开关控制")]
        [SerializeField] private bool useArmStopTime = false; // 开关
        [Header("结束游戏时间")]
        [SerializeField] float stopGameTime;
        [Header("结束时武器销毁特效")] [SerializeField] public ParticleSystem armStopEffect;

        [Space, Header("移动路径参数:")]
        [Header("移动路径路点")]
        [SerializeField] private List<GameObject> pathPoints;
        [Header("移动时间")]
        [SerializeField] private float moveTime;
        [Header("移动高度开关控制")]
        [SerializeField] private bool useMoveHigh = false; // 开关
        [Header("移动高度")]
        [SerializeField] private float moveHigh;

        [Space, Header("武器生成配置参数:")]
        [Header("武器总数")]
        [SerializeField] private int armTotal;
        //[Header("武器生成间隔")]
        //[SerializeField] private float timeInterval;

        [Header("攻击单位预设体")]
        [SerializeField] private GameObject armUnitPrefab;

        [Space, Header("武器移动触发事件:")]
        [Header("武器移动事件挂载")]
        [SerializeField]
        List<ArmMoveEvent> armMoveEvents;


        private float timeInterval;

        private void Start()
        {
            Timer.Register(animatorDelay, SpawnArmUnits);
        }

        private void SpawnArmUnits()
        {
            //// 循环生成攻击单位
            timeInterval = moveTime / armTotal;
            for (int i = 0; i < armTotal; i++)
            {
                if (Time.time < stopSpawnArmUnitsTime)
                //if (Time.time < stopSpawnArmUnitsTime && Time.time < stopGameTime - 0.01f)
                {
                    // 为每个攻击单位生成设置延迟，间隔生成
                    Timer.Register(i * timeInterval, () =>
                    {
                        CreateArmUnit();
                    });
                }
                
            }
        }

        private void CreateArmUnit()
        {
            if (Time.time > stopSpawnArmUnitsTime) return;

            // 实例化攻击单位并激活它
            //GameObject newArmUnit = Instantiate(armUnitPrefab, spawnPosition, Quaternion.identity);
            GameObject newArmUnit = Instantiate(armUnitPrefab, transform.position, Quaternion.identity);
            if (newArmUnit == null)
            {
                return;
            }

            // 激活攻击单位
            newArmUnit.SetActive(true);

            // 获取攻击单位的脚本组件
            ArmUnit armUnitScript = newArmUnit.GetComponent<ArmUnit>();
            if (armUnitScript == null)
            {
                return;
            }

            // 设置攻击单位的路径、移动时间和高度
            armUnitScript.Setup(pathPoints, moveTime, moveHigh, armMoveEvents, stopSpawnArmUnitsTime, useMoveHigh,stopGameTime, armStopEffect, useArmStopTime); 
            //armUnitScript.Setup(pathPoints, moveTime, moveHigh, highDif, armMoveEvents, stopSpawnArmUnitsTime); 
        }
        [Serializable]
        public class ArmMoveEvent
        {
            [Header("触发动画路径点(不可重复!!!)")]
            [SerializeField]
            public int pathPoint;
            [Header("武器特效")] [SerializeField]public ParticleSystem dieEffect;

            public static implicit operator ArmMoveEvent(MoveEvent v)
            {
                throw new NotImplementedException();
            }
        }
    }

    
}
