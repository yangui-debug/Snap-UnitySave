using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Art.View
{
    public class BattleNpcScene : MonoBehaviour, IBattleArtScene
    {
        private static readonly int Idle = Animator.StringToHash("Idle");
        private static readonly int Victory = Animator.StringToHash("Victory");
        private static readonly int VictoryIndex = Animator.StringToHash("VictoryIndex");

        private static readonly int Fail = Animator.StringToHash("Fail");
        private static readonly int FailIndex = Animator.StringToHash("FailIndex");

        [SerializeField] private List<Animator> npcAnimators;
        [SerializeField] private Vector2 victoryIntervalRange = new(3f, 6f); // 胜利动画随机间隔范围
        [SerializeField] private Vector2 idleIntervalRange = new(3f, 6f); // 闲置动画随机间隔范围
        [SerializeField] private Vector2Int victoryIndexRange = new(0, 3); // 胜利动画索引范围
        [SerializeField] private Vector2Int failIndexRange = new(0, 2); // 失败动画索引范围
        [SerializeField] private float idleVictoryChance = 0.75f; // 闲置状态下触发胜利动画的概率

        private float timer;
        private float randomInterval; // 随机间隔时间
        private int state;

        public void Enter()
        {
            state = 1;
            timer = 0;
            PlayIdleAnim();
        }

        private void Update()
        {
            switch (state)
            {
                case 1:
                    IdleUpdate();
                    break;

                case 2:
                    VictoryUpdate();
                    break;
            }
        }

        public void BattleEnd(bool isWin)
        {
            timer = 0;
            if (isWin)
            {
                state = 2;
                PlayVictoryAnim();
            }
            else
            {
                state = 3;
                PlayFailAnim();
            }
        }

        public void Exit()
        {
            state = 0;
        }

        private void PlayIdleAnim()
        {
            randomInterval = Random.Range(idleIntervalRange.x, idleIntervalRange.y);
            foreach (Animator animator in npcAnimators)
            {
                if (!animator)
                {
                    continue;
                }

                if (Random.value < idleVictoryChance)
                {
                    animator.SetInteger(VictoryIndex, Random.Range(victoryIndexRange.x, victoryIndexRange.y + 1));
                    animator.SetTrigger(Victory);
                }
                else
                {
                    animator.SetTrigger(Idle);
                }
            }
        }

        private void PlayVictoryAnim()
        {
            randomInterval = Random.Range(victoryIntervalRange.x, victoryIntervalRange.y);
            foreach (Animator animator in npcAnimators)
            {
                if (!animator)
                {
                    continue;
                }

                animator.SetInteger(VictoryIndex, Random.Range(victoryIndexRange.x, victoryIndexRange.y + 1));
                animator.SetTrigger(Victory);
            }
        }

        private void PlayFailAnim()
        {
            foreach (Animator animator in npcAnimators)
            {
                if (!animator)
                {
                    continue;
                }

                animator.SetInteger(FailIndex, Random.Range(failIndexRange.x, failIndexRange.y + 1));
                animator.SetTrigger(Fail);
            }
        }

        private void IdleUpdate()
        {
            timer += Time.deltaTime;
            if (timer >= randomInterval)
            {
                timer = 0;
                PlayIdleAnim();
            }
        }

        private void VictoryUpdate()
        {
            timer += Time.deltaTime;
            if (timer >= randomInterval)
            {
                PlayVictoryAnim();
            }
        }
    }
}