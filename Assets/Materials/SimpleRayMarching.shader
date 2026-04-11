Shader "Custom/SimpleRayMarching"
{
    Properties
    {
        _MainVoxelTex ("Volume Texture (3D)", 3D) = "white" {}
        _StepSize ("Step Size", Range(0.01, 0.1)) = 0.02
        _DensityScale ("Density Scale", Range(0, 100)) = 20.0
        _SmokeColor ("Smoke Color", Color) = (0.8, 0.8, 0.8, 1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Front
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 objectPos : TEXCOORD0; 
                float3 objectCamPos : TEXCOORD1; 
            };

            sampler3D _MainVoxelTex;
            float _StepSize;
            float _DensityScale;
            float4 _SmokeColor;

            float2 IntersectAABB(float3 rayOrigin, float3 rayDir)
            {
                float3 boxMin = float3(-0.5, -0.5, -0.5);
                float3 boxMax = float3(0.5, 0.5, 0.5);
                
                float3 invDir = 1.0 / rayDir;
                float3 t0 = (boxMin - rayOrigin) * invDir;
                float3 t1 = (boxMax - rayOrigin) * invDir;
                
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);
                
                float dstA = max(max(tmin.x, tmin.y), tmin.z); 
                float dstB = min(min(tmax.x, tmax.y), tmax.z); 
                
                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);
                
                return float2(dstToBox, dstInsideBox);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.objectPos = v.vertex.xyz;
                o.objectCamPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 rayDir = normalize(i.objectPos - i.objectCamPos);

                float2 hitInfo = IntersectAABB(i.objectCamPos, rayDir);
                float dstToBox = hitInfo.x;
                float dstInsideBox = hitInfo.y;

                if (dstInsideBox <= 0.0) return fixed4(0,0,0,0);

                float3 rayOrigin = i.objectCamPos + rayDir * dstToBox;

                float totalDensity = 0;
                float transmittance = 1.0;

                float distanceTravelled = 0;

                [loop]
                for (int step = 0; step < 16; step++)
                {
                    if (distanceTravelled >= dstInsideBox) break;
                    
                    float3 currentPos = rayOrigin + rayDir * distanceTravelled;
                    
                    float3 uvw = currentPos + float3(0.5, 0.5, 0.5);
                    
                    float rawDensity = tex3D(_MainVoxelTex, uvw).r;

                    float shapedDensity = smoothstep(0.1, 0.3, rawDensity); 

                    float density = shapedDensity * _DensityScale;
                    
                    //Beer-Lambert

                    if (density > 0.01)
                    {
                        float dTr = exp(-density * _StepSize);
                        transmittance *= dTr;
                        
                        totalDensity += density * _StepSize;
                    }
                    
                    if (transmittance < 0.01) break;
                    
                    distanceTravelled += _StepSize;
                }

                float finalAlpha = 1.0 - transmittance;
                
                return fixed4(_SmokeColor.rgb, finalAlpha);
            }
            ENDCG
        }
    }
}
