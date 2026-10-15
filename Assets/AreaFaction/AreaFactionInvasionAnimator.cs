using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.i89.AreaFaction
{
    /// <summary>
    /// 势力侵蚀动画管理器
    /// </summary>
    public class AreaFactionInvasionAnimator : MonoBehaviour
    {
        [Header("动画配置")]
        [SerializeField] private Material invasionMaterialTemplate;
        [SerializeField] private float invasionDuration = 2.4f;
        [SerializeField] private AnimationCurve invasionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("边界效果")]
        [SerializeField] private Color borderGlowColor = Color.yellow;
        [SerializeField] private float borderWidth = 0.08f;
        [SerializeField] private float borderIntensity = 1.4f;

        [Header("拉锯效果")]
        [SerializeField] private float flickerSpeed = 8f;
        [SerializeField] private float flickerAmount = 0.28f;

        [Header("噪声")]
        [SerializeField] private float noiseScale = 3.2f;
        [SerializeField] private float noiseAmount = 0.18f;

        [Header("战线颗粒")]
        [SerializeField] private float battleBandWidth = 0.18f;
        [SerializeField] private float battleDotDensity = 18f;
        [SerializeField] private float battleDotSize = 0.16f;
        [SerializeField] private float battleIntensity = 1.25f;
        [SerializeField] private float battleFlowSpeed = 8f;

        private AreaFactionView areaView;
        private Dictionary<int, InvasionData> activeInvasions = new Dictionary<int, InvasionData>();
        private readonly Dictionary<int, InvasionData> previewInvasions = new Dictionary<int, InvasionData>();

        private class InvasionData
        {
            public int siteIndex;
            public int fromFaction;
            public int toFaction;
            public Material invasionMaterial;
            public MeshRenderer renderer;
            public Vector2 borderCenter;
            public Vector2 borderDirection;
            public Coroutine coroutine;
        }

        public void Init(AreaFactionView view)
        {
            areaView = view;
        }

        /// <summary>
        /// 开始侵蚀动画
        /// </summary>
        public void StartInvasion(int siteIndex, int fromFaction, int toFaction, MeshRenderer renderer, int sourceSiteIndex = -1)
        {
            if (activeInvasions.ContainsKey(siteIndex))
            {
                StopInvasion(siteIndex);
            }

            InvasionData data = CreateInvasionData(siteIndex, fromFaction, toFaction, renderer, sourceSiteIndex);
            if (data == null)
            {
                return;
            }

            data.coroutine = StartCoroutine(InvasionCoroutine(data));
            activeInvasions[siteIndex] = data;

            // 标记等待动画
            areaView.MarkTileWaitingAnimation(siteIndex);
        }

        public void PreviewInvasion(int siteIndex, int fromFaction, int toFaction, MeshRenderer renderer, float progress, int sourceSiteIndex = -1)
        {
            ClearPreview(siteIndex);

            InvasionData data = CreateInvasionData(siteIndex, fromFaction, toFaction, renderer, sourceSiteIndex);
            if (data == null)
            {
                return;
            }

            data.invasionMaterial.SetFloat("_InvasionProgress", Mathf.Clamp01(progress));
            previewInvasions[siteIndex] = data;
        }

        public void ClearPreview(int siteIndex)
        {
            if (!previewInvasions.TryGetValue(siteIndex, out InvasionData data))
            {
                return;
            }

            ApplyFinalMaterial(data.renderer, data.toFaction);
            if (data.invasionMaterial)
            {
                DestroyImmediate(data.invasionMaterial);
            }

            previewInvasions.Remove(siteIndex);
        }

        public void ClearAllPreviews()
        {
            foreach (int siteIndex in new List<int>(previewInvasions.Keys))
            {
                ClearPreview(siteIndex);
            }
        }

        public void RefreshActiveVisuals()
        {
            foreach (InvasionData data in activeInvasions.Values)
            {
                ApplyMaterialParameters(data.invasionMaterial, data.fromFaction, data.toFaction, data.borderCenter, data.borderDirection, data.siteIndex);
            }

            foreach (InvasionData data in previewInvasions.Values)
            {
                float progress = data.invasionMaterial ? data.invasionMaterial.GetFloat("_InvasionProgress") : 0f;
                ApplyMaterialParameters(data.invasionMaterial, data.fromFaction, data.toFaction, data.borderCenter, data.borderDirection, data.siteIndex);
                if (data.invasionMaterial)
                {
                    data.invasionMaterial.SetFloat("_InvasionProgress", progress);
                }
            }
        }

        private IEnumerator InvasionCoroutine(InvasionData data)
        {
            float elapsed = 0f;

            while (elapsed < invasionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / invasionDuration);
                float progress = invasionCurve.Evaluate(t);

                data.invasionMaterial.SetFloat("_InvasionProgress", progress);

                yield return null;
            }

            // 动画完成，切换到最终材质
            FinishInvasion(data.siteIndex);
        }

        private void FinishInvasion(int siteIndex)
        {
            if (!activeInvasions.TryGetValue(siteIndex, out InvasionData data))
                return;

            // 切换到目标阵营的正常材质
            ApplyFinalMaterial(data.renderer, data.toFaction);

            // 清理
            if (data.invasionMaterial)
            {
                Destroy(data.invasionMaterial);
            }

            activeInvasions.Remove(siteIndex);

            // 移除动画等待标记
            areaView.RemoveTileWaitingAnimation(siteIndex);
        }

        private void StopInvasion(int siteIndex)
        {
            if (activeInvasions.TryGetValue(siteIndex, out InvasionData data))
            {
                if (data.coroutine != null)
                {
                    StopCoroutine(data.coroutine);
                }

                FinishInvasion(siteIndex);
            }
        }

        /// <summary>
        /// 计算两个相邻地块的共享边界
        /// </summary>
        private bool CalculateBorder(int siteIndex, int sourceSiteIndex, out Vector2 borderCenter, out Vector2 borderDirection)
        {
            borderCenter = Vector2.zero;
            borderDirection = Vector2.right;

            if (sourceSiteIndex >= 0 &&
                TryFindSharedBorder(sourceSiteIndex, siteIndex, out borderCenter, out borderDirection))
            {
                return true;
            }

            // 获取当前地块的顶点
            if (!areaView.TryGetSiteVertices(siteIndex, out List<Vector2> vertices))
                return false;

            if (!areaView.TryGetSiteCenter(siteIndex, out Vector3 center3D))
                return false;

            Vector2 center = new Vector2(center3D.x, center3D.z);

            // 简化方案：找到距离中心最远的边作为"入侵边界"
            // 更精确的方案需要找到相邻地块并计算共享边
            float maxDist = 0f;
            int edgeIndex = 0;

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 v1 = vertices[i];
                Vector2 v2 = vertices[(i + 1) % vertices.Count];
                Vector2 edgeCenter = (v1 + v2) * 0.5f;
                float dist = Vector2.Distance(center, edgeCenter);

                if (dist > maxDist)
                {
                    maxDist = dist;
                    edgeIndex = i;
                }
            }

            // 计算边界中心和方向
            Vector2 v1Edge = vertices[edgeIndex];
            Vector2 v2Edge = vertices[(edgeIndex + 1) % vertices.Count];
            borderCenter = (v1Edge + v2Edge) * 0.5f;
            borderDirection = (v2Edge - v1Edge).normalized;

            return true;
        }

        private bool TryFindSharedBorder(int sourceSiteIndex, int targetSiteIndex, out Vector2 borderCenter, out Vector2 borderDirection)
        {
            borderCenter = Vector2.zero;
            borderDirection = Vector2.right;

            if (!areaView.TryGetSiteVertices(sourceSiteIndex, out List<Vector2> sourceVertices) ||
                !areaView.TryGetSiteVertices(targetSiteIndex, out List<Vector2> targetVertices) ||
                !areaView.TryGetSiteCenter(targetSiteIndex, out Vector3 targetCenter3D))
            {
                return false;
            }

            const float epsilon = 0.02f;
            float bestLength = 0f;
            Vector2 bestA = Vector2.zero;
            Vector2 bestB = Vector2.zero;

            for (int i = 0; i < targetVertices.Count; i++)
            {
                Vector2 targetA = targetVertices[i];
                Vector2 targetB = targetVertices[(i + 1) % targetVertices.Count];

                for (int j = 0; j < sourceVertices.Count; j++)
                {
                    Vector2 sourceA = sourceVertices[j];
                    Vector2 sourceB = sourceVertices[(j + 1) % sourceVertices.Count];

                    bool sameDirection = Approximately(targetA, sourceA, epsilon) && Approximately(targetB, sourceB, epsilon);
                    bool oppositeDirection = Approximately(targetA, sourceB, epsilon) && Approximately(targetB, sourceA, epsilon);
                    if (!sameDirection && !oppositeDirection)
                    {
                        continue;
                    }

                    float length = Vector2.Distance(targetA, targetB);
                    if (length > bestLength)
                    {
                        bestLength = length;
                        bestA = targetA;
                        bestB = targetB;
                    }
                }
            }

            if (bestLength <= epsilon)
            {
                return false;
            }

            borderCenter = (bestA + bestB) * 0.5f;
            borderDirection = (bestB - bestA).normalized;

            Vector2 targetCenter = new Vector2(targetCenter3D.x, targetCenter3D.z);
            Vector2 inwardNormal = new Vector2(-borderDirection.y, borderDirection.x);
            if (Vector2.Dot(targetCenter - borderCenter, inwardNormal) < 0f)
            {
                borderDirection = -borderDirection;
            }

            return true;
        }

        private static bool Approximately(Vector2 a, Vector2 b, float epsilon)
        {
            return (a - b).sqrMagnitude <= epsilon * epsilon;
        }

        private Color GetFactionColor(int factionId)
        {
            Material mat = areaView.GetFactionMaterial(factionId);
            if (mat)
            {
                if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
                if (mat.HasProperty("_Color")) return mat.GetColor("_Color");
            }
            return factionId == 0 ? Color.red : Color.blue;
        }

        public void StopAllInvasions()
        {
            foreach (int siteIndex in new List<int>(activeInvasions.Keys))
            {
                StopInvasion(siteIndex);
            }

            ClearAllPreviews();
        }

        private void OnDestroy()
        {
            StopAllInvasions();
        }

        private InvasionData CreateInvasionData(int siteIndex, int fromFaction, int toFaction, MeshRenderer renderer, int sourceSiteIndex)
        {
            // 计算共享边界
            Vector2 borderCenter, borderDirection;
            if (!CalculateBorder(siteIndex, sourceSiteIndex, out borderCenter, out borderDirection))
            {
                Debug.LogWarning($"无法计算地块 {siteIndex} 的边界，跳过动画");
                return null;
            }

            Material invasionMat = new Material(invasionMaterialTemplate);
            ApplyMaterialParameters(invasionMat, fromFaction, toFaction, borderCenter, borderDirection, siteIndex);
            if (renderer)
            {
                renderer.sharedMaterial = invasionMat;
            }

            return new InvasionData
            {
                siteIndex = siteIndex,
                fromFaction = fromFaction,
                toFaction = toFaction,
                invasionMaterial = invasionMat,
                renderer = renderer,
                borderCenter = borderCenter,
                borderDirection = borderDirection
            };
        }

        private void ApplyMaterialParameters(Material invasionMat, int fromFaction, int toFaction, Vector2 borderCenter, Vector2 borderDirection, int siteIndex)
        {
            if (!invasionMat)
            {
                return;
            }

            Color oldColor = GetFactionColor(fromFaction);
            Color newColor = GetFactionColor(toFaction);

            float tileRadius = 3f;
            if (areaView.TryGetSiteRadius(siteIndex, out float r)) tileRadius = r;

            invasionMat.SetColor("_OldColor", oldColor);
            invasionMat.SetColor("_NewColor", newColor);
            invasionMat.SetVector("_BorderCenter", new Vector4(borderCenter.x, 0f, borderCenter.y, 0f));
            invasionMat.SetVector("_BorderDirection", new Vector4(borderDirection.x, 0f, borderDirection.y, 0f));
            invasionMat.SetFloat("_TileRadius", tileRadius);
            invasionMat.SetColor("_BorderGlowColor", borderGlowColor);
            invasionMat.SetFloat("_BorderWidth", borderWidth);
            invasionMat.SetFloat("_BorderIntensity", borderIntensity);
            invasionMat.SetFloat("_FlickerSpeed", flickerSpeed);
            invasionMat.SetFloat("_FlickerAmount", flickerAmount);
            invasionMat.SetFloat("_NoiseScale", noiseScale);
            invasionMat.SetFloat("_NoiseAmount", noiseAmount);
            invasionMat.SetFloat("_BattleBandWidth", battleBandWidth);
            invasionMat.SetFloat("_BattleDotDensity", battleDotDensity);
            invasionMat.SetFloat("_BattleDotSize", battleDotSize);
            invasionMat.SetFloat("_BattleIntensity", battleIntensity);
            invasionMat.SetFloat("_BattleFlowSpeed", battleFlowSpeed);
        }

        private void ApplyFinalMaterial(MeshRenderer renderer, int factionId)
        {
            Material finalMat = areaView.GetFactionMaterial(factionId);
            if (renderer && finalMat)
            {
                renderer.sharedMaterial = finalMat;
            }
        }
    }
}
