// 无尽的魔化莲池：从画面顶上往下渗，_Reveal 0 看不见、1 盖满。
// 边缘按横向几道正弦摆出触手状的毛边，前沿带一圈火龙果色的光。
Shader "InkLine/Reveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Reveal ("Reveal", Range(0, 1)) = 0
        _Soft ("Edge Softness", Range(0.01, 0.5)) = 0.12
        _Rim ("Rim Color", Color) = (0.95, 0.25, 0.85, 1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _Rim;
            float _Reveal;
            float _Soft;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.texcoord) * i.color;
                float x = i.texcoord.x;
                float t = _Time.y;
                float wob = sin(x * 9.0 + 0.6) * 0.035
                          + sin(x * 23.0 + t * 0.8) * 0.018
                          + sin(x * 57.0 + 1.7 - t * 1.3) * 0.009;
                float edge = 1.0 - _Reveal * (1.0 + _Soft + 0.08) + wob * (1.0 - _Reveal);
                float a = saturate((i.texcoord.y - edge) / _Soft);
                float rim = a * (1.0 - a) * 4.0 * (1.0 - step(0.999, _Reveal));
                c.rgb += _Rim.rgb * rim * 0.55;
                c.a *= a;
                return c;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
