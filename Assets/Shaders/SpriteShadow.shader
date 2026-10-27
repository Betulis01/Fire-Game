// Draws a sprite as a flat drop shadow: the renderer's color (SpriteShadow sets
// semi-transparent black) wherever the texture is opaque. A stencil bit lets each
// screen pixel take shadow only once per frame, so the paper doll's overlapping
// layers (body, head, gear) -- and two entities' shadows crossing -- don't darken
// where they overlap. Unlit, so the shadow reads the same under any 2D light.
Shader "Sprites/Shadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        // high bit only, clear of anything that counts up from the low bits
        Stencil
        {
            Ref 128
            ReadMask 128
            WriteMask 128
            Comp NotEqual
            Pass Replace
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed _Cutoff;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // hard pixel-art edge; transparent texels must not claim the stencil
                clip(tex2D(_MainTex, i.uv).a - _Cutoff);
                // Premultiplied output to match Blend One OneMinusSrcAlpha.
                return fixed4(i.color.rgb * i.color.a, i.color.a);
            }
            ENDCG
        }
    }
}
