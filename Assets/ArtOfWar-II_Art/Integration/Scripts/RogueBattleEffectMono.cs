using UnityEngine;

namespace Core.View.BagFight.GamePlay
{
    /// <summary>
    /// rogue玩法表现的实例对象
    /// </summary>
    public class RogueBattleEffectMono : MonoBehaviour
    {
        public static Color? originColor { get; set; }

        public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public GameObject[] hpProgress = default;
        public GameObject[] wallShieldEffects = default;
        public Material wallMat = default;
        public Color beAttackEffectColor = default;
        public float hdrIntensity = default;
        public ParticleSystem brokenEffect = default;
        public GameObject wallObject = default;

#if UNITY_EDITOR

        private void OnDestroy()
        {
            if (originColor.HasValue)
            {
                wallMat.SetColor(BaseColor, originColor.Value);
            }
        }
#endif
    }
}