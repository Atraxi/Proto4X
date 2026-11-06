// Hologram material for the geometry pass: build ghosts and blueprint previews.
// Unlit and alpha-blended, with rim brightening, scrolling scanlines and a slow pulse.
// Input is MonoGameLibrary.Graphics.MeshVertex: POSITION0, NORMAL0, COLOR0, TEXCOORD0.
// World, WorldViewProjection, CameraPosition and Time are bound by name by Material.

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 World;
float4x4 WorldViewProjection;
float3 CameraPosition;
float Time;

// Mixed under the vertex colour, so a red "can't place" ghost still reads red.
float3 Tint = float3(0.35, 0.82, 1.0);

struct VertexInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 WorldPosition : TEXCOORD0;
    float3 Normal : TEXCOORD1;
};

VertexOutput MainVS(in VertexInput input)
{
    VertexOutput output;
    output.Position = mul(input.Position, WorldViewProjection);
    output.WorldPosition = mul(input.Position, World).xyz;
    output.Normal = mul(input.Normal, (float3x3)World);
    output.Color = input.Color;
    return output;
}

float4 MainPS(VertexOutput input) : COLOR0
{
    float3 normal = normalize(input.Normal);
    float3 toCamera = normalize(CameraPosition - input.WorldPosition);
    float rim = 1.0 - abs(dot(normal, toCamera));
    float scan = 0.5 + 0.5 * sin(input.WorldPosition.z * 36.0 - Time * 5.0);
    float pulse = 0.85 + 0.15 * sin(Time * 2.5);

    float3 colour = lerp(Tint, input.Color.rgb, 0.65) * (0.7 + rim * 0.8);
    float alpha = saturate((0.22 + rim * 0.55 + scan * 0.12) * pulse);
    // Premultiplied, to match BlendState.AlphaBlend.
    return float4(colour * alpha, alpha);
}

technique Hologram
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
