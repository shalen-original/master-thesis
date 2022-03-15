using System;
using UnityEngine;
using static DoubleNumerics;

public class ProjectionHelper
{

    public struct CylinderProjectionInfo
    {
        public Vector3Double cylinderCenter;
        public Vector3Double projectionEyePoint;
        public double cylinderRadius;
    }

    public static void ProjectToCylinder(ref Vector3Double point, CylinderProjectionInfo info)
    {
        Vector3Double e = info.projectionEyePoint;
        Vector3Double m = info.cylinderCenter;
        double r = info.cylinderRadius;

        Vector3Double n = (point - e).normalized;

        double a = n.x * n.x + n.z * n.z;
        double b = 2 * (n.x * (e.x - m.x) + n.z * (e.z - m.z));
        double c = Math.Pow(e.x - m.x, 2) + Math.Pow(e.z - m.z, 2) - r * r;

        double delta = Math.Pow(b, 2) - 4 * a * c;

        if (delta >= 0)
        {
            double d1 = (-b + Math.Sqrt(delta)) / (2 * a);
            double d2 = (-b - Math.Sqrt(delta)) / (2 * a);

            var p1 = e + d1 * n;
            var p2 = e + d2 * n;

            // Pick the point of the cylinder that is "on the side" of the original point
            // `point` that we are projecting, that is, the one that is closer in
            // space to `point`
            point = (point - p1).sqrMagnitude < (point - p2).sqrMagnitude ? p1 : p2;
        }
        else
        {
            Debug.LogError("Delta < 0 for point " + point);
        }
    }
}
