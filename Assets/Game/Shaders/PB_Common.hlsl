// Shared look-dev helpers: hash noise, fbm and the stylised sky gradient (the water reflects the same sky).
#ifndef LD_COMMON_INCLUDED
#define LD_COMMON_INCLUDED

float  _LDTime;
float4 _LDSkyTop, _LDSkyHorizon, _LDSkyBottom, _LDSunSky, _LDSkyMid; // rgb; _LDSunSky.a = sun disc size
float4 _LDSunDir;                                          // xyz towards the sun, w = glow power
float4 _LDAmbSky, _LDAmbEq, _LDAmbGround;                  // trilight ambient

float LDHash(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }

float LDNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = LDHash(i), b = LDHash(i + float2(1, 0)), c = LDHash(i + float2(0, 1)), d = LDHash(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float LDFbm(float2 p)
{
    float v = 0, a = 0.5;
    [unroll] for (int k = 0; k < 5; k++) { v += a * LDNoise(p); p = p * 2.03 + 17.1; a *= 0.5; }
    return v;
}

// The sky without the sun disc (the glow stays): what the sea reflects. A reflected disc made soft white blobs
// drift over the waves wherever a facet tilted to the sun; the sea draws its own glint instead.
float3 LDSkyNoDisc(float3 dir)
{
    float y = dir.y;
    float3 mid = _LDSkyMid.a > 0 ? _LDSkyMid.rgb : lerp(_LDSkyHorizon.rgb, _LDSkyTop.rgb, 0.5);
    float3 c = y > 0 ? lerp(lerp(_LDSkyHorizon.rgb, mid, smoothstep(0.0, 0.16, y)), _LDSkyTop.rgb, smoothstep(0.16, 0.75, y)) // (the sea keeps its reflection as it was: look audit 3 Nov, "keep the sea colour")
                     : lerp(_LDSkyHorizon.rgb, _LDSkyBottom.rgb, saturate(-y * 6));
    float s = saturate(dot(dir, normalize(_LDSunDir.xyz)));
    return c + _LDSunSky.rgb * pow(s, max(_LDSunDir.w, 1)) * 0.6;
}

float3 LDSkyGradient(float3 dir)
{
    float y = dir.y;
    float3 mid = _LDSkyMid.a > 0 ? _LDSkyMid.rgb : lerp(_LDSkyHorizon.rgb, _LDSkyTop.rgb, 0.5);
    float3 c = y > 0 ? lerp(lerp(_LDSkyHorizon.rgb, mid, smoothstep(0.0, 0.05, y)), _LDSkyTop.rgb, smoothstep(0.05, 0.16, y)) // the cream only in the bottom ~3 deg, the full Ponyo blue from ~9 deg up (look audit 3 Nov: it came only at ~50 deg, above anything the game camera frames)
                     : lerp(_LDSkyHorizon.rgb, _LDSkyBottom.rgb, saturate(-y * 6));
    float s = saturate(dot(dir, normalize(_LDSunDir.xyz)));
    c += _LDSunSky.rgb * pow(s, max(_LDSunDir.w, 1)) * 0.6;             // glow around the sun
    c += _LDSunSky.rgb * smoothstep(1 - _LDSunSky.a, 1 - _LDSunSky.a * 0.6, s) * 3; // disc
    return c;
}

float3 LDAmbient(float3 n)
{
    return n.y > 0 ? lerp(_LDAmbEq.rgb, _LDAmbSky.rgb, n.y) : lerp(_LDAmbEq.rgb, _LDAmbGround.rgb, -n.y);
}

#endif
