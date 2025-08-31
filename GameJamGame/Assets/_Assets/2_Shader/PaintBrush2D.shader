Shader "Blit/PaintBrush2D"
{
    Properties{
        _MainTex ("Canvas In/Out", 2D) = "black" {}
        _BrushTex("Brush (optional)", 2D) = "white" {}
        _UseBrushTex("Use BrushTex", Float) = 0
        _Center ("Center UV", Vector) = (0.5,0.5,0,0)
        _Radius ("Radius UV", Float) = 0.03
        _Hardness("Hardness", Range(0,1)) = 0.7
        _Flow    ("Flow", Range(0,1)) = 1
        _Color   ("Color", Color) = (0.8,0,0,0.9)
        _Eraser  ("Eraser", Float) = 0
    }
    SubShader{
        Tags{ "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass{
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BrushTex;
            float2 _Center;
            float _Radius, _Hardness, _Flow, _UseBrushTex, _Eraser;
            float4 _Color;

            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(uint id:SV_VertexID){
                v2f o;
                float2 v = float2((id==1||id==2)?1:0, (id>=2)?1:0);
                o.pos = float4(v*2-1,0,1); o.uv = v; return o;
            }

            float4 frag(v2f i):SV_Target{
                float4 baseC = tex2D(_MainTex, i.uv);

                // radial mask if no brush texture
                float m;
                if (_UseBrushTex > 0.5)
                {
                    // map canvas uv to brush uv: distance in UV space
                    float2 d  = (i.uv - _Center) / _Radius;
                    float2 buv = d * 0.5 + 0.5;
                    float a = tex2D(_BrushTex, buv).a;
                    m = a;
                }
                else
                {
                    float d = distance(i.uv, _Center);
                    float e0 = _Radius * _Hardness;
                    float e1 = _Radius;
                    m = saturate(1 - smoothstep(e0, e1, d));
                }

                float a = saturate(_Color.a * _Flow) * m;

                if (_Eraser > 0.5)
                {
                    // erase: remove alpha (and proportionally color)
                    float keep = 1 - a;
                    float3 rgb = baseC.rgb * keep;
                    float  A   = baseC.a   * keep;
                    return float4(rgb, A);
                }
                else
                {
                    // over composite
                    float3 rgb = _Color.rgb * a + baseC.rgb * (1 - a);
                    float  A   = a + baseC.a * (1 - a);
                    return float4(rgb, A);
                }
            }
            ENDHLSL
        }
    }
}
