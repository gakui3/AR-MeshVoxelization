Shader "Unlit/VoxelRenderer"
{
    Properties

     {

        [Header(UniversalRP Default Shader code)]
        [Space(20)]
        _TintColor("TintColor", color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
    }  

    SubShader
    {
    
        Name  "URPDefault"

        Tags {"RenderPipeline"="UniversalRenderPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
     
       LOD 300
    //    Cull [_Cull]

        Pass
        {          
           HLSLPROGRAM


            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex vert
            #pragma fragment frag
          
            //include fog
            #pragma multi_compile_fog           

            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma shader_feature _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
           
             CBUFFER_START(UnityPerMaterial)
             half4 _TintColor;
             sampler2D _MainTex;
             float4 _MainTex_ST;
             float   _Alpha;
             CBUFFER_END
            
             struct VertexInput
             {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float3 normal : NORMAL;
              
                UNITY_VERTEX_INPUT_INSTANCE_ID                              
              };

            struct VertexOutput
            {
                float4 vertex      : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float fogCoord     : TEXCOORD1;
                float3 normal      : NORMAL;
                float4 color       : TEXCOORD3;
                            
                float4 shadowCoord : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct VoxelData
            {
                float3 position;
                float4 color;
                int isRendering;
            };

            StructuredBuffer<VoxelData> VoxelBuffer;
            float voxelScale;

          VertexOutput vert(VertexInput v, uint instanceId : SV_InstanceID)
            {
                VertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                VoxelData voxel = VoxelBuffer[instanceId];
                if(voxel.isRendering == 1){
                    float3 pos = (v.vertex.xyz * voxelScale) + voxel.position + float3(voxelScale*0.5,voxelScale*0.5,voxelScale*0.5);
                    o.vertex = mul(UNITY_MATRIX_VP, float4(pos, 1.0));
                }else{
                    float3 pos = voxel.position + float3(100,100,100);
                    o.vertex = mul(UNITY_MATRIX_VP, float4(pos, 1.0));
                }

                o.uv = v.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw; ;
                o.normal = normalize(mul(v.normal, (float3x3)UNITY_MATRIX_I_M));
                
                o.fogCoord = ComputeFogFactor(o.vertex.z);
                    
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                o.shadowCoord = GetShadowCoord(vertexInput);

                o.color = voxel.color;
                
                return o;
            }

            half4 frag(VertexOutput i) : SV_Target
            {
              UNITY_SETUP_INSTANCE_ID(i);
              UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
              
            float4 col = i.color * _TintColor;
              Light mainLight = GetMainLight(i.shadowCoord);           
              float NdotL = saturate(dot(normalize(_MainLightPosition.xyz), i.normal));            
              float3 ambient = SampleSH(i.normal);             

              col.rgb *= NdotL * _MainLightColor.rgb * mainLight.shadowAttenuation + ambient;
             
              #if _ALPHATEST_ON
              clip(col.a - _Alpha);
              #endif

              //apply fog
              col.rgb = MixFog(col.rgb, i.fogCoord);
             

              return col;            
            }

            ENDHLSL  
        }
    }
}