using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Playables.DebugComponent
{
    public class FindTargetsComponent : PlayableComponent
    {
        public override void Run()
        {
            base.Run();

            List<GameObject> targets = GetComponentsInChildren<Transform>().ToList().ConvertAll(x => x.gameObject);
            targets.Remove(transform.gameObject);

            agent.SetData(PlayableConstant.targets, targets);
        }
    }
}