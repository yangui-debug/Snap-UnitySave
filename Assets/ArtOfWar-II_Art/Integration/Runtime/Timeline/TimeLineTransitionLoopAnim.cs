using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ArtOfWar_II_Art.Integration.Scripts
{
    public class TimeLineTransitionLoopAnim : MonoBehaviour
    {
        [SerializeField] private string loopAnim = "Idle";
        private PlayableGraph playableGraph;

        private void Start()
        {
            SetupPlayableDirector();
        }

        private void OnDestroy()
        {
            if (playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }
        }

        private void SetupPlayableDirector()
        {
            PlayableDirector director = gameObject.GetComponentInChildren<PlayableDirector>();
            if (director)
            {
                director.stopped += d =>
                {
                    if (FindAnimator(d) is { } animator && FindAnimClip(d, animator, loopAnim) is { } clip)
                    {
                        PlayAnima(animator, clip);
                    }
                };
            }
        }

        public float PlayAnimFromTimeline(string animClipName)
        {
            PlayableDirector director = gameObject.GetComponentInChildren<PlayableDirector>();
            if (director && FindAnimator(director) is { } animator && FindAnimClip(director, animator, animClipName) is { } clip)
            {
                PlayAnima(animator, clip);
                return clip.length;
            }

            return 0;
        }

        private Animator FindAnimator(PlayableDirector director)
        {
            TimelineAsset timelineAsset = (TimelineAsset) director.playableAsset;
            foreach (TrackAsset track in timelineAsset.GetOutputTracks())
            {
                if (track is AnimationTrack animationTrack && director.GetGenericBinding(animationTrack) is Animator animator)
                {
                    return animator;
                }
            }

            return null;
        }

        private AnimationClip FindAnimClip(PlayableDirector director, Animator animator, string animName)
        {
            // 先从runtimeAnimatorController上找, 没有的话从TimelineAsset上找
            AnimationClip anima = animator.runtimeAnimatorController?.animationClips.FirstOrDefault(x => x.name.Equals(animName));
            if (anima == null || anima.empty)
            {
                return FindAnimFromTL(director, animName);
            }

            return anima;
        }

        private AnimationClip FindAnimFromTL(PlayableDirector director, string animClipName)
        {
            TimelineAsset timelineAsset = (TimelineAsset) director.playableAsset;
            foreach (TrackAsset track in timelineAsset.GetOutputTracks())
            {
                if (track is AnimationTrack animationTrack)
                {
                    foreach (TimelineClip timelineClip in animationTrack.GetClips())
                    {
                        AnimationClip animationClip = timelineClip.animationClip;
                        if (animationClip.name.Equals(animClipName))
                        {
                            return animationClip;
                        }
                    }
                }
            }

            return null;
        }

        private void PlayAnima(Animator animator, AnimationClip animationClip)
        {
            playableGraph = PlayableGraph.Create();
            AnimationPlayableOutput playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);
            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(playableGraph, animationClip);

            playableOutput.SetSourcePlayable(clipPlayable);
            playableGraph.Play();
        }
    }
}