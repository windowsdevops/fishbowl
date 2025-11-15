//-----------------------------------------------------------------------
// Pixel Shader 2.0 source code for the Bulge and Pinch effect
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------
float2 radii : register(C0);
float2 center : register(C1);
float2 bulgeMultiplier : register(C2);

SamplerState  ImageSampler : register(S0)
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

float2 unitMapper(float2 val)
{
    return bulgeMultiplier * (1-sqrt(1-val));  // bloat, w/ higher multiples extending further, lower magnifying.
}

float4 PS(float2 uv : TEXCOORD) : COLOR
{
   float2 origUv = uv;
   float2 ray = origUv - center;
   float2 rt = ray / radii;
   float2 texuv;
   // Outside of radii, we jus show the regular image.  Radii is ellipse radii, so width x height radius 
   float lengthRt = length(rt);

   if (lengthRt > 1)
   {
       texuv = origUv;
   }
   else
   {
       float len = length(ray);
       float2 unitLen = len / radii;
       float2 mappedUnitLen = unitMapper(unitLen);
       texuv = center + mappedUnitLen * ray / unitLen;
   }
   
   return tex2D(ImageSampler, texuv);
}

