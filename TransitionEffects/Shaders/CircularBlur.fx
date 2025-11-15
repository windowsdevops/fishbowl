float progress : register(C0);
sampler2D implicitInput : register(s0);
sampler2D oldInput : register(s1);
sampler2D trigInput : register(s2);

float4 CircularBlur(float2 uv)
{
	float2 center = float2(0.5,0.5);
	float2 toUV = uv - center;
	float2 normToUV = toUV;
	float2 normTan = float2(-normToUV.y, normToUV.x);
	
	float4 c1 = float4(0,0,0,0);
	int count = 25;
	float s = progress * 0.02;
	
	for(int i=0; i<count; i++)
	{
		c1 += tex2D(oldInput, uv + normTan * s * i); 
	}
	
	c1 /= count;
    float4 c2 = tex2D(implicitInput, uv);

	return lerp(c1, c2, progress);
}

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------
float4 main(float2 uv : TEXCOORD0) : COLOR0
{
	return CircularBlur(uv);
}

