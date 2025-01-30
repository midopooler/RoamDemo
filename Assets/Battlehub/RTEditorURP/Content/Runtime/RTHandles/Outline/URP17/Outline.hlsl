#ifndef BATTLEHUB_URP_OUTLINE_INCLUDED
#define BATTLEHUB_URP_OUTLINE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

struct MaskVertexInput {
	float4 pos : POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct MaskVertexOutput {
	float4 clipPos : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

MaskVertexOutput MaskVertex(MaskVertexInput input) {
	MaskVertexOutput output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
	output.clipPos = TransformObjectToHClip(input.pos.xyz);
	return output;
}

float4 MaskFragment(MaskVertexOutput input) : SV_TARGET{

	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	return float4(1, 0, 0, 1);
}

float _BlurStrength;
float _OutlineStrength;

static const float4 kCurveWeights[9] = {
	float4(0.0204001988,0.0204001988,0.0204001988,0),
	float4(0.0577929595,0.0577929595,0.0577929595,0),
	float4(0.1215916882,0.1215916882,0.1215916882,0),
	float4(0.1899858519,0.1899858519,0.1899858519,0),
	float4(0.2204586031,0.2204586031,0.2204586031,1),
	float4(0.1899858519,0.1899858519,0.1899858519,0),
	float4(0.1215916882,0.1215916882,0.1215916882,0),
	float4(0.0577929595,0.0577929595,0.0577929595,0),
	float4(0.0204001988,0.0204001988,0.0204001988,0)
};

float4 Sample(float2 uv, float2 offset)
{
#if UNITY_VERSION >= 600000
	float2 step = _BlitTexture_TexelSize.xy * offset;
#else
	float2 step = float2(0, 0);
#endif

	uv = uv - step * 4;
	float4 col = 0;
	for (int tap = 0; tap < 9; ++tap)
	{
		col += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv) * kCurveWeights[tap];
		uv += step;
	}
	
	return col * _OutlineStrength;
}

float4 BlurVerticalPassFragment(Varyings input) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
	return Sample(uv, float2(0, _BlurStrength));
}

float4 BlurHorizontalPassFragment(Varyings input) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
	return Sample(uv, float2(_BlurStrength, 0));
}

TEXTURE2D_X(_MaskTex);
SAMPLER(sampler_MaskTex);

TEXTURE2D_X(_BlurTex);
SAMPLER(sampler_BlurTex);

float4 _OutlineColor;

float4 CompositePassFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
	float4 glow = max(0, SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, uv) - SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv));
	float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
	
	return lerp(col, _OutlineColor, glow.r);
}

#endif 