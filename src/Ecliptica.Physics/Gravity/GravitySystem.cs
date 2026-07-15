using System;
using Ecliptica.Core.Constants;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;

namespace Ecliptica.Physics.Gravity;

public class GravitySystem : IPhysicsSystem
{
    public string Name => "Newtonian Gravity";
    public int Priority => 0;

    // Softening factor to prevent infinite force / DivisionByZero during close encounters/collisions
    public double SofteningFactor { get; set; } = 1e5; // in meters (about 100km)

    public void Update(SimulationState state, double deltaTime)
    {
        if (state.Bodies.Count < 2) return;

        // Calculate accelerations for all bodies
        Vector3d[] accelerations = new Vector3d[state.Bodies.Count];

        for (int i = 0; i < state.Bodies.Count; i++)
        {
            var bodyA = state.Bodies[i];
            for (int j = i + 1; j < state.Bodies.Count; j++)
            {
                var bodyB = state.Bodies[j];

                Vector3d direction = bodyB.Position - bodyA.Position;
                double distSq = direction.LengthSquared();
                double dist = Math.Sqrt(distSq);

                if (dist < 1e-10) continue;

                // Softened gravitational force calculation
                double softenedDistSq = distSq + SofteningFactor * SofteningFactor;
                double softenedDist = Math.Sqrt(softenedDistSq);

                // F = G * m1 * m2 / r^2
                // a_A = G * m_B / r^2 in direction of B
                double forceMagnitudeA = (PhysicalConstants.G * bodyB.Mass) / (softenedDistSq * softenedDist);
                double forceMagnitudeB = (PhysicalConstants.G * bodyA.Mass) / (softenedDistSq * softenedDist);

                accelerations[i] += direction * forceMagnitudeA;
                accelerations[j] -= direction * forceMagnitudeB;
            }
        }

        // Apply velocities and integration (Euler-Cromer)
        for (int i = 0; i < state.Bodies.Count; i++)
        {
            var body = state.Bodies[i];
            body.Velocity += accelerations[i] * deltaTime;
            body.Position += body.Velocity * deltaTime;
        }
    }
}
