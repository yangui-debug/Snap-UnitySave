using ArtOfWar_II_Art.Playables.PlayableBehaviors;
using Playables;
using Playables.Core;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ArtOfWar_II_Art.Playables.PlayableAssets
{
    [ExcludeFromPreset]
    public class EffectPlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        public ExposedReference<ParticleSystem> effect;
        public EffectPlayMode playMode;

        public override double duration { get; } = 1;
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            ScriptPlayable<EffectPlayableBehaviour> playable = ScriptPlayable<EffectPlayableBehaviour>.Create(graph);

            EffectPlayableBehaviour behaviour = playable.GetBehaviour();

            behaviour.Agent = owner.GetComponent<PlayableAgent>();
            behaviour.effect = effect.Resolve(graph.GetResolver());
            behaviour.playMode = playMode;

            return playable;
        }
    }
}