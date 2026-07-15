using Xunit;
using Ecliptica.Core.Models;
using Ecliptica.Physics.Collision;

namespace Ecliptica.Physics.Tests;

public class CollisionTests
{
    [Fact]
    public void CollisionHelper_Merge_ShouldConserveMassAndMomentum()
    {
        var survivor = new CelestialBody 
        { 
            Id = "1", 
            Name = "A", 
            Mass = 10.0, 
            Radius = 2.0,
            Position = new Vector3d(0, 0, 0),
            Velocity = new Vector3d(10, 0, 0)
        };

        var absorbed = new CelestialBody 
        { 
            Id = "2", 
            Name = "B", 
            Mass = 20.0, 
            Radius = 2.0,
            Position = new Vector3d(1, 0, 0),
            Velocity = new Vector3d(0, 0, 0)
        };

        CollisionHelper.Merge(survivor, absorbed);

        Assert.Equal(30.0, survivor.Mass);
        // Position COM = (0 * 10 + 1 * 20) / 30 = 20/30 = 0.666...
        Assert.True(Math.Abs(survivor.Position.X - 0.666666) < 1e-4);
        // Velocity COM = (10 * 10 + 0 * 20) / 30 = 100/30 = 3.333...
        Assert.True(Math.Abs(survivor.Velocity.X - 3.333333) < 1e-4);
        // Volume = 2^3 + 2^3 = 8 + 8 = 16 => R = cbrt(16) = 2.5198
        Assert.True(Math.Abs(survivor.Radius - 2.51984) < 1e-4);
    }

    [Fact]
    public void CollisionSystem_OverlappingBodies_ShouldMerge()
    {
        var bodyA = new CelestialBody { Id = "1", Name = "A", Mass = 10.0, Radius = 5.0, Position = new Vector3d(0, 0, 0) };
        var bodyB = new CelestialBody { Id = "2", Name = "B", Mass = 5.0, Radius = 5.0, Position = new Vector3d(2, 0, 0) }; // distance 2 < sum of radius 10

        var state = new SimulationState();
        state.Bodies.Add(bodyA);
        state.Bodies.Add(bodyB);

        var system = new CollisionSystem();
        system.Update(state, 1.0);

        Assert.Single(state.Bodies);
        Assert.Equal(15.0, state.Bodies[0].Mass);
    }
}
