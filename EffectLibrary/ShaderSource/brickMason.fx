//-----------------------------------------------------------------------
// Pixel Shader 2.0 source code for the brick laying effect
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------
float4 brickCounts : register(C0);
float4 mortarPixels : register(C1);
float4 mortarColor : register(C2);
float4 destinationSize : register(C6);

SamplerState  ImageSampler : register(S0)
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

float4 PS(float2 uv : TEXCOORD) : COLOR
{
   float2 texuv = uv;
   float4 brickSize = 1.0 / brickCounts;

   // Offset every other row of bricks
   float2 offsetuv = texuv;
   bool oddRow = floor(texuv.y / brickSize.y) % 2.0 >= 1.0;
   if (oddRow)
   {
       offsetuv.x += brickSize.x / 2.0;
   }
   
   float4 color;
   float4 pixelSize = mortarPixels / destinationSize;
   float2 modded = offsetuv % brickSize;
   if ( modded.x < pixelSize.x || modded.y < pixelSize.y)
   {
       color = mortarColor;
   }
   else
   {
       float2 brickNum = floor(offsetuv / brickSize);
       float2 centerOfBrick = brickNum * brickSize + brickSize / 2;
       color = tex2D(ImageSampler, centerOfBrick);
   }
   return color;
}


