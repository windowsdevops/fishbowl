float progress : register(C0);
float fuzzyAmount : register(C1);
sampler2D implicitInput : register(s0);
sampler2D oldInput : register(s1);

float4 Circle(float2 uv)
{
	float radius = -fuzzyAmount + progress * (0.70710678 + 2.0 * fuzzyAmount);
	float fromCenter = length(uv - float2(0.5,0.5));
	float distFromCircle = fromCenter - radius;
	
	float4 c1 = tex2D(oldInput, uv); 
    float4 c2 = tex2D(implicitInput, uv);
    
	float p = saturate((distFromCircle + fuzzyAmount) / (2.0 * fuzzyAmount));
	return lerp(c2, c1, p);
}

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------
float4 main(float2 uv : TEXCOORD0) : COLOR0
{
	return Circle(uv);
}

