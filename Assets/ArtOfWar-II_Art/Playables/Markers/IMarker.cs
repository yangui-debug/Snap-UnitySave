using System;
using Playables.Core;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Playables.Markers
{
    [Serializable]
    [TrackBindingType(typeof(PlayableAgent))]
    public abstract class IMarker : Marker, INotification
    {
        public PropertyName id => new PropertyName(name);

        public abstract void Do(PlayableAgent agent);
    }
}