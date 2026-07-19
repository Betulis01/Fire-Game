// Renders a sprite as a flat silhouette of _Color (default solid white), keeping only
// the texture's alpha. FlashOnHit swaps a SpriteRenderer to this for a split second on
// hit. Deliberately unlit: a hit flash should read as bright regardless of 2D lighting.
Shader "Sprites/WhiteFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Flash Color", Color) = (1,1,1,1)
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

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Premultiplied output to match Blend One OneMinusSrcAlpha.
                fixed a = tex2D(_MainTex, i.uv).a * _Color.a;
                return fixed4(_Color.rgb * a, a);
            }
            ENDCG
        }
    }
}
