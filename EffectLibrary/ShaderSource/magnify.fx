//-----------------------------------------------------------------------
// Pixel Shader 2.0 source code for the magnifying effect
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------
float2 radii : register(C0);
float2 center : register(C1);
float  amount : register(C2);

SamplerState  ImageSampler : register(S0);

float4 PS( float2 uv : TEXCOORD) : COLOR
{
   float2 origUv = uv;
   float2 ray = origUv - center;
   float2 rt = ray / radii;
   float lengthRt = length(rt); // Outside of radii, we just show the regular image.  Radii is ellipse radii, so width x height radius 
   float2 texuv;

   if (lengthRt > 1)
   {
       texuv = origUv;
   }
   else
   {
       texuv = center + amount * ray;
   }
   
   return tex2D(ImageSampler, texuv);
}

