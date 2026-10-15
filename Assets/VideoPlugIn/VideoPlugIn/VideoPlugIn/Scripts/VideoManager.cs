using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityTimer;
using Random = UnityEngine.Random;

namespace VideoPlugIn.Scripts
{
    public class VideoManager : MonoBehaviour
    {
        [Header("游戏结束开关控制")]
        [SerializeField] private bool useStopTime = false; // 开关
        [Header("结束游戏时间")]
        [SerializeField] float stopGameTime;
        [Header("生成单位参数:")] [Space, Header("生成单位总数")] [SerializeField]
        private int createUnitTotal;

        [Header("生成的延迟时间")] [SerializeField] private float createDelay;

        [Header("生成范围")] [SerializeField] private BoxCollider createRange;

        [Header("单一波次生成个数(一波N个单位)")] [SerializeField]
        private int singleWaveCreateCount;

        [Header("波次间隔时间")] [SerializeField] private float waveIntervalTime;
        [Header("生成单位")] [SerializeField] private List<MoveUnit> moveUnits;
        [Header("移动高度开关控制")]
        [SerializeField] private bool useCteateHigh = false; // 开关
        [Header("生成高度")] [SerializeField] private float createHigh;
        //[Header("高度差")] [SerializeField] private float highDif;

        [Space, Header("移动路径参数:")] [Header("移动路径路点")] [SerializeField]
        private List<GameObject> pathPoints;

        [Header("移动时间")] [SerializeField] private float moveTime;

        [Space, Header("移动触发事件:")] [Header("移动事件挂载")] [SerializeField]
        List<MoveEvent> moveEvents;

        [Space, Header("全局触发事件:")] [Header("全局事件挂载")] [SerializeField]
        List<TriggerEvent> triggerEvents;

        [Space, Header("特效:")] [Header("死亡特效")] [SerializeField]
        private ParticleSystem dieEffect;
        [Header("特效生成高度")] [SerializeField] private float effectCreateHigh = 0;

        private List<Vector4> createRangeVector4s;
        private int createUnitCount;
        private List<Vector3> pathPointVector;
        private List<MoveUnit> moveUnitTotal;

        private void Start()
        {
            InitializePathHeights();
            Init();
            InvokeTriggerEvent();
        }

        private void Init()
        {
            if (!CheckData())
            {
                return;
            }

            InitData();
            Timer.Register(createDelay, CreateUnit);
        }

        //private void Update()
        //{
        //    if (Time.time >= stopGameTime)
        //    {
        //        Destroy(gameObject);
        //    }
        //}

        private void InitializePathHeights()
        {
            foreach (var pathPoint in pathPoints)
            {
                //Debug.LogError($"wei进入初始化高度方法,但是在 for 循环里");
                // 获取路径点的当前位置
                Vector3 pos = pathPoint.transform.position;

                if (useCteateHigh)
                {
                    Vector3 newPosition = gameObject.transform.position; 
                    newPosition.y = createHigh; 
                    gameObject.transform.position = newPosition; 

                    pos.y = createHigh;
                }

                //if (pathPoint.CompareTag("Up"))
                //{
                //    pos.y = createHigh + highDif;
                //    //Debug.LogError($"进入初始化高度方法");
                //}
                //else if (pathPoint.CompareTag("Down"))
                //{
                //    pos.y = createHigh - highDif;
                //}
                //else
                //{
                //    pos.y = createHigh;
                //}

                // 更新路径点的位置
                pathPoint.transform.position = pos;
                //Debug.LogError($"初始化位置坐标： {pathPoint.transform.position.ToString()}");

            }
        }

        public void InvokeTriggerEvent()
        {
            foreach (var triggerEvent in triggerEvents)
            {
                Timer.Register(triggerEvent.triggerEventDuration, () => { TriggerGlobalEvent(triggerEvent); });
            }
        }

        private void CreateUnit()
        {
            if(Time.time < stopGameTime)
            {
                foreach (var createRandomRange in createRangeVector4s)
                {
                    //Vector3 pos = new Vector3(Random.Range(createRandomRange.w, createRandomRange.x), createHigh,
                    Vector3 pos = new Vector3(Random.Range(createRandomRange.w, createRandomRange.x), 0,
                        Random.Range(createRandomRange.y, createRandomRange.z));
                    //Debug.LogError($"查看士兵生成时，位置坐标：x: {pos.x} ,y: {pos.y},z: {pos.z}");

                    int unitIndex = Random.Range(0, moveUnits.Count);
                    MoveUnit moveUnit = Instantiate(moveUnits[unitIndex], transform.TransformPoint(pos), transform.rotation, null);
                    moveUnit.transform.LookAt(pathPoints[0].transform.position);
                    moveUnitTotal.Add(moveUnit);
                    moveUnit.Init(pathPointVector, moveTime, dieEffect, moveEvents, effectCreateHigh, stopGameTime, useStopTime);
                }

                createUnitCount += singleWaveCreateCount;
                if (createUnitCount < createUnitTotal)
                {
                    Timer.Register(waveIntervalTime, CreateUnit);
                }
            }
            
        }

        private void InitData()
        {
            moveUnitTotal = new List<MoveUnit>();
            pathPointVector = new List<Vector3>();
            createRangeVector4s = new List<Vector4>();
            createUnitCount = 0;
            float singleCreateRangeX = createRange.size.x / singleWaveCreateCount;
            for (int i = 0; i < singleWaveCreateCount; i++)
            {
                createRangeVector4s.Add(new Vector4()
                {
                    w = (i - singleWaveCreateCount / 2f) * singleCreateRangeX + createRange.center.x,
                    x = (i - singleWaveCreateCount / 2f + 1) * singleCreateRangeX + createRange.center.x,
                    y = createRange.center.z - createRange.size.z,
                    z = createRange.center.z + createRange.size.z,
                });
            }

            Vector3 pos = transform.position;
            for (int i = 0; i < pathPoints.Count; i++)
            {
                pos = pathPoints[i].transform.position;
                pathPointVector.Add(pathPoints[i].transform.position -
                                    (i == 0 ? transform.TransformPoint(createRange.center) : pathPoints[i - 1].transform.position));
            }
        }

        private void TriggerGlobalEvent(TriggerEvent triggerEvent)
        {
            foreach (var moveUnit in moveUnitTotal)
            {
                if (moveUnit)
                {
                    moveUnit.TriggerGlobalEvent(triggerEvent);
                }
            }
        }

        private bool CheckData()
        {
            if (createUnitTotal <= 0)
            {
                Debug.LogError("生成单位数量小于0");
                return false;
            }

            if (!createRange)
            {
                Debug.LogError("未规定生成范围");
                return false;
            }

            if (singleWaveCreateCount <= 0)
            {
                Debug.LogError("每波生成单位数量小于0");
                return false;
            }

            if (waveIntervalTime <= 0)
            {
                Debug.LogError("生成间隔时间错误");
                return false;
            }

            if (moveUnits is { Count: 0 })
            {
                Debug.LogError("未挂载生成单位");
                return false;
            }

            if (pathPoints is { Count: 0 })
            {
                Debug.LogError("移动路径路点");
                return false;
            }

            if (moveTime <= 0)
            {
                Debug.LogError("移动时间设置错误");
                return false;
            }

            if (!dieEffect)
            {
                Debug.LogError("未挂载死亡特效");
                return false;
            }
            //float durations = triggerEvents.Where(e => e.animatorEvent == "Die").Select(e => e.triggerEventDuration).DefaultIfEmpty(0).Max(); ;

            //if (durations > stopGameTime)
            //{
            //    Debug.LogError("全局时间触发时间应小于游戏结束时间");
            //}

            //bool allValid = triggerEvents.All(e => e.triggerEventDuration > 0);

            //if (!allValid)
            //{
            //    Debug.LogError("所有元素的 triggerEventDuration 都 > 0");
            //}

            return true;
        }
    }

    [Serializable]
    public class MoveEvent
    {
        [Header("触发动画路径点(不可重复!!!)")] [SerializeField]
        public int pathPoint;

        [Header("动画名称")] [SerializeField] public string animatorEvent;
        [Header("动画持续时间")] [SerializeField] public float duration;
    }

    [Serializable]
    public class TriggerEvent
    {
        [Header("触发事件时间")] [SerializeField] public float triggerEventDuration;
        [Header("动画名称")] [SerializeField] public string animatorEvent;
        [Header("动画持续时间")] [SerializeField] public float duration;
    }
}