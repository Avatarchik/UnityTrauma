Shader "Custom/ToonOutline" 
{ 
   Properties 
   { 
      _OutlineColor ("Outline Color", Color) = (0,1,0,1) 
      _Outline ("Outline width", Range (0.002, 0.03)) = 0.01
      _Noise ("Base (RGB) Trans (A)", 2D) = "white" {}
   } 

   SubShader 
   { 
      Tags {"Queue"="Transparent" "RenderType"="Transparent"} 
      LOD 200
      
      Pass
      {
		Name "BASE"
		Cull Back
		Blend Zero One
		
		// uncomment this to hide inner details:
		Offset -20, -20
		
		SetTexture [_OutlineColor]
		{
			ConstantColor (0,0,0,0)
			Combine constant
		}
      }

      Pass 
      { 
         Name "OUTLINE" 
         Tags { "LightMode" = "Always" } 

         Blend One OneMinusDstColor
         Cull Front
         //ZWrite On
         //ColorMask RGBA
         
         CGPROGRAM 
// Upgrade NOTE: excluded shader from DX11 and Xbox360; has structs without semantics (struct appdata members vertex,normal)
#pragma exclude_renderers d3d11 xbox360
		#pragma exclude_renderers gles
		#pragma exclude_renderers xbox360
         #pragma vertex vert
         #pragma fragment frag
         #include "UnityCG.cginc"

         struct appdata { 
             float4 vertex; 
             float3 normal; 
         }; 

         struct v2f { 
            float4 pos : POSITION; 
            float4 color : COLOR; 
            //float fog : FOGC; 
         }; 
         uniform float _Outline; 
         uniform float4 _OutlineColor; 

         v2f vert(appdata v) { 
            v2f o; 
            o.pos = mul(UNITY_MATRIX_MVP, v.vertex); 
            float3 norm = mul ((float3x3)UNITY_MATRIX_MV, v.normal); 
            //norm.x *= UNITY_MATRIX_P[0][0]; 
            //norm.y *= UNITY_MATRIX_P[1][1]; 
            float2 offset = TransformViewToProjection(norm.xy);

            o.pos.xy += offset * o.pos.z * _Outline; 

            //o.fog = o.pos.z; 
            o.color = _OutlineColor; 
            return o; 
         } 
         
         half4 frag(v2f i) : COLOR
         {
			return i.color;
         }

         ENDCG 
      } // End Pass
   } 

   Fallback "Unlit/Texture"
 
}