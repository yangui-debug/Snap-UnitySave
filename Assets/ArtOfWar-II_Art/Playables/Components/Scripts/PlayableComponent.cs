using Playables.Core;
using UnityEngine;

namespace Playables.DebugComponent
{
    public class PlayableComponent : MonoBehaviour
    {
        protected PlayableAgent agent;

        public virtual void Run()
        {
            agent = GetComponentInParent<PlayableAgent>();
        }
    }
}