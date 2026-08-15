float TessellationFactor = 8.0;

struct VertexInput
{
    float4 Position : POSITION0;
};

struct ControlPoint
{
    float4 Position : POSITION0;
};

struct PatchConstants
{
    float TessFactors[2] : SV_TessFactor;
};

struct DomainOutput
{
    float4 Position : SV_POSITION;
};

ControlPoint VertexShaderFunction(VertexInput input)
{
    ControlPoint output;
    output.Position = input.Position;
    return output;
}

PatchConstants PatchConstantFunction(InputPatch<ControlPoint, 4> patch)
{
    PatchConstants output;
    output.TessFactors[0] = TessellationFactor;
    output.TessFactors[1] = 1.0;
    return output;
}

[domain("isoline")]
[partitioning("fractional_even")]
[outputtopology("line")]
[outputcontrolpoints(4)]
[patchconstantfunc("PatchConstantFunction")]
[maxtessfactor(64.0)]
ControlPoint HullShaderFunction(
    InputPatch<ControlPoint, 4> patch,
    uint controlPointId : SV_OutputControlPointID)
{
    return patch[controlPointId];
}

[domain("isoline")]
DomainOutput DomainShaderFunction(
    PatchConstants constants,
    float2 domainLocation : SV_DomainLocation,
    const OutputPatch<ControlPoint, 4> patch)
{
    float t = domainLocation.x;
    float oneMinusT = 1.0 - t;

    DomainOutput output;
    output.Position =
        oneMinusT * oneMinusT * oneMinusT * patch[0].Position +
        3.0 * oneMinusT * oneMinusT * t * patch[1].Position +
        3.0 * oneMinusT * t * t * patch[2].Position +
        t * t * t * patch[3].Position;
    return output;
}

float4 PixelShaderFunction(DomainOutput input) : SV_TARGET
{
    return float4(1.0, 1.0, 1.0, 1.0);
}

technique Tessellation
{
    pass Pass1
    {
        VertexShader = compile vs_5_0 VertexShaderFunction();
        HullShader = compile hs_5_0 HullShaderFunction();
        DomainShader = compile ds_5_0 DomainShaderFunction();
        PixelShader = compile ps_5_0 PixelShaderFunction();
    }
}
