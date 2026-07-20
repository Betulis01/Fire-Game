// Renders a sprite at reduced opacity, texture colors untouched. Unlike WhiteFlash
// (which flattens the sprite to a silhouette), this keeps the sprite's own colors and
// just fades it toward see-through, for hit reactions that read as "faded" not "hit".
Shader "Sprites/TransparentFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint (alpha = opacity)", Color) = (1,1,1,0.4)
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
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed a = tex.a * _Color.a;
                return fixed4(tex.rgb * _Color.rgb * a, a);
            }
            ENDCG
        }
    }
}
