using System.Linq;
using UnityEngine;

namespace Playables.DebugComponent
{
    public class FindUiPositionComponent : PlayableComponent
    {
        [SerializeField] private GameObject[] uiGameObjects = default;
        public override void Run()
        {
            base.Run();

            agent.SetData(PlayableConstant.positions, uiGameObjects.ToList().ConvertAll(x => x.transform.position));
        }
    }
}