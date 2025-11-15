//-----------------------------------------------------------------------
// Pixel Shader 2.0 source code for the zooming blur effect
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------
float CenterX : register(C0);
float CenterY : register(C1);
float BlurAmount : register(C2);

SamplerState  ImageSampler : register(S0);

float4 PS(float2 uv : TEXCOORD) : COLOR
{
    float4 c = 0;
    float2 Center = {CenterX, CenterY};
    uv -= Center;

    [unroll]
    for(int i=0; i<15; i++)
    {
        float scale = 1.0 + BlurAmount * (i / 14.0);
        c += tex2D(ImageSampler, uv * scale + Center );
    }
   
    c /= 15;
    return c;
}