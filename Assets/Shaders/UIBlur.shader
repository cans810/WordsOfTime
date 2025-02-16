Shader "Custom/UIBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0, 10)) = 2
    }
 
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True"
        }

        GrabPass 
        { 
            "_GrabTexture" 
        }
 
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

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
            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;
 
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                return o;
            }
            
            // Gaussian weight calculation
            float gaussian(float x, float sigma)
            {
                float coeff = 1.0 / (sigma * sqrt(2.0 * 3.14159));
                return coeff * exp(-(x * x) / (2.0 * sigma * sigma));
            }
 
            half4 frag(v2f i) : SV_Target
            {
                float2 grabPos = i.grabPos.xy / i.grabPos.w;
                float blur = _BlurSize / 800; // Further reduced blur factor
                
                half4 color = half4(0,0,0,0);
                float totalWeight = 0;
                
                // 17x17 gaussian blur
                const int samples = 17;
                const float sigma = 15.0; // Increased sigma for more softness
                
                for(int x = -samples/2; x <= samples/2; x++)
                {
                    for(int y = -samples/2; y <= samples/2; y++)
                    {
                        float2 offset = float2(x, y) * blur;
                        float weight = gaussian(length(offset), sigma);
                        color += tex2D(_GrabTexture, grabPos + offset) * weight;
                        totalWeight += weight;
                    }
                }
                
                color /= totalWeight;
                return color;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}