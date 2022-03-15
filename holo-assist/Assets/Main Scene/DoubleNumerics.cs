using System;
using UnityEngine;

/// <summary>
/// Unity does not support matrices/vectors with double precision. System.Numerics does not
/// support matrices/vectors with double precisions (at the time of writing, they are working on
/// it). This class implements (the needed) maths operation and types on doubles. Yes, the implementation
/// is not efficient. No, do not improve it. Please swap this out with System.Numerics as soon as it has
/// double operations.
/// </summary>
public class DoubleNumerics
{
    /// <summary>
    ///  This implementation was copy-pasted from: https://stackoverflow.com/a/20800090
    /// An is probably the slowest possible implementation of this kind of algorithm. Deal with it.
    /// </summary>
    public static double[,] MultiplyDoubleMatrix(double[,] A, double[,] B)
    {
        int rA = A.GetLength(0);
        int cA = A.GetLength(1);
        int rB = B.GetLength(0);
        int cB = B.GetLength(1);
        double temp = 0;
        double[,] kHasil = new double[rA, cB];
        if (cA != rB)
        {
            throw new Exception("matrix can't be multiplied !!");
        }


        for (int i = 0; i < rA; i++)
        {
            for (int j = 0; j < cB; j++)
            {
                temp = 0;
                for (int k = 0; k < cA; k++)
                {
                    temp += A[i, k] * B[k, j];
                }
                kHasil[i, j] = temp;
            }
        }
        return kHasil;
    }

    public static double LerpDouble(double a, double b, double t)
    {
        return a + t * (b - a);
    }

    public struct Vector3Double
    {
        public double x;
        public double y;
        public double z;

        public double sqrMagnitude
        {
            get => x * x + y * y + z * z;
        }

        public double magnitude
        {
            get => Math.Sqrt(sqrMagnitude);
        }

        public static double Distance(Vector3Double a, Vector3Double b)
        {
            return (a - b).magnitude;
        }

        public static Vector3Double Lerp(Vector3Double a, Vector3Double b, double t)
        {
            return a + t * (b - a);
        }

        public Vector3Double normalized
        {
            get
            {
                double m = magnitude;
                Vector3Double d;
                d.x = x / m;
                d.y = y / m;
                d.z = z / m;
                return d;
            }
        }

        public Vector3 ToVector3()
        {
            Vector3 ans;
            ans.x = (float)x;
            ans.y = (float)y;
            ans.z = (float)z;
            return ans;
        }

        public static Vector3Double From(Vector3 v)
        {
            Vector3Double ans;
            ans.x = v.x;
            ans.y = v.y;
            ans.z = v.z;
            return ans;
        }

        public static Vector3Double operator +(Vector3Double a, Vector3Double b)
        {
            Vector3Double ans;
            ans.x = a.x + b.x;
            ans.y = a.y + b.y;
            ans.z = a.z + b.z;
            return ans;
        }

        public static Vector3Double operator -(Vector3Double a, Vector3Double b)
        {
            Vector3Double ans;
            ans.x = a.x - b.x;
            ans.y = a.y - b.y;
            ans.z = a.z - b.z;
            return ans;
        }

        public static Vector3Double operator *(double a, Vector3Double b)
        {
            Vector3Double ans;
            ans.x = a * b.x;
            ans.y = a * b.y;
            ans.z = a * b.z;
            return ans;
        }

        public static Vector3Double operator *(QuaternionDouble rot, Vector3Double vec)
        {
            var inv = QuaternionDouble.Inverse(rot);

            QuaternionDouble vecQ;
            vecQ.x = vec.x;
            vecQ.y = vec.y;
            vecQ.z = vec.z;
            vecQ.w = 0;

            var ans = rot * (vecQ * inv);

            Vector3Double ansV;
            ansV.x = ans.x;
            ansV.y = ans.y;
            ansV.z = ans.z;

            return ansV;
        }

        public static Vector3Double RotateAroundPivot(Vector3Double point, Vector3Double pivot, QuaternionDouble rotation)
        {
            // https://answers.unity.com/questions/1751620/rotating-around-a-pivot-point-using-a-quaternion.html
            return (rotation * (point - pivot)) + pivot;
        }
    }

    public struct QuaternionDouble
    {
        public double x;
        public double y;
        public double z;
        public double w;

        public static QuaternionDouble From(Quaternion q)
        {
            QuaternionDouble ans;
            ans.x = q.x;
            ans.y = q.y;
            ans.z = q.z;
            ans.w = q.w;
            return ans;
        }

        public static QuaternionDouble operator *(QuaternionDouble q1, QuaternionDouble q2)
        {
            // https://www.euclideanspace.com/maths/algebra/realNormedAlgebra/quaternions/code/index.htm
            QuaternionDouble ans;
            ans.x = q1.x * q2.w + q1.y * q2.z - q1.z * q2.y + q1.w * q2.x;
            ans.y = -q1.x * q2.z + q1.y * q2.w + q1.z * q2.x + q1.w * q2.y;
            ans.z = q1.x * q2.y - q1.y * q2.x + q1.z * q2.w + q1.w * q2.z;
            ans.w = -q1.x * q2.x - q1.y * q2.y - q1.z * q2.z + q1.w * q2.w;
            return ans;
        }

        public static QuaternionDouble Inverse(QuaternionDouble a)
        {
            QuaternionDouble ans;
            ans.x = -a.x;
            ans.y = -a.y;
            ans.z = -a.z;
            ans.w = a.w;
            return ans;
        }
    }
}
