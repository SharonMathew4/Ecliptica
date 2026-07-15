using Xunit;
using Ecliptica.Core.Constants;
using Ecliptica.Core.Enums;
using Ecliptica.Core.Models;
using Ecliptica.Physics.Events;
using Ecliptica.Physics.Remnants;
using Ecliptica.Physics.Medium;
using Ecliptica.Simulation;

namespace Ecliptica.Physics.Tests;

public class Phase5AstrophysicalSystemTests
{
    [Fact]
    public void StarBirthEvent_GasCloudCollapse_ShouldBecomeProtostar()
    {
        var gasCloud = new CelestialBody
        {
            Id = "cloud1",
            Name = "Orion Nursery",
            BodyType = CelestialBodyType.GasCloud,
            ObjectType = AstrophysicalObjectType.GasCloud,
            Mass = PhysicalConstants.SolarMass * 10.0, // Exceeds Jeans Mass default threshold of 5M
            Radius = PhysicalConstants.SolarRadius * 100.0,
            Position = Vector3d.Zero,
            Velocity = Vector3d.Zero
        };

        var state = new SimulationState();
        state.Bodies.Add(gasCloud);

        var evSystem = new AstrophysicalEventSystem();
        evSystem.RegisterEvent(new StarBirthEvent());
        
        evSystem.Update(state, 1.0);

        Assert.Equal(CelestialBodyType.Star, gasCloud.BodyType);
        Assert.Equal(AstrophysicalObjectType.Star, gasCloud.ObjectType);
        Assert.NotNull(gasCloud.Stellar);
        Assert.Equal(StellarPhase.Protostar, gasCloud.Stellar.Phase);
        Assert.Contains("collapsed into a star", state.EventLog[0]);
    }

    [Fact]
    public void SupernovaEvent_MassiveStarExplosion_ShouldCreateNeutronStarOrPulsar()
    {
        var massiveStar = new CelestialBody
        {
            Id = "star1",
            Name = "Betelgeuse",
            BodyType = CelestialBodyType.Star,
            ObjectType = AstrophysicalObjectType.Star,
            Mass = PhysicalConstants.SolarMass * 15.0,
            Radius = PhysicalConstants.SolarRadius * 500.0,
            Position = Vector3d.Zero,
            Velocity = Vector3d.Zero,
            Stellar = new StellarProperties
            {
                Phase = StellarPhase.Supernova,
                Age = 1e15
            }
        };

        var state = new SimulationState();
        state.Bodies.Add(massiveStar);

        var evSystem = new AstrophysicalEventSystem();
        evSystem.RegisterEvent(new SupernovaEvent());

        evSystem.Update(state, 1.0);

        // Core mass remnants (should drop to 20% mass conservation)
        Assert.Equal(PhysicalConstants.SolarMass * 3.0, massiveStar.Mass, precision: 4);
        Assert.Equal(CelestialBodyType.NeutronStar, massiveStar.BodyType);
        Assert.True(massiveStar.ObjectType == AstrophysicalObjectType.Pulsar || massiveStar.ObjectType == AstrophysicalObjectType.Magnetar);
        
        // Expanding shell check (should have spawned 8 shell gas remnants)
        Assert.Equal(9, state.Bodies.Count); // Remnant core + 8 shell pieces
    }

    [Fact]
    public void AccretionDiskSystem_RocheLimitEncroachment_ShouldShredAsteroid()
    {
        var blackHole = new CelestialBody
        {
            Id = "bh1",
            Name = "Sagittarius A*",
            BodyType = CelestialBodyType.BlackHole,
            ObjectType = AstrophysicalObjectType.BlackHole,
            Mass = PhysicalConstants.SolarMass * 100.0,
            Radius = 1000.0,
            Position = Vector3d.Zero,
            Velocity = Vector3d.Zero
        };

        var targetAsteroid = new CelestialBody
        {
            Id = "asteroid1",
            Name = "Stray Dust",
            BodyType = CelestialBodyType.Asteroid,
            ObjectType = AstrophysicalObjectType.Planet,
            Mass = 100.0, // tiny mass compared to remnant
            Radius = 5.0,
            Position = new Vector3d(1500.0, 0, 0), // within accretion boundary of 3.0 * radius (3000m)
            Velocity = Vector3d.Zero
        };

        var state = new SimulationState();
        state.Bodies.Add(blackHole);
        state.Bodies.Add(targetAsteroid);

        var accretionSystem = new AccretionDiskSystem { AccretionLimitFactor = 3.0 };
        accretionSystem.Update(state, 1.0);

        // Target asteroid is consumed and shredded into disk/jet particles
        Assert.Single(state.Bodies.Where(b => b.Id == "bh1"));
        Assert.Empty(state.Bodies.Where(b => b.Id == "asteroid1"));
        Assert.True(state.Bodies.Count > 2); // original black hole + disk & jet particles
    }

    [Fact]
    public void InterstellarMediumSystem_GasDrag_ShouldSlowBodyAndProduceThermodynamicHeat()
    {
        var projectile = new CelestialBody
        {
            Id = "probe",
            Name = "Voyager 3",
            BodyType = CelestialBodyType.Planet,
            ObjectType = AstrophysicalObjectType.Planet,
            Mass = 1000.0,
            Radius = 10.0,
            Position = Vector3d.Zero,
            Velocity = new Vector3d(100000.0, 0, 0), // traveling fast
            Thermodynamics = new ThermodynamicState
            {
                Temperature = 100.0,
                InternalEnergy = 1e8,
                HeatCapacity = 1e6
            }
        };

        var state = new SimulationState();
        state.Bodies.Add(projectile);

        var mediumSystem = new InterstellarMediumSystem
        {
            MediumDensity = 1e-6, // heavy density for fast, measurable drag response
            DragCoefficient = 2.0
        };

        mediumSystem.Update(state, 1.0);

        // Voyager should have slowed down and heated up due to drag
        Assert.True(projectile.Velocity.X < 100000.0);
        Assert.True(projectile.Thermodynamics.Temperature > 100.0);
    }
}
