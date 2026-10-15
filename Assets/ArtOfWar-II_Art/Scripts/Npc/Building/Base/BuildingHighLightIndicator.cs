using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Npc
{
    public class BuildingHighLightIndicator : MonoBehaviour
    {
        private static readonly int EmissiveColor = Shader.PropertyToID("_EmissiveColor");

        [SerializeField] private GameObject bubblePrefab;

        [FormerlySerializedAs("renderer"), SerializeField]
        private Renderer buildingRenderer;

        public float glowDuration = 0.5f; // 单次渐变持续时间
        public Color baseColor = Color.black; // 初始颜色（无发光时的颜色）
        public Color highlightColor = new(1, 1, 1, 0); // 发光的颜色
        public float maxIntensity = 1.0f; // 最大亮度强度

        public float duangScaleMultiplier = 1.2f; // 放大的比例
        public float duangDuration = 0.2f; // “duang”效果持续时间
        private TempBuildingBubble bubble;
        private Material buildingMaterial;
        private Coroutine glowCoroutine; // 保存当前的渐变协程
        private Coroutine glowCoroutine1; // 保存当前的渐变协程
        private Coroutine glowCoroutine2; // 保存当前的渐变协程
        private bool isGlowing;
        private Vector3 originalScale;

        // Start is called before the first frame update
        private void Start()
        {
            bubble = null;
            originalScale = transform.localScale; // 记录原始大小
        }

        private void OnDestroy()
        {
            if (buildingMaterial)
            {
                Destroy(buildingMaterial);
            }
        }

        public void ShowHighlight(Quaternion lookAtRot)
        {
            if (!buildingMaterial)
            {
                CreateNewMaterial(); // 第一次点击时创建新的材质实例
            }

            StartGlowEffect();
            ShowBubble(lookAtRot);
        }

        public void StopHighlight()
        {
            StopGlowEffect();
            CloseBubble();
        }

        // 创建新的材质实例
        private void CreateNewMaterial()
        {
            // 如果材质已经存在，就不再创建
            if (buildingMaterial)
            {
                return;
            }

            // 使用原材质的 Shader 和其他设置来创建新的材质
            buildingMaterial = new Material(buildingRenderer.sharedMaterial);
            buildingRenderer.material = buildingMaterial; // 替换当前的材质实例
            baseColor = buildingMaterial.GetColor(EmissiveColor); // 获取原始颜色
        }

        public void StartGlowEffect()
        {
            if (glowCoroutine == null && buildingMaterial)
            {
                isGlowing = true;
                buildingMaterial.EnableKeyword("_EMISSION");
                glowCoroutine = StartCoroutine(GlowLoop());

                // 放大然后缩小
                transform.DOScale(originalScale * duangScaleMultiplier, duangDuration / 2).SetEase(Ease.OutQuad) // 设置缓动效果
                    .OnComplete(() =>
                    {
                        // 缩回原始大小
                        transform.DOScale(originalScale, duangDuration / 2).SetEase(Ease.InQuad);
                    });
            }
        }

        public void StopGlowEffect()
        {
            if (glowCoroutine != null)
            {
                isGlowing = false;
                StopCoroutine(glowCoroutine);
                glowCoroutine = null;
                buildingMaterial.SetColor(EmissiveColor, baseColor); // 恢复到基础颜色
                buildingMaterial.DisableKeyword("_EMISSION");
            }
        }

        private void ShowBubble(Quaternion lookAtRot)
        {
            if (bubble != null) CloseBubble();

            bubble = Instantiate(bubblePrefab, transform).GetComponent<TempBuildingBubble>();
            bubble.transform.localPosition = Vector3.up * 5f;
            bubble.Init(lookAtRot);
            bubble.ShowBubble();
        }

        private void CloseBubble()
        {
            if (bubble == null) return;
            bubble.CloseBubble();
            bubble = null;
        }

        private IEnumerator GlowLoop()
        {
            while (true) // 无限循环渐变
            {
                yield return StartCoroutine(ChangeEmissionIntensity(0, maxIntensity)); // 逐渐变亮
                yield return StartCoroutine(ChangeEmissionIntensity(maxIntensity, 0)); // 逐渐变暗
            }
        }

        private IEnumerator ChangeEmissionIntensity(float fromIntensity, float toIntensity)
        {
            var elapsedTime = 0f;
            while (elapsedTime < glowDuration && isGlowing)
            {
                elapsedTime += Time.deltaTime;
                var t = Mathf.Clamp01(elapsedTime / glowDuration);

                // 插值计算当前强度
                var currentIntensity = Mathf.Lerp(fromIntensity, toIntensity, t);
                var emissiveColor = baseColor + highlightColor * currentIntensity; // 基础颜色乘以强度
                buildingMaterial.SetColor(EmissiveColor, emissiveColor); // 设置颜色到材质属性
                yield return null;
            }
        }
    }
}