Shader "Parade/Type" {
    Properties { _MainTex("Font atlas",2D)="white"{} }
    SubShader {
        Pass {
            Cull Off ZWrite Off ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct input { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct output { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            output vert(input v){output o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
            fixed4 frag(output i):SV_Target { return fixed4(i.color.rgb,i.color.a*tex2D(_MainTex,i.uv).a); }
            ENDCG
        }
    }
}
