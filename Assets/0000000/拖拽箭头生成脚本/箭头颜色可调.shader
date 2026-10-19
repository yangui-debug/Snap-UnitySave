// ====================================================================================
//  箭头颜色可调.shader  （URP / Universal Render Pipeline 专用）
//
//  用途：
//   配合 CurvedArrowController（LineRenderer）画箭头用的材质 Shader。
//   效果：
//    1. “箭头颜色”参数可直接调色 —— 箭头线条显示为指定颜色（默认蓝色）；
//    2. 箭头贴图（箭头.png）中纯白色的背景会被自动裁剪为透明，
//       只保留箭头形状，不会再显示白底矩形；
//    3. 透明度参数可整体控制箭头的明隐。
//
//  兼容性说明（重要）：
//   · CurvedArrowController 脚本只操作 LineRenderer 的位置与宽度，
//     不读写任何材质属性，因此替换 Shader 不影响脚本画箭头。
//   · LineRenderer 组件上的“箭头材质”引用保持不变（guid 不变），
//     场景里 6 个箭头物体会自动切换到新效果。
//
//  注意：
//   · 属性显示名与注释均为中文；HLSL 代码内变量名使用英文，
//     以保证 D3D11 / OpenGL / 移动端各平台编译稳定。
// ====================================================================================

Shader "自定义/箭头颜色可调"
{
    Properties
    {
        // —— 材质参数（Inspector 中显示为中文）——
        // 箭头颜色：箭头线条显示的颜色，直接修改即可给箭头换色
        _ArrowColor ("箭头颜色", Color) = (0, 0.5, 1, 1)

        // 箭头贴图：用于显示箭头形状（箭头.png，白底部分会自动透明）
        _ArrowTex ("箭头贴图", 2D) = "white" {}

        // 背景裁剪阈值：用于区分“白色背景”和“箭头本身”
        // 亮度高于阈值的像素视为背景并透明；阈值越小裁剪越激进
        _BgCutoff ("背景裁剪阈值", Range(0, 1)) = 0.5

        // 透明度：0 = 完全透明（看不到），1 = 不透明
        _Alpha ("透明度", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            // 渲染队列：透明（LineRenderer 箭头是 UI 指示，最后绘制）
            "Queue" = "Transparent"
            // 渲染类型：透明
            "RenderType" = "Transparent"
            // 声明本 Shader 属于 URP 渲染管线
            "RenderPipeline" = "UniversalPipeline"
            // 忽略投影器
            "IgnoreProjector" = "True"
        }

        // =====================================================================
        // 主 Pass：输出“可调颜色箭头 + 白底透明”
        // =====================================================================
        Pass
        {
            Name "Forward"
            // 光照模式：URP 前向渲染主 Pass
            Tags { "LightMode" = "UniversalForward" }

            // 半透明混合
            Blend SrcAlpha OneMinusSrcAlpha
            // 关闭深度写入（半透明线条）
            ZWrite Off
            // 双面渲染：LineRenderer 线条无论从哪边看都能显示
            Cull Off

            HLSLPROGRAM
            // 顶点着色器入口
            #pragma vertex vert
            // 片元着色器入口
            #pragma fragment frag

            // URP 核心库：裁剪空间变换、贴图采样宏等
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // —— 材质参数缓存块（SRP Batcher 兼容）——
            CBUFFER_START(UnityPerMaterial)
                float4 _ArrowColor;    // 箭头颜色
                float4 _ArrowTex_ST;   // 贴图缩放/偏移（Tiling/Offset）
                float  _BgCutoff;      // 背景裁剪阈值
                float  _Alpha;         // 透明度
            CBUFFER_END

            // 声明箭头贴图及其采样器
            TEXTURE2D(_ArrowTex);
            SAMPLER(sampler_ArrowTex);

            // —— 顶点输入结构 ——
            struct Attributes
            {
                float4 positionOS : POSITION;   // 模型空间顶点位置
                float2 uv : TEXCOORD0;          // 贴图 UV（LineRenderer 沿线段 0~1）
            };

            // —— 顶点 → 片元 插值结构 ——
            struct Varyings
            {
                float4 positionCS : SV_POSITION;  // 裁剪空间位置
                float2 uv : TEXCOORD0;            // 贴图 UV
            };

            // -------------------------------------------------------------
            // 顶点着色器：把顶点变换到裁剪空间，并传递 UV
            // -------------------------------------------------------------
            Varyings vert(Attributes input)
            {
                Varyings output;
                // 模型空间 → 裁剪空间（URP 标准变换）
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // 应用 Tiling/Offset 后传 UV（显式写法，等价于 TRANSFORM_TEX 宏）
                output.uv = input.uv * _ArrowTex_ST.xy + _ArrowTex_ST.zw;
                return output;
            }

            // -------------------------------------------------------------
            // 片元着色器：箭头颜色可调 + 白色背景自动透明
            // -------------------------------------------------------------
            half4 frag(Varyings input) : SV_Target
            {
                // 采样箭头贴图
                float4 texColor = SAMPLE_TEXTURE2D(_ArrowTex, sampler_ArrowTex, input.uv);

                // 计算贴图亮度：纯白背景 ≈ 1.0，蓝色箭头 ≈ 0.1 ~ 0.5
                // （用加权灰度公式，白底与彩色箭头区分明显）
                float luminance = dot(texColor.rgb, float3(0.299, 0.587, 0.114));

                // 背景系数：1 = 白色背景，0 = 箭头本体；smoothstep 让边缘柔和过渡
                float bgFactor = smoothstep(_BgCutoff, _BgCutoff + 0.05, luminance);

                // 最终不透明度：背景区域透明，箭头区域显示，再乘整体透明度
                half alpha = (1.0 - bgFactor) * _Alpha;

                // 输出：箭头颜色由参数直接决定，可自由调色
                return half4(_ArrowColor.rgb, alpha);
            }
            ENDHLSL
        }
    }

    // 回退 Shader：平台不支持时退回 URP 自带无光照透明 Shader
    FallBack "Universal Render Pipeline/Unlit"
}
