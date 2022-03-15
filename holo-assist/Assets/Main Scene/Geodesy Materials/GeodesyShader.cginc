#ifndef __GEODESY_SHADER__
#define __GEODESY_SHADER__

#define WGS84_A 6378137.0
#define WGS84_B 6356752.3142

float4 wgs2ecef(float4 wgsRad) {
    float lambda = wgsRad.x;
    float phi = wgsRad.y;

    float f = (WGS84_A - WGS84_B) / WGS84_A;
    float eccentricitySquared = f * (2 - f);
    float N = WGS84_A / sqrt(1 - eccentricitySquared * pow(sin(lambda), 2));

    float4 ecef;
    ecef.x = (N + wgsRad.z) * cos(lambda) * cos(phi);
    ecef.y = (N + wgsRad.z) * cos(lambda) * sin(phi);
    ecef.z = ((1 - eccentricitySquared) * N + wgsRad.z) * sin(lambda);
    ecef.w = wgsRad.w;

    return ecef;
}

float4 ecef2enu(float4 pEcef, float4 enuOriginWgs) {
    float lambda = enuOriginWgs.x;
    float phi = enuOriginWgs.y;

    float4x4 rotations = {
        -sin(phi), cos(phi), 0, 0,
        -cos(phi) * sin(lambda), -sin(phi) * sin(lambda), cos(lambda), 0,
        cos(phi) * cos(lambda), sin(phi) * cos(lambda), sin(lambda), 0,
        0, 0, 0, 1
    };

    float4 enuOriginEcef = wgs2ecef(enuOriginWgs);
    float4 diff = pEcef - enuOriginEcef;
    diff.w = 1;

    float4 enu = mul(rotations, diff);
    return enu;
}

float4 enu2unity(float4 enu) {
    float4 unity;
    unity.x = enu.x;
    unity.y = enu.z;
    unity.z = enu.y;
    unity.w = enu.w;
    return unity;
}

#endif