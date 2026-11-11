// Post-process: a light saturation and contrast curve plus a vignette, applied to the
// rendered scene through PostProcessor. The scene texture arrives in sampler register 0
// from SpriteBatch; the vertex shader is our own (see MainVS).

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Pixels to clip space, set by FullscreenPass.
float4x4 MatrixTransform;

Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

float Saturation = 1.12;
float Contrast = 1.06;
float Vignette = 0.9;

struct VertexInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

// Our own vertex shader rather than SpriteBatch's: on DesktopGL a pixel-only effect's inputs
// didn't line up with SpriteBatch's outputs, so the uv arrived constant and the frame was flat.
VertexOutput MainVS(VertexInput input)
{
    VertexOutput output;
    output.Position = mul(input.Position, MatrixTransform);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float4 MainPS(VertexOutput input) : COLOR0
{
    float4 scene = tex2D(SpriteTextureSampler, input.TextureCoordinates);
    float luma = dot(scene.rgb, float3(0.299, 0.587, 0.114));
    float3 graded = lerp(float3(luma, luma, luma), scene.rgb, Saturation);
    graded = (graded - 0.5) * Contrast + 0.5;
    float2 fromCentre = input.TextureCoordinates - 0.5;
    graded *= 1.0 - dot(fromCentre, fromCentre) * Vignette;
    return float4(saturate(graded), 1.0);
}

technique Grade
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
