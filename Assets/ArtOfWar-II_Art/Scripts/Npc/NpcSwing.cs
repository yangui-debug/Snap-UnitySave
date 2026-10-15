using ArtDefines;
using UnityEngine;

namespace Npc
{
    public class NpcSwing : MonoBehaviour
    {
        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void SetSwingState(bool isSwing)
        {
            gameObject.SetActive(isSwing);

            if (isSwing)
            {
                animator.SetTrigger(AnimDefines.SitLoop);
            }
        }
    }
}