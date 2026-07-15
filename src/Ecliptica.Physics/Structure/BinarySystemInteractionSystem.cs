using System;
using System.Linq;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;
using Ecliptica.Core.Enums;
using Ecliptica.Core.Constants;

namespace Ecliptica.Physics.Structure;

public class BinarySystemInteractionSystem : IPhysicsSystem
{
    public string Name => "Binary Star Interaction System";
    public int Priority => 6; // After clustering, before main gravity

    public double MassTransferRate { get; set; } = 1e18; // kg/s during overflow
    public double OrbitDecayRate { get; set; } = 0.9999; // factor per second during interaction

    public void Update(SimulationState state, double deltaTime)
    {
        var stars = state.Bodies.Where(b => b.BodyType == CelestialBodyType.Star || 
                                            b.BodyType == CelestialBodyType.NeutronStar || 
                                            b.BodyType == CelestialBodyType.BlackHole).ToList();
        if (stars.Count < 2) return;

        for (int i = 0; i < stars.Count; i++)
        {
            for (int j = i + 1; j < stars.Count; j++)
            {
                var s1 = stars[i];
                var s2 = stars[j];

                double dist = Vector3d.Distance(s1.Position, s2.Position);
                double RocheLobeRadius1 = dist * 0.4; // rough approximation of Roche lobe radius

                // If one star fills its Roche Lobe, trigger mass transfer and orbital decay
                if (s1.Radius > RocheLobeRadius1 && s1.Mass > 0)
                {
                    double dM = Math.Min(s1.Mass, MassTransferRate * deltaTime);
                    s1.Mass -= dM;
                    s2.Mass += dM;

                    // Decaying orbits
                    s1.Velocity *= Math.Pow(OrbitDecayRate, deltaTime);
                    s2.Velocity *= Math.Pow(OrbitDecayRate, deltaTime);

                    // Add tidal heating
                    if (s1.Thermodynamics != null) s1.Thermodynamics.InternalEnergy += dM * 1e5;
                    if (s2.Thermodynamics != null) s2.Thermodynamics.InternalEnergy += dM * 1e5;

                    state.LogEvent($"Roche lobe overflow detected: Mass transferring from '{s1.Name}' to '{s2.Name}'!");
                }
                else if (s2.Radius > (dist * 0.4) && s2.Mass > 0)
                {
                    double dM = Math.Min(s2.Mass, MassTransferRate * deltaTime);
                    s2.Mass -= dM;
                    s1.Mass += dM;

                    s1.Velocity *= Math.Pow(OrbitDecayRate, deltaTime);
                    s2.Velocity *= Math.Pow(OrbitDecayRate, deltaTime);

                    if (s1.Thermodynamics != null) s1.Thermodynamics.InternalEnergy += dM * 1e5;
                    if (s2.Thermodynamics != null) s2.Thermodynamics.InternalEnergy += dM * 1e5;

                    state.LogEvent($"Roche lobe overflow detected: Mass transferring from '{s2.Name}' to '{s1.Name}'!");
                }
            }
        }
    }
}
