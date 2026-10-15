using ArtOfWar_II_Art.Playables.PlayableBehaviors;
using Playables;
using Playables.Core;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ArtOfWar_II_Art.Playables.PlayableAssets
{
    public class DynamicEffectPlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        public ExposedReference<GameObject> sourceEffect;
        public EffectPlayMode playMode;
        public EffectMode effectMode;

        public override double duration { get; } = 1;
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            ScriptPlayable<DynamicEffectBehaviour> playable = ScriptPlayable<DynamicEffectBehaviour>.Create(graph);

            DynamicEffectBehaviour behaviour = playable.GetBehaviour();

            behaviour.Agent = owner.GetComponent<PlayableAgent>();
            behaviour.Effect = sourceEffect.Resolve(graph.GetResolver());
            behaviour.PlayMode = playMode;
            behaviour.EffectMode = effectMode;

            return playable;
        }
    }
}