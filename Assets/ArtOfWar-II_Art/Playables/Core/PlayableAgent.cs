using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Playables.DebugComponent;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using IMarker = Playables.Markers.IMarker;
using Object = UnityEngine.Object;

namespace Playables.Core
{
    public class PlayableAgent : MonoBehaviour, INotificationReceiver
    {
        [SerializeField] private PlayableDirector director = default;

        private Dictionary<string, object> tmpData;

        private List<GroupTrack> taskGroups;

        public Func<Vector3, Vector3> GetUiPosFunc { get; set; }

        private List<IAgentComp> customComps;

        // 维护绑定在自身上的效果
        private List<GameObject> bindEffects;

        private Coroutine timer;

        private void Start()
        {
#if UNITY_EDITOR
            if (Application.isPlaying && director)
            {
                InitSetup();
            }
#endif

            SetupGroups();
        }

        public void InitSetup()
        {
            Animator animator = GetAnimatorFromTimeline();
            if (animator)
            {
                animator.fireEvents = false;
            }

            foreach (PlayableComponent component in GetComponentsInChildren<PlayableComponent>())
            {
                component.Run();
            }
        }

        private void SetupGroups()
        {
            if (director)
            {
                TimelineAsset timelineAsset = (TimelineAsset) director.playableAsset;

                taskGroups = timelineAsset.GetRootTracks().OfType<GroupTrack>().ToList();
            }
        }

        public void Play(List<string> groups, bool isEnd, Func<Vector3, Vector3> getUiPos)
        {
            GetUiPosFunc = getUiPos;
            CallCustomBuildup();

            if (director)
            {
                PlayWithGroup(groups, isEnd);
            }
            else
            {
                OnPlayStopped();
            }
        }

        public void BindEffect(GameObject effect)
        {
            bindEffects ??= new List<GameObject>();
            bindEffects.Add(effect);

            Transform et = effect.transform;
            et.SetParent(transform);
        }

        public void Recycle(GameObject receiver)
        {
            this.RecycleEffects(receiver);

            // TODO @志达？？？
            if (receiver.gameObject.GetComponent<PlayableDirector>())
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(false);
                }
            }

            // 移除绑定在自身上的效果
            if (bindEffects != null)
            {
                foreach (GameObject effect in bindEffects)
                {
                    PlayablePool.InternalPut(effect);
                }

                bindEffects.Clear();
            }
        }

        public void SetData(string key, object value)
        {
            tmpData ??= new Dictionary<string, object>();
            tmpData[key] = value;
        }

        public object GetData(string key)
        {
            if (tmpData == null)
            {
                return null;
            }

            tmpData.TryGetValue(key, out object value);
            return value;
        }

        private Animator GetAnimatorFromTimeline()
        {
            AnimationTrack animaTrack = GetAnimationTrack();
            if (!animaTrack)
            {
                return null;
            }

            Object binding = director.GetGenericBinding(animaTrack);
            return binding as Animator;
        }

        private AnimationTrack GetAnimationTrack()
        {
            PlayableAsset playableAsset = director.playableAsset;

            foreach (var track in ((TimelineAsset) playableAsset).GetOutputTracks())
            {
                if (track is AnimationTrack animaTrack)
                {
                    return animaTrack;
                }
            }

            return null;
        }

        private void PlayWithGroup(List<string> groups, bool isEnd)
        {
            List<TrackAsset> tracks = new List<TrackAsset>();
            if (groups.Count > 0 && taskGroups != null)
            {
                foreach (GroupTrack track in taskGroups)
                {
                    bool muted = !groups.Contains(track.name);
                    track.muted = muted;

                    if (!muted)
                    {
                        tracks.AddRange(track.GetChildTracks());
                    }
                }
            }

            if (tracks.Count > 0)
            {
                double start = tracks.Min(x => x.start);
                double end = tracks.Max(x => x.end);

                int speed = isEnd ? 10 : 1;

                // director.RebuildGraph();
                // director.playableGraph.GetRootPlayable(0).SetSpeed(speed);
                director.initialTime = start;
                director.Play();
                Schedule(OnPlayStopped, (float) (end - start) / speed);
            }
            else
            {
                OnPlayStopped();
            }
        }

        public void Stop()
        {
            if (director && director.state == PlayState.Playing)
            {
                director.time = director.duration;
                director.Evaluate();
                OnPlayStopped();
            }
        }

        private void OnPlayStopped()
        {
            // clear放在回调complete回调前面，防止外面回调后再play同一个单位的下一个group,导致清除了第二次需要的数据
            tmpData?.Clear();

            if (timer != null)
            {
                StopCoroutine(timer);
            }

            if (director)
            {
                director.Stop();
            }
        }

        public void Schedule(Action task, float delay)
        {
            timer = StartCoroutine(schedule(task, delay));
        }

        private IEnumerator schedule(Action task, float delay)
        {
            yield return new WaitForSeconds(delay);
            task();
        }

        public void RegisterComp(IAgentComp comp)
        {
            customComps ??= new List<IAgentComp>();
            customComps.Add(comp);
        }

        private void CallCustomBuildup()
        {
            if (customComps != null)
            {
                foreach (IAgentComp comp in customComps)
                {
                    comp.Buildup(this);
                }
            }
        }

        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is IMarker marker)
            {
                marker.Do(this);
            }
        }
    }
}