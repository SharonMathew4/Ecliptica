using System;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;
using Ecliptica.Core.Enums;

namespace Ecliptica.Physics.Medium;

public class InterstellarMediumSystem : IPhysicsSystem
{
    public string Name => "Interstellar Medium Interaction";
    public int Priority => 12; // Process medium collisions before accretion

    public double MediumDensity { get; set; } = 1e-18; // kg/m^3 (approx space density range)
    public double DragCoefficient { get; set; } = 2.0;  // typical drag factor

    public void Update(SimulationState state, double deltaTime)
    {
        var gasClouds = state.Bodies.Where(b => b.ObjectType == AstrophysicalObjectType.GasCloud).ToList();

        foreach (var body in state.Bodies)
        {
            // Skip gas clouds themselves applying self-drag
            if (body.ObjectType == AstrophysicalObjectType.GasCloud) continue;

            double localDensity = MediumDensity;

            // Enhance local density if the body is passing directly inside a gas cloud
            foreach (var cloud in gasClouds)
            {
                double dist = Vector3d.Distance(body.Position, cloud.Position);
                if (dist < cloud.Radius)
                {
                    // Basic volumetric gas density boost inside cloud
                    double cloudDensity = cloud.Mass / ((4.0 / 3.0) * Math.PI * Math.Pow(cloud.Radius, 3));
                    localDensity += cloudDensity;
                }
            }

            // Calculate drag force: F = 0.5 * rho * v^2 * Cd * A
            double speed = body.Velocity.Length();
            if (speed < 1e-5) continue;

            double area = Math.PI * body.Radius * body.Radius;
            double dragForceMagnitude = 0.5 * localDensity * speed * speed * DragCoefficient * area;

            Vector3d dragDirection = body.Velocity.Normalize() * -1.0;
            Vector3d dragForce = dragDirection * dragForceMagnitude;

            // Update velocity: dv = F/m * dt
            if (body.Mass > 0.0)
            {
                body.Velocity += (dragForce / body.Mass) * deltaTime;
            }

            // Convert lost mechanical energy into thermodynamic heat: dQ = F * dx
            if (body.Thermodynamics != null)
            {
                double workDoneByDrag = dragForceMagnitude * speed * deltaTime;
                body.Thermodynamics.InternalEnergy += workDoneByDrag;
                body.Thermodynamics.Temperature = body.Thermodynamics.InternalEnergy / body.Thermodynamics.HeatCapacity;
            }
        }
    }
}
