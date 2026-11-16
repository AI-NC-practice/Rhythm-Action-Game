// 먼 배경 원통 (작업 T15): 산·궁궐·지붕 실루엣을 그린 그림을 안쪽 면에 조명·안개 없이 그린다.
// 캐릭터가 가지 않는 외곽을 실제 오브젝트 대신 착시로 채운다. 아래는 안개 색으로 녹아들게 _FogColor 쪽으로 섞는다.
Shader "RhythmActionGame/Backdrop"
{
    Properties
    {
        _MainTex ("Painting", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0.86,0.68,0.56,1)
        _FogBlend ("Fog Blend (bottom)", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+400" }
        Pass
        {
            Name "Backdrop"
            Tags { "LightMode"="UniversalForward" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _FogColor;
                half _FogBlend;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A i) { V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = TRANSFORM_TEX(i.uv, _MainTex); return o; }
            half4 frag(V i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half k = saturate((0.45 - i.uv.y) / 0.45) * _FogBlend;    // 아래쪽일수록 안개 색
                return half4(lerp(c.rgb, _FogColor.rgb, k), 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
