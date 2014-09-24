sampler sprite : register(s0);

/*
* Convert from BGRX to RGBA
*/
float4 BGR2RGB(float2 texCoord : TEXCOORD0) : COLOR0
{
	float4 tex = tex2D(sprite, texCoord);
	return float4(tex.b, tex.g, tex.r, 1.0);
}

technique Swap
{
	pass Swap
	{
		PixelShader = compile ps_2_0 BGR2RGB();
	}
}