Shader "Skybox/Blended" {
    Properties {
        _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
        _TexA ("Skybox A (Start)", Cube) = "white" {}
        _TexB ("Skybox B (End)", Cube) = "white" {}
        _Blend ("Blend (0=A, 1=B)", Range(0.0, 1.0)) = 0
        _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation ("Rotation (Spin)", Range(0, 360)) = 0
        _Tilt ("Tilt (Bring Clouds Down)", Range(-90, 90)) = 0  // <--- NEW
    }
    SubShader {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _TexA;
            samplerCUBE _TexB;
            half _Blend;
            half4 _Tint;
            half _Exposure;
            float _Rotation;
            float _Tilt;

            struct appdata_t {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            // Helper to handle the math
            float3 RotateSkybox (float3 vertex, float rotation, float tilt) {
                // 1. Convert angles to radians
                float radRot = rotation * UNITY_PI / 180.0;
                float radTilt = tilt * UNITY_PI / 180.0;

                float sRot, cRot;
                sincos(radRot, sRot, cRot);
                float sTilt, cTilt;
                sincos(radTilt, sTilt, cTilt);

                // 2. Apply Rotation (Y-Axis Spin)
                float2x2 mRot = float2x2(cRot, -sRot, sRot, cRot);
                float3 res = float3(mul(mRot, vertex.xz), vertex.y).xzy;

                // 3. Apply Tilt (X-Axis Rotation) - brings Z up/down
                // We rotate Y and Z around X
                float2x2 mTilt = float2x2(cTilt, -sTilt, sTilt, cTilt);
                // Swizzle to apply to Y and Z
                float2 yz = mul(mTilt, res.yz);
                res = float3(res.x, yz.x, yz.y);

                return res;
            }

            v2f vert (appdata_t v) {
                v2f o;
                // Apply both rotations
                float3 rotated = RotateSkybox(v.vertex.xyz, _Rotation, _Tilt);
                o.vertex = UnityObjectToClipPos(rotated);
                o.texcoord = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                half4 texA = texCUBE(_TexA, i.texcoord);
                half4 texB = texCUBE(_TexB, i.texcoord);
                half4 col = lerp(texA, texB, _Blend); 
                return col * _Tint * _Exposure;
            }
            ENDCG
        }
    }
    Fallback Off
}