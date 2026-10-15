using Playables.Markers;
using UnityEngine.Timeline;

namespace Playables.PlayableAssets
{
    [TrackClipType(typeof(UpdateHpMarker), true)]
    [HideInMenu]
    public class EvtMarkerTrack : MarkerTrack
    {
    }
}