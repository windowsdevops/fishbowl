float progress : register(C0);
float frequency : register(C1);
sampler2D implicitInput : register(s0);
sampler2D oldInput : register(s1);


float4 Ripple(float2 uv)
{
	float speed = 10;
	float amplitude = 0.05;
	float2 center = float2(0.5,0.5);
	float2 toUV = uv - center;
	float distanceFromCenter = length(toUV);
	float2 normToUV = toUV / distanceFromCenter;

	float wave = cos(frequency * distanceFromCenter - speed * progress);
	float offset1 = progress * wave * amplitude;
	float offset2 = (1.0 - progress) * wave * amplitude;
	
	float2 newUV1 = center + normToUV * (distanceFromCenter + offset1);
	float2 newUV2 = center + normToUV * (distanceFromCenter + offset2);
	
	float4 c1 = tex2D(oldInput, newUV1); 
    float4 c2 = tex2D(implicitInput, newUV2);

	return lerp(c1, c2, progress);
}

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------
float4 main(float2 uv : TEXCOORD0) : COLOR0
{
	return Ripple(uv);
}

