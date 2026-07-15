using System;
using Ecliptica.Core.Constants;
using Ecliptica.Core.Enums;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;

namespace Ecliptica.Physics.Events;

public class StarBirthEvent : IAstrophysicalEvent
{
    public string Name => "Star Birth";

    // Jeans Mass approximation trigger limit
    public double JeansMassThreshold { get; set; } = PhysicalConstants.SolarMass * 5.0;

    public bool ShouldTrigger(CelestialBody body, SimulationState state)
    {
        // Triggers if a large enough gas cloud collects high enough mass density
        return body.ObjectType == AstrophysicalObjectType.GasCloud && body.Mass >= JeansMassThreshold;
    }

    public void Execute(CelestialBody body, SimulationState state, Action<CelestialBody> spawnCallback)
    {
        state.LogEvent($"Star birth triggered! Gas cloud '{body.Name}' (Mass: {body.Mass / PhysicalConstants.SolarMass:F2} M_sun) collapsed into a star.");

        // Transform gas cloud into Protostar
        body.BodyType = CelestialBodyType.Star;
        body.ObjectType = AstrophysicalObjectType.Star;
        body.Radius = PhysicalConstants.SolarRadius * (body.Mass / PhysicalConstants.SolarMass);

        body.Stellar = new StellarProperties
        {
            Phase = StellarPhase.Protostar,
            Age = 0.0,
            MainSequenceLifetime = 0.0,
            Luminosity = 0.1 * PhysicalConstants.SolarLuminosity,
            SurfaceTemperature = 3000.0,
            CoreTemperature = 1e6
        };

        body.Thermodynamics = new ThermodynamicState
        {
            Temperature = 3000.0,
            HeatCapacity = 1e12,
            InternalEnergy = 3000.0 * 1e12
        };
    }
}
