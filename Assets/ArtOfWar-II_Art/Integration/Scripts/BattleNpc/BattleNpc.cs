using UnityEngine;

namespace Art.View
{
    public class BattleNpc : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        public void SetAnimator(Animator input)
        {
            animator = input;
        }

        public void Enter()
        {
            EnterIdle();
        }

        public void OnBattleStart()
        {
            // 开局随机victory1-3随机
            animator.SetTrigger($"Victory_{Random.Range(1, 4)}");
        }

        public void OnSoldierNpcEnter()
        {
            // idle    
        }

        public void OnSoldierNpcBossAppear()
        {
            // 士兵出现boss, 开始victory
            animator.SetTrigger($"Victory_1");
        }

        public void OnStage3()
        {
            // 进入第"3"阶段, 换victory2-4随机
            animator.SetTrigger($"Victory_{Random.Range(2, 5)}");
        }

        public void OnFail()
        {
            // 失败, fail1-3随机
            animator.SetTrigger($"Fail_{Random.Range(1, 4)}");
        }

        public void Exit()
        {
            EnterIdle();
        }

        private void EnterIdle()
        {
            animator.Play("Idle");
        }
    }
}