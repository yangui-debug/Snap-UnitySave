using Playables;
using Playables.Core;
using UnityEngine;
using UnityEngine.Playables;

namespace ArtOfWar_II_Art.Playables.PlayableBehaviors
{
    public class EffectPlayableBehaviour : PlayableBehaviour
    {
        public PlayableAgent Agent { get; set; }
        public ParticleSystem effect { get; set; }
        public EffectPlayMode playMode { get; set; }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (effect)
            {
                effect.gameObject.SetActive(true);
                effect.Play();
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!effect)
            {
                return;
            }
            
            if (playMode == EffectPlayMode.Clamp)
            {
                effect.Stop();
                effect.Clear();
                effect.gameObject.SetActive(false);
            }
            else if (playMode == EffectPlayMode.Free)
            {
                Agent.Schedule(() => { effect.gameObject.SetActive(false); }, 5);
            }
        }
    }
}