using UnityEditor;
using UnityEditor.Rendering.Universal;

[CustomEditor(typeof(CurvedWorldRenderPipeline), true)]
public class CurvedWorldRenderPipelineEditor : Editor
{
    private Editor originalEditor;

    public override void OnInspectorGUI()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CurvedWorldRenderPipeline.detailLitShader)));
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CurvedWorldRenderPipeline.wavingGrassShader)));
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CurvedWorldRenderPipeline.wavingGrassBillboardShader)));
        EditorGUILayout.Space();

        if (originalEditor == null)
        {
            originalEditor = Editor.CreateEditor(target, typeof(UniversalRenderPipelineAssetEditor));
        }
        originalEditor.OnInspectorGUI();
        serializedObject.ApplyModifiedProperties();
    }
}
