Shader "YingYun/UI/OpeningVideoSharpen"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Sharpness ("Sharpness", Range(0, 0.6)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _Sharpness;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;
                fixed4 center = tex2D(_MainTex, input.uv);
                fixed4 neighbors =
                    tex2D(_MainTex, input.uv + float2(texel.x, 0)) +
                    tex2D(_MainTex, input.uv - float2(texel.x, 0)) +
                    tex2D(_MainTex, input.uv + float2(0, texel.y)) +
                    tex2D(_MainTex, input.uv - float2(0, texel.y));
                fixed4 sharpened = center + (center - neighbors * 0.25) * _Sharpness;
                sharpened.a = center.a;
                return saturate(sharpened) * input.color;
            }
            ENDCG
        }
    }
}
