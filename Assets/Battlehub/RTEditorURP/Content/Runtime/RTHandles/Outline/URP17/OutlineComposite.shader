Shader "Battlehub/URP17/OutlineComposite"
{
	Properties
	{
		_OutlineColor("OutlineColor", Color) = (1, .5, 0, 1)
	}
	SubShader
	{ 
		Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
		Pass
		{	
			Name "Composite"
			
			HLSLPROGRAM

			#include "Outline.hlsl"
			#pragma multi_compile_instancing
			#pragma vertex Vert
			#pragma fragment CompositePassFragment

			ENDHLSL
		}
	}
}
