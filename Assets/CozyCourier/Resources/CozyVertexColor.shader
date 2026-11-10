// Cozy Courier: binolar, yo'llar va sahnalar uchun oddiy shader.
// Faqat vertex rangini ishlatadi, shuning uchun Sprites/Default'dagi "_RendererColor" muammosi
// (meshlar ba'zan ko'rinmay qolishi) bo'lmaydi. Built-in va URP'da ishlaydi.
Shader "CozyCourier/VertexColor"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite Off
        ZTest Always
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _CozyLinear; // C# dan: 1 = loyiha Linear rang fazosida

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float4 c = v.color;
                if (_CozyLinear > 0.5) c.rgb = GammaToLinearSpace(c.rgb);
                o.color = c * _Color;
                return o;
            }

            float4 frag (v2f i) : SV_Target { return i.color; }
            ENDCG
        }
    }
    Fallback Off
}
