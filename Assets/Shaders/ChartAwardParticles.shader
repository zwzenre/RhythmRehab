Shader "RhythmRehab/UI/Chart Award Particles"
{
    Properties
    {
        [PerRendererData] _MainTex ("JMO Flare", 2D) = "black" {}
        _SparkTex ("JMO Electric Spark", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One One
        ColorMask RGB

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _SparkTex;

            struct Attributes
            {
                float4 position : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.position);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // JMO's grayscale textures store light in RGB against opaque black.
                half light = input.uv.x >= 1.5
                    ? tex2D(_SparkTex, input.uv - float2(2, 0)).r
                    : tex2D(_MainTex, input.uv).r;
                return half4(input.color.rgb * (light * input.color.a), 0);
            }
            ENDHLSL
        }
    }
}
