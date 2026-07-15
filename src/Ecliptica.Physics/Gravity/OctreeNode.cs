using System;
using Ecliptica.Core.Models;

namespace Ecliptica.Physics.Gravity;

public class OctreeNode
{
    public Vector3d Center { get; }
    public double HalfWidth { get; }
    public double TotalMass { get; private set; }
    public Vector3d CenterOfMass { get; private set; }
    public int BodyCount { get; private set; }
    public CelestialBody? Body { get; private set; }
    public OctreeNode?[]? Children { get; private set; }

    public OctreeNode(Vector3d center, double halfWidth)
    {
        Center = center;
        HalfWidth = halfWidth;
        TotalMass = 0.0;
        CenterOfMass = Vector3d.Zero;
        BodyCount = 0;
        Body = null;
        Children = null;
    }

    public int GetOctant(Vector3d position)
    {
        int octant = 0;
        if (position.X >= Center.X) octant += 4;
        if (position.Y >= Center.Y) octant += 2;
        if (position.Z >= Center.Z) octant += 1;
        return octant;
    }

    public void Insert(CelestialBody body)
    {
        if (BodyCount == 0)
        {
            Body = body;
            TotalMass = body.Mass;
            CenterOfMass = body.Position;
            BodyCount = 1;
            return;
        }

        if (BodyCount == 1)
        {
            // Subdivide and re-insert the existing body, then insert the new body
            Subdivide();
            if (Body != null)
            {
                int existingOctant = GetOctant(Body.Position);
                Children![existingOctant]!.Insert(Body);
                Body = null;
            }
        }

        int newOctant = GetOctant(body.Position);
        Children![newOctant]!.Insert(body);
        BodyCount++;
    }

    private void Subdivide()
    {
        Children = new OctreeNode[8];
        double quarterWidth = HalfWidth * 0.5;

        for (int i = 0; i < 8; i++)
        {
            double offsetX = ((i & 4) != 0) ? quarterWidth : -quarterWidth;
            double offsetY = ((i & 2) != 0) ? quarterWidth : -quarterWidth;
            double offsetZ = ((i & 1) != 0) ? quarterWidth : -quarterWidth;

            Vector3d childCenter = new Vector3d(Center.X + offsetX, Center.Y + offsetY, Center.Z + offsetZ);
            Children[i] = new OctreeNode(childCenter, quarterWidth);
        }
    }

    public void ComputeMassDistribution()
    {
        if (BodyCount == 0)
        {
            TotalMass = 0.0;
            CenterOfMass = Vector3d.Zero;
            return;
        }

        if (BodyCount == 1 && Body != null)
        {
            TotalMass = Body.Mass;
            CenterOfMass = Body.Position;
            return;
        }

        double totalMass = 0.0;
        Vector3d massWeightedPosition = Vector3d.Zero;

        if (Children != null)
        {
            foreach (var child in Children)
            {
                if (child != null && child.BodyCount > 0)
                {
                    child.ComputeMassDistribution();
                    totalMass += child.TotalMass;
                    massWeightedPosition += child.CenterOfMass * child.TotalMass;
                }
            }
        }

        TotalMass = totalMass;
        if (totalMass > 0.0)
        {
            CenterOfMass = massWeightedPosition / totalMass;
        }
        else
        {
            CenterOfMass = Center;
        }
    }
}
