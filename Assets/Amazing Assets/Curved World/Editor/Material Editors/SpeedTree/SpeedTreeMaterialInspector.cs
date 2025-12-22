using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;


namespace AmazingAssets.CurvedWorldEditor
{
    [CanEditMultipleObjects]
    public class SpeedTreeMaterialInspector : ShaderGUI
    {
        private enum SpeedTreeGeometryType
        {
            Branch,
            BranchDetail,
            Frond,
            Leaf,
            Mesh,
        }

        private string[] speedTreeGeometryTypeString = new string[5]
        {
      "GEOM_TYPE_BRANCH",
      "GEOM_TYPE_BRANCH_DETAIL",
      "GEOM_TYPE_FROND",
      "GEOM_TYPE_LEAF",
      "GEOM_TYPE_MESH"
        };



        private bool ShouldEnableAlphaTest(SpeedTreeGeometryType geomType)
        {
            return geomType == SpeedTreeGeometryType.Frond || geomType == SpeedTreeGeometryType.Leaf;
        }
        private bool? ToggleShaderProperty(MaterialEditor materialEditor, MaterialProperty prop, bool enable, bool hasMixedEnable)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = hasMixedEnable;
            Rect controlRect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(false));
            controlRect.width = (double)controlRect.width > (double)EditorGUIUtility.fieldWidth ? controlRect.width - EditorGUIUtility.fieldWidth : controlRect.width;
            enable = EditorGUI.ToggleLeft(controlRect, prop.displayName, enable);
            EditorGUI.showMixedValue = false;
            bool? nullable = EditorGUI.EndChangeCheck() ? new bool?(enable) : new bool?();
            GUILayout.Space(-EditorGUIUtility.singleLineHeight);
            using (new EditorGUI.DisabledScope(!enable && !hasMixedEnable))
            {
                EditorGUI.showMixedValue = prop.hasMixedValue;
                materialEditor.ShaderProperty(prop, " ");
                EditorGUI.showMixedValue = false;
            }
            return nullable;
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            //Curved World
            MaterialProperties.InitCurvedWorldMaterialProperties(properties);
            MaterialProperties.DrawCurvedWorldMaterialProperties(materialEditor, MaterialProperties.STYLE.HelpBox, false, false);
             


            List<MaterialProperty> materialPropertyList = new List<MaterialProperty>((IEnumerable<MaterialProperty>)MaterialEditor.GetMaterialProperties(materialEditor.targets));
            materialEditor.SetDefaultGUIWidths();
            SpeedTreeGeometryType[] source1 = new SpeedTreeGeometryType[materialEditor.targets.Length];
            for (int index1 = 0; index1 < materialEditor.targets.Length; ++index1)
            {
                source1[index1] = SpeedTreeGeometryType.Branch;
                for (int index2 = 0; index2 < this.speedTreeGeometryTypeString.Length; ++index2)
                {
                    if (((IEnumerable<string>)((Material)materialEditor.targets[index1]).shaderKeywords).Contains<string>(this.speedTreeGeometryTypeString[index2]))
                    {
                        source1[index1] = (SpeedTreeGeometryType)index2;
                        break;
                    }
                }
            }

            EditorGUI.showMixedValue = ((IEnumerable<SpeedTreeGeometryType>)source1).Distinct<SpeedTreeGeometryType>().Count<SpeedTreeGeometryType>() > 1;
            EditorGUI.BeginChangeCheck();
            SpeedTreeGeometryType geomType = (SpeedTreeGeometryType)EditorGUILayout.EnumPopup("Geometry Type", (Enum)source1[0]);
            if (EditorGUI.EndChangeCheck())
            {
                bool flag = this.ShouldEnableAlphaTest(geomType);
                CullMode cullMode = flag ? CullMode.Off : CullMode.Back;
                foreach (Material material in materialEditor.targets.Cast<Material>())
                {
                    if (flag)
                        material.SetOverrideTag("RenderType", "treeTransparentCutout");
                    for (int index = 0; index < this.speedTreeGeometryTypeString.Length; ++index)
                        material.DisableKeyword(this.speedTreeGeometryTypeString[index]);
                    material.EnableKeyword(this.speedTreeGeometryTypeString[(int)geomType]);
                    material.renderQueue = flag ? 2450 : 2000;
                    material.SetInt("_Cull", (int)cullMode);
                }
            }


            EditorGUI.showMixedValue = false;
            MaterialProperty prop1 = materialPropertyList.Find((Predicate<MaterialProperty>)(prop => prop.name == "_MainTex"));
            if (prop1 != null)
            {
                materialPropertyList.Remove(prop1);
                materialEditor.ShaderProperty(prop1, prop1.displayName);
            }
            MaterialProperty prop2 = materialPropertyList.Find((Predicate<MaterialProperty>)(prop => prop.name == "_BumpMap"));
            if (prop2 != null)
            {
                materialPropertyList.Remove(prop2);
                IEnumerable<bool> source2 = ((IEnumerable<UnityEngine.Object>)materialEditor.targets).Select<UnityEngine.Object, bool>((Func<UnityEngine.Object, bool>)(t => ((IEnumerable<string>)((Material)t).shaderKeywords).Contains<string>("EFFECT_BUMP")));
                bool? nullable = ToggleShaderProperty(materialEditor, prop2, source2.First<bool>(), source2.Distinct<bool>().Count<bool>() > 1);
                if (nullable.HasValue)
                {
                    foreach (Material material in materialEditor.targets.Cast<Material>())
                    {
                        if (nullable.Value)
                            material.EnableKeyword("EFFECT_BUMP");
                        else
                            material.DisableKeyword("EFFECT_BUMP");
                    }
                }
            }
            MaterialProperty prop3 = materialPropertyList.Find((Predicate<MaterialProperty>)(prop => prop.name == "_DetailTex"));
            if (prop3 != null)
            {
                materialPropertyList.Remove(prop3);
                if (((IEnumerable<SpeedTreeGeometryType>)source1).Contains<SpeedTreeGeometryType>(SpeedTreeGeometryType.BranchDetail))
                    materialEditor.ShaderProperty(prop3, prop3.displayName);
            }
            IEnumerable<bool> source3 = ((IEnumerable<UnityEngine.Object>)materialEditor.targets).Select<UnityEngine.Object, bool>((Func<UnityEngine.Object, bool>)(t => ((IEnumerable<string>)((Material)t).shaderKeywords).Contains<string>("EFFECT_HUE_VARIATION")));
            MaterialProperty prop4 = materialPropertyList.Find((Predicate<MaterialProperty>)(prop => prop.name == "_HueVariation"));
            if (source3 != null && prop4 != null)
            {
                materialPropertyList.Remove(prop4);
                bool? nullable = ToggleShaderProperty(materialEditor, prop4, source3.First<bool>(), source3.Distinct<bool>().Count<bool>() > 1);
                if (nullable.HasValue)
                {
                    foreach (Material material in materialEditor.targets.Cast<Material>())
                    {
                        if (nullable.Value)
                            material.EnableKeyword("EFFECT_HUE_VARIATION");
                        else
                            material.DisableKeyword("EFFECT_HUE_VARIATION");
                    }
                }
            }
            MaterialProperty prop5 = materialPropertyList.Find((Predicate<MaterialProperty>)(prop => prop.name == "_Cutoff"));
            if (prop5 != null)
            {
                materialPropertyList.Remove(prop5);
                if (((IEnumerable<SpeedTreeGeometryType>)source1).Any<SpeedTreeGeometryType>((Func<SpeedTreeGeometryType, bool>)(t => this.ShouldEnableAlphaTest(t))))
                    materialEditor.ShaderProperty(prop5, prop5.displayName);
            }
            foreach (MaterialProperty prop6 in materialPropertyList)
            {
                if ((uint)(prop6.flags & MaterialProperty.PropFlags.HideInInspector) <= 0U)
                    materialEditor.ShaderProperty(prop6, prop6.displayName);
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();


            base.OnGUI(materialEditor, properties);
        }
    }
}
