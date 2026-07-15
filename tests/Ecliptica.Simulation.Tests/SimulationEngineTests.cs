using Xunit;
using Ecliptica.Core.Constants;
using Ecliptica.Core.Enums;
using Ecliptica.Core.Models;
using Ecliptica.Simulation;

namespace Ecliptica.Simulation.Tests;

public class SimulationEngineTests
{
    [Fact]
    public void SimulationBuilder_ShouldAssembleEngineAndRunTick()
    {
        // Assemble simulation with Newtonian Gravity, Stellar Evolution, and Thermodynamics systems
        var star = new CelestialBody
        {
            Id = "star1",
            Name = "Sun",
            BodyType = CelestialBodyType.Star,
            Mass = PhysicalConstants.SolarMass,
            Radius = PhysicalConstants.SolarRadius,
            Position = Vector3d.Zero,
            Velocity = Vector3d.Zero,
            Stellar = new StellarProperties
            {
                Phase = StellarPhase.MainSequence,
                Age = 0.0
            },
            Thermodynamics = new ThermodynamicState
            {
                Temperature = 5778.0,
                InternalEnergy = 5778.0 * 1e10, // dummy energy
                HeatCapacity = 1e10
            }
        };

        var planet = new CelestialBody
        {
            Id = "planet1",
            Name = "Earth",
            BodyType = CelestialBodyType.Planet,
            Mass = 5.972e24,
            Radius = 6.371e6,
            Position = new Vector3d(1.496e11, 0, 0), // 1 AU
            Velocity = new Vector3d(0, 29780, 0),    // ~29.78 km/s orbital speed
            Thermodynamics = new ThermodynamicState
            {
                Temperature = 288.0,
                InternalEnergy = 288.0 * 1e8,
                HeatCapacity = 1e8
            }
        };

        var engine = new SimulationBuilder()
            .WithGravity(softeningFactor: 0.0)
            .WithStellarEvolution()
            .WithThermodynamics()
            .WithBody(star)
            .WithBody(planet)
            .Build();

        Assert.Equal(2, engine.State.Bodies.Count);
        Assert.Equal(3, engine.Systems.Count); // Gravity, Stellar, Thermo

        // Run a simulation tick (e.g. 1 hour = 3600 seconds)
        engine.Tick(3600.0);

        Assert.Equal(3600.0, engine.State.ElapsedTime);

        // Planet should have moved under gravitational acceleration
        Assert.NotEqual(1.496e11, planet.Position.X);
        Assert.NotEqual(0.0, planet.Position.Y);

        // Thermodynamics check: planet temperature should respond to radiation/irradiance
        Assert.NotEqual(288.0, planet.Thermodynamics.Temperature);
    }
}
