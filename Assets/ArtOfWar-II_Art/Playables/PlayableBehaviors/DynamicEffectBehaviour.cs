using System.Collections.Generic;
using System.Linq;
using Playables;
using Playables.Core;
using UnityEngine;
using UnityEngine.Playables;

namespace ArtOfWar_II_Art.Playables.PlayableBehaviors
{
    public class DynamicEffectBehaviour : PlayableBehaviour
    {
        public GameObject Effect { get; set; }
        public PlayableAgent Agent { get; set; }
        public EffectPlayMode PlayMode { get; set; }
        public EffectMode EffectMode { get; set; }

        private readonly Dictionary<GameObject, GameObject> effectDic = new Dictionary<GameObject, GameObject>();

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ClearAll();

            if (EffectMode == EffectMode.Both)
            {
                LoadTargetEffects();
                LoadPositionEffects();
            }
            else
            {
                if (EffectMode.HasFlag(EffectMode.Self))
                {
                    LoadSelfEffect();
                }

                if (EffectMode.HasFlag(EffectMode.Targets))
                {
                    LoadTargetEffects();
                }

                if (EffectMode.HasFlag(EffectMode.Positions))
                {
                    LoadPositionEffects();
                }
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (PlayMode == EffectPlayMode.Clamp)
            {
                ClearAll();
            }
            else if (PlayMode == EffectPlayMode.Loop)
            {
                // do something
            }
            else
            {
                Agent.Schedule(ClearAll, 10);
            }
        }

        private void LoadSelfEffect()
        {
            LoadEffects(1, null);
            foreach (GameObject value in effectDic.Keys)
            {
                value.transform.position = Agent.transform.position;
            }
        }

        private void LoadTargetEffects()
        {
            if (Agent.GetData(PlayableConstant.targets) is List<GameObject> targets)
            {
                LoadEffects(targets.Count, targets);

                foreach (KeyValuePair<GameObject, GameObject> pair in effectDic)
                {
                    pair.Key.transform.position = pair.Value.transform.position;
                    if (PlayMode != EffectPlayMode.Free)
                    {
                        pair.Value.GetComponentInChildren<PlayableAgent>()?.BindEffect(pair.Key);
                    }
                }
            }
        }

        private void LoadPositionEffects()
        {
            if (Agent.GetData(PlayableConstant.positions) is List<Vector3> positions && positions.Count > 0)
            {
                LoadEffects(positions.Count, null);

                List<GameObject> effects = effectDic.Keys.ToList();
                for (int i = 0; i < positions.Count; i++)
                {
                    effects[i].transform.position = positions[i];
                }
            }
        }

        private void LoadEffects(int count, List<GameObject> targets)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject target = Agent.gameObject;
                if (targets != null)
                {
                    target = targets[i];
                }

                GameObject effect = Agent.Get(Effect, target);
                effect.SetActive(true);
                effectDic.Add(effect, target);
            }
        }

        private void ClearAll()
        {
            foreach (KeyValuePair<GameObject, GameObject> effectPair in effectDic)
            {
                Agent.Put(effectPair.Key, effectPair.Value);
            }

            effectDic.Clear();
        }
    }
}