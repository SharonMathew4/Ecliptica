using System;
using Ecliptica.Core.Constants;
using Ecliptica.Core.Enums;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;

namespace Ecliptica.Physics.Events;

public class SupernovaEvent : IAstrophysicalEvent
{
    public string Name => "Supernova Explosion";

    public bool ShouldTrigger(CelestialBody body, SimulationState state)
    {
        return body.Stellar != null && body.Stellar.Phase == StellarPhase.Supernova;
    }

    public void Execute(CelestialBody body, SimulationState state, Action<CelestialBody> spawnCallback)
    {
        state.LogEvent($"Supernova event triggered for star '{body.Name}' (Mass: {body.Mass / PhysicalConstants.SolarMass:F2} M_sun)!");

        // Determine core remnant type based on progenitor mass
        double massSolar = body.Mass / PhysicalConstants.SolarMass;
        CelestialBodyType remnantBodyType;
        AstrophysicalObjectType remnantObjectType;

        if (massSolar >= 20.0)
        {
            remnantBodyType = CelestialBodyType.BlackHole;
            remnantObjectType = AstrophysicalObjectType.BlackHole;
        }
        else
        {
            remnantBodyType = CelestialBodyType.NeutronStar;
            // Introduce Pulsar/Magnetar outcomes on high mass ranges
            remnantObjectType = (massSolar > 12.0) ? AstrophysicalObjectType.Magnetar : AstrophysicalObjectType.Pulsar;
        }

        // 1. Transform progenitor into compact remnant (e.g. 20% mass conservation for core remnant)
        double remnantMass = body.Mass * 0.2;
        double gasShellMass = body.Mass * 0.8;

        body.Mass = remnantMass;
        body.BodyType = remnantBodyType;
        body.ObjectType = remnantObjectType;
        body.Radius = (remnantObjectType == AstrophysicalObjectType.BlackHole)
            ? (2.0 * PhysicalConstants.G * remnantMass) / (PhysicalConstants.c * PhysicalConstants.c)
            : 12000.0; // ~12 km Neutron remnant

        if (body.Stellar != null)
        {
            body.Stellar.Phase = (remnantObjectType == AstrophysicalObjectType.BlackHole) ? StellarPhase.BlackHole : StellarPhase.NeutronStar;
            body.Stellar.Luminosity = (remnantObjectType == AstrophysicalObjectType.BlackHole) ? 0.0 : 1e-5 * PhysicalConstants.SolarLuminosity;
        }

        state.LogEvent($"Compact remnant '{body.Name}' created as a {remnantObjectType}.");

        // 2. Spawn expanding gas shell (Supernova remnant particles)
        int particleCount = 8;
        double shellExpansionVelocity = 1e7; // ~10,000 km/s expansion speed

        for (int i = 0; i < particleCount; i++)
        {
            double angle = (2.0 * Math.PI / particleCount) * i;
            Vector3d direction = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0.0);

            var gasParticle = new CelestialBody
            {
                Id = $"{body.Id}-remnant-{i}",
                Name = $"{body.Name} Shell Remnant {i}",
                BodyType = CelestialBodyType.GasCloud,
                ObjectType = AstrophysicalObjectType.GasCloud,
                Mass = gasShellMass / particleCount,
                Radius = body.Radius * 5.0,
                Position = body.Position + direction * (body.Radius * 2.0),
                Velocity = body.Velocity + direction * shellExpansionVelocity
            };
            spawnCallback(gasParticle);
        }
    }
}
