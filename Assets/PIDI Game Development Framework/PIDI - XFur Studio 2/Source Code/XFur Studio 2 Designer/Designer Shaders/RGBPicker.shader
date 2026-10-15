Shader "Hidden/XFur Studio 2/Designer/RGBPicker"
{
    Properties
    {
        _Hue("HSV Step", Range(0,1)) = 1
        _Sat("Sat", Range(0,1)) = 1
        _Val("Val", Range(0,1)) = 1
        _Resolution("Resolution", Float) = 256
        _BorderSize("Border Size", Range(0,8)) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };


            float _Hue;
            float _Sat;
            float _Val;
            float _Resolution;
            int _BorderSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            static const float recip2Pi = 0.159154943;
            static const float twoPi = 6.2831853;

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);

            }

            half4 hueRing(float2 uv)
            {
                float2 coords = uv - .5;

                float r = length(coords);

                float fw = fwidth(r);

                float a = smoothstep(.5, .5 - fw, r) * smoothstep(0.4 - fw, 0.4, r);

                float angle = atan2(coords.y, coords.x) * recip2Pi;

                return half4(hsv2rgb(float3(angle, 1, 1)), a);
            }




            fixed4 mix(fixed4 bot, fixed4 top)
            {
                return fixed4(lerp(bot.rgb, top.rgb, top.a), max(bot.a, top.a));
            }



            fixed4 frag(v2f i) : SV_Target
            {


#if UNITY_UV_STARTS_AT_TOP
                i.uv.y = 1.0 - i.uv.y;
#endif
                int currentPixelX = i.uv.x / ( 1.0 / _Resolution );
                int currentPixelY = i.uv.y / ( 1.0 / _Resolution );

                int satPixel = clamp( _Sat / (1.0 / _Resolution), _BorderSize * 2 + 4, _Resolution - _BorderSize * 2 - 4 );
                int valPixel = clamp( (1-_Val) / (1.0 / _Resolution), _BorderSize * 2 + 4, _Resolution - _BorderSize * 2 - 4 );

                _Resolution--;

                half3 targetColor = hsv2rgb( float3(_Hue, 1, 1) );

                half4 col = half4(1,1,1,1);

                col.rgb = lerp(col.rgb, targetColor, saturate( (currentPixelX - _BorderSize*2)/(_Resolution-_BorderSize*4) ) );

                col.rgb = lerp(col.rgb, half3(0,0,0), saturate( (currentPixelY - _BorderSize*2) / (_Resolution - _BorderSize * 4) ) );

                col.rgb = lerp(half3(1,1,1), col.rgb, saturate(currentPixelX-_BorderSize));
                col.rgb = lerp(half3(1,1,1), col.rgb, saturate(_Resolution-currentPixelX-_BorderSize));
                col.rgb = lerp(half3(1,1,1), col.rgb, saturate(currentPixelY-_BorderSize));
                col.rgb = lerp(half3(1,1,1), col.rgb, saturate(_Resolution-currentPixelY-_BorderSize));
                
                col.rgb = lerp(half3(0, 0, 0), col.rgb, saturate( saturate( distance(currentPixelX,satPixel) - 4 ) + saturate(distance(currentPixelY, valPixel) - 4) ) );
                col.rgb = lerp(half3(1, 1, 1), col.rgb, saturate( saturate( distance(currentPixelX,satPixel) - 2 ) + saturate(distance(currentPixelY, valPixel) - 2) ) );

                return pow ( col, 2 );
            }
            ENDCG
        }
    }
}
