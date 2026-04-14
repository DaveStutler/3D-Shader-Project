Shader "Custom/URP_RaymarchingSmoke"
{
    Properties
    {
        _MainVoxelTex ("Main Voxel Texture (3D)", 3D) = "white" {}
        _SmokeBaseColor ("Smoke Base Color", Color) = (0.8, 0.8, 0.8, 1)
        _AmbientColor ("Ambient Color (Shadows)", Color) = (0.1, 0.1, 0.15, 1)
        
        [Header(Raymarching)]
        _StepSize ("Step Size", Range(0.01, 0.1)) = 0.02
        _DensityScale ("Global Density", Range(0, 50)) = 15.0
        
        [Header(Shaping)]
        _EdgeMin ("Edge Cutoff (Min)", Range(0, 1)) = 0.2
        _EdgeMax ("Edge Solid (Max)", Range(0, 1)) = 0.4
        
        [Header(Lighting)]
        _LightAbsorption ("Light Absorption", Range(0, 10)) = 2.0
        _HG_G ("Phase Function (g)", Range(-0.99, 0.99)) = 0.4
    }

    SubShader
    {
        Tags { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Front

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            TEXTURE3D(_MainVoxelTex);
            SAMPLER(sampler_MainVoxelTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _SmokeBaseColor;
                float4 _AmbientColor;
                float _StepSize;
                float _DensityScale;
                float _EdgeMin;
                float _EdgeMax;
                float _LightAbsorption;
                float _HG_G;
            CBUFFER_END

            float HGPhase(float cosTheta, float g)
            {
                float g2 = g * g;
                float denom = 1.0 + g2 - 2.0 * g * cosTheta;
                return (1.0 - g2) / (4.0 * 3.1415926 * pow(abs(denom), 1.5));
            }

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

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float3 cameraPosWS = GetCameraPositionWS();
                float3 cameraPosOS = TransformWorldToObject(cameraPosWS);

                float3 rayDirOS = normalize(input.positionOS - cameraPosOS);
                float2 hitInfo = IntersectAABB(cameraPosOS, rayDirOS);
                
                if (hitInfo.y <= 0.0) return half4(0,0,0,0);
                
                float3 rayOrigin = cameraPosOS + rayDirOS * hitInfo.x;
                
                Light mainLight = GetMainLight();
                float3 lightDirWS = mainLight.direction;
                float3 lightColor = mainLight.color;
                
                float3 localLightDir = normalize(TransformWorldToObjectDir(lightDirWS));
                
                float cosTheta = dot(rayDirOS, localLightDir);
                float phase = HGPhase(cosTheta, _HG_G);

                float transmittance = 1.0;
                float3 scatteredLight = 0;
                float distanceTravelled = 0;
                
                [loop]
                for (int step = 0; step < 64; step++)
                {
                    if (distanceTravelled >= hitInfo.y) break;
                    
                    float3 currentPos = rayOrigin + rayDirOS * distanceTravelled;
                    float3 uvw = currentPos + 0.5;
                    
                    float rawDensity = SAMPLE_TEXTURE3D_LOD(_MainVoxelTex, sampler_MainVoxelTex, uvw, 0).r;
                    
                    float shapedDensity = smoothstep(_EdgeMin, _EdgeMax, rawDensity);
                    float density = shapedDensity * _DensityScale;
                    
                    if (density > 0.01)
                    {
                        float lightDensity = 0;
                        float3 lightMarchPos = currentPos;
                        
                        for(int j = 0; j < 3; j++)
                        {
                            lightMarchPos += localLightDir * (_StepSize * 2.0);
                            float3 l_uvw = lightMarchPos + 0.5;
                            if(any(l_uvw < 0) || any(l_uvw > 1)) break;
                            
                            float shadowRawDensity = SAMPLE_TEXTURE3D_LOD(_MainVoxelTex, sampler_MainVoxelTex, l_uvw, 0).r;
                            lightDensity += smoothstep(_EdgeMin, _EdgeMax, shadowRawDensity) * _DensityScale;
                        }
                        
                        float lightAttenuation = exp(-lightDensity * _LightAbsorption);
                        float3 stepLightColor = _AmbientColor.rgb + (lightColor * lightAttenuation * phase * 2.0);
                        
                        float dTr = exp(-density * _StepSize);
                        scatteredLight += stepLightColor * (1.0 - dTr) * transmittance * _SmokeBaseColor.rgb;
                        
                        transmittance *= dTr;
                    }
                    
                    if (transmittance < 0.01) break;
                    
                    distanceTravelled += _StepSize;
                }
                
                return half4(scatteredLight, 1.0 - transmittance);
            }
            ENDHLSL
        }
    }
}