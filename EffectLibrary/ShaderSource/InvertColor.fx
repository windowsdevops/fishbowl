//-----------------------------------------------------------------------
// Pixel Shader 2.0 source code for the color invert effect
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------
SamplerState  ImageSampler : register(S0);

float4 PS( float2 uv : TEXCOORD) : COLOR
{
   float4 color = tex2D( ImageSampler, uv );
   float4 inverted_color = 1 - color;
   inverted_color.a = color.a;
   inverted_color.rgb *= inverted_color.a;
   return inverted_color;
}
