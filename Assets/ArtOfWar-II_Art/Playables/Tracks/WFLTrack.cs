using System;
using ArtOfWar_II_Art.Playables.PlayableAssets;
using UnityEngine;
using UnityEngine.Timeline;

[Serializable]
[TrackClipType(typeof(DynamicEffectPlayableAsset))]
[TrackClipType(typeof(EffectPlayableAsset))]
[TrackClipType(typeof(BulletPlayableAsset))]
[TrackBindingType(typeof(GameObject))]
[ExcludeFromPreset]
public class WFLTrack : MarkerTrack
{
}