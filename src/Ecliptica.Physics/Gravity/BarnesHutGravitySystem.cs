using System;
using Ecliptica.Core.Constants;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;

namespace Ecliptica.Physics.Gravity;

public class BarnesHutGravitySystem : IPhysicsSystem
{
    public string Name => "Barnes-Hut Gravity";
    public int Priority => 0;

    public double Theta { get; set; } = 0.5;
    public double SofteningFactor { get; set; } = 1e5;

    public void Update(SimulationState state, double deltaTime)
    {
        if (state.Bodies.Count == 0) return;

        // 1. Determine spatial bounds for root node
        double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

        foreach (var body in state.Bodies)
        {
            minX = Math.Min(minX, body.Position.X);
            minY = Math.Min(minY, body.Position.Y);
            minZ = Math.Min(minZ, body.Position.Z);
            maxX = Math.Max(maxX, body.Position.X);
            maxY = Math.Max(maxY, body.Position.Y);
            maxZ = Math.Max(maxZ, body.Position.Z);
        }

        double sizeX = maxX - minX;
        double sizeY = maxY - minY;
        double sizeZ = maxZ - minZ;
        double maxDimension = Math.Max(sizeX, Math.Max(sizeY, sizeZ));

        // Center position and half width calculation
        Vector3d rootCenter = new Vector3d(
            minX + sizeX * 0.5,
            minY + sizeY * 0.5,
            minZ + sizeZ * 0.5
        );
        double rootHalfWidth = maxDimension * 0.5;
        if (rootHalfWidth < 1.0) rootHalfWidth = 1.0; // avoid zero boundaries

        // 2. Build Octree
        OctreeNode root = new OctreeNode(rootCenter, rootHalfWidth);
        foreach (var body in state.Bodies)
        {
            root.Insert(body);
        }

        // 3. Compute distribution properties
        root.ComputeMassDistribution();

        // 4. Calculate accelerations
        Vector3d[] accelerations = new Vector3d[state.Bodies.Count];
        for (int i = 0; i < state.Bodies.Count; i++)
        {
            accelerations[i] = ComputeAcceleration(state.Bodies[i], root);
        }

        // 5. Update velocities and positions
        for (int i = 0; i < state.Bodies.Count; i++)
        {
            var body = state.Bodies[i];
            body.Velocity += accelerations[i] * deltaTime;
            body.Position += body.Velocity * deltaTime;
        }
    }

    private Vector3d ComputeAcceleration(CelestialBody target, OctreeNode node)
    {
        if (node.BodyCount == 0) return Vector3d.Zero;

        if (node.BodyCount == 1)
        {
            if (node.Body == target) return Vector3d.Zero;
            return CalculateGravityForce(target, node.CenterOfMass, node.TotalMass);
        }

        Vector3d direction = node.CenterOfMass - target.Position;
        double distance = direction.Length();
        if (distance < 1e-10) return Vector3d.Zero;

        // Ratio of node size to distance
        double s = node.HalfWidth * 2.0;
        if (s / distance < Theta)
        {
            // Treat as single massive object
            return CalculateGravityForce(target, node.CenterOfMass, node.TotalMass);
        }

        // Otherwise recurse into children
        Vector3d totalAcc = Vector3d.Zero;
        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                if (child != null)
                {
                    totalAcc += ComputeAcceleration(target, child);
                }
            }
        }
        return totalAcc;
    }

    private Vector3d CalculateGravityForce(CelestialBody target, Vector3d sourcePos, double sourceMass)
    {
        Vector3d direction = sourcePos - target.Position;
        double distSq = direction.LengthSquared();
        double softenedDistSq = distSq + SofteningFactor * SofteningFactor;
        double softenedDist = Math.Sqrt(softenedDistSq);

        if (softenedDistSq < 1e-20) return Vector3d.Zero;

        double accMagnitude = (PhysicalConstants.G * sourceMass) / (softenedDistSq * softenedDist);
        return direction * accMagnitude;
    }
}
