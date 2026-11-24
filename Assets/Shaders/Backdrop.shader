// 먼 배경 원통 (작업 T15): 산·궁궐·지붕 실루엣을 그린 그림을 안쪽 면에 조명·안개 없이 그린다.
// 캐릭터가 가지 않는 외곽을 실제 오브젝트 대신 착시로 채운다. 아래는 안개 색으로 녹아들게 _FogColor 쪽으로 섞는다.
// 작업 R37: _Mist = 시작화면 안개 동안 그림 전체를 안개 색으로 (AttractMode가 MaterialPropertyBlock으로).
// 작업 R20: 위쪽을 안개 색으로 칠하면 하늘(스카이박스)과 색이 달라 원통 윗 경계가 넓은 화각에서 활처럼 휜 띠로 보였다
//   → 위쪽은 투명하게 녹여 하늘 위에 겹친다 (하늘을 그린 뒤 그리도록 투명 큐 맨 앞).
Shader "RhythmActionGame/Backdrop"
{
    Properties
    {
        _MainTex ("Painting", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0.86,0.68,0.56,1)
        _FogBlend ("Fog Blend (bottom)", Range(0,1)) = 0.35
        _TopFade ("Top Fade Start (v)", Range(0,1)) = 0.55
        _Warm ("Warm Tint (dusk)", Range(0,1)) = 0.3
        _TopToFog ("Top to fog color (old look, R20 compare only)", Range(0,1)) = 0
        _Mist ("Mist (title screen, R37)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-499" "IgnoreProjector"="True" }
        Pass
        {
            Name "Backdrop"
            Tags { "LightMode"="UniversalForward" }
            Cull Off ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _FogColor;
                half _FogBlend;
                half _TopFade;
                half _Warm;
                half _TopToFog;
                half _Mist;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A i) { V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = TRANSFORM_TEX(i.uv, _MainTex); return o; }
            half4 frag(V i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half k = saturate((0.45 - i.uv.y) / 0.45) * _FogBlend;    // 아래쪽일수록 안개 색
                half3 rgb = lerp(c.rgb, c.rgb * _FogColor.rgb * 1.25, _Warm);   // 낮 그림을 해 질 녘 빛으로 (작업 R03)
                rgb = lerp(rgb, _FogColor.rgb, k);
                rgb = lerp(rgb, _FogColor.rgb, _Mist);                    // 작업 R37: 시작화면 새벽 안개 (멀리 산도 안개에 묻힘)
                half top = smoothstep(_TopFade, 0.92, i.uv.y);            // 위쪽 그림 하늘은 투명하게 녹여 하늘(스카이박스)과 잇는다 (R20)
                // _TopToFog = 1: 예전 모습(위쪽을 안개 색 불투명으로) — R20 전후 비교 테스트만 쓴다
                return half4(lerp(rgb, _FogColor.rgb, top * _TopToFog), 1.0 - top * (1.0 - _TopToFog));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
