using UnityEngine;

namespace Core.View.BagFight.GamePlay
{
    /// <summary>
    /// rogue玩法表现的实例对象
    /// </summary>
    public class RogueWallMono : MonoBehaviour
    {
        public static Color? originColor { get; set; }

        public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] public GameObject[] hpProgress = default;
        [SerializeField] public GameObject[] wallShieldEffects = default;
        [SerializeField] public Material wallMat = default;
        [SerializeField] public Color beAttackEffectColor = default;
        [SerializeField] public float hdrIntensity = default;
        [SerializeField] public ParticleSystem brokenEffect = default;
        [SerializeField] public GameObject wallObject = default;

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