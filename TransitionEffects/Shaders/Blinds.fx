float progress : register(C0);
sampler2D implicitInput : register(s0);
sampler2D oldInput : register(s1);

float4 Blinds(float2 uv)
{		
	if(frac(uv.y * 5) < progress)
	{
		return tex2D(implicitInput, uv);
	}
	else
	{
		return tex2D(oldInput, uv);
	}
}

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------
float4 main(float2 uv : TEXCOORD0) : COLOR0
{
	return Blinds(uv);
}

