using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

//using EffectVAT;

namespace EffectVATGUI
{
    public class EffectUniversalShaderGUI : ShaderGUI
    {
        #region [所有用到的成员变量]

        public enum BlendMode
        {
            Additive,
            AlphaBlend
        }

        public enum ZTestMode
        {
            LessEqual,
            Always
        }

        public enum CullMode
        {
            Off,
            Front,
            Back
        }

        public enum WrapMode
        {
            Repeat,
            Clamp,
            ClampX,
            ClampY
        }

        public enum MainChannelData
        {
            R,
            G,
            B,
            A
        }

        public enum TypeMode
        {
            TypeA,
            TypeB,
            Off
        }

        public enum EnabledOffOn
        {
            Off,
            On
        }

        public enum RampUVChooseMode
        {
            MaskTex,
            FinalAlpha
        }

        //所有声明的变量
        MaterialEditor m_materialEditor;
        MaterialProperty blendModeProp;
        MaterialProperty srcFactorProp;
        MaterialProperty dstFactorProp;

        MaterialProperty zTestModeProp;
        MaterialProperty zTestProp;

        MaterialProperty cullModeProp;
        MaterialProperty cullProp;
        MaterialProperty customDataEnabledProp, customDataEnabledModeProp;
        MaterialProperty OffsetProp;

        MaterialProperty finalColorProp;
        MaterialProperty finalColorIntensityProp, finalAlphaAProp, finalAlphaBProp;

        MaterialProperty rampUVChooseProp, rampUVPowProp, rampUVAffectedEdgeGlowProp;
        MaterialProperty rampUVChooseModeProp, rampUVAffectedEdgeGlowModeProp;
        MaterialProperty rampTexProp;
        MaterialProperty rampEnabledProp;

        MaterialProperty mainWrapModeProp;
        MaterialProperty mainWrapProp, mainWrapXProp, mainWrapYProp;
        MaterialProperty mainTexProp;
        MaterialProperty mainChannelDataProp;
        MaterialProperty mainChannelVectorProp;
        MaterialProperty mainOffsetUProp, mainOffsetVProp;

        MaterialProperty maskEnabledProp;
        MaterialProperty maskTexProp;
        MaterialProperty maskChannelVectorProp, maskChannelDataProp;
        MaterialProperty maskWrapProp, maskWrapXProp, maskWrapYProp, maskWrapModeProp;
        MaterialProperty maskAlphaAProp, maskAlphaBProp;
        MaterialProperty maskOffsetUProp, maskOffsetVProp, maskRandomUProp, maskRandomVProp;

        MaterialProperty dissolveEnabledProp;
        MaterialProperty dissolveTexProp;
        MaterialProperty dissolveChannelVectorProp, dissolveChannelDataProp;
        MaterialProperty dissIsFilpProp, dissIsFilpModeProp;
        MaterialProperty dissolveProp, dissEdgeProp;
        MaterialProperty dissOffsetUProp, dissOffsetVProp;
        MaterialProperty dissEdgeGlowAProp, dissEdgeGlowBProp, dissEdgeGlowCProp;
        MaterialProperty dissRandomUProp, dissRandomVProp;

        MaterialProperty distortEnabledProp;
        MaterialProperty distortTexProp;
        MaterialProperty distChannelDataProp, distChannelVectorProp;
        MaterialProperty distPowerProp, distPowerXProp, distPowerYProp;
        MaterialProperty distOffsetUProp, distOffsetVProp;
        MaterialProperty distPolarEnabledProp;
        MaterialProperty distPolarTilingXProp, distPolarTilingYProp, distPolarOffsetXProp, distPolarOffsetYProp;
        MaterialProperty distPolarSpeedXProp, distPolarSpeedYProp;
        MaterialProperty distPolarCenterXProp, distPolarCenterYProp;
        MaterialProperty distPolarStylesProp, distPolarStylesModeProp, isDistPolarUVProp;
        MaterialProperty distSpheticalStylesProp, distSpheticalStylesModeProp, isDistSpheticalProp;

        MaterialProperty fresnelEnabledProp, fresnelColorProp, fresnelIntensityProp, fresnelSmoonthProp, fresnelIsFilpProp;

        #endregion

        #region [所有寻找到的属性]

        public void FindProperties(MaterialProperty[] properties)
        {
            blendModeProp = FindProperty("__BlendMode", properties);
            srcFactorProp = FindProperty("_SrcFactor", properties);
            dstFactorProp = FindProperty("_DstFactor", properties);

            zTestModeProp = FindProperty("__ZTestMode", properties);
            zTestProp = FindProperty("_ZTest", properties);

            cullModeProp = FindProperty("__CullMode", properties);
            cullProp = FindProperty("_Cull", properties);

            customDataEnabledModeProp = FindProperty("__CustomDataEnabledMode", properties);
            customDataEnabledProp = FindProperty("_CustomDataEnabled", properties);

            OffsetProp = FindProperty("_Offset", properties);

            finalColorProp = FindProperty("_FinalColor", properties);
            finalColorIntensityProp = FindProperty("_FinalColorIntensity", properties);
            finalAlphaAProp = FindProperty("_FinalAlphaA", properties);
            finalAlphaBProp = FindProperty("_FinalAlphaB", properties);

            rampUVChooseProp = FindProperty("_RampUVChoose", properties);
            rampUVPowProp = FindProperty("_RampUVPow", properties);
            rampUVAffectedEdgeGlowProp = FindProperty("_RampUVAffectedEdgeGlow", properties);
            rampUVChooseModeProp = FindProperty("__RampUVChooseMode", properties);
            rampUVAffectedEdgeGlowModeProp = FindProperty("__RampUVAffectedEdgeGlowMode", properties);
            rampTexProp = FindProperty("_RampTex", properties);
            rampEnabledProp = FindProperty("_RampEnabled", properties);

            mainWrapModeProp = FindProperty("__MainWrapMode", properties);
            mainWrapProp = FindProperty("_MainWrap", properties);
            mainWrapXProp = FindProperty("_MainWrapX", properties);
            mainWrapYProp = FindProperty("_MainWrapY", properties);
            mainTexProp = FindProperty("_MainTex", properties);
            mainChannelDataProp = FindProperty("__MainChannelData", properties);
            mainChannelVectorProp = FindProperty("_MainChannelVector", properties);
            mainOffsetUProp = FindProperty("_MainOffestU", properties);
            mainOffsetVProp = FindProperty("_MainOffestV", properties);

            maskEnabledProp = FindProperty("_MaskEnabled", properties);
            maskTexProp = FindProperty("_MaskTex", properties);
            maskChannelVectorProp = FindProperty("_MaskChannelVector", properties);
            maskChannelDataProp = FindProperty("__MaskChannelData", properties);
            maskWrapProp = FindProperty("_MaskWrap", properties);
            maskWrapXProp = FindProperty("_MaskWrapX", properties);
            maskWrapYProp = FindProperty("_MaskWrapY", properties);
            maskWrapModeProp = FindProperty("__MaskWrapMode", properties);
            maskAlphaAProp = FindProperty("_MaskAlphaA", properties);
            maskAlphaBProp = FindProperty("_MaskAlphaB", properties);
            maskOffsetUProp = FindProperty("_MaskOffsetU", properties);
            maskOffsetVProp = FindProperty("_MaskOffsetV", properties);
            maskRandomUProp = FindProperty("_MaskRandomU", properties);
            maskRandomVProp = FindProperty("_MaskRandomV", properties);

            dissolveEnabledProp = FindProperty("_DissolveEnabled", properties);
            dissolveTexProp = FindProperty("_DissolveTex", properties);
            dissolveChannelVectorProp = FindProperty("_DissolveChannelVector", properties);
            dissolveChannelDataProp = FindProperty("__DissolveChannelData", properties);
            dissIsFilpProp = FindProperty("_DissIsFilp", properties);
            dissIsFilpModeProp = FindProperty("__DissIsFilpMode", properties);
            dissolveProp = FindProperty("_Dissolve", properties);
            dissEdgeProp = FindProperty("_DissEdge", properties);
            dissOffsetUProp = FindProperty("_DissOffsetU", properties);
            dissOffsetVProp = FindProperty("_DissOffsetV", properties);
            dissEdgeGlowAProp = FindProperty("_DissEdgeGlowA", properties);
            dissEdgeGlowBProp = FindProperty("_DissEdgeGlowB", properties);
            dissEdgeGlowCProp = FindProperty("_DissEdgeGlowC", properties);

            dissRandomUProp = FindProperty("_DissRandomU", properties);
            dissRandomVProp = FindProperty("_DissRandomV", properties);

            distortEnabledProp = FindProperty("_DistortEnabled", properties);
            distortTexProp = FindProperty("_DistortTex", properties);
            distChannelDataProp = FindProperty("__DistChannelData", properties);
            distChannelVectorProp = FindProperty("_DistChannelVector", properties);
            distPowerProp = FindProperty("_DistPower", properties);
            distPowerXProp = FindProperty("_DistPowerX", properties);
            distPowerYProp = FindProperty("_DistPowerY", properties);
            distOffsetUProp = FindProperty("_DistOffsetU", properties);
            distOffsetVProp = FindProperty("_DistOffsetV", properties);
            distPolarEnabledProp = FindProperty("_DistPolarEnabled", properties);
            distPolarTilingXProp = FindProperty("_DistPolarTilingX", properties);
            distPolarTilingYProp = FindProperty("_DistPolarTilingY", properties);
            distPolarOffsetXProp = FindProperty("_DistPolarOffsetX", properties);
            distPolarOffsetYProp = FindProperty("_DistPolarOffsetY", properties);
            distPolarSpeedXProp = FindProperty("_DistPolarSpeedX", properties);
            distPolarSpeedYProp = FindProperty("_DistPolarSpeedY", properties);
            distPolarCenterXProp = FindProperty("_DistPolarCenterX", properties);
            distPolarCenterYProp = FindProperty("_DistPolarCenterY", properties);
            distPolarStylesProp = FindProperty("_DistPolarStyles", properties);
            distPolarStylesModeProp = FindProperty("__DistPolarStylesMode", properties);
            isDistPolarUVProp = FindProperty("_IsDistPolarUV", properties);
            distSpheticalStylesProp = FindProperty("_DistSpheticalStyles", properties);
            distSpheticalStylesModeProp = FindProperty("__DistSpheticalStylesMode", properties);
            isDistSpheticalProp = FindProperty("_IsDistSphetical", properties);

            fresnelEnabledProp = FindProperty("_FresnelEnabled", properties);
            fresnelColorProp = FindProperty("_FresnelColor", properties);
            fresnelIntensityProp = FindProperty("_FresnelIntensity", properties);
            fresnelSmoonthProp = FindProperty("_FresnelSmoonth", properties);
            fresnelIsFilpProp = FindProperty("_FresnelIsFilp", properties);
        }

        #endregion

        #region [文本显示与提示文字]

        //材质球属性
        public static class VATGUIStyles
        {
            public static string BlendModeText = "BlendMode";
            public static readonly string[] BlendModeNames = Enum.GetNames(typeof(BlendMode));

            public static string ZTestmodeText = "ZTestMode";
            public static readonly string[] ZTestModeNames = Enum.GetNames(typeof(ZTestMode));

            public static string CullModeText = "CullMode";
            public static readonly string[] CullModeNames = Enum.GetNames(typeof(CullMode));

            public static string CustomDataEnabledText = "CustomData";

            public static GUIContent CustomDataEnabledText1 = EditorGUIUtility.TrTextContent("CustomData",
                "CustomData.xy=MainTexOffsetXY");

            public static string OffsetText = "Offset";

            public static string FinalColorText = "FinalColor";
            public static string FinalColorIntensityText = "FinalColorIntensity";
            public static string[] FinalAlphaText = { "FinalAlphaA", "FinalAlphaB" };

            public static string[] RampUVDataText =
            {
                "RampUVChoose", "RampUVPow", "RampUVIsAffectedByDissolveEdgeGlow",
                "RampTex", "RampEnabled"
            };

            public static readonly string[] RampUVChooseNames = Enum.GetNames(typeof(RampUVChooseMode));

            public static string WrapModeText = "WrapMode";
            public static readonly string[] WrapModeNames = Enum.GetNames(typeof(WrapMode));
            public static string MainTexText = "MainTex";
            public static string MainChannelText = "MainChannel";
            public static readonly string[] MainChannelDataNames = Enum.GetNames(typeof(MainChannelData));
            public static string MainOffsetUText = "MainOffsetSpeedX";
            public static string MainOffsetVText = "MainOffsetSpeedY";

            public static string MaskEnabledText = "MaskEnabled";

            public static string[] MaskDataText =
                { "MaskTex", "MaskChannel", "MaskAlphaA", "MaskAlphaB", "MaskOffsetSpeedX", "MaskOffsetSpeedY", "MaskRandomU", "MaskRandomV" };

            public static string[] DissolveDataText =
            {
                "DissolveEnabled", "DissolveTex", "DissolveChannel",
                "IsFilp", "Dissolve", "DissolveEdge", "DissolveOffsetSpeedX", "DissolveOffsetSpeedY"
            };

            public static readonly string[] IsFilpNames = Enum.GetNames(typeof(EnabledOffOn));
            public static string[] DissolveEdgeGlowText = { "DissolveEdgeGlowA", "DissolveEdgeGlowB", "DissolveEdgeGlowC" };
            public static string[] DissRandomUVText = { "DissRandomU", "DissRandomV" };
            public static string DistortEnabledText = "DistortEnabled";
            public static string DistortTexText = "DistortTex";
            public static string DistChannelText = "DistChannel";
            public static string DistPowerText = "DistPower";
            public static string DistPowerXText = "DistPowerX";
            public static string DistPowerYText = "DistPowerY";
            public static string DistOffsetUText = "DistOffsetSpeedX";
            public static string DistOffsetVText = "DistOffsetSpeedY";
            public static string DistPolarEnabledText = "DistPolarEnabled";
            public static string[] DistPolarTilingOffsetText = { "DistPolarTilingX", "DistPolarTilingY", "DistPolarOffsetX", "DistPolarOffsetY" };
            public static string[] DistPolarSpeedText = { "DistPolarOffsetSpeedX", "DistPolarOffsetSpeedY" };
            public static string[] DistPolarCenterText = { "DistPolarCenterX", "DistPolarCenterY" };
            public static string DistPolarStylesText = "DistPolarStyles";
            public static string DistSpheticalStylesText = "DistSpheticalStyles";
            public static readonly string[] TypeModeNames = Enum.GetNames(typeof(TypeMode));

            public static string[] FresnelText = { "FresenlEnabled", "Color", "Intensity", "Smoonth", "IsFilp" };
        }

        //界面显示标题等
        public static class VATEditorGUIStyles
        {
            public static GUIStyle EditorStylesTitle = new GUIStyle("PreLabel");
            public static GUIStyle EditorstylesColorSquare = new GUIStyle("flow node 0");
            public static GUIStyle EditorstylesPopup = new GUIStyle("MiniPopup");
            public static string EditorGlobalText = "Global Settings";
            public static string EditorFinalColorText = "Final Color";
            public static string EditorRampColorText = "Ramp Color";
            public static string EditorMainText = "Main";
            public static string EditorMainOffsetText = "Main Offset Speed";
            public static string EditorMaskText = "Mask";
            public static string EditorDissolveText = "Dissolve";
            public static string EditorDistText = "Distort";
            public static string EditorDistPolarText = "Distort Polar";
            public static string[] EditorDistPolarDataText = { "Distort Polar TilingOffset", "Distort Polar Offset Speed", "Distort Polar Center" };
            public static string EditorFresnelText = "Fresnel";
        }

        #endregion

        #region [最终绘制方法]

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            //base.OnGUI(materialEditor, properties);
            m_materialEditor = materialEditor;

            //贴图框设置成矩形
            m_materialEditor.SetDefaultGUIWidths();

            //查找到的所有参数
            FindProperties(properties);

            //Global
            EditorGUILayout.BeginVertical();
            {
                EditorDrawGlobalText();
                DrawBlendMode();
                DrawZTestMode();
                DrawCullMode();
                DrawCustomDataEnabled();

                //DrawOffset();
            }
            EditorGUILayout.EndVertical();

            // EditorGUILayout.Space(3);

            EditorGUILayout.BeginVertical(new GUIStyle("flow node 0"));
            {
                EditorDrawFinalColorText();
                DrawFinalColor();
                DrawFinalColorIntensity();
                DrawFinalAlpha();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);

            //RampUV
            EditorGUILayout.BeginVertical(new GUIStyle("flow node 0"));
            {
                EditorRampColorText();
                DrawRampEnabled();
                DrawRampTex();
                DrawRampUVChooseMode();
                DrawRampUVPow();
                DrawRampUVIsAffectedByDissolveEdgeGlowMode();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);

            //Main
            EditorGUILayout.BeginVertical(VATEditorGUIStyles.EditorstylesColorSquare);
            {
                EditorDrawMainText();
                DrawMainTex();
                DrawMainChannel();
                DrawMainWrapMode();
                DrawMainOffsetSpeed();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);

            //Mask
            EditorGUILayout.BeginVertical(VATEditorGUIStyles.EditorstylesColorSquare);
            {
                EditorDrawMaskText();
                DrawMaskEnabled();
                DrawMaskTex();
                DrawMaskChannel();
                DrawMaskWrapMode();
                DrawMaskAlpha();
                DrawMaskOffsetSpeed();
                DrawMaskRnadomUV();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);

            //Dissolve
            EditorGUILayout.BeginVertical(new GUIStyle("flow node 0"));
            {
                EditorDrawDissolveText();
                DrawDissolveEnabled();
                DrawDissolveTex();
                DrawDissolveChannel();
                DrawDissolveIsFilpMode();
                DrawDissolve();
                DrawDissolveEdge();
                EditorGUILayout.Space(2);
                DrawDissolveOffsetSpeed();
                EditorGUILayout.Space(2);
                DrawDissolveEdgeGlow();
                EditorGUILayout.Space(2);
                DrawRandomUV();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);

            //Distort
            EditorGUILayout.BeginVertical(new GUIStyle("flow node 0"));
            {
                EditorDrawDistortText();
                DrawDistortEnabled();
                DrawDistortTex();
                DrawDistChannel();
                DrawDistPower();

                // DrawDistPowerX();
                // DrawDistPowerY();
                DrawDistOffsetSpeed();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);

            //DistortPolar
            EditorGUILayout.BeginVertical(new GUIStyle("flow node 0"));
            {
                EditorDrawDistPolarText();
                DrawDistPolarEnabled();

                if (distPolarEnabledProp.floatValue == 1)
                {
                    DrawDistPolarStyles();
                    DrawDistSpheticalStyles();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorDrawDistPolarTilingOffsetText();
                        DrawDistPolarTilingOffset();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorDrawDistPolarOffsetSpeedText();
                        DrawDistPolarOffsetSpeed();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorDrawPolarCenterText();
                        DrawDistPolarCenter();
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(3);
            EditorGUILayout.BeginVertical(new GUIStyle("flow node 0"));
            {
                EditorDrawFresnelText();
                DrawFresnelEnabled();
                DrawFresnelColor();
                DrawFresnelIntensity();
                DrawFresnelSmoonth();
                DrawFresnelIsFilp();
            }
            EditorGUILayout.EndVertical();

            DrawQueueControl();
        }

        #endregion

        #region [具体绘制方法(材质球属性)]

        void DrawBlendMode()
        {
            EditorGUI.showMixedValue = blendModeProp.hasMixedValue;

            //把上次修改后的属性值翻译成枚举值
            var blendMode = (BlendMode) blendModeProp.floatValue;
            EditorGUI.BeginChangeCheck();
            blendMode = (BlendMode) EditorGUILayout.Popup(VATGUIStyles.BlendModeText,
                (int) blendMode, VATGUIStyles.BlendModeNames, VATEditorGUIStyles.EditorstylesPopup);
            if (EditorGUI.EndChangeCheck())
            {
                blendModeProp.floatValue = (float) blendMode;
                switch (blendMode)
                {
                    case BlendMode.Additive:
                        srcFactorProp.floatValue = (int) UnityEngine.Rendering.BlendMode.SrcAlpha;
                        dstFactorProp.floatValue = (int) UnityEngine.Rendering.BlendMode.One;
                        break;
                    case BlendMode.AlphaBlend:
                        srcFactorProp.floatValue = (int) UnityEngine.Rendering.BlendMode.SrcAlpha;
                        dstFactorProp.floatValue = (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                        break;
                }
            }
        }

        void DrawZTestMode()
        {
            EditorGUI.showMixedValue = zTestModeProp.hasMixedValue;
            var zTestMode = (ZTestMode) zTestModeProp.floatValue;
            EditorGUI.BeginChangeCheck();
            zTestMode = (ZTestMode) EditorGUILayout.Popup(VATGUIStyles.ZTestmodeText,
                (int) zTestMode, VATGUIStyles.ZTestModeNames, VATEditorGUIStyles.EditorstylesPopup);
            if (EditorGUI.EndChangeCheck())
            {
                zTestModeProp.floatValue = (float) zTestMode;
                switch (zTestMode)
                {
                    case ZTestMode.LessEqual:
                        zTestProp.floatValue = (float) UnityEngine.Rendering.CompareFunction.LessEqual;
                        break;
                    case ZTestMode.Always:
                        zTestProp.floatValue = (float) UnityEngine.Rendering.CompareFunction.Always;
                        break;
                }
            }
        }

        void DrawCullMode()
        {
            EditorGUI.showMixedValue = cullModeProp.hasMixedValue;
            var cullMode = (CullMode) cullModeProp.floatValue;
            EditorGUI.BeginChangeCheck();
            cullMode = (CullMode) EditorGUILayout.Popup(VATGUIStyles.CullModeText,
                (int) cullMode, VATGUIStyles.CullModeNames, VATEditorGUIStyles.EditorstylesPopup);
            if (EditorGUI.EndChangeCheck())
            {
                cullModeProp.floatValue = (float) cullMode;
                switch (cullMode)
                {
                    case CullMode.Back:
                        cullProp.floatValue = (float) UnityEngine.Rendering.CullMode.Back;
                        break;
                    case CullMode.Front:
                        cullProp.floatValue = (float) UnityEngine.Rendering.CullMode.Front;
                        break;
                    case CullMode.Off:
                        cullProp.floatValue = (float) UnityEngine.Rendering.CullMode.Off;
                        break;
                }
            }
        }

        void DrawCustomDataEnabled()
        {
            EditorGUI.showMixedValue = customDataEnabledModeProp.hasMixedValue;
            var isAffected = (EnabledOffOn) customDataEnabledModeProp.floatValue;
            EditorGUI.BeginChangeCheck();
            isAffected = (EnabledOffOn) EditorGUILayout.Popup(VATGUIStyles.CustomDataEnabledText,
                (int) isAffected, VATGUIStyles.IsFilpNames, VATEditorGUIStyles.EditorstylesPopup);
            if (EditorGUI.EndChangeCheck())
            {
                customDataEnabledModeProp.floatValue = (float) isAffected;
                switch (isAffected)
                {
                    case EnabledOffOn.Off:
                        customDataEnabledProp.floatValue = 0;
                        break;
                    case EnabledOffOn.On:
                        customDataEnabledProp.floatValue = 1;
                        break;
                }
            }
        }

        void DrawOffset()
        {
            m_materialEditor.FloatProperty(OffsetProp, VATGUIStyles.OffsetText);
        }

        void DrawFinalColor()
        {
            m_materialEditor.ColorProperty(finalColorProp, VATGUIStyles.FinalColorText);
        }

        void DrawFinalColorIntensity()
        {
            m_materialEditor.FloatProperty(finalColorIntensityProp, VATGUIStyles.FinalColorIntensityText);
        }

        void DrawFinalAlpha()
        {
            m_materialEditor.FloatProperty(finalAlphaAProp, VATGUIStyles.FinalAlphaText[0]);
            m_materialEditor.FloatProperty(finalAlphaBProp, VATGUIStyles.FinalAlphaText[1]);
        }

        void DrawRampEnabled()
        {
            m_materialEditor.ShaderProperty(rampEnabledProp, VATGUIStyles.RampUVDataText[4]);
        }

        void DrawRampUVChooseMode()
        {
            EditorGUI.showMixedValue = rampUVChooseModeProp.hasMixedValue;
            var rampUV = (RampUVChooseMode) rampUVChooseModeProp.floatValue;
            EditorGUI.BeginChangeCheck();
            rampUV = (RampUVChooseMode) EditorGUILayout.Popup(VATGUIStyles.RampUVDataText[0],
                (int) rampUV, VATGUIStyles.RampUVChooseNames, VATEditorGUIStyles.EditorstylesPopup);
            if (EditorGUI.EndChangeCheck())
            {
                rampUVChooseModeProp.floatValue = (float) rampUV;
                switch (rampUV)
                {
                    case RampUVChooseMode.MaskTex:
                        rampUVChooseProp.floatValue = 0;
                        break;
                    case RampUVChooseMode.FinalAlpha:
                        rampUVChooseProp.floatValue = 1;
                        break;
                }
            }
        }

        void DrawRampUVPow()
        {
            m_materialEditor.FloatProperty(rampUVPowProp, VATGUIStyles.RampUVDataText[1]);
        }

        void DrawRampUVIsAffectedByDissolveEdgeGlowMode()
        {
            EditorGUI.showMixedValue = rampUVAffectedEdgeGlowModeProp.hasMixedValue;
            var isAffected = (EnabledOffOn) rampUVAffectedEdgeGlowModeProp.floatValue;
            EditorGUI.BeginChangeCheck();
            isAffected = (EnabledOffOn) EditorGUILayout.Popup(VATGUIStyles.RampUVDataText[2],
                (int) isAffected, VATGUIStyles.IsFilpNames, VATEditorGUIStyles.EditorstylesPopup);
            if (EditorGUI.EndChangeCheck())
            {
                rampUVAffectedEdgeGlowModeProp.floatValue = (float) isAffected;
                switch (isAffected)
                {
                    case EnabledOffOn.Off:
                        rampUVAffectedEdgeGlowProp.floatValue = 0;
                        break;
                    case EnabledOffOn.On:
                        rampUVAffectedEdgeGlowProp.floatValue = 1;
                        break;
                }
            }
        }

        void DrawRampTex()
        {
            m_materialEditor.TextureProperty(rampTexProp, VATGUIStyles.RampUVDataText[3]);
        }

        void DrawMainWrapMode()
        {
            EditorGUI.showMixedValue = mainWrapModeProp.hasMixedValue;
            var mainWrapMode = (WrapMode) mainWrapModeProp.floatValue;
            EditorGUI.BeginChangeCheck();
            mainWrapMode = (WrapMode) EditorGUILayout.Popup(VATGUIStyles.WrapModeText,
                (int) mainWrapMode, VATGUIStyles.WrapModeNames, VATEditorGUIStyles.EditorstylesPopup);
            if (EditorGUI.EndChangeCheck())
            {
                mainWrapModeProp.floatValue = (float) mainWrapMode;
                switch (mainWrapMode)
                {
                    case WrapMode.Repeat:
                        mainWrapProp.floatValue = 0;
                        mainWrapXProp.floatValue = 0;
                        mainWrapYProp.floatValue = 0;
                        break;
                    case WrapMode.Clamp:
                        mainWrapProp.floatValue = 1;
                        mainWrapXProp.floatValue = 0;
                        mainWrapYProp.floatValue = 0;
                        break;
                    case WrapMode.ClampX:
                        mainWrapProp.floatValue = 0;
                        mainWrapXProp.floatValue = 1;
                        mainWrapYProp.floatValue = 0;
                        break;
                    case WrapMode.ClampY:
                        mainWrapProp.floatValue = 0;
                        mainWrapXProp.floatValue = 0;
                        mainWrapYProp.floatValue = 1;
                        break;
                }
            }
        }

        void DrawMainTex()
        {
            m_materialEditor.TextureProperty(mainTexProp, VATGUIStyles.MainTexText);
        }

        void DrawMainChannel()
        {
            EditorGUI.showMixedValue = mainChannelDataProp.hasMixedValue;
            var mainChannelData = (MainChannelData) mainChannelDataProp.floatValue;
            EditorGUI.BeginChangeCheck();
            mainChannelData = (MainChannelData) EditorGUILayout.Popup(VATGUIStyles.MainChannelText,
                (int) mainChannelData, VATGUIStyles.MainChannelDataNames, VATEditorGUIStyles.EditorstylesPopup);
            if (EditorGUI.EndChangeCheck())
            {
                mainChannelDataProp.floatValue = (float) mainChannelData;
                switch (mainChannelData)
                {
                    case MainChannelData.R:
                        mainChannelVectorProp.vectorValue = new Vector4(1, 0, 0, 0);
                        break;
                    case MainChannelData.G:
                        mainChannelVectorProp.vectorValue = new Vector4(0, 1, 0, 0);
                        break;
                    case MainChannelData.B:
                        mainChannelVectorProp.vectorValue = new Vector4(0, 0, 1, 0);
                        break;
                    case MainChannelData.A:
                        mainChannelVectorProp.vectorValue = new Vector4(0, 0, 0, 1);
                        break;
                }
            }
        }

        void DrawMainOffsetSpeed()
        {
            m_materialEditor.FloatProperty(mainOffsetUProp, VATGUIStyles.MainOffsetUText);
            m_materialEditor.FloatProperty(mainOffsetVProp, VATGUIStyles.MainOffsetVText);
        }

        void DrawMaskEnabled()
        {
            m_materialEditor.ShaderProperty(maskEnabledProp, VATGUIStyles.MaskEnabledText);
        }

        void DrawMaskTex()
        {
            m_materialEditor.TextureProperty(maskTexProp, VATGUIStyles.MaskDataText[0]);
        }

        void DrawMaskChannel()
        {
            DrawChannelFunction(maskChannelDataProp, maskChannelVectorProp, VATGUIStyles.MaskDataText[1]);
        }

        void DrawMaskWrapMode()
        {
            EditorGUI.showMixedValue = maskWrapModeProp.hasMixedValue;
            var mainWrapMode = (WrapMode) maskWrapModeProp.floatValue;
            EditorGUI.BeginChangeCheck();
            mainWrapMode = (WrapMode) EditorGUILayout.Popup(VATGUIStyles.WrapModeText,
                (int) mainWrapMode, VATGUIStyles.WrapModeNames, VATEditorGUIStyles.EditorstylesPopup);
            if (EditorGUI.EndChangeCheck())
            {
                maskWrapModeProp.floatValue = (float) mainWrapMode;
                switch (mainWrapMode)
                {
                    case WrapMode.Repeat:
                        maskWrapProp.floatValue = 0;
                        maskWrapXProp.floatValue = 0;
                        maskWrapYProp.floatValue = 0;
                        break;
                    case WrapMode.Clamp:
                        maskWrapProp.floatValue = 1;
                        maskWrapXProp.floatValue = 1;
                        maskWrapYProp.floatValue = 1;
                        break;
                    case WrapMode.ClampX:
                        maskWrapProp.floatValue = 0;
                        maskWrapXProp.floatValue = 1;
                        maskWrapYProp.floatValue = 0;
                        break;
                    case WrapMode.ClampY:
                        maskWrapProp.floatValue = 0;
                        maskWrapXProp.floatValue = 0;
                        maskWrapYProp.floatValue = 1;
                        break;
                }
            }
        }

        void DrawMaskAlpha()
        {
            m_materialEditor.FloatProperty(maskAlphaAProp, VATGUIStyles.MaskDataText[2]);
            m_materialEditor.FloatProperty(maskAlphaBProp, VATGUIStyles.MaskDataText[3]);
        }

        void DrawMaskOffsetSpeed()
        {
            m_materialEditor.FloatProperty(maskOffsetUProp, VATGUIStyles.MaskDataText[4]);
            m_materialEditor.FloatProperty(maskOffsetVProp, VATGUIStyles.MaskDataText[5]);
        }

        void DrawMaskRnadomUV()
        {
            m_materialEditor.FloatProperty(maskRandomUProp, VATGUIStyles.MaskDataText[6]);
            m_materialEditor.FloatProperty(maskRandomVProp, VATGUIStyles.MaskDataText[7]);
        }

        void DrawDissolveEnabled()
        {
            m_materialEditor.ShaderProperty(dissolveEnabledProp, VATGUIStyles.DissolveDataText[0]);
        }

        void DrawDissolveTex()
        {
            m_materialEditor.TextureProperty(dissolveTexProp, VATGUIStyles.DissolveDataText[1]);
        }

        void DrawDissolveChannel()
        {
            DrawChannelFunction(dissolveChannelDataProp, dissolveChannelVectorProp, VATGUIStyles.DissolveDataText[2]);
        }

        void DrawDissolveIsFilpMode()
        {
            EditorGUI.showMixedValue = dissIsFilpModeProp.hasMixedValue;
            var isFilp = (EnabledOffOn) dissIsFilpModeProp.floatValue;
            EditorGUI.BeginChangeCheck();
            isFilp = (EnabledOffOn) EditorGUILayout.Popup(VATGUIStyles.DissolveDataText[3],
                (int) isFilp, VATGUIStyles.IsFilpNames, VATEditorGUIStyles.EditorstylesPopup);
            if (EditorGUI.EndChangeCheck())
            {
                dissIsFilpModeProp.floatValue = (float) isFilp;
                switch (isFilp)
                {
                    case EnabledOffOn.Off:
                        dissIsFilpProp.floatValue = 0;
                        break;
                    case EnabledOffOn.On:
                        dissIsFilpProp.floatValue = 1;
                        break;
                }
            }
        }

        void DrawDissolve()
        {
            m_materialEditor.FloatProperty(dissolveProp, VATGUIStyles.DissolveDataText[4]);
        }

        void DrawDissolveEdge()
        {
            m_materialEditor.FloatProperty(dissEdgeProp, VATGUIStyles.DissolveDataText[5]);
        }

        void DrawDissolveOffsetSpeed()
        {
            m_materialEditor.FloatProperty(dissOffsetUProp, VATGUIStyles.DissolveDataText[6]);
            m_materialEditor.FloatProperty(dissOffsetVProp, VATGUIStyles.DissolveDataText[7]);
        }

        void DrawDissolveEdgeGlow()
        {
            m_materialEditor.FloatProperty(dissEdgeGlowAProp, VATGUIStyles.DissolveEdgeGlowText[0]);
            m_materialEditor.FloatProperty(dissEdgeGlowBProp, VATGUIStyles.DissolveEdgeGlowText[1]);
            m_materialEditor.FloatProperty(dissEdgeGlowCProp, VATGUIStyles.DissolveEdgeGlowText[2]);
        }

        void DrawRandomUV()
        {
            m_materialEditor.FloatProperty(dissRandomUProp, VATGUIStyles.DissRandomUVText[0]);
            m_materialEditor.FloatProperty(dissRandomVProp, VATGUIStyles.DissRandomUVText[1]);
        }

        void DrawDistortEnabled()
        {
            m_materialEditor.ShaderProperty(distortEnabledProp, VATGUIStyles.DistortEnabledText);
        }

        void DrawDistortTex()
        {
            m_materialEditor.TextureProperty(distortTexProp, VATGUIStyles.DistortTexText);
        }

        void DrawDistChannel()
        {
            DrawChannelFunction(distChannelDataProp, distChannelVectorProp, VATGUIStyles.DistChannelText);
        }

        void DrawDistPower()
        {
            m_materialEditor.FloatProperty(distPowerProp, VATGUIStyles.DistPowerText);
        }

        void DrawDistPowerX()
        {
            m_materialEditor.FloatProperty(distPowerXProp, VATGUIStyles.DistPowerXText);
        }

        void DrawDistPowerY()
        {
            m_materialEditor.FloatProperty(distPowerYProp, VATGUIStyles.DistPowerYText);
        }

        void DrawDistOffsetSpeed()
        {
            m_materialEditor.FloatProperty(distOffsetUProp, VATGUIStyles.DistOffsetUText);
            m_materialEditor.FloatProperty(distOffsetVProp, VATGUIStyles.DistOffsetVText);
        }

        void DrawDistPolarEnabled()
        {
            m_materialEditor.ShaderProperty(distPolarEnabledProp, VATGUIStyles.DistPolarEnabledText);
        }

        void DrawDistPolarStyles()
        {
            EditorGUI.showMixedValue = distPolarStylesModeProp.hasMixedValue;
            var typeMode = (TypeMode) distPolarStylesModeProp.floatValue;
            EditorGUI.BeginChangeCheck();
            typeMode = (TypeMode) EditorGUILayout.Popup(VATGUIStyles.DistPolarStylesText,
                (int) typeMode, VATGUIStyles.TypeModeNames, VATEditorGUIStyles.EditorstylesPopup);
            if (EditorGUI.EndChangeCheck())
            {
                distPolarStylesModeProp.floatValue = (float) typeMode;
                switch (typeMode)
                {
                    case TypeMode.TypeA:
                        distPolarStylesProp.floatValue = 0;
                        isDistPolarUVProp.floatValue = 0;
                        break;
                    case TypeMode.TypeB:
                        distPolarStylesProp.floatValue = 1;
                        isDistPolarUVProp.floatValue = 0;
                        break;
                    case TypeMode.Off:
                        isDistPolarUVProp.floatValue = 1;
                        break;
                }
            }
        }

        void DrawDistSpheticalStyles()
        {
            EditorGUI.showMixedValue = distSpheticalStylesModeProp.hasMixedValue;
            var typeMode = (TypeMode) distSpheticalStylesModeProp.floatValue;
            EditorGUI.BeginChangeCheck();
            typeMode = (TypeMode) EditorGUILayout.Popup(VATGUIStyles.DistSpheticalStylesText,
                (int) typeMode, VATGUIStyles.TypeModeNames, VATEditorGUIStyles.EditorstylesPopup);
            if (EditorGUI.EndChangeCheck())
            {
                distSpheticalStylesModeProp.floatValue = (float) typeMode;
                switch (typeMode)
                {
                    case TypeMode.TypeA:
                        distSpheticalStylesProp.floatValue = 0;
                        isDistSpheticalProp.floatValue = 0;
                        break;
                    case TypeMode.TypeB:
                        distSpheticalStylesProp.floatValue = 1;
                        isDistSpheticalProp.floatValue = 0;
                        break;
                    case TypeMode.Off:
                        isDistSpheticalProp.floatValue = 1;
                        break;
                }
            }
        }

        void DrawDistPolarTilingOffset()
        {
            m_materialEditor.FloatProperty(distPolarTilingXProp, VATGUIStyles.DistPolarTilingOffsetText[0]);
            m_materialEditor.FloatProperty(distPolarTilingYProp, VATGUIStyles.DistPolarTilingOffsetText[1]);
            m_materialEditor.FloatProperty(distPolarOffsetXProp, VATGUIStyles.DistPolarTilingOffsetText[2]);
            m_materialEditor.FloatProperty(distPolarOffsetYProp, VATGUIStyles.DistPolarTilingOffsetText[3]);
        }

        void DrawDistPolarOffsetSpeed()
        {
            m_materialEditor.FloatProperty(distPolarSpeedXProp, VATGUIStyles.DistPolarSpeedText[0]);
            m_materialEditor.FloatProperty(distPolarSpeedYProp, VATGUIStyles.DistPolarSpeedText[1]);
        }

        void DrawDistPolarCenter()
        {
            m_materialEditor.FloatProperty(distPolarCenterXProp, VATGUIStyles.DistPolarCenterText[0]);
            m_materialEditor.FloatProperty(distPolarCenterYProp, VATGUIStyles.DistPolarCenterText[1]);
        }

        void DrawFresnelEnabled()
        {
            m_materialEditor.ShaderProperty(fresnelEnabledProp, VATGUIStyles.FresnelText[0]);
        }

        void DrawFresnelColor()
        {
            m_materialEditor.ColorProperty(fresnelColorProp, VATGUIStyles.FresnelText[1]);
        }

        void DrawFresnelIntensity()
        {
            m_materialEditor.FloatProperty(fresnelIntensityProp, VATGUIStyles.FresnelText[2]);
        }

        void DrawFresnelSmoonth()
        {
            m_materialEditor.FloatProperty(fresnelSmoonthProp, VATGUIStyles.FresnelText[3]);
        }

        void DrawFresnelIsFilp()
        {
            m_materialEditor.ShaderProperty(fresnelIsFilpProp, VATGUIStyles.FresnelText[4]);
        }

        #endregion

        #region [具体绘制方法(界面显示)]

        void EditorDrawGlobalText()
        {
            EditorGUILayout.LabelField(VATEditorGUIStyles.EditorGlobalText, VATEditorGUIStyles.EditorStylesTitle);
        }

        void EditorDrawMainText()
        {
            EditorGUILayout.LabelField(VATEditorGUIStyles.EditorMainText, VATEditorGUIStyles.EditorStylesTitle);
        }

        void EditorDrawMainOffsetSpeedText()
        {
            EditorGUILayout.LabelField(VATEditorGUIStyles.EditorMainOffsetText, VATEditorGUIStyles.EditorStylesTitle);
        }

        void EditorDrawMaskText()
        {
            EditorGUILayout.LabelField(VATEditorGUIStyles.EditorMaskText, VATEditorGUIStyles.EditorStylesTitle);
        }

        void EditorDrawDissolveText()
        {
            EditorGUILayout.LabelField(VATEditorGUIStyles.EditorDissolveText, VATEditorGUIStyles.EditorStylesTitle);
        }

        void EditorDrawDistortText()
        {
            EditorGUILayout.LabelField(VATEditorGUIStyles.EditorDistText, VATEditorGUIStyles.EditorStylesTitle);
        }

        void EditorDrawDistPolarText()
        {
            EditorGUILayout.LabelField(VATEditorGUIStyles.EditorDistPolarText, VATEditorGUIStyles.EditorStylesTitle);
        }

        void EditorDrawDistPolarTilingOffsetText()
        {
            EditorGUILayout.LabelField(VATEditorGUIStyles.EditorDistPolarDataText[0], VATEditorGUIStyles.EditorStylesTitle);
        }

        void EditorDrawDistPolarOffsetSpeedText()
        {
            EditorGUILayout.LabelField(VATEditorGUIStyles.EditorDistPolarDataText[1], VATEditorGUIStyles.EditorStylesTitle);
        }

        void EditorDrawPolarCenterText()
        {
            EditorGUILayout.LabelField(VATEditorGUIStyles.EditorDistPolarDataText[2], VATEditorGUIStyles.EditorStylesTitle);
        }

        void EditorDrawFinalColorText()
        {
            EditorGUILayout.LabelField(VATEditorGUIStyles.EditorFinalColorText, VATEditorGUIStyles.EditorStylesTitle);
        }

        void EditorRampColorText()
        {
            EditorGUILayout.LabelField(VATEditorGUIStyles.EditorRampColorText, VATEditorGUIStyles.EditorStylesTitle);
        }

        void EditorDrawFresnelText()
        {
            EditorGUILayout.LabelField(VATEditorGUIStyles.EditorFresnelText, VATEditorGUIStyles.EditorStylesTitle);
        }

        #endregion

        #region [绘制内置自带的部分]

        void DrawQueueControl()
        {
            m_materialEditor.RenderQueueField();
        }

        #endregion

        #region [公共方法]

        void DrawChannelFunction(MaterialProperty enumDataProp, MaterialProperty channelDataProp, string Name)
        {
            EditorGUI.showMixedValue = enumDataProp.hasMixedValue;
            var mainChannelData = (MainChannelData) enumDataProp.floatValue;
            EditorGUI.BeginChangeCheck();
            mainChannelData = (MainChannelData) EditorGUILayout.Popup(Name, (int) mainChannelData, VATGUIStyles.MainChannelDataNames,
                VATEditorGUIStyles.EditorstylesPopup);
            if (EditorGUI.EndChangeCheck())
            {
                enumDataProp.floatValue = (float) mainChannelData;
                switch (mainChannelData)
                {
                    case MainChannelData.R:
                        channelDataProp.vectorValue = new Vector4(1, 0, 0, 0);
                        break;
                    case MainChannelData.G:
                        channelDataProp.vectorValue = new Vector4(0, 1, 0, 0);
                        break;
                    case MainChannelData.B:
                        channelDataProp.vectorValue = new Vector4(0, 0, 1, 0);
                        break;
                    case MainChannelData.A:
                        channelDataProp.vectorValue = new Vector4(0, 0, 0, 1);
                        break;
                }
            }
        }

        #endregion
    }
}