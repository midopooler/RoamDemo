Shader "Battlehub/URP17/OutlineBlur"
{
	Properties
	{
		_BlurStrength("BlurStrength", Float) = 1
		_OutlineStrength("OutlineStrength", Float) = 5
		
	}
	SubShader
	{ 
		Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
		Pass
		{	
			
			Name "VerticalBlur"
			
			HLSLPROGRAM

			#include "Outline.hlsl"
			#pragma multi_compile_instancing
			#pragma vertex Vert
			#pragma fragment BlurVerticalPassFragment

			ENDHLSL
		}

		Pass
		{	
			
			Name "HorizontalBlur"
			
			HLSLPROGRAM

			#include "Outline.hlsl"
			#pragma multi_compile_instancing
			#pragma vertex Vert
			#pragma fragment BlurHorizontalPassFragment

			ENDHLSL
		}
	}
}
