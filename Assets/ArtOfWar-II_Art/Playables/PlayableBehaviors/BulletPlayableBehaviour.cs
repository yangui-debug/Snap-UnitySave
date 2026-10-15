using System.Collections.Generic;
using Playables;
using Playables.Core;
using UnityEngine;
using UnityEngine.Playables;

namespace ArtOfWar_II_Art.Playables.PlayableBehaviors
{
    public struct BulletStruct
    {
        public Vector3 from;
        public Vector3 to;
        public Vector3 speed;
        public float ft;
    }

    public class BulletPlayableBehaviour : PlayableBehaviour
    {
        public GameObject Bullet { get; set; }
        public PlayableAgent Agent { get; set; }
        public Transform ShootPoint { get; set; }
        public bool IsUi { get; set; }
        public bool UseTarget { get; set; }
        public bool UsePosition { get; set; }
        public bool IsReversed { get; set; }

        private readonly List<BulletStruct> structs = new List<BulletStruct>();
        private readonly List<GameObject> bullets = new List<GameObject>();

        public float Shifting { get; set; } = 10;
        public float Length { get; set; }
        public float StartPosShifting { get; set; }
        public float TargetPosShifting { get; set; }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (UsePosition && Agent.GetData(PlayableConstant.positions) is List<Vector3> positions && positions.Count > 0)
            {
                LoadBullets(playable, positions);
            }
            else if (UseTarget && Agent.GetData(PlayableConstant.targets) is List<GameObject> targets)
            {
                LoadBullets(playable, targets.ConvertAll(x => x.transform.position));
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            ClearAll();
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);

            float time = (float) playable.GetTime();

            for (int i = 0; i < bullets.Count; i++)
            {
                GameObject bullet = bullets[i];

                Vector3 p = CalculatePosition(structs[i], time);
                Vector3 d = p - bullet.transform.position;

                bullet.transform.position = p;
                if (d != Vector3.zero)
                {
                    bullet.transform.forward = d;
                }
            }
        }

        private void LoadBullets(Playable playable, List<Vector3> positions)
        {
            Vector3 fromPos = GetFromPos();

            for (int i = 0; i < positions.Count; i++)
            {
                BulletStruct st = new BulletStruct();

                if (IsUi)
                {
                    fromPos.z = positions[i].z;
                }

                st.from = fromPos;
                st.to = positions[i];
                st.ft = (float) playable.GetDuration();

                if (IsReversed)
                {
                    (st.from, st.to) = (st.to, st.from);
                }

                st.speed = CalculateSpeed(st.from, st.to, st.ft);

                structs.Add(st);
                bullets.Add(Object.Instantiate(Bullet));
            }
        }

        private Vector3 GetFromPos()
        {
            if (Agent.GetData(PlayableConstant.fromPos) is Vector3 p)
            {
                return p;
            }

            Vector3 fromPos = ShootPoint ? ShootPoint.position : Agent.transform.position;
            if (IsUi && Agent.GetUiPosFunc != null)
            {
                fromPos = Agent.GetUiPosFunc.Invoke(fromPos);
            }

            return fromPos;
        }

        private Vector3 CalculatePosition(BulletStruct st, float ft)
        {
            float shiftingY = Shifting / st.ft;
            float reduceY = shiftingY * ft / st.ft;

            Vector3 delta = st.speed;
            delta.y -= reduceY;

            Vector3 position = st.from + delta * ft;
            return position;
        }

        private Vector3 CalculateSpeed(Vector3 from, Vector3 to, float flyTime)
        {
            Vector3 targetPosition = Length > 0 ? GetLineBulletEndPos(from, to) : to;
            Vector3 pointAPos = from + Vector3.up * StartPosShifting;
            Vector3 targetPos = targetPosition + Vector3.up * TargetPosShifting;

            float x = (targetPos.x - pointAPos.x) / flyTime;
            float y = (targetPos.y - pointAPos.y + Shifting) / flyTime;
            float z = (targetPos.z - pointAPos.z) / flyTime;
            Vector3 speed = new Vector3(x, y, z);
            return speed;
        }

        private Vector3 GetLineBulletEndPos(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            direction.Normalize();

            Vector3 targetPosition = from + direction * Length;
            return targetPosition;
        }

        private void ClearAll()
        {
            foreach (GameObject bullet in bullets)
            {
                Object.Destroy(bullet);
            }

            bullets.Clear();
            structs.Clear();
        }
    }
}