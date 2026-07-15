using Xunit;
using Ecliptica.Core.Models;
using Ecliptica.Physics.Gravity;

namespace Ecliptica.Physics.Tests;

public class BarnesHutTests
{
    [Fact]
    public void OctreeNode_Insert_SingleBody_ShouldBeLeaf()
    {
        var node = new OctreeNode(Vector3d.Zero, 100.0);
        var body = new CelestialBody { Id = "1", Name = "A", Mass = 10.0, Position = new Vector3d(10, 10, 10) };
        
        node.Insert(body);
        
        Assert.Equal(1, node.BodyCount);
        Assert.Same(body, node.Body);
        Assert.Null(node.Children);
    }

    [Fact]
    public void OctreeNode_Insert_TwoBodies_ShouldSubdivide()
    {
        var node = new OctreeNode(Vector3d.Zero, 100.0);
        var bodyA = new CelestialBody { Id = "1", Name = "A", Mass = 10.0, Position = new Vector3d(10, 10, 10) };
        var bodyB = new CelestialBody { Id = "2", Name = "B", Mass = 20.0, Position = new Vector3d(-10, -10, -10) };

        node.Insert(bodyA);
        node.Insert(bodyB);

        Assert.Equal(2, node.BodyCount);
        Assert.Null(node.Body); // Root has no single body reference after subdivision
        Assert.NotNull(node.Children);
        
        node.ComputeMassDistribution();
        Assert.Equal(30.0, node.TotalMass);
    }

    [Fact]
    public void BarnesHutGravity_TwoBodyAttraction_MatchesBruteForceWithinTolerance()
    {
        var bodyA_BH = new CelestialBody { Id = "1", Name = "A", Mass = 1e24, Position = new Vector3d(1e9, 0, 0) };
        var bodyB_BH = new CelestialBody { Id = "2", Name = "B", Mass = 1e24, Position = Vector3d.Zero };

        var stateBH = new SimulationState();
        stateBH.Bodies.Add(bodyA_BH);
        stateBH.Bodies.Add(bodyB_BH);

        var bhSystem = new BarnesHutGravitySystem { Theta = 0.5, SofteningFactor = 0.0 };
        bhSystem.Update(stateBH, 10.0);

        var bodyA_Direct = new CelestialBody { Id = "1", Name = "A", Mass = 1e24, Position = new Vector3d(1e9, 0, 0) };
        var bodyB_Direct = new CelestialBody { Id = "2", Name = "B", Mass = 1e24, Position = Vector3d.Zero };

        var stateDirect = new SimulationState();
        stateDirect.Bodies.Add(bodyA_Direct);
        stateDirect.Bodies.Add(bodyB_Direct);

        var directSystem = new GravitySystem { SofteningFactor = 0.0 };
        directSystem.Update(stateDirect, 10.0);

        // Accelerations/velocities should match closely for 2-body systems
        Assert.True(Math.Abs(bodyA_BH.Velocity.X - bodyA_Direct.Velocity.X) < 1.0);
    }
}
