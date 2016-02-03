Shader "Custom/Hit"
{
	Properties 
	{
		_Color ("Main Color", Color) = (1, 1, 1, 1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_HitMap ("Hit Map", 2D) = "white" {}
	}
	
	SubShader
	{
		Tags { "RenderType"="Opaque" "HitRender"="Hit" }
		LOD 400
		
		Pass
		{
			Lighting Off
			SetTexture [_HitMap] { combine texture }
		}
	}
}
