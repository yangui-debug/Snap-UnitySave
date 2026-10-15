using UnityEngine;

namespace Coffee.UIExtensions
{
    [ExecuteInEditMode]
    public class Vector2UIParticle : UIParticle
    {
        [SerializeField] Vector2 m_CustomVector2 = Vector2.zero;

        private Vector2 oldCustomVector2;

        protected override void Awake()
        {
            base.Awake();
            oldCustomVector2 = m_CustomVector2;
        }

        private void Update()
        {
            if (Vector2.SqrMagnitude(m_CustomVector2 - oldCustomVector2) > 0.0001f)
            {
                oldCustomVector2 = m_CustomVector2;
                if (m_Particles.Count != 0)
                {
                    foreach (ParticleSystem ps in m_Particles)
                    {
                        ParticleSystemRenderer particleSystemRenderer = ps.GetComponent<ParticleSystemRenderer>();
                        particleSystemRenderer.material.SetTextureOffset("_MainTexture", m_CustomVector2);
                    }
                }
            }
        }
    }
}