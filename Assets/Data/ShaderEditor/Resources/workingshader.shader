Shader "ShaderEditor/EditorShaderCache"
{
	Properties 
	{
_Sampler2D0("_Sampler2D0", 2D) = "white" {}
_EditorTime("_EditorTime", Vector) = (0.0,0.0,0.0,0.0)

	}
	
	SubShader 
	{
		Tags
		{
"Queue"="Transparent"
"IgnoreProjector"="True"
"RenderType"="Transparent"

		}
		
		CGPROGRAM
			#pragma surface surf BlinnPhongEditor alpha
			
			struct EditorSurfaceOutput {
				half3 Albedo;
				half3 Normal;
				half3 Emission;
				half3 Gloss;
				half Specular;
				half Alpha;
			};
			
			inline half4 LightingBlinnPhongEditor (EditorSurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
			{
				#ifndef USING_DIRECTIONAL_LIGHT
				lightDir = normalize(lightDir);
				#endif
				viewDir = normalize(viewDir);
				half3 h = normalize (lightDir + viewDir);
				
				half diff = max (0, dot (s.Normal, lightDir));
				
				float nh = max (0, dot (s.Normal, h));
				float3 spec = pow (nh, s.Specular*128.0) * s.Gloss;
				
				half4 c;
				c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * (atten * 2);
				c.a = s.Alpha + _LightColor0.a * Luminance(spec) * atten;
				return c;
			}
			
			inline half4 LightingBlinnPhongEditor_PrePass (EditorSurfaceOutput s, half4 light)
			{
				half3 spec = light.a * s.Gloss;
				
				half4 c;
				c.rgb = (s.Albedo * light.rgb + light.rgb * spec);
				c.a = s.Alpha + Luminance(spec);
				return c;
			}
			
			struct Input {
float2 uv_Sampler2D0;

			};
			
sampler2D _Sampler2D0;
float4 _EditorTime;

			void surf (Input IN, inout EditorSurfaceOutput o) {
				o.Albedo = 0.0;
				o.Normal = float3(0.0,0.0,1.0);
				o.Emission = 0.0;
				o.Gloss = 0.0;
				o.Specular = 0.0;
				o.Alpha = 1.0;
float4 UV_Pan0=float4((IN.uv_Sampler2D0.xyxy).x + _EditorTime.x,(IN.uv_Sampler2D0.xyxy).y,(IN.uv_Sampler2D0.xyxy).z,(IN.uv_Sampler2D0.xyxy).w);
float4 Tex2D0=tex2D(_Sampler2D0,UV_Pan0.xy);
float4 Splat0=Tex2D0.w;
float4 Master0_Normal_NoInput = float4(0,0,1,1);
float4 Master0_Emission_NoInput = float4(0,0,0,0);
float4 Master0_Specular_NoInput = float4(0,0,0,0);
float4 Master0_Gloss_NoInput = float4(0,0,0,0);
o.Albedo = Tex2D0;
o.Normal = float3( 0.0, 0.0, 1.0);
o.Alpha = Splat0;

			}
		ENDCG
	}
	Fallback "Diffuse"
}