using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using System.Linq;
using static VideoPlugIn.Scripts.ArmUnitManager;
using UnityTimer;

namespace VideoPlugIn.Scripts
{
    public class ArmUnit : MonoBehaviour
    {
        [Header("动画器控制器")] [SerializeField] private Animator ArmMoveAnimator;
        [Header("动画播放速度")] [SerializeField] private float animatorSpeed;
        [Header("旋转角速度")] [SerializeField] private float angleSpeed;

        private Tween armMoveTween;

        private List<GameObject> pathPoints;
        private float moveTime;
        private float moveHigh;
        private Dictionary<int, ArmMoveEvent> armMoveEvents;
        private float stopSpawnArmUnitsTime;
        private bool stopLoop; 
        private Vector3 startPosition;
        private bool useMoveHigh; // 判断是否使用 默认高度

        private bool useArmStopTime;
        private float stopGameTime;
        private ParticleSystem armStopEffect;

        //private Dictionary<int, ArmMoveEvent> armMoveEvents;
        public Action OnUnitCollision { get; internal set; }

        private void Update()
        {
            if (useArmStopTime)
            {
                DestroyAndPlayEffects();
            }
        }

        public void DestroyAndPlayEffects()
        {
            if (Time.time > stopGameTime + 0.01f)
            {
                if (armStopEffect != null)
                {
                    armMoveTween.Pause();
                    ParticleSystem particleSystem = Instantiate(armStopEffect, transform.position, Quaternion.Euler(transform.forward), null);
                    particleSystem.Play();
                }
                Destroy(gameObject);
            }
        }

        public void Init(List<ArmMoveEvent> armMoveEvents)
        {
            this.armMoveEvents = new Dictionary<int, ArmMoveEvent>();
            //foreach (var moveEvent in moveEvents)
            //{
            //    this.moveEvents.Add(moveEvent.pathPoint, moveEvent);
            //}
            foreach (var armMoveEvent in armMoveEvents)
            {
                //Debug.LogError($"武器移动挂载key： {armMoveEvent.ToString()}");
                if (!this.armMoveEvents.ContainsKey(armMoveEvent.pathPoint))
                {
                    this.armMoveEvents.Add(armMoveEvent.pathPoint, armMoveEvent);
                }
                else
                {
                    Debug.LogWarning($"Duplicate pathPoint detected: {armMoveEvent.pathPoint}. Skipping this moveEvent.");
                }
            }
        }


        public void Setup(List<GameObject> pathPoints, float moveTime, float moveHigh,List<ArmMoveEvent> armMoveEvents,float stopSpawnArmUnitsTime,bool useMoveHigh, float stopGameTime, ParticleSystem armStopEffect, bool useArmStopTime)
        {
            
            startPosition = gameObject.transform.position;
            if (useMoveHigh)
            {
                startPosition.y = moveHigh;
            }
            this.pathPoints = pathPoints;
            this.moveTime = moveTime;
            this.moveHigh = moveHigh;
            this.stopSpawnArmUnitsTime = stopSpawnArmUnitsTime;
            this.useMoveHigh = useMoveHigh;
            this.stopGameTime = stopGameTime;
            this.armStopEffect = armStopEffect;
            this.useArmStopTime = useArmStopTime;
            Init(armMoveEvents);
            StartAnimator();
        }

        public void StartAnimator()
        {
            //if (Time.time > stopGameTime + 0.01f)
            //{
            //    Debug.LogError($"Time.time = {Time.time}.stopGameTime = {stopGameTime}");
            //    Debug.LogError($"到达游戏结束时间时，播放特效");
            //    armMoveTween.Pause();
            //    ParticleSystem particleSystem = Instantiate(armStopEffect, transform.position, Quaternion.Euler(transform.forward), null);
            //    particleSystem.Play();
            //    Destroy(gameObject);
            //}

            gameObject.SetActive(true);

            List<Vector3> movePath = new List<Vector3>();
            foreach (var pathPoint in pathPoints)
            {
                var pos = pathPoint.transform.position;
                //Debug.LogError($"路径点高度，x：{pos.x} ,y: {pos.y} ,z: {pos.z}");
                if (useMoveHigh)
                {
                    //Debug.LogError($"使用默认高度:{moveHigh}");
                    pos.y = moveHigh;
                }
                //if (pathPoint.CompareTag("Up"))
                //{
                //    pos.y = moveHigh + highDif;
                //}
                //else if (pathPoint.CompareTag("Down"))
                //{
                //    pos.y = moveHigh - highDif;
                //}
                //else
                //{
                //    pos.y = moveHigh;
                //}
                movePath.Add(pos);
            }
            //Debug.LogError("movePath: " + string.Join(", ", movePath.Select(p => p.ToString()).ToArray()));
            transform.DOKill();
            transform.position = startPosition; // 重置到 空物体位置，然后继续沿着路径点执行
            //armMoveTween = transform.DOPath(movePath.ToArray(), moveTime).SetEase(Ease.Linear).OnWaypointChange(OnWaypointReached).OnComplete(() =>
            armMoveTween = transform.DOPath(movePath.ToArray(), moveTime).SetLookAt(0.01f).SetEase(Ease.Linear).OnWaypointChange(OnWaypointReached).OnComplete(() =>
            //transform.DOPath(movePath.ToArray(), moveTime).SetEase(Ease.Linear).OnComplete(() =>
            {
                    StartAnimator(); // 重新开始动画，继续循环
            });


        }
        private void ContinueAnimation()
        {
            if (armMoveTween != null)
            {
                ArmMoveAnimator.SetTrigger("Move");
                armMoveTween.Play();
            }
        }
        private void OnWaypointReached(int waypointIndex)
        {
            if (armMoveEvents.ContainsKey(waypointIndex))
            {
                ParticleSystem particleSystem = Instantiate(armMoveEvents[waypointIndex].dieEffect, transform.position, Quaternion.Euler(transform.forward), null);
                particleSystem.Play();

            }
        }
        void UpdateRotation()
        {
            if (armMoveTween == null) return;

            float t = armMoveTween.ElapsedPercentage();

            if (t >= 0.99f)
            {
                return;
            }

            Vector3 currentPos = armMoveTween.PathGetPoint(t);
            Vector3 nextPos = armMoveTween.PathGetPoint(t + 0.01f);

            Vector3 direction = (nextPos - currentPos).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, angleSpeed * Time.deltaTime);
        }


        private void OnTriggerEnter(Collider other)
        {
            MoveUnit otherMoveUnit = other.GetComponent<MoveUnit>();
            if (otherMoveUnit)
            {
                otherMoveUnit.Die();
            }
        }
    }
}
