Shader "RhythmRehab/UI/IrisTransition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Cover Color", Color) = (0.003,0.005,0.012,1)
        _Radius ("Opening Radius", Float) = 2
        _Aspect ("Screen Aspect", Float) = 1.777778
        _Softness ("Edge Softness", Range(0.001,0.03)) = 0.003
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            fixed4 _Color;
            float _Radius, _Aspect, _Softness;
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 offset=(i.uv-.5)*float2(_Aspect,1);
                float outside=smoothstep(_Radius-_Softness,_Radius+_Softness,length(offset));
                return fixed4(_Color.rgb*i.color.rgb,_Color.a*i.color.a*outside);
            }
            ENDCG
        }
    }
}
