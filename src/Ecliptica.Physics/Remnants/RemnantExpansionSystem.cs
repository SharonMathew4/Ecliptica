using System;
using System.Linq;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;
using Ecliptica.Core.Enums;

namespace Ecliptica.Physics.Remnants;

public class RemnantExpansionSystem : IPhysicsSystem
{
    public string Name => "Supernova Remnant & Planetary Nebula Expansion System";
    public int Priority => 13; // Run after ISM drag interaction

    public double ExpansionVelocity { get; set; } = 1e5; // m/s default expansion
    public double DissipationThreshold { get; set; } = 1e11; // meters radius before dissipation starts

    public void Update(SimulationState state, double deltaTime)
    {
        var gasClouds = state.Bodies.Where(b => b.ObjectType == AstrophysicalObjectType.GasCloud || 
                                                b.BodyType == CelestialBodyType.GasCloud).ToList();
        if (gasClouds.Count == 0) return;

        var dissipatedBodies = new System.Collections.Generic.List<CelestialBody>();

        foreach (var cloud in gasClouds)
        {
            // Expand cloud radius
            cloud.Radius += ExpansionVelocity * deltaTime;

            // Heat transfer feedback to surrounding medium
            if (cloud.Thermodynamics != null && cloud.Thermodynamics.Temperature > 3.0)
            {
                // Simple radiative/convective heat decay to background medium
                double tempDiff = cloud.Thermodynamics.Temperature - 3.0;
                double heatLost = tempDiff * 1e5 * deltaTime;
                cloud.Thermodynamics.InternalEnergy = Math.Max(0.0, cloud.Thermodynamics.InternalEnergy - heatLost);
                cloud.Thermodynamics.Temperature = cloud.Thermodynamics.InternalEnergy / cloud.Thermodynamics.HeatCapacity;
            }

            // Dissipate/disperse gas cloud mass into the ISM once it reaches threshold size
            if (cloud.Radius > DissipationThreshold)
            {
                cloud.Mass *= Math.Max(0.0, 1.0 - 0.05 * deltaTime); // lose 5% mass per second
                if (cloud.Mass < 1e10)
                {
                    dissipatedBodies.Add(cloud);
                }
            }
        }

        if (dissipatedBodies.Count > 0)
        {
            lock (state.Bodies)
            {
                state.Bodies.RemoveAll(b => dissipatedBodies.Contains(b));
            }
            foreach (var b in dissipatedBodies)
            {
                state.LogEvent($"Gas remnant '{b.Name}' has completely dissipated into the Interstellar Medium.");
            }
        }
    }
}
