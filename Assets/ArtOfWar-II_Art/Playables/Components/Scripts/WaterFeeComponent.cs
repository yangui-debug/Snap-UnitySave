using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Playables.DebugComponent
{
    public class WaterFeeComponent : PlayableComponent
    {
        public Camera uiCamera = default;
        public Camera worldCamera = default;

        public Image from = default;

        public override void Run()
        {
            base.Run();

            WorldMode();
        }

        private void WorldMode()
        {
            Vector3 p = from.transform.position;
            agent.SetData(PlayableConstant.fromPos, UIToWorld(p));
            agent.SetData(PlayableConstant.positions, new List<Vector3> {transform.position});
        }

        private Vector3 UIToWorld(Vector3 pos)
        {
            Vector3 p1 = uiCamera.WorldToScreenPoint(pos);
            Ray ray = worldCamera.ScreenPointToRay(p1);

            Plane plane = new Plane(Vector3.up, Vector3.zero);
            plane.Raycast(ray, out float enter);
            return ray.GetPoint(enter);
        }
    }
}