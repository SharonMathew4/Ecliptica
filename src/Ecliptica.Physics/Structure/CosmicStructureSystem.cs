using System;
using System.Linq;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;
using Ecliptica.Core.Enums;

namespace Ecliptica.Physics.Structure;

public class CosmicStructureSystem : IPhysicsSystem
{
    public string Name => "Cosmic Structure & Clustering Evaluator";
    public int Priority => 5; // Run before core gravity updates

    public double ClusterAttractionCoeff { get; set; } = 1e-11;

    public void Update(SimulationState state, double deltaTime)
    {
        var clusters = state.Bodies.Where(b => b.BodyType == CelestialBodyType.Galaxy || b.Name.Contains("Cluster")).ToList();
        if (clusters.Count < 2) return;

        // Apply mutual clustering forces to represent galaxy clustering and structure formation
        for (int i = 0; i < clusters.Count; i++)
        {
            for (int j = i + 1; j < clusters.Count; j++)
            {
                var c1 = clusters[i];
                var c2 = clusters[j];

                Vector3d diff = c2.Position - c1.Position;
                double dist = diff.Length();
                if (dist < 1e3) continue;

                // F = G * m1 * m2 * coeff / r^2
                double forceMag = (Ecliptica.Core.Constants.PhysicalConstants.G * c1.Mass * c2.Mass * ClusterAttractionCoeff) / (dist * dist);
                Vector3d force = diff.Normalize() * forceMag;

                c1.Velocity += (force / c1.Mass) * deltaTime;
                c2.Velocity -= (force / c2.Mass) * deltaTime;
            }
        }
    }
}
