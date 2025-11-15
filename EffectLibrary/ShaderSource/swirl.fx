//-----------------------------------------------------------------------
// Pixel Shader 2.0 source code for the swirl effect
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------
float2 center : register(C0);
float spiralStrength : register(C1);
float2 angleFrequency : register(C2);

SamplerState ImageSampler : register(S0);

float4 PS(float2 uv : TEXCOORD) : COLOR
{
   float2 dir = uv - center;
   float l = length(dir);
   float angle = atan2(dir.y, dir.x);
   
   float newAng = angle + spiralStrength * l;
   float xAmt = cos(angleFrequency.x * newAng) * l;
   float yAmt = sin(angleFrequency.y * newAng) * l;
   
   float2 newCoord = center + float2(xAmt, yAmt);
   
   return tex2D( ImageSampler, newCoord );
}


