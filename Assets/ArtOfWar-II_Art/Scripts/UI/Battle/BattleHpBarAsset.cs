using UnityEngine;

namespace ArtOfWar_II_Art.Scripts.UI.Battle
{
    public class BattleHpBarAsset : MonoBehaviour
    {
        [SerializeField] private Color[] bloodBarFxColors;
        [SerializeField] private float fxAlpha1 = 0.85f;
        [SerializeField] private float fxAlpha2 = 0.5f;
        [SerializeField] private ParticleSystem bloodBarParticle;
        [SerializeField] private ParticleSystem bloodBarParticle2;

        public void PlayEffect(int teColor)
        {
            // 设置特效颜色
            int attr = teColor == 0 ? 0 : (teColor % 10 + 1); // 见枚举TEColor，+1为了配一个普通的颜色 
            SetParticle(bloodBarParticle, attr, fxAlpha1);
            SetParticle(bloodBarParticle2, attr, fxAlpha2);
            bloodBarParticle.Play();
            bloodBarParticle2.Play();
        }

        private void SetParticle(ParticleSystem particle, int attr, float a)
        {
            var main = particle.main;
            Color startColor = bloodBarFxColors[attr % bloodBarFxColors.Length];
            startColor.a = a;
            main.startColor = startColor;
        }
    }
}