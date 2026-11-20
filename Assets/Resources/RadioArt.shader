Shader "Parade/RadioArt" {
 Properties { _MainTex("Illustration",2D)="white"{} }
 SubShader { Pass { Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 struct v {float4 pos:POSITION; float2 uv:TEXCOORD0; fixed4 tint:COLOR;};
 struct f {float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 tint:COLOR;};
 f vert(v a){ f b;b.pos=UnityObjectToClipPos(a.pos);b.uv=a.uv;b.tint=a.tint;return b; }
 fixed4 frag(f a):SV_Target{return tex2D(_MainTex,a.uv)*a.tint;}
 ENDCG
 } }
}
