Shader "Custom/Simple Transparent Glass" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_Alpha ("Alpha", Color) = (1,1,1,1)
	}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 200
	

CGPROGRAM
#pragma surface surf Lambert alpha




fixed4 _Color;
fixed4 _Alpha;

struct Input {
	float2 uv_MainTex;
	float3 worldRefl;
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 c = _Color;
	fixed4 alph = _Alpha;
	
	o.Albedo = c.rgb;
	o.Alpha = alph;
}
ENDCG
}

Fallback "Transparent/VertexLit"
}