//-----------------------------------------------------------------------
// Pixel Shader 2.0 source code for the ripple effect
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------
float2 center : register(C0);
float amplitude : register(C1);
float frequency: register(C2);
float phase: register(C3);

SamplerState ImageSampler : register(S0);

float4 PS(float2 uv : TEXCOORD) : COLOR
{
    float2 dir = uv - center;
    float2 toPixel = uv - center; // vector from center to pixel
    float distance = length(toPixel);
    float2 direction = toPixel/distance;
    float angle = atan2(direction.y, direction.x);
    float2 wave;

    sincos(frequency * distance + phase, wave.x, wave.y);
    	
    float falloff = saturate(1-distance);
    falloff *= falloff;

    distance += amplitude * wave.x * falloff;
    sincos(angle, direction.y, direction.x);
    float2 uv2 = center + distance * direction;

    float lighting = saturate(wave.y * falloff) * 0.2 + 0.8;

    float4 color = tex2D( ImageSampler, uv2 );
    color.rgb *= lighting;

    return color;
}
