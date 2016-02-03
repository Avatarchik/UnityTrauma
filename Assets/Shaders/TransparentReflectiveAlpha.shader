Shader "Custom/Transparent Reflective with Alpha" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_Alpha ("Alpha", Color) = (0,0,0,0)
	_Strength ("Reflect Strength", Color) = (0,0,0,0)
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_Cube ("Reflection Cubemap", Cube) = "_Skybox" { TexGen CubeReflect }
	}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 200
	

CGPROGRAM
#pragma surface surf Lambert alpha


sampler2D _MainTex;
samplerCUBE _Cube;


fixed4 _Color;
fixed4 _Alpha;
fixed4 _Strength;

struct Input {
	float2 uv_MainTex;
	float3 worldRefl;
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
	fixed4 alph = _Alpha;
	fixed4 reflcol = (texCUBE (_Cube, IN.worldRefl))*_Strength;
	
	o.Emission = reflcol.rgb;
	o.Albedo = c.rgb;
	o.Alpha = c.a+alph;
}
ENDCG
}

Fallback "Transparent/VertexLit"
}