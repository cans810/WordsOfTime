Shader "Custom/UIBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0, 10)) = 2
    }
 
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        GrabPass { "_GrabTexture" }
 
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
            };
 
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 grabPos : TEXCOORD0;
            };
 
            float _BlurSize;
 
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                return o;
            }
 
            sampler2D _GrabTexture;
 
            half4 frag(v2f i) : SV_Target
            {
                float2 grabPos = i.grabPos.xy / i.grabPos.w;
                half4 color = half4(0,0,0,0);
                float blur = _BlurSize/100;
 
                // Simple 9-tap box blur
                color += tex2D(_GrabTexture, float2(grabPos.x - blur, grabPos.y - blur));
                color += tex2D(_GrabTexture, float2(grabPos.x, grabPos.y - blur));
                color += tex2D(_GrabTexture, float2(grabPos.x + blur, grabPos.y - blur));
                color += tex2D(_GrabTexture, float2(grabPos.x - blur, grabPos.y));
                color += tex2D(_GrabTexture, float2(grabPos.x, grabPos.y));
                color += tex2D(_GrabTexture, float2(grabPos.x + blur, grabPos.y));
                color += tex2D(_GrabTexture, float2(grabPos.x - blur, grabPos.y + blur));
                color += tex2D(_GrabTexture, float2(grabPos.x, grabPos.y + blur));
                color += tex2D(_GrabTexture, float2(grabPos.x + blur, grabPos.y + blur));
                
                return color / 9;
            }
            ENDCG
        }
    }
} 