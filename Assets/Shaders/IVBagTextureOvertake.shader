Shader "Custom/TextureOvertake" {
Properties {
	_OvertakePercent ("OvertakePercent", Float) = 0.0
	_Color ("Main Color", Color) = (1,1,1,1)
	_Strength ("Reflect Strength", Color) = (0,0,0,0)
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_SecondTex ("Base (RGB) Trans (A)", 2D) = "red" {}
	_Cube ("Reflection Cubemap", Cube) = "_Skybox" { TexGen CubeReflect }
}

SubShader {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	LOD 200

	Pass {
	Blend SrcAlpha OneMinusSrcAlpha

	CGPROGRAM
	#pragma exclude_renderers gles
	#pragma vertex vert
	#pragma fragment frag
	#include "UnityCG.cginc"

		float _OvertakePercent;
		float4 _Color;
		float4 _Strength;
		sampler2D _MainTex;
		sampler2D _SecondTex;
		samplerCUBE _Cube;

		struct appdata {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float2 texcoord : TEXCOORD0;
		};

		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float3 reflection : TEXCOORD1;
		};
		
		struct Input {
			float2 uv_MainTex;
			float3 worldRefl;
		};

		v2f vert (appdata v)
		{
			v2f o;
			o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
			o.uv = float2(v.texcoord.xy);
			
			// Create reflection
			float3 normalDirection;
			float3 viewDirection;
			
			normalDirection = normalize(mul(float4(v.normal,0.0), _World2Object));
			viewDirection = float3(mul(_Object2World, v.vertex) - float4(_WorldSpaceCameraPos, 1.0));			
			o.reflection = reflect(viewDirection, normalize(normalDirection));
			
			return o;
		}

		half4 frag (v2f i) : COLOR
		{
			half4 o;
			float temp = 1 - i.uv.y;
			if(temp >= _OvertakePercent)
				o = tex2D(_MainTex, i.uv);
			else
				o = tex2D(_SecondTex, i.uv);
			o = o * _Color;
			
			o = o + texCUBE(_Cube, i.reflection) * _Strength;
			
			return o;
		}
		
		ENDCG
	}
}
}