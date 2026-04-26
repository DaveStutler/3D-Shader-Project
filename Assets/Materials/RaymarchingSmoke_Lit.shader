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
        
        [Header(Dynamics and Details)]
        _BoilSpeed ("Boiling Speed", Range(0, 10)) = 2.0
        _BoilStrength ("Boiling Strength", Range(0, 0.1)) = 0.01
        _DetailScale ("Detail Scale", Range(0, 50)) = 25
        _DetailStrength ("Detail Erosion", Range(0, 5)) = 0.8
        _WindDirection ("Wind Direction", Vector) = (0.1, 0.5, 0.2, 0)
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
        ZTest Always
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
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
                float _BoilSpeed;
                float _BoilStrength;
                float _DetailScale;
                float _DetailStrength;
                float4 _WindDirection;
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
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            float RandomHash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            half4 frag (Varyings input) : SV_Target
            {
                float3 cameraPosWordlSpace = GetCameraPositionWS();
                float3 cameraPosObjectSpace = TransformWorldToObject(cameraPosWordlSpace);

                float3 rayDirOS = normalize(input.positionOS - cameraPosObjectSpace);
                float2 hitInfo = IntersectAABB(cameraPosObjectSpace, rayDirOS);
                
                if (hitInfo.y <= 0.0) return half4(0,0,0,0);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float3 scenePosWordlSpace = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
                float3 scenePosObjectSpace = TransformWorldToObject(scenePosWordlSpace);

                float maxDistOS = length(scenePosObjectSpace - cameraPosObjectSpace);

                float maxTravelInsideBox = maxDistOS - hitInfo.x;

                hitInfo.y = min(hitInfo.y, maxTravelInsideBox);

                if (hitInfo.y <= 0.0) return half4(0,0,0,0);
                
                float3 rayOrigin = cameraPosObjectSpace + rayDirOS * hitInfo.x;
                
                Light mainLight = GetMainLight();
                float3 lightDirWS = mainLight.direction;
                float3 lightColor = mainLight.color;
                
                float3 localLightDir = normalize(TransformWorldToObjectDir(lightDirWS));
                
                float cosTheta = dot(rayDirOS, localLightDir);
                float phase = HGPhase(cosTheta, _HG_G);

                float transmittance = 1.0;
                float3 scatteredLight = 0;

                // random start
                float2 pixelCoords = input.positionCS.xy;
                float randomValue = RandomHash(pixelCoords);
                float distanceTravelled = randomValue * _StepSize;
                
                [loop]
                for (int step = 0; step < 64; step++)
                {
                    if (distanceTravelled >= hitInfo.y) break;
                    
                    float3 currentPos = rayOrigin + rayDirOS * distanceTravelled;
                    float3 uvw = currentPos + 0.5;

                    float3 timeOffset = _Time.y * _BoilSpeed * float3(0.5, 0.8, 0.3);
                    float3 distortion = float3(
                        sin(currentPos.y * 15.0 + timeOffset.x),
                        cos(currentPos.z * 15.0 + timeOffset.y),
                        sin(currentPos.x * 15.0 + timeOffset.z)
                    );


                    float rawDensity = SAMPLE_TEXTURE3D_LOD(_MainVoxelTex, sampler_MainVoxelTex, uvw, 0).r;

                    float3 detailUVW = currentPos * _DetailScale + _WindDirection.xyz * _Time.y;
                    float3 microWarp = float3(
                        sin(detailUVW.y * 1.5 + _Time.y * 2.0),
                        cos(detailUVW.z * 1.5 - _Time.y * 1.8),
                        sin(detailUVW.x * 1.5 + _Time.y * 2.1)
                    ) * 0.05; 
                    float3 warpedUVW = detailUVW + microWarp * _BoilSpeed;


                    float n1 = sin(warpedUVW.x) * cos(warpedUVW.y) * sin(warpedUVW.z);
                    float n2 = sin(warpedUVW.x * 3.5 - _Time.y * 3.0) * cos(warpedUVW.y * 3.5) * sin(warpedUVW.z * 3.5);
                    float detailNoise = (n1 + n2 * 0.5) * 0.33 + 0.5;
                    
                    float erodedShape = rawDensity - detailNoise * _DetailStrength;

                    float crispDensity = smoothstep(_EdgeMin, _EdgeMin + 0.05, erodedShape);

                    float density = crispDensity * _DensityScale;
                    
                    if (density > 0.01)
                    {
                        float lightDensity = 0;
                        float3 lightMarchPos = currentPos;
                        
                        for(int j = 0; j < 5; j++)
                        {
                            lightMarchPos += localLightDir * (_StepSize * 4.0);
                            float3 l_uvw = lightMarchPos + 0.5;
                            if(any(l_uvw < 0) || any(l_uvw > 1)) break;
                            
                            float shadowRawDensity = SAMPLE_TEXTURE3D_LOD(_MainVoxelTex, sampler_MainVoxelTex, l_uvw, 0).r;
                            lightDensity += max(0.0, shadowRawDensity - _EdgeMin) * _DensityScale;
                        }

                        float lightAttenuation = exp(-lightDensity * _LightAbsorption);
                        float powderEffect = 1.0 - exp(-density * 2.0); 
                        float3 stepLightColor = _AmbientColor.rgb + (lightColor * lightAttenuation * phase * 2.5 * powderEffect);
                        
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