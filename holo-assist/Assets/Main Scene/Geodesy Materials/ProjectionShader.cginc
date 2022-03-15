#ifndef __PROJECTION_SHADER__
#define __PROJECTION_SHADER__

float distSquared(float4 a, float4 b) {
    float4 d = a - b;
    return dot(d, d);
}

float4 projectToCylinder(float4 pUnity, float4 projectionCenterInObjectSpace, float4 cylinderCenterInObjectSpace, float cylinderRadius) {
    float4 e = projectionCenterInObjectSpace;
    float4 m = cylinderCenterInObjectSpace;
    float r = cylinderRadius;

    float4 n = normalize(pUnity - e);

    float a = n.x * n.x + n.z * n.z;
    float b = 2 * (n.x * (e.x - m.x) + n.z * (e.z - m.z));
    float c = pow(e.x - m.x, 2) + pow(e.z - m.z, 2) - r * r;

    // Delta should always be >= 0 except for points
    // that lie on the cylinder axis. Moreover, it will
    // most likely behave "wierdly" from the simulator
    // perspective if you try to project any point that
    // is inside the cylinder. At the moment this is good enough
    // TODO maybe find something better
    float delta = pow(b, 2) - 4 * a * c;

    float d1 = (-b + sqrt(delta)) / (2 * a);
    float d2 = (-b - sqrt(delta)) / (2 * a);

    float4 p1 = e + d1 * n;
    float4 p2 = e + d2 * n;

    // Pick the point of the cylinder that is "on the side" of the original point
    // `pUnity` that we are projecting, that is, the one that is closer in
    // space to `pUnity`
    return distSquared(pUnity, p1) < distSquared(pUnity, p2) ? p1 : p2;
}

#endif