using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityTimer;

namespace VideoPlugIn.Scripts
{
    public class MoveUnit : MonoBehaviour
    {
        [Header("动画器控制器")] [SerializeField] private Animator moveAnimator;
        [Header("动画播放速度")] [SerializeField] private float animatorSpeed;
        [Header("旋转角速度")] [SerializeField] private float angleSpeed;
        private float moveTime;
        private List<Vector3> movePath;
        private Tween moveTween;
        private ParticleSystem dieEffect;
        private Dictionary<int, MoveEvent> moveEvents;
        private int currentWaypointIndex;
        private float effectCreateHigh;
        private float stopGameTime;
        private bool useStopTime;
        private bool isDestroyed = false; // 标记对象是否已销毁

        public void Init(List<Vector3> pathPointVector, float moveTime, ParticleSystem dieEffect, List<MoveEvent> moveEvents, float effectCreateHigh, float stopGameTime,bool useStopTime)
        {
            this.moveTime = moveTime;
            this.effectCreateHigh = effectCreateHigh;
            this.stopGameTime = stopGameTime;
            this.useStopTime = useStopTime;
            currentWaypointIndex = 0;
            moveAnimator.speed = animatorSpeed;
            movePath = new List<Vector3>();


            // 这个是通过相对位置的偏移量来确定  士兵移动路径的
            Vector3 startPos = transform.position;
            foreach (var pathPoint in pathPointVector)
            {
                startPos += pathPoint;
                movePath.Add(startPos);
            }

            this.moveEvents = new Dictionary<int, MoveEvent>();
            //foreach (var moveEvent in moveEvents)
            //{
            //    this.moveEvents.Add(moveEvent.pathPoint, moveEvent);
            //}
            foreach (var moveEvent in moveEvents)
            {
                if (!this.moveEvents.ContainsKey(moveEvent.pathPoint))
                {
                    this.moveEvents.Add(moveEvent.pathPoint, moveEvent);
                }
                else
                {
                    Debug.LogWarning($"Duplicate pathPoint detected: {moveEvent.pathPoint}. Skipping this moveEvent.");
                }
            }

            this.dieEffect = dieEffect;
            StartMove();
        }

        private void StartMove()
        {
            //if (Time.time >= stopGameTime) return;
            ////{
            ////    Destroy(gameObject);
            ////}
            moveTween = transform.DOPath(movePath.ToArray(), moveTime).SetEase(Ease.Linear).OnWaypointChange(OnWaypointReached)
                .OnUpdate(UpdateRotation);
        }
        private void Update()
        {
            if (useStopTime)
            {
                DestroyUnit();
            }
            
        }
        private void DestroyUnit()
        {
            if (Time.time >= stopGameTime)
            {
                isDestroyed = true;  // 标记对象即将被销毁
                moveAnimator = null; // 避免后续访问已销毁的 Animator
                moveTween.Kill(); // 取消动画，防止出错
                Destroy(gameObject);
            }
        }
        void UpdateRotation()
        {
            if (moveTween == null) return;

            float t = moveTween.ElapsedPercentage();

            if (t >= 0.99f)
            {
                return;
            }

            Vector3 currentPos = moveTween.PathGetPoint(t);
            Vector3 nextPos = moveTween.PathGetPoint(t + 0.01f);

            Vector3 direction = (nextPos - currentPos).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, angleSpeed * Time.deltaTime);
        }

        private void PauseAnimation()
        {
            if (moveTween != null)
            {
                moveTween.Pause();
            }
        }

        private void ContinueAnimation()
        {
            if (isDestroyed) return;  // 确保对象未被销毁
            if (moveTween == null) return; // 避免访问空的 moveTween
            if (moveAnimator == null)
            {
                Debug.LogWarning("MoveAnimator 已被销毁，无法继续动画");
                return;
            }
            if(moveTween != null)
            {
                moveAnimator.SetTrigger("Move");
                moveTween.Play();
            }
        }

        private void OnWaypointReached(int waypointIndex)
        {
            if (moveEvents.ContainsKey(waypointIndex))
            {
                if (moveEvents[waypointIndex].animatorEvent == "Die")
                {
                    Die();
                }
                else
                {
                    currentWaypointIndex = waypointIndex;
                    PauseAnimation();
                    MoveEvent moveEvent = moveEvents[waypointIndex];
                    moveAnimator.SetTrigger(moveEvent.animatorEvent);
                    if (moveEvent.duration > 0f)
                    {
                        Timer.Register(moveEvent.duration, ContinueAnimation);
                    }
                }
            }
        }

        public void Die()
        {
            moveTween.Pause();
            Vector3 pos = transform.position;
            pos.y = effectCreateHigh;
            ParticleSystem particleSystem = Instantiate(dieEffect, pos, Quaternion.Euler(transform.forward), null);
            particleSystem.Play();
            Destroy(gameObject);
        }

        public void TriggerGlobalEvent(TriggerEvent triggerEvent)
        {
            if (moveTween != null)
            {
                if (triggerEvent.animatorEvent == "Die")
                {
                    Die();
                }
                else
                {
                    PauseAnimation();
                    moveAnimator.SetTrigger(triggerEvent.animatorEvent);
                    if (triggerEvent.duration > 0f)
                    {
                        Timer.Register(triggerEvent.duration, ContinueAnimation);
                    }
                }
            }
        }
    }
}