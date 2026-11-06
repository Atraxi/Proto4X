// Procedural starfield, drawn as a full-screen background through FullscreenPass.
// Pixel shader only: SpriteBatch's vertex shader supplies position, colour and uv.
// Three hashed star layers, each offset by the camera's ground position times its own
// parallax factor, so near layers slide faster than far ones as the view pans.

#if OPENGL
    #define SV_POSITION POSITION
    #define PS_SHADERMODEL ps_3_0
#else
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 Resolution;
float Time;

// Camera ground position (tiles) scaled to pixels by the game.
float2 CameraOffset;

struct VertexOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float Hash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float3 StarLayer(float2 pixel, float2 offset, float cell, float density, float brightness)
{
    float2 p = (pixel + offset) / cell;
    float2 id = floor(p);
    float2 local = frac(p) - 0.5;
    float h = Hash(id);
    float present = step(1.0 - density, h);
    float2 star = float2(Hash(id + 1.7), Hash(id + 3.1)) - 0.5;
    float distance = length(local - star * 0.7);
    float twinkle = 0.7 + 0.3 * sin(Time * (1.0 + h * 3.0) + h * 40.0);
    float glow = 1.0 - smoothstep(0.0, 0.07, distance);
    float3 colour = lerp(float3(0.7, 0.8, 1.0), float3(1.0, 0.86, 0.7), Hash(id + 9.2));
    return colour * glow * brightness * twinkle * present;
}

float4 MainPS(VertexOutput input) : COLOR0
{
    float2 pixel = input.TextureCoordinates * Resolution;
    float3 colour = float3(0.03, 0.035, 0.06) + float3(0.02, 0.015, 0.04) * input.TextureCoordinates.y;
    colour += StarLayer(pixel, CameraOffset * 0.25, 96.0, 0.25, 0.55);
    colour += StarLayer(pixel, CameraOffset * 0.6, 64.0, 0.18, 0.8);
    colour += StarLayer(pixel, CameraOffset * 1.2, 44.0, 0.08, 1.0);
    return float4(colour, 1.0);
}

technique Starfield
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
