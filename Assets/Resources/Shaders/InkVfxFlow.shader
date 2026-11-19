Shader "InkLine/VfxFlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "gray" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ColorCore ("Core Color", Color) = (1,1,1,1)
        _ColorMid ("Mid Color", Color) = (1,0.5,0.1,1)
        _ColorRim ("Rim Color", Color) = (0.18,0.06,0.04,1)
        // xy: scroll speed, z: 0=linear 1=polar swirl, w: swirl twist / distort amp
        _Flow ("Flow (sx, sy, polar, twist)", Vector) = (0, -1.6, 0, 0.18)
        // x: dissolve cutoff (0..1), y: noise distort amp, z: phase offset, w: hard-edge sharpness
        _Dissolve ("Dissolve (cut, distort, phase, sharp)", Vector) = (0, 0.22, 0, 1)
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
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            fixed4 _Color;
            fixed4 _ColorCore;
            fixed4 _ColorMid;
            fixed4 _ColorRim;
            float4 _Flow;
            float4 _Dissolve;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y + _Dissolve.z;
                float2 uv = i.uv;
                float2 nUv;

                if (_Flow.z > 0.5)
                {
                    // 极坐标旋涡：适合坍缩、旋风、风火轮、符文环
                    float2 d = uv - 0.5;
                    float r = length(d) * 2.0;
                    float ang = atan2(d.y, d.x) / 6.2831853 + 0.5;
                    float twist = r * _Flow.w;
                    nUv = float2(ang * 2.0 + twist + t * _Flow.x, r * 1.15 + t * _Flow.y);
                }
                else
                {
                    // 线性流体：适合火焰、鬼火、魂烟、能量柱
                    nUv = uv * float2(1.4, 1.4) + float2(_Flow.x, _Flow.y) * t;
                }

                float n1 = tex2D(_NoiseTex, nUv).r;
                float n2 = tex2D(_NoiseTex, nUv * 1.73 - float2(_Flow.y, -_Flow.x) * t * 0.45 + 0.37).r;
                float noise = n1 * 0.65 + n2 * 0.35;

                // 用噪声轻微扭曲主遮罩 UV，让轮廓自然流动摇曳
                float2 warpedUv = uv + (float2(n1, n2) - 0.5) * _Dissolve.y;
                fixed4 maskSample = tex2D(_MainTex, saturate(warpedUv));
                float mask = maskSample.a * maskSample.r;

                // 能量值 = 形状遮罩被噪声蚀刻后减去溶解阈值
                float energy = mask * (0.68 + 0.56 * noise) - _Dissolve.x * 1.15;

                // 硬边卡通阈值：彻底杜绝浅色宣纸上的发灰半透明脏边
                float aa = lerp(0.06, 0.02, saturate(_Dissolve.w));
                float alpha = smoothstep(0.04, 0.04 + aa, energy);

                // 三阶卡通色带（专为浅色底校准）：扎实深色外轮廓 -> 大面积高饱和主色 -> 精炼白热高光芯
                float toMid = smoothstep(0.20, 0.26, energy);
                float toCore = smoothstep(0.66, 0.75, energy);
                fixed3 col = lerp(_ColorRim.rgb, _ColorMid.rgb, toMid);
                col = lerp(col, _ColorCore.rgb, toCore);

                fixed4 outCol;
                outCol.rgb = col * i.color.rgb;
                outCol.a = alpha * i.color.a;
                return outCol;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
