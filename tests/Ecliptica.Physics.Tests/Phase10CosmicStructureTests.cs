using System;
using System.Collections.Generic;
using Xunit;
using Ecliptica.Core.Models;
using Ecliptica.Core.Enums;
using Ecliptica.Physics.Structure;
using Ecliptica.Physics.Remnants;

namespace Ecliptica.Physics.Tests;

public class Phase10CosmicStructureTests
{
    [Fact]
    public void TestCosmicStructureClusteringForce()
    {
        var state = new SimulationState();
        var galaxy1 = new CelestialBody
        {
            Id = "gal1",
            Name = "Galaxy Cluster 1",
            BodyType = CelestialBodyType.Galaxy,
            Mass = 1e40,
            Position = new Vector3d(0, 0, 0),
            Velocity = new Vector3d(0, 0, 0)
        };
        var galaxy2 = new CelestialBody
        {
            Id = "gal2",
            Name = "Galaxy Cluster 2",
            BodyType = CelestialBodyType.Galaxy,
            Mass = 1e40,
            Position = new Vector3d(1e9, 0, 0),
            Velocity = new Vector3d(0, 0, 0)
        };
        state.Bodies.Add(galaxy1);
        state.Bodies.Add(galaxy2);

        var system = new CosmicStructureSystem();
        system.Update(state, 1.0);

        // Galaxies should attract each other, making galaxy1 velocity positive along X and galaxy2 negative along X
        Assert.True(galaxy1.Velocity.X > 0.0);
        Assert.True(galaxy2.Velocity.X < 0.0);
    }

    [Fact]
    public void TestBinarySystemRocheLobeOverflow()
    {
        var state = new SimulationState();
        var star1 = new CelestialBody
        {
            Id = "star1",
            Name = "Primary Star",
            BodyType = CelestialBodyType.Star,
            Mass = 2e30,
            Radius = 5e8, // Large enough to exceed Roche lobe limit (dist * 0.4 = 4e8)
            Position = new Vector3d(0, 0, 0),
            Velocity = new Vector3d(0, 1000, 0),
            Thermodynamics = new ThermodynamicState { InternalEnergy = 1e40, HeatCapacity = 1e30, Temperature = 10 }
        };
        var star2 = new CelestialBody
        {
            Id = "star2",
            Name = "Secondary Star",
            BodyType = CelestialBodyType.Star,
            Mass = 1e30,
            Radius = 1e8,
            Position = new Vector3d(1e9, 0, 0),
            Velocity = new Vector3d(0, -2000, 0),
            Thermodynamics = new ThermodynamicState { InternalEnergy = 1e40, HeatCapacity = 1e30, Temperature = 10 }
        };
        state.Bodies.Add(star1);
        state.Bodies.Add(star2);

        var system = new BinarySystemInteractionSystem();
        system.Update(state, 1.0);

        // Mass should transfer from star1 to star2
        Assert.True(star2.Mass > 1e30);
        Assert.True(star1.Mass < 2e30);
        // Velocities should decay due to interaction drag
        Assert.True(star1.Velocity.Y < 1000.0);
    }

    [Fact]
    public void TestRemnantExpansionAndDispersal()
    {
        var state = new SimulationState();
        var remnantGas = new CelestialBody
        {
            Id = "rem1",
            Name = "Supernova Shell",
            BodyType = CelestialBodyType.GasCloud,
            ObjectType = AstrophysicalObjectType.GasCloud,
            Mass = 1e30,
            Radius = 2e11, // Larger than DissipationThreshold (1e11)
            Position = new Vector3d(0, 0, 0),
            Velocity = new Vector3d(0, 0, 0),
            Thermodynamics = new ThermodynamicState { InternalEnergy = 1e6, HeatCapacity = 1e5, Temperature = 10 }
        };
        state.Bodies.Add(remnantGas);

        var system = new RemnantExpansionSystem();
        double initialRadius = remnantGas.Radius;
        system.Update(state, 1.0);

        // Radius should expand
        Assert.True(remnantGas.Radius > initialRadius);
        // Temperature/Internal energy should dissipate/cool down
        Assert.True(remnantGas.Thermodynamics.Temperature < 10.0);
        // Mass should dissipate since radius > 1e11
        Assert.True(remnantGas.Mass < 1e30);
    }
}
