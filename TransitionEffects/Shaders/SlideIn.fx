float progress : register(C0);
float2 slideAmount : register(C1);
sampler2D implicitInput : register(s0);
sampler2D oldInput : register(s1);


float4 SlideIn(float2 uv)
{
	uv += slideAmount * progress;
	if(any(saturate(uv)-uv))
	{	
		uv = frac(uv);
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
	return SlideIn(uv);
}

