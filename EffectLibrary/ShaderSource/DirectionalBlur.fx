//-----------------------------------------------------------------------
// Pixel Shader 2.0 source code for the directional blur effect
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------

float Angle : register(C0);  // Defines the blurring direction
float BlurAmount : register(C1);  // Defines the blurring magnitude
sampler2D ImageSampler : register(S0);  // Defines the image/texture to be blurred

float4 PS( float2 uv : TEXCOORD) : COLOR
{
    float4 output = 0;  // Defines the output color of a pixel
    float2 offset;  // Defines the blurring direction as a direction vector
    int count = 24;  // Defines the number of blurring iterations
    
    // First compute a direction vector which defines the direction of blurring. This is
    // done using the sincos instruction and the Angle input parameter, and the result
    // is stored in the variable offset. This vector is of unit length. Multiply this 
    // unit vector by BlurAmount to adjust its length to reflect the blurring magnitude.
    sincos(Angle, offset.y, offset.x);
    offset *= BlurAmount;
    
    // To generate the blurred image, we generate multiple copies of the input image, shifted according
    // to the blurring direction vector, and then sum them all up to get the final image.
    for(int i=0; i<count; i++)
    {
        output += tex2D(ImageSampler, uv - offset * i);
    }
    output /= count; // Normalize the color
    
    return output;
}