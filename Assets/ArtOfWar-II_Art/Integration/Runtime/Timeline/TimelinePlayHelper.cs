using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Art.View
{
    public static class TimelinePlayHelper
    {
        public static void PlayTimeLineGroup(PlayableDirector director, string group)
        {
            SetTimelineGroupState(director, group);

            // 重新评估播放
            director.time = 0;
            director.RebuildGraph();
            director.Play();
        }

        public static void SetTimeLineGroupTime(PlayableDirector director, string group, float time)
        {
            SetTimelineGroupState(director, group);
            director.time = time;
            director.Evaluate();
        }

        public static void SetTimelineGroupState(PlayableDirector director, string group)
        {
            // 获取 Timeline 资产
            if (director.playableAsset is not TimelineAsset timeline)
            {
                return;
            }

            // 遍历所有轨道，找到属于目标 Group 的 Track
            foreach (TrackAsset track in timeline.GetOutputTracks())
            {
                bool isTargetGroup = track.parent && track.parent.name.Equals(group);
                track.muted = !isTargetGroup; // 只启用目标 Group 的 Track
            }
        }
    }
}