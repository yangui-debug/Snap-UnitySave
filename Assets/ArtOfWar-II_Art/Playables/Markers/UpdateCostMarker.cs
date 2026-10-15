using System.Collections.Generic;
using Playables.Core;
using UnityEngine;

namespace Playables.Markers
{
    public class UpdateCostMarker : IMarker
    {
        public override void Do(PlayableAgent agent)
        {
            // 影响多目标效果
            if (agent.GetData(PlayableConstant.targets) is List<GameObject> targets)
            {
                foreach (GameObject target in targets)
                {
                    ICostUpdateTrigger trigger = target.GetComponentInParent<ICostUpdateTrigger>();
                    trigger?.UpdateCost();
                }
            }
        }
    }
}
