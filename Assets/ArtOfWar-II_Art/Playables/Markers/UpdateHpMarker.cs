using System.Collections.Generic;
using Playables.Core;
using UnityEngine;

namespace Playables.Markers
{
    public class UpdateHpMarker : IMarker
    {
        public override void Do(PlayableAgent agent)
        {
            // 影响多目标效果
            if (agent.GetData(PlayableConstant.targets) is List<GameObject> targets)
            {
                foreach (GameObject target in targets)
                {
                    IHpUpdateTrigger trigger = target.GetComponentInParent<IHpUpdateTrigger>();
                    trigger?.UpdateBlood();
                }
            }
            
            // 影响自身
            IHpUpdateTrigger selfTrigger = agent.GetComponentInParent<IHpUpdateTrigger>();
            selfTrigger?.UpdateBlood();
        }
    }
}