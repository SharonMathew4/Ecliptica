using System;
using System.Collections.Generic;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;

namespace Ecliptica.Physics.Collision;

public class CollisionSystem : IPhysicsSystem
{
    public string Name => "Collision Detection";
    public int Priority => 5;

    public void Update(SimulationState state, double deltaTime)
    {
        if (state.Bodies.Count < 2) return;

        var destroyedBodies = new HashSet<CelestialBody>();

        for (int i = 0; i < state.Bodies.Count; i++)
        {
            var bodyA = state.Bodies[i];
            if (destroyedBodies.Contains(bodyA)) continue;

            for (int j = i + 1; j < state.Bodies.Count; j++)
            {
                var bodyB = state.Bodies[j];
                if (destroyedBodies.Contains(bodyB)) continue;

                double dist = Vector3d.Distance(bodyA.Position, bodyB.Position);
                double minCollisionDist = bodyA.Radius + bodyB.Radius;

                if (dist < minCollisionDist)
                {
                    // Merge: Larger body survives
                    if (bodyA.Mass >= bodyB.Mass)
                    {
                        CollisionHelper.Merge(bodyA, bodyB);
                        destroyedBodies.Add(bodyB);
                    }
                    else
                    {
                        CollisionHelper.Merge(bodyB, bodyA);
                        destroyedBodies.Add(bodyA);
                        break; // bodyA is destroyed, stop comparing it
                    }
                }
            }
        }

        // Clean up merged/destroyed bodies
        if (destroyedBodies.Count > 0)
        {
            state.Bodies.RemoveAll(b => destroyedBodies.Contains(b));
        }
    }
}
