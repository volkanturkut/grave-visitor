using UnityEngine;
using UnityEditor;


namespace AmazingAssets.CurvedWorldEditor
{
    class SpeedTree8ShaderGUI : ShaderGUI
    {
        private bool m_FirstTimeApply = true;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (this.m_FirstTimeApply)
            {
                foreach (Material target in materialEditor.targets)
                    SpeedTree8ShaderGUI.MaterialChanged(target);
                this.m_FirstTimeApply = false;
            }
            UnityEditor.EditorGUIUtility.labelWidth = 0.0f;
            EditorGUI.BeginChangeCheck();

            AmazingAssets.CurvedWorldEditor.MaterialProperties.InitCurvedWorldMaterialProperties(properties);
            AmazingAssets.CurvedWorldEditor.MaterialProperties.DrawCurvedWorldMaterialProperties(materialEditor, AmazingAssets.CurvedWorldEditor.MaterialProperties.STYLE.HelpBox, false, false);


            GUILayout.Label(SpeedTree8ShaderGUI.Styles.primaryMapsText, EditorStyles.boldLabel);
            MaterialProperty property1 = ShaderGUI.FindProperty("_MainTex", properties);
            MaterialProperty property2 = ShaderGUI.FindProperty("_Color", properties);
            materialEditor.TexturePropertySingleLine(SpeedTree8ShaderGUI.Styles.colorText, property1, (MaterialProperty)null, property2);
            MaterialProperty property3 = ShaderGUI.FindProperty("_BumpMap", properties);
            materialEditor.TexturePropertySingleLine(SpeedTree8ShaderGUI.Styles.normalMapText, property3);
            MaterialProperty property4 = ShaderGUI.FindProperty("_ExtraTex", properties);
            materialEditor.TexturePropertySingleLine(SpeedTree8ShaderGUI.Styles.extraMapText, property4, (MaterialProperty)null);
            if ((Object)property4.textureValue == (Object)null)
            {
                MaterialProperty property5 = ShaderGUI.FindProperty("_Glossiness", properties);
                materialEditor.ShaderProperty(property5, SpeedTree8ShaderGUI.Styles.smoothnessText, 2);
                MaterialProperty property6 = ShaderGUI.FindProperty("_Metallic", properties);
                materialEditor.ShaderProperty(property6, SpeedTree8ShaderGUI.Styles.metallicText, 2);
            }
            MaterialProperty property7 = ShaderGUI.FindProperty("_SubsurfaceTex", properties);
            MaterialProperty property8 = ShaderGUI.FindProperty("_SubsurfaceColor", properties);
            materialEditor.TexturePropertySingleLine(SpeedTree8ShaderGUI.Styles.subsurfaceMapText, property7, (MaterialProperty)null, property8);
            EditorGUILayout.Space();
            GUILayout.Label(SpeedTree8ShaderGUI.Styles.optionsText, EditorStyles.boldLabel);
            SpeedTree8ShaderGUI.MakeAlignedProperty(ShaderGUI.FindProperty("_TwoSided", properties), SpeedTree8ShaderGUI.Styles.twoSidedText, materialEditor, true);
            SpeedTree8ShaderGUI.MakeAlignedProperty(ShaderGUI.FindProperty("_WindQuality", properties), SpeedTree8ShaderGUI.Styles.windQualityText, materialEditor, true);
            SpeedTree8ShaderGUI.MakeCheckedProperty(ShaderGUI.FindProperty("_HueVariationKwToggle", properties), ShaderGUI.FindProperty("_HueVariationColor", properties), SpeedTree8ShaderGUI.Styles.hueVariationText, materialEditor);
            SpeedTree8ShaderGUI.MakeAlignedProperty(ShaderGUI.FindProperty("_NormalMapKwToggle", properties), SpeedTree8ShaderGUI.Styles.normalMappingText, materialEditor, true);
            MaterialProperty property9 = ShaderGUI.FindProperty("_SubsurfaceKwToggle", properties);
            SpeedTree8ShaderGUI.MakeAlignedProperty(property9, SpeedTree8ShaderGUI.Styles.subsurfaceText, materialEditor, true);
            if ((double)property9.floatValue > 0.0)
            {
                MaterialProperty property10 = ShaderGUI.FindProperty("_SubsurfaceIndirect", properties);
                materialEditor.ShaderProperty(property10, SpeedTree8ShaderGUI.Styles.subsurfaceIndirectText, 2);
            }
            MaterialProperty property11 = ShaderGUI.FindProperty("_BillboardKwToggle", properties);
            SpeedTree8ShaderGUI.MakeAlignedProperty(property11, SpeedTree8ShaderGUI.Styles.billboardText, materialEditor, true);
            if ((double)property11.floatValue > 0.0)
            {
                MaterialProperty property12 = ShaderGUI.FindProperty("_BillboardShadowFade", properties);
                materialEditor.ShaderProperty(property12, SpeedTree8ShaderGUI.Styles.billboardShadowFadeText, 2);
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material target in materialEditor.targets)
                {
                    SpeedTree8ShaderGUI.MaterialChanged(target);

                    AmazingAssets.CurvedWorldEditor.MaterialProperties.SetKeyWords(target);
                }
            }
            EditorGUILayout.Space();
            GUILayout.Label(SpeedTree8ShaderGUI.Styles.advancedText, EditorStyles.boldLabel);
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        }

        private static void MakeAlignedProperty(
          MaterialProperty prop,
          GUIContent text,
          MaterialEditor materialEditor,
          bool doubleWide = false)
        {
            Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);
            controlRect.width = EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth * (doubleWide ? 2f : 1f);
            materialEditor.ShaderProperty(controlRect, prop, text);
        }

        private static void MakeCheckedProperty(
          MaterialProperty keywordToggleProp,
          MaterialProperty prop,
          GUIContent text,
          MaterialEditor materialEditor,
          bool doubleWide = false)
        {
            Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);
            controlRect.width = EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth / 2f;

            materialEditor.ShaderProperty(controlRect, keywordToggleProp, text);
            using (new EditorGUI.DisabledScope((double)keywordToggleProp.floatValue == 0.0))
            {
                controlRect.width = EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth * (doubleWide ? 2f : 1f);
                controlRect.x += EditorGUIUtility.fieldWidth / 2f;
                materialEditor.ShaderProperty(controlRect, prop, " ");
            }
        }

        private static void MaterialChanged(Material material) => SpeedTree8ShaderGUI.SetKeyword(material, "EFFECT_EXTRA_TEX", (bool)(Object)material.GetTexture("_ExtraTex"));

        private static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        private static class Styles
        {
            public static GUIContent colorText = new GUIContent("Color", "Color (RGB) and Opacity (A)");
            public static GUIContent normalMapText = new GUIContent("Normal", "Normal (RGB)");
            public static GUIContent extraMapText = new GUIContent("Extra", "Smoothness (R), Metallic (G), AO (B)");
            public static GUIContent subsurfaceMapText = new GUIContent("Subsurface", "Subsurface (RGB)");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Smoothness value");
            public static GUIContent metallicText = new GUIContent("Metallic", "Metallic value");
            public static GUIContent twoSidedText = new GUIContent("Two-Sided", "Set this material to render as two-sided");
            public static GUIContent windQualityText = new GUIContent("Wind Quality", "Wind quality setting");
            public static GUIContent hueVariationText = new GUIContent("Hue Variation", "Hue variation Color (RGB) and Amount (A)");
            public static GUIContent normalMappingText = new GUIContent("Normal Map", "Enable normal mapping");
            public static GUIContent subsurfaceText = new GUIContent("Subsurface", "Enable subsurface scattering");
            public static GUIContent subsurfaceIndirectText = new GUIContent("Indirect Subsurface", "Scalar on subsurface from indirect light");
            public static GUIContent billboardText = new GUIContent("Billboard", "Enable billboard features (crossfading, etc.)");
            public static GUIContent billboardShadowFadeText = new GUIContent("Shadow Fade", "Fade shadow effect on billboards");
            public static GUIContent primaryMapsText = new GUIContent("Maps");
            public static GUIContent optionsText = new GUIContent("Options");
            public static GUIContent advancedText = new GUIContent("Advanced Options");
        }
    }
}