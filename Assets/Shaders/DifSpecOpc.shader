Shader "Custom/Specular wAlpha" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_MainTex ("Base (RGB) Opacity (A)", 2D) = "white" {}
	_SpcTex ("Gloss (RGB)", 2D) = "white" {}
}

SubShader {
	Tags { "Queue"="Transparent" "RenderType"="Transparent" }
	LOD 300
	
	Blend srcAlpha oneMinusSrcAlpha
	
	CGPROGRAM
	#pragma surface surf BlinnPhong finalcolor:finalAlpha
	
	sampler2D _MainTex;
	sampler2D _SpcTex;
	fixed4 _Color;
	half _Shininess;
	
	struct Input {
		float2 uv_MainTex;
	};
	
	void finalAlpha(Input IN, SurfaceOutput o, inout fixed4 color)
	{
		color *= _Color.a;
	}
	
	void surf (Input IN, inout SurfaceOutput o) {
		fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
		fixed4 spc = tex2D(_SpcTex, IN.uv_MainTex);
		o.Albedo = tex.rgb * _Color.rgb;
		o.Gloss = spc.a;
		o.Alpha = tex.a;
		o.Specular = _Shininess;
	}
	ENDCG
}

Fallback "VertexLit"
}
