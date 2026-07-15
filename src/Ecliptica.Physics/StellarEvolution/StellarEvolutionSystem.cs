using System;
using Ecliptica.Core.Enums;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;

namespace Ecliptica.Physics.StellarEvolution;

public class StellarEvolutionSystem : IPhysicsSystem
{
    public string Name => "Stellar Evolution";
    public int Priority => 10;

    public void Update(SimulationState state, double deltaTime)
    {
        foreach (var body in state.Bodies)
        {
            if (body.Stellar == null) continue;

            var stellar = body.Stellar;

            // Initialize MS lifetime if not set
            if (stellar.MainSequenceLifetime < 1e-5)
            {
                stellar.MainSequenceLifetime = StellarEvolutionHelper.ComputeMainSequenceLifetime(body.Mass);
            }

            // Update age
            stellar.Age += deltaTime;

            // Determine phase transitions
            StellarPhase newPhase = StellarEvolutionHelper.DetermineNextPhase(
                stellar.Phase, 
                body.Mass, 
                stellar.Age, 
                stellar.MainSequenceLifetime
            );

            if (stellar.Phase != newPhase)
            {
                stellar.Phase = newPhase;

                // Handle structural/type updates based on new phase
                if (newPhase == StellarPhase.BlackHole)
                {
                    body.BodyType = CelestialBodyType.BlackHole;
                }
                else if (newPhase == StellarPhase.NeutronStar)
                {
                    body.BodyType = CelestialBodyType.NeutronStar;
                }
            }

            // Update physical properties (Luminosity, Radius, Temperature)
            stellar.Luminosity = StellarEvolutionHelper.ComputeLuminosity(stellar.Phase, body.Mass);
            body.Radius = StellarEvolutionHelper.ComputeRadius(stellar.Phase, body.Mass);
            stellar.SurfaceTemperature = StellarEvolutionHelper.ComputeSurfaceTemperature(stellar.Luminosity, body.Radius);

            // Core temperature approximation
            stellar.CoreTemperature = ComputeCoreTemperature(stellar.Phase, body.Mass);

            // Sync body mass if mass is ejected (e.g. planetary nebula or supernova)
            double currentMass = body.Mass;
            double targetMass = ComputeEvolvedMass(stellar.Phase, body.Mass);
            if (currentMass > targetMass)
            {
                // Gradually lose mass or lose it instantly on explosion
                if (stellar.Phase == StellarPhase.Supernova || stellar.Phase == StellarPhase.PlanetaryNebula)
                {
                    body.Mass = targetMass;
                }
            }
        }
    }

    private double ComputeCoreTemperature(StellarPhase phase, double mass)
    {
        // Simple scaling relation for core temperatures based on phase
        return phase switch
        {
            StellarPhase.Protostar => 1e6,
            StellarPhase.MainSequence => 1.5e7 * Math.Pow(mass / Ecliptica.Core.Constants.PhysicalConstants.SolarMass, 0.2),
            StellarPhase.SubGiant => 3e7,
            StellarPhase.RedGiant => 1e8,
            StellarPhase.HorizontalBranch => 1.2e8,
            StellarPhase.AsymptoticGiantBranch => 2e8,
            StellarPhase.PlanetaryNebula => 1e8,
            StellarPhase.WhiteDwarf => 1e7,
            StellarPhase.Supernova => 1e9,
            StellarPhase.NeutronStar => 1e7,
            StellarPhase.BlackHole => 0.0,
            _ => 0.0
        };
    }

    private double ComputeEvolvedMass(StellarPhase phase, double initialMass)
    {
        double initialSolar = initialMass / Ecliptica.Core.Constants.PhysicalConstants.SolarMass;

        return phase switch
        {
            StellarPhase.WhiteDwarf => Math.Min(1.4, 0.5 * initialSolar) * Ecliptica.Core.Constants.PhysicalConstants.SolarMass, // Chandrasekhar limit clamp
            StellarPhase.NeutronStar => Math.Min(2.5, 0.2 * initialSolar) * Ecliptica.Core.Constants.PhysicalConstants.SolarMass, // TOV limit clamp
            StellarPhase.BlackHole => 0.7 * initialSolar * Ecliptica.Core.Constants.PhysicalConstants.SolarMass, // Keep most of core mass
            _ => initialMass
        };
    }
}
