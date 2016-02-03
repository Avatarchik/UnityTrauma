Shader "Custom/ToonOutlineNoise" 
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

         Blend SrcAlpha OneMinusSrcAlpha
         Cull Front
         //ZWrite On
         ColorMask RGBA
         
         CGPROGRAM 
		#pragma exclude_renderers gles
		#pragma exclude_renderers xbox360
         #pragma vertex vert
         #pragma fragment frag
         #include "UnityCG.cginc"

         struct appdata { 
             float4 vertex : POSITION; 
             float3 normal : NORMAL; 
             float2 texcoord : TEXCOORD0;
         }; 

         struct v2f { 
            float4 pos : POSITION; 
            float4 color : COLOR;
            float2 time : TEXCOORD0;
         }; 
         uniform float _Outline; 
         uniform float4 _OutlineColor; 
         uniform sampler2D _Noise;

         v2f vert(appdata v) { 
            v2f o; 
            o.pos = mul(UNITY_MATRIX_MVP, v.vertex); 
            float3 norm = mul ((float3x3)UNITY_MATRIX_MV, v.normal); 
            float2 offset = TransformViewToProjection(norm.xy);

            o.pos.xy += offset * o.pos.z * _Outline; 

            o.color = _OutlineColor;
            o.time.x = v.texcoord.x;
            o.time.y = -_Time + o.pos.y * 100;
            return o; 
         } 
         
         half4 frag(v2f i) : COLOR
         {
			half4 color, color2;
			color2 = tex2D(_Noise, i.time);
			
			color = i.color;
			color.a = color2.r;
			return color;
         }

         ENDCG 
      } // End Pass
   } 

   Fallback "Unlit/Texture"
}