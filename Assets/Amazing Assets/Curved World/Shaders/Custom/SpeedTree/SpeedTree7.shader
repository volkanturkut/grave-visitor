Shader "Amazing Assets/Curved World/Nature/SpeedTree7"
{
    Properties
    {
        [HideInInspector] [CurvedWorldBendSettings] _CurvedWorldBendSettings("0|1|1", Vector) = (0, 0, 0, 0)

        [HideInInspector]_Color("Main Color", Color) = (1,1,1,1)
        [HideInInspector]_HueVariation("Hue Variation", Color) = (1.0,0.5,0.0,0.1)
        [HideInInspector]_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        [HideInInspector]_DetailTex("Detail", 2D) = "black" {}
        [HideInInspector]_BumpMap("Normal Map", 2D) = "bump" {}
        [HideInInspector]_Cutoff("Alpha Cutoff", Range(0,1)) = 0.333
        [HideInInspector][MaterialEnum(Off,0,Front,1,Back,2)] _Cull("Cull", Int) = 2
        [HideInInspector][MaterialEnum(None,0,Fastest,1,Fast,2,Better,3,Best,4,Palm,5)] _WindQuality("Wind Quality", Range(0,5)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
            "RenderType" = "Opaque"
            "DisableBatching" = "LODFading"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "SimpleLit"
        }
        LOD 400
        Cull [_Cull]

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex SpeedTree7Vert
            #pragma fragment SpeedTree7Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTERED_RENDERING

            #pragma multi_compile_fog

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer assumeuniformscaling maxcount:50

            #pragma shader_feature_local GEOM_TYPE_BRANCH GEOM_TYPE_BRANCH_DETAIL GEOM_TYPE_FROND GEOM_TYPE_LEAF GEOM_TYPE_MESH
            #pragma shader_feature_local EFFECT_BUMP
            #pragma shader_feature_local EFFECT_HUE_VARIATION

            #define ENABLE_WIND
            #define VERTEX_COLOR

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Nature/SpeedTree7Input.hlsl"


#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
#define CURVEDWORLD_BEND_ID_1
#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON    
#include "../../Core/CurvedWorldTransform.cginc"


            #include "SpeedTree7Passes.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags{"LightMode" = "SceneSelectionPass"}

            HLSLPROGRAM

            #pragma vertex SpeedTree7VertDepth
            #pragma fragment SpeedTree7FragDepth

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling maxcount:50

            #pragma shader_feature_local GEOM_TYPE_BRANCH GEOM_TYPE_BRANCH_DETAIL GEOM_TYPE_FROND GEOM_TYPE_LEAF GEOM_TYPE_MESH

            #define ENABLE_WIND
            #define DEPTH_ONLY
            #define SCENESELECTIONPASS

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Nature/SpeedTree7Input.hlsl"

#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
#define CURVEDWORLD_BEND_ID_1
#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON    
#include "../../Core/CurvedWorldTransform.cginc"


            #include "SpeedTree7Passes.hlsl"
            ENDHLSL
        }
	
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ColorMask 0

            HLSLPROGRAM

            #pragma vertex SpeedTree7VertDepth
            #pragma fragment SpeedTree7FragDepth

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling maxcount:50

            #pragma shader_feature_local GEOM_TYPE_BRANCH GEOM_TYPE_BRANCH_DETAIL GEOM_TYPE_FROND GEOM_TYPE_LEAF GEOM_TYPE_MESH

            #define ENABLE_WIND
            #define DEPTH_ONLY
            #define SHADOW_CASTER

			// -------------------------------------
            // Universal Pipeline keywords

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Nature/SpeedTree7Input.hlsl"


#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
#define CURVEDWORLD_BEND_ID_1
#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON    
#include "../../Core/CurvedWorldTransform.cginc"


            #include "SpeedTree7Passes.hlsl"
            ENDHLSL
        }
		
		Pass
        {
            Name "GBuffer"
            Tags{"LightMode" = "UniversalGBuffer"}

            HLSLPROGRAM

            #pragma exclude_renderers gles
            #pragma vertex SpeedTree7Vert
            #pragma fragment SpeedTree7Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer assumeuniformscaling maxcount:50

            #pragma shader_feature_local GEOM_TYPE_BRANCH GEOM_TYPE_BRANCH_DETAIL GEOM_TYPE_FROND GEOM_TYPE_LEAF GEOM_TYPE_MESH
            #pragma shader_feature_local EFFECT_BUMP
            #pragma shader_feature_local EFFECT_HUE_VARIATION

            #define ENABLE_WIND
            #define VERTEX_COLOR
            #define GBUFFER

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Nature/SpeedTree7Input.hlsl"
            
#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
#define CURVEDWORLD_BEND_ID_1
#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON    
#include "../../Core/CurvedWorldTransform.cginc"

            #include "SpeedTree7Passes.hlsl"

            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ColorMask 0

            HLSLPROGRAM

            #pragma vertex SpeedTree7VertDepth
            #pragma fragment SpeedTree7FragDepth

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling maxcount:50

            #pragma shader_feature_local GEOM_TYPE_BRANCH GEOM_TYPE_BRANCH_DETAIL GEOM_TYPE_FROND GEOM_TYPE_LEAF GEOM_TYPE_MESH

            #define ENABLE_WIND
            #define DEPTH_ONLY

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Nature/SpeedTree7Input.hlsl"


#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
#define CURVEDWORLD_BEND_ID_1
#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON    
#include "../../Core/CurvedWorldTransform.cginc"


            #include "SpeedTree7Passes.hlsl"

            ENDHLSL
        }

		// This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            HLSLPROGRAM
            #pragma vertex SpeedTree7VertDepthNormal
            #pragma fragment SpeedTree7FragDepthNormal

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling maxcount:50

            #pragma shader_feature_local GEOM_TYPE_BRANCH GEOM_TYPE_BRANCH_DETAIL GEOM_TYPE_FROND GEOM_TYPE_LEAF GEOM_TYPE_MESH
            #pragma shader_feature_local EFFECT_BUMP

            #define ENABLE_WIND

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Nature/SpeedTree7Input.hlsl"

#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
#define CURVEDWORLD_BEND_ID_1
#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON    
#include "../../Core/CurvedWorldTransform.cginc"

            #include "SpeedTree7Passes.hlsl"

            ENDHLSL
        }

        //PassName "ScenePickingPass"
		Pass
        {
            Name "ScenePickingPass"
            Tags { "LightMode" = "Picking" }

            BlendOp Add
            Blend One Zero
            ZWrite On
            Cull Off

            CGPROGRAM
			#include "HLSLSupport.cginc"
			#include "UnityShaderVariables.cginc"
			#include "UnityShaderUtilities.cginc"


            #pragma target 3.0

            #pragma shader_feature_local GEOM_TYPE_BRANCH GEOM_TYPE_BRANCH_DETAIL GEOM_TYPE_FROND GEOM_TYPE_LEAF GEOM_TYPE_MESH
            #ifdef GEOM_TYPE_LEAF
                #ifndef _ALPHATEST_ON
                #define _ALPHATEST_ON
                #endif
            #endif

            #pragma multi_compile_instancing

            #pragma vertex vertEditorPass
            #pragma fragment fragScenePickingPass


#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
#define CURVEDWORLD_BEND_ID_1
#pragma shader_feature_local CURVEDWORLD_DISABLED_ON


            #include "../../Core/SceneSelection.cginc" 
            ENDCG
        }	//Pass "ScenePickingPass"		
    }

    Dependency "BillboardShader" = "Amazing Assets/Curved World/Nature/SpeedTree7 Billboard"
    CustomEditor "AmazingAssets.CurvedWorldEditor.SpeedTreeMaterialInspector"
}
