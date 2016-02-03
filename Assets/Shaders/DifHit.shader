Shader "Custom/DifHit" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1, 1, 1, 1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_AltTex ("Alternate (RGBa)", 2D) = "white" {}
		_HitMap ("Hit Map", 2D) = "white" {}
		_Fade ("Fade Amount", Range(0.0, 1.0)) = 0.0
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Opaque" "HitRender"="Hit"}
		LOD 400
		
		Blend srcAlpha oneMinusSrcAlpha
		
		CGPROGRAM
		#pragma surface surf Lambert finalcolor:finalAlpha

		sampler2D _MainTex;
		sampler2D _AltTex;
		fixed4 _Color;
		half _Fade;

		struct Input
		{
			float2 uv_MainTex;
		};
		
		void finalAlpha(Input IN, SurfaceOutput o, inout fixed4 color)
		{
			color *= _Color.a;
		}

		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed4 tex1 = tex2D(_MainTex, IN.uv_MainTex);
			fixed4 tex2 = tex2D(_AltTex, IN.uv_MainTex);
			o.Albedo = (tex1.rgb * (1.0 - _Fade) + (tex2.rgb * tex2.a) * _Fade) * _Color.rgb;
			o.Alpha = _Color.a;
		}
		
		ENDCG
	}
}
