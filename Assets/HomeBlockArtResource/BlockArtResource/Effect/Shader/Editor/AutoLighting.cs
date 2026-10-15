using System.Collections;
using UnityEngine;

namespace BuildingAssets._02.Scripts.Core_Camp
{
    public class AutoLighting : MonoBehaviour
    {
        /// <summary>
        /// 闪烁颜色
        /// </summary>
        private readonly Color color = Color.white;

        /// <summary>
        /// 最低发光亮度,需小于最高发光亮度。
        /// </summary>
        private readonly float minBrightness = -0.3f;

        /// <summary>
        /// 最高发光亮度，需大于最低发光亮度。
        /// </summary>
        private readonly float maxBrightness = 0.2f;

        /// <summary>
        /// 闪烁频率
        /// </summary>
        private readonly float rate = 1.5f;

        private float _deltaBrightness; // 最低最高亮度差
        private Material material;
        private readonly string _keyword = "_EMISSION";
        private readonly string _colorName = "_EmissionColor";
        private readonly string mainColor = "_Color";

        private Coroutine _glinting;
        private bool startLighting;

        public void LightingStatus(bool openStatus, Material material)
        {
            this.material = material;
            //如果打开
            if (openStatus && gameObject.activeSelf)
            {
                StartGlinting();
            }
            //关闭
            else
            {
                StopGlinting();
            }
        }

        private void OnDestroy()
        {
            StopGlinting();
        }

        /// <summary>
        /// 开始闪烁。
        /// </summary>
        private void StartGlinting()
        {
            material.EnableKeyword(_keyword);
            startLighting = true;
            if (_glinting != null)
            {
                StopCoroutine(_glinting);
            }

            _glinting = StartCoroutine(IEGlinting());
        }

        /// <summary>
        /// 停止闪烁。
        /// </summary>
        private void StopGlinting()
        {
            material.SetColor(_colorName, Color.black);
            material.SetColor(mainColor, Color.white);

            material.DisableKeyword(_keyword);
            startLighting = false;

            if (_glinting != null)
            {
                StopCoroutine(_glinting);
            }
        }

        /// <summary>
        /// 控制自发光强度。
        /// </summary>
        /// <returns></returns>
        private IEnumerator IEGlinting()
        {
            float _h, _s, _v; // 色调，饱和度，亮度
            Color.RGBToHSV(color, out _h, out _s, out _v);
            _v = minBrightness;
            _deltaBrightness = maxBrightness - minBrightness;

            bool increase = true;
            while (startLighting)
            {
                if (increase)
                {
                    _v += _deltaBrightness * Time.deltaTime * rate;
                    increase = _v <= maxBrightness;
                }
                else
                {
                    _v -= _deltaBrightness * Time.deltaTime * rate;
                    increase = _v <= minBrightness;
                }

                material.SetColor(_colorName, Color.HSVToRGB(_h, _s, _v));
                yield return null;
            }
        }
    }
}