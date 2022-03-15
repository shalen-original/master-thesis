using System;
using UnityEngine;
using static DoubleNumerics;

/*
    Implements all the conversion between different geodetic coordinate systems.
*/

public class GeodesyHelper
{
    private const double WGS84_ELLIPSOID_MAJOR_SEMI_AXIS = 6378137.0;
    private const double WGS84_ELLIPSOID_MINOR_SEMI_AXIS = 6356752.3142;
    private const double WGS84_FLATTENING = 1 / 298.257223563;

    /// <summary>
    /// Represents a point in the WGS84 coordianate system.
    /// </summary>
    public struct WGS84Point
    {
        public double LatitudeRadians;
        public double LongitudeRadians;
        public double AltitudeMeters;

        public static WGS84Point RawFrom(Vector3 v)
        {
            WGS84Point p;
            p.LatitudeRadians = v.x;
            p.LongitudeRadians = v.y;
            p.AltitudeMeters = v.z;
            return p;
        }

        public Vector3 RawToVector3()
        {
            Vector3 v;
            v.x = (float) LatitudeRadians;
            v.y = (float) LongitudeRadians;
            v.z = (float) AltitudeMeters;
            return v;
        }

        public ECEFPoint ToECEF()
        {
            var phi = LatitudeRadians;
            var lambda = LongitudeRadians;

            var f = (WGS84_ELLIPSOID_MAJOR_SEMI_AXIS - WGS84_ELLIPSOID_MINOR_SEMI_AXIS) / WGS84_ELLIPSOID_MAJOR_SEMI_AXIS;
            var eccentricitySquared = f * (2 - f);
            var N = WGS84_ELLIPSOID_MAJOR_SEMI_AXIS / Math.Sqrt(1 - eccentricitySquared * Math.Pow(Math.Sin(phi), 2));

            ECEFPoint ans;
            ans.xMeters = (N + AltitudeMeters) * Math.Cos(phi) * Math.Cos(lambda);
            ans.yMeters = (N + AltitudeMeters) * Math.Cos(phi) * Math.Sin(lambda);
            ans.zMeters = ((1 - eccentricitySquared) * N + AltitudeMeters) * Math.Sin(phi);
            return ans;
        }

        public Vector3Double ToUnity(WGS84Point enuOrigin)
        {
            return ToECEF().ToENU(enuOrigin).ToUnity();
        }

        public override string ToString()
        {
            return $"WGS84Point(lat = {LatitudeRadians}rad, lon = {LongitudeRadians}rad, alt = {AltitudeMeters}m";
        }
    }

    /// <summary>
    /// Represents a point in the <see href="https://en.wikipedia.org/wiki/ECEF">ECEF (Earth-Centered, Earth-Fixed)</see> cartesian coordinate system. 
    /// </summary>
    public struct ECEFPoint
    {
        public double xMeters;
        public double yMeters;
        public double zMeters;

        public ENUPoint ToENU(WGS84Point enuOrigin)
        {
            var phi = enuOrigin.LatitudeRadians;
            var lambda = enuOrigin.LongitudeRadians;

            var rotations = new double[,]
            {
                { -Math.Sin(lambda),  Math.Cos(lambda), 0, 0 },
                { -Math.Cos(lambda) * Math.Sin(phi),  -Math.Sin(lambda) * Math.Sin(phi), Math.Cos(phi), 0 },
                { Math.Cos(lambda) * Math.Cos(phi),  Math.Sin(lambda) * Math.Cos(phi), Math.Sin(phi), 0 },
                { 0, 0, 0, 1 }
            };

            var enuOriginEcef = enuOrigin.ToECEF();

            var diff = new double[,]
            {
                { xMeters - enuOriginEcef.xMeters },
                { yMeters - enuOriginEcef.yMeters },
                { zMeters - enuOriginEcef.zMeters },
                { 1 }
            };

            var enu = MultiplyDoubleMatrix(rotations, diff);

            ENUPoint ans;
            ans.EastMeters = enu[0,0];
            ans.NorthMeters = enu[1,0];
            ans.UpMeters = enu[2,0];
            return ans;
        }

        public Vector3 RawToVector3()
        {
            Vector3 v;
            v.x = (float)xMeters;
            v.y = (float)yMeters;
            v.z = (float)zMeters;
            return v;
        }

        public Vector3Double RawToVector3Double()
        {
            Vector3Double v;
            v.x = xMeters;
            v.y = yMeters;
            v.z = zMeters;
            return v;
        }

        public static ECEFPoint RawFrom(Vector3Double v)
        {
            ECEFPoint p;
            p.xMeters = v.x;
            p.yMeters = v.y;
            p.zMeters = v.z;
            return p;
        }

        public override string ToString()
        {
            return $"ECEFPoint(x = {xMeters}m, y = {yMeters}m, z = {zMeters}m";
        }
    }

    /// <summary>
    /// Represents a point in the 
    /// <see href="https://en.wikipedia.org/wiki/Local_tangent_plane_coordinates">
    ///     ENU (East, North, Up) local tangent plane
    /// </see> 
    /// coordinate system.
    /// </summary>
    public struct ENUPoint
    {
        public double EastMeters;
        public double NorthMeters;
        public double UpMeters;

        public Vector3Double ToUnity()
        {
            Vector3Double ans;
            ans.x = EastMeters;
            ans.y = UpMeters;
            ans.z = NorthMeters;
            return ans;
        }

        public ECEFPoint ToECEF(WGS84Point enuOrigin)
        {
            var phi = enuOrigin.LatitudeRadians;
            var lambda = enuOrigin.LongitudeRadians;

            // Rotation matrices are orthogonal, R^-1 = R^T
            var inversOfEcefRotations = new double[,]
            {
                { -Math.Sin(lambda), -Math.Sin(phi) * Math.Cos(lambda), Math.Cos(phi) * Math.Cos(lambda), 0 },
                { Math.Cos(lambda), -Math.Sin(phi) * Math.Sin(lambda), Math.Cos(phi) * Math.Sin(lambda), 0 },
                { 0, Math.Cos(phi), Math.Sin(phi), 0 },
                { 0, 0, 0, 1 }
            };

            var enu = new double[,]
            {
                { EastMeters },
                { NorthMeters },
                { UpMeters },
                { 1 }
            };

            var ecef = MultiplyDoubleMatrix(inversOfEcefRotations, enu);

            var enuOriginEcef = enuOrigin.ToECEF();

            ECEFPoint ans;
            ans.xMeters = enuOriginEcef.xMeters + ecef[0, 0];
            ans.yMeters = enuOriginEcef.yMeters + ecef[1, 0];
            ans.zMeters = enuOriginEcef.zMeters + ecef[2, 0];
            return ans;
        }

        public static ENUPoint FromUnity(Vector3Double v)
        {
            ENUPoint ans;
            ans.EastMeters = v.x;
            ans.UpMeters = v.y;
            ans.NorthMeters = v.z;
            return ans;
        }

        public override string ToString()
        {
            return $"ENUPoint(east = {EastMeters}m, north = {NorthMeters}m, up = {UpMeters}m";
        }
    }

    /// <summary>
    /// Computes the great circle segment between two points.
    /// It uses the <see href="http://www.movable-type.co.uk/scripts/latlong-vincenty.html">Vincenty calculation</see>,
    /// which should be accurate up to 0.5mm of distance. However, this method does not account for height with 
    /// respect to the WGS84 ellipsoid, leading to a less precise result.
    /// </summary>
    public static WGS84GreatCircleSegment GreatCircleSegmentBetween(WGS84Point p1, WGS84Point p2)
    {
        // TODO This doesn't accout for height wrt the WGS84 ellipsoid.

        double phi1 = p1.LatitudeRadians, lambda1 = p1.LongitudeRadians;
        double phi2 = p2.LatitudeRadians, lambda2 = p2.LongitudeRadians;

        double a = WGS84_ELLIPSOID_MAJOR_SEMI_AXIS;
        double b = WGS84_ELLIPSOID_MINOR_SEMI_AXIS;
        double f = WGS84_FLATTENING;

        double L = lambda2 - lambda1;
        double tanU1 = (1 - f) * Math.Tan(phi1), cosU1 = 1 / Math.Sqrt((1 + tanU1 * tanU1)), sinU1 = tanU1 * cosU1;
        double tanU2 = (1 - f) * Math.Tan(phi2), cosU2 = 1 / Math.Sqrt((1 + tanU2 * tanU2)), sinU2 = tanU2 * cosU2;

        bool antipodal = Math.Abs(L) > Math.PI / 2 || Math.Abs(phi2 - phi1) > Math.PI / 2;

        double lambda = L, sinlambda, coslambda ;
        double sigma = antipodal ? Math.PI : 0, sinsigma = 0, cossigma = antipodal ? -1 : 1, sinSqsigma;
        double cos2sigmaM = 1;
        double sinalpha, cosSqalpha = 1;
        double C;

        double lambdaPrime, iterations = 0;
        do
        {
            sinlambda = Math.Sin(lambda);
            coslambda = Math.Cos(lambda);
            sinSqsigma = (cosU2 * sinlambda) * (cosU2 * sinlambda) + (cosU1 * sinU2 - sinU1 * cosU2 * coslambda) * (cosU1 * sinU2 - sinU1 * cosU2 * coslambda);
            if (Math.Abs(sinSqsigma) < Double.Epsilon) break;
            sinsigma = Math.Sqrt(sinSqsigma);
            cossigma = sinU1 * sinU2 + cosU1 * cosU2 * coslambda;
            sigma = Math.Atan2(sinsigma, cossigma);
            sinalpha = cosU1 * cosU2 * sinlambda / sinsigma;
            cosSqalpha = 1 - sinalpha * sinalpha;
            cos2sigmaM = (cosSqalpha != 0) ? (cossigma - 2 * sinU1 * sinU2 / cosSqalpha) : 0;
            C = f / 16 * cosSqalpha * (4 + f * (4 - 3 * cosSqalpha));
            lambdaPrime = lambda;
            lambda = L + (1 - C) * f * sinalpha * (sigma + C * sinsigma * (cos2sigmaM + C * cossigma * (-1 + 2 * cos2sigmaM * cos2sigmaM)));
            double iterationCheck = antipodal ? Math.Abs(lambda) - Math.PI : Math.Abs(lambda);
            if (iterationCheck > Math.PI) throw new Exception("lambda > Math.PI");
        } while (Math.Abs(lambda - lambdaPrime) > 1e-12 && ++iterations < 1000);

        double uSq = cosSqalpha * (a * a - b * b) / (b * b);
        double A = 1 + uSq / 16384 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
        double B = uSq / 1024 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));
        double deltasigma = B * sinsigma * (cos2sigmaM + B / 4 * (cossigma * (-1 + 2 * cos2sigmaM * cos2sigmaM) -
            B / 6 * cos2sigmaM * (-3 + 4 * sinsigma * sinsigma) * (-3 + 4 * cos2sigmaM * cos2sigmaM)));

        double distance = b * A * (sigma - deltasigma);
        double alpha1 = Math.Abs(sinSqsigma) < Double.Epsilon ? 0 : Math.Atan2(cosU2 * sinlambda, cosU1 * sinU2 - sinU1 * cosU2 * coslambda);
        double initialBearingRadians = Math.Abs(distance) < Double.Epsilon ? Double.NaN : alpha1;

        WGS84GreatCircleSegment ans;
        ans.distanceMeters = distance;
        ans.initialBearingRadians = initialBearingRadians;
        return ans;
    }

    /// <summary>
    /// Computes the point that is reached by starting at a certain point on the WGS84 ellipsoid
    /// and moving along a great circle segment.
    /// It uses the <see href="http://www.movable-type.co.uk/scripts/latlong-vincenty.html">Vincenty calculation</see>,
    /// which should be accurate up to 0.5mm of distance. However, this method does not account for height with 
    /// respect to the WGS84 ellipsoid, leading to a less precise result.
    /// </summary>
    public static WGS84Point GreatCircleMove(WGS84Point p, WGS84GreatCircleSegment segment)
    {
        // TODO This doesn't accout for height wrt the WGS84 ellipsoid.

        double phi1 = p.LatitudeRadians, lambda1 = p.LongitudeRadians;
        double alpha1 = segment.initialBearingRadians;
        double s = segment.distanceMeters;

        double a = WGS84_ELLIPSOID_MAJOR_SEMI_AXIS;
        double b = WGS84_ELLIPSOID_MINOR_SEMI_AXIS;
        double f = WGS84_FLATTENING;

        double sinalpha1 = Math.Sin(alpha1);
        double cosalpha1 = Math.Cos(alpha1);

        double tanU1 = (1 - f) * Math.Tan(phi1), cosU1 = 1 / Math.Sqrt((1 + tanU1 * tanU1)), sinU1 = tanU1 * cosU1;
        double sigma1 = Math.Atan2(tanU1, cosalpha1);
        double sinalpha = cosU1 * sinalpha1;
        double cosSqalpha = 1 - sinalpha * sinalpha;
        double uSq = cosSqalpha * (a * a - b * b) / (b * b);
        double A = 1 + uSq / 16384 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
        double B = uSq / 1024 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));

        double sigma = s / (b * A), sinsigma, cossigma, deltasigma;
        double cos2sigmaM;
        double sigmaPrime;
        do
        {
            cos2sigmaM = Math.Cos(2 * sigma1 + sigma);
            sinsigma = Math.Sin(sigma);
            cossigma = Math.Cos(sigma);
            deltasigma = B * sinsigma * (cos2sigmaM + B / 4 * (cossigma * (-1 + 2 * cos2sigmaM * cos2sigmaM) -
                B / 6 * cos2sigmaM * (-3 + 4 * sinsigma * sinsigma) * (-3 + 4 * cos2sigmaM * cos2sigmaM)));
            sigmaPrime = sigma;
            sigma = s / (b * A) + deltasigma;
        } while (Math.Abs(sigma - sigmaPrime) > 1e-12);

        double x = sinU1 * sinsigma - cosU1 * cossigma * cosalpha1;
        double phi2 = Math.Atan2(sinU1 * cossigma + cosU1 * sinsigma * cosalpha1, (1 - f) * Math.Sqrt(sinalpha * sinalpha + x * x));
        double lambda = Math.Atan2(sinsigma * sinalpha1, cosU1 * cossigma - sinU1 * sinsigma * cosalpha1);
        double C = f / 16 * cosSqalpha * (4 + f * (4 - 3 * cosSqalpha));
        double L = lambda - (1 - C) * f * sinalpha * (sigma + C * sinsigma * (cos2sigmaM + C * cossigma * (-1 + 2 * cos2sigmaM * cos2sigmaM)));
        double lambda2 = lambda1 + L;

        double alpha2 = Math.Atan2(sinalpha, -x);

        WGS84Point ans;
        ans.LatitudeRadians = phi2;
        ans.LongitudeRadians = lambda2;
        ans.AltitudeMeters = 0;
        return ans;
    }

    /// <summary>
    /// Splits the great circle segment between `p1` and `p2` in chunks with length of
    /// at most `maxChunkLengthMeters`.
    /// </summary>
    public static WGS84Point[] InterpolateSegment(WGS84Point p1, WGS84Point p2, double maxChunkLengthMeters)
    {
        var segment = GreatCircleSegmentBetween(p1, p2);

        if (segment.distanceMeters <= maxChunkLengthMeters)
        {
            return new WGS84Point[] { p1, p2 };
        }

        int numberOfPoints = (int) Math.Ceiling(segment.distanceMeters / maxChunkLengthMeters);
        double distanceIncrement = segment.distanceMeters / numberOfPoints;
        var ans = new WGS84Point[numberOfPoints];

        ans[0] = p1;
        for (int i = 1; i < numberOfPoints - 1; i++)
        {
            WGS84GreatCircleSegment s;
            s.initialBearingRadians = segment.initialBearingRadians;
            s.distanceMeters = distanceIncrement * i;
            ans[i] = GreatCircleMove(p1, s);

            // Sooo, this is wrong (technically). The GreatCircleSegmentBetween and GreatCircleMove
            // do not take into account the altitude of the WGS point with respect to the actual
            // WGS84 ellipsoid. Since we are not dealing with huge heights and/or huge change in heights,
            // I'll call a linear interpolation of heights good enough for now.
            // The proper solution would be to project `p1` and `p2` to the ellipsoid, do the
            // interpolation and then backproject all the points to the correct altitude
            ans[i].AltitudeMeters = LerpDouble(p1.AltitudeMeters, p2.AltitudeMeters, (1.0 / numberOfPoints) * i);
        }
        ans[numberOfPoints - 1] = p2;

        return ans;
    }

    public struct WGS84GreatCircleSegment
    {
        public double distanceMeters;
        public double initialBearingRadians;
    }

    public static ECEFPoint[] LinearlyInterpolateSegment(ECEFPoint p1, ECEFPoint p2, float interpolatedMaxSegmentLengthMeters)
    {
        var p1V = p1.RawToVector3Double();
        var p2V = p2.RawToVector3Double();

        var distance = Vector3Double.Distance(p1V, p2V);
        if (distance <= interpolatedMaxSegmentLengthMeters)
            return new ECEFPoint[] { p1, p2 };

        int numberOfPoints = (int)Math.Ceiling(distance / interpolatedMaxSegmentLengthMeters);

        var ans = new ECEFPoint[numberOfPoints];

        for (int i = 1; i < numberOfPoints - 1; i++)
        {
            ans[i] = ECEFPoint.RawFrom(Vector3Double.Lerp(p1V, p2V, (1.0d / numberOfPoints) * i));
        }

        return ans;
    }

}
