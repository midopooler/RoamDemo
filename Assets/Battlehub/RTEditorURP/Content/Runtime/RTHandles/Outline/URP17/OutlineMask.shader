Shader "Battlehub/URP17/OutlineMask"
{
	Properties
	{
	}
	SubShader
	{
		Pass
		{
			HLSLPROGRAM

			#include "Outline.hlsl"
			
			#pragma multi_compile_instancing
			#pragma vertex MaskVertex
			#pragma fragment MaskFragment

			ENDHLSL
		}
	}
}
