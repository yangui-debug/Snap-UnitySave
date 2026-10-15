using ArtOfWar_II_Art.Playables.PlayableBehaviors;
using Playables.Core;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ArtOfWar_II_Art.Playables.PlayableAssets
{
    public class BulletPlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        public override double duration { get; } = 1.5f;

        [SerializeField] private ExposedReference<Transform> shootPoint;
        [SerializeField] private ExposedReference<GameObject> bullet;
        [SerializeField] private float shifting = 10;
        [SerializeField] private bool isUi = false;
        [SerializeField] private bool useTarget = true;
        [SerializeField] private bool usePosition = true;
        [SerializeField] private bool isReversed = false;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            ScriptPlayable<BulletPlayableBehaviour> playable = ScriptPlayable<BulletPlayableBehaviour>.Create(graph);
            BulletPlayableBehaviour behaviour = playable.GetBehaviour();

            behaviour.Agent = owner.GetComponent<PlayableAgent>();
            behaviour.Bullet = bullet.Resolve(graph.GetResolver());
            behaviour.ShootPoint = shootPoint.Resolve(graph.GetResolver());
            behaviour.Shifting = shifting;
            behaviour.IsUi = isUi;
            behaviour.UseTarget = useTarget;
            behaviour.UsePosition = usePosition;
            behaviour.IsReversed = isReversed;

            return playable;
        }
    }
}