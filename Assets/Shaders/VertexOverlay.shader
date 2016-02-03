shader "Custom/vertexOverlay"
{
	Properties{
		_MainTex("Diffuse Texture Gloss (A)",2D) = "white" {} // load in diffuse texture
		_BumpMap ("NormalSpec Texture", 2D) = "bump" {} //load in normal map
		_vertStrength ("Vert Color Strength", Range (0.0,2.0)) = 1.0 //float to determine vertex color blend strength
		_Specular ("Specular Exponent", Range (0,1)) = 0.5 //specular exponent
		_SpecColor ("Specular Color", Color) = (1,1,1,1) //specular color
	}
	SubShader{
		Tags { "RenderType" = "Opaque"} //render before transparent objects
		
		CGPROGRAM
		#pragma surface surf BlinnPhong vertex:vert
		//we specify vert as our vertex part of the surface shader, we don't need a fragment (pixel) part as it's a surface shader
			sampler2D _MainTex; //sample diffuse tex
			sampler2D _BumpMap; //sample normal text
			float _vertStrength; //sample vert strength
			float _Specular; // sample specular exponent
			struct Input {
				float2 uv_MainTex; //grap the uv map into the input
				float3 vertColors; //grap teh vert colors into the input
			};
			
			void vert (inout appdata_full v, out Input o) {
				o.vertColors = v.color.rgb; //grab the vertex colors from the appdata
				o.uv_MainTex = v.texcoord; //passtexture coordinates to surface program
			}
			void surf (Input IN, inout SurfaceOutput o) {
				half4 tex = tex2D (_MainTex, IN.uv_MainTex);
				tex.rgb = lerp (tex.rgb,tex.rgb*(IN.vertColors.rgb*2),_vertStrength); //blend the vertex colors over the diffuse
				half4 normalMap = tex2D (_BumpMap, IN.uv_MainTex);
				o.Albedo = tex.rgb;
				o.Specular = _Specular;
				o.Normal = UnpackNormal (normalMap);
				o.Gloss = tex.a;
			}
			ENDCG
		}
		Fallback "Specular"
}
				
		