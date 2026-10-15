using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Playables.DebugComponent
{
    public class FindPositionComponent : PlayableComponent
    {
        public override void Run()
        {
            base.Run();

            List<Transform> targets = GetComponentsInChildren<Transform>().ToList();
            targets.Remove(transform);

            agent.SetData(PlayableConstant.positions, targets.ConvertAll(x => x.position));
        }
    }
}