using System;

namespace Ecliptica.Core.Models;

public struct Vector3d
{
    public double X;
    public double Y;
    public double Z;

    public Vector3d(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3d Zero => new(0, 0, 0);

    public static Vector3d operator +(Vector3d a, Vector3d b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3d operator -(Vector3d a, Vector3d b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3d operator *(Vector3d a, double scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);
    public static Vector3d operator *(double scalar, Vector3d a) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);
    public static Vector3d operator /(Vector3d a, double scalar) => new(a.X / scalar, a.Y / scalar, a.Z / scalar);

    public double LengthSquared() => X * X + Y * Y + Z * Z;
    public double Length() => Math.Sqrt(LengthSquared());

    public Vector3d Normalize()
    {
        double len = Length();
        if (len < 1e-30) return Zero;
        return this / len;
    }

    public static double Dot(Vector3d a, Vector3d b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vector3d Cross(Vector3d a, Vector3d b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X
    );

    public static double Distance(Vector3d a, Vector3d b) => (a - b).Length();
    public static double DistanceSquared(Vector3d a, Vector3d b) => (a - b).LengthSquared();
}
