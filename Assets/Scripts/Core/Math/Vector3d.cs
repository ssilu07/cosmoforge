// ============================================================================
// CosmoForge: The Gravity & Cosmos Sandbox
// Double-Precision 3D Vector Implementation
// Conforms to PROJECT.md § Interface Contracts
// ============================================================================

namespace CosmoForge.Core.Math
{
    using System;
    using System.Runtime.CompilerServices;
    using UnityEngine;
    using Unity.Mathematics;

    /// <summary>
    /// Double-precision 3D vector for high-accuracy astronomical N-body physics.
    /// Eliminates floating-point cancellation across interplanetary scale distances.
    /// </summary>
    [Serializable]
    public struct Vector3d : IEquatable<Vector3d>
    {
        public double x;
        public double y;
        public double z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3d(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        // Standard Constants
        public static Vector3d zero => new Vector3d(0.0, 0.0, 0.0);
        public static Vector3d one => new Vector3d(1.0, 1.0, 1.0);
        public static Vector3d forward => new Vector3d(0.0, 0.0, 1.0);
        public static Vector3d back => new Vector3d(0.0, 0.0, -1.0);
        public static Vector3d up => new Vector3d(0.0, 1.0, 0.0);
        public static Vector3d down => new Vector3d(0.0, -1.0, 0.0);
        public static Vector3d left => new Vector3d(-1.0, 0.0, 0.0);
        public static Vector3d right => new Vector3d(1.0, 0.0, 0.0);

        // Operator Overloads
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator +(Vector3d a, Vector3d b) => new Vector3d(a.x + b.x, a.y + b.y, a.z + b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator -(Vector3d a, Vector3d b) => new Vector3d(a.x - b.x, a.y - b.y, a.z - b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator *(Vector3d a, double d) => new Vector3d(a.x * d, a.y * d, a.z * d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator *(double d, Vector3d a) => new Vector3d(a.x * d, a.y * d, a.z * d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator /(Vector3d a, double d) => new Vector3d(a.x / d, a.y / d, a.z / d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator -(Vector3d a) => new Vector3d(-a.x, -a.y, -a.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector3d lhs, Vector3d rhs) => lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector3d lhs, Vector3d rhs) => !(lhs == rhs);

        // Properties
        public double sqrMagnitude => x * x + y * y + z * z;
        public double magnitude => System.Math.Sqrt(sqrMagnitude);

        public Vector3d normalized
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                double mag = magnitude;
                return mag > 1e-15 ? this / mag : zero;
            }
        }

        // Vector Algebra Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vector3d a, Vector3d b) => a.x * b.x + a.y * b.y + a.z * b.z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d Cross(Vector3d a, Vector3d b)
        {
            return new Vector3d(
                a.y * b.z - a.z * b.y,
                a.z * b.x - a.x * b.z,
                a.x * b.y - a.y * b.x
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(Vector3d a, Vector3d b) => (a - b).magnitude;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SqrDistance(Vector3d a, Vector3d b) => (a - b).sqrMagnitude;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d Lerp(Vector3d a, Vector3d b, double t)
        {
            t = System.Math.Clamp(t, 0.0, 1.0);
            return new Vector3d(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t,
                a.z + (b.z - a.z) * t
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approximately(Vector3d a, Vector3d b, double tolerance = 1e-9)
        {
            return System.Math.Abs(a.x - b.x) <= tolerance &&
                   System.Math.Abs(a.y - b.y) <= tolerance &&
                   System.Math.Abs(a.z - b.z) <= tolerance;
        }

        // Conversions & Interop
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 ToVector3() => new Vector3((float)x, (float)y, (float)z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d FromVector3(Vector3 v) => new Vector3d(v.x, v.y, v.z);

        // Unity Mathematics double3 Interop
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator double3(Vector3d v) => new double3(v.x, v.y, v.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3d(double3 v) => new Vector3d(v.x, v.y, v.z);

        // Indexer
        public double this[int index]
        {
            get => index switch
            {
                0 => x,
                1 => y,
                2 => z,
                _ => throw new IndexOutOfRangeException($"Invalid Vector3d index {index}")
            };
            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                    default: throw new IndexOutOfRangeException($"Invalid Vector3d index {index}");
                }
            }
        }

        // Equality & Object Overrides
        public bool Equals(Vector3d other) => x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z);

        public override bool Equals(object obj) => obj is Vector3d other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(x, y, z);

        public override string ToString() => $"({x:F4}, {y:F4}, {z:F4})";
        public string ToString(string format) => $"({x.ToString(format)}, {y.ToString(format)}, {z.ToString(format)})";
    }

    public static class Vector3dExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d ToVector3d(this Vector3 v) => new Vector3d(v.x, v.y, v.z);
    }
}
