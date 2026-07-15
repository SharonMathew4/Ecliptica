using System;
using System.IO;
using Xunit;
using Ecliptica.Core.Models;
using Ecliptica.Core.Enums;
using Ecliptica.Infrastructure;

namespace Ecliptica.Simulation.Tests;

public class ProjectPersistenceTests
{
    [Fact]
    public void ProjectStorageManager_SaveAndLoad_ShouldPreserveStructureExactly()
    {
        string baseDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var manager = new ProjectStorageManager(baseDir);

        var info = new ProjectInfo(
            Id: "test_project_id",
            Name: "Test Project",
            FilePath: baseDir,
            CreatedAt: DateTime.Now,
            LastModifiedAt: DateTime.Now
        );

        var state = new SimulationState();
        for (int i = 0; i < 1000; i++)
        {
            state.Bodies.Add(new CelestialBody
            {
                Id = $"body-{i}",
                Name = $"Planet {i}",
                BodyType = CelestialBodyType.Planet,
                ObjectType = AstrophysicalObjectType.Planet,
                Position = new Vector3d(i * 1.0, i * 2.0, i * 3.0),
                Velocity = new Vector3d(0.1, 0.2, 0.3),
                Mass = i * 1000.0,
                Radius = i * 10.0
            });
        }

        // Save
        manager.SaveProject(info, state);

        // Load
        var loadedState = manager.LoadProjectBodies("test_project_id", out var loadedInfo);

        // Verify
        Assert.Equal(info.Id, loadedInfo.Id);
        Assert.Equal(info.Name, loadedInfo.Name);
        Assert.Equal(1000, loadedState.Bodies.Count);

        for (int i = 0; i < 1000; i++)
        {
            var original = state.Bodies[i];
            var loaded = loadedState.Bodies[i];

            Assert.Equal(original.Id, loaded.Id);
            Assert.Equal(original.Name, loaded.Name);
            Assert.Equal(original.Position.X, loaded.Position.X);
            Assert.Equal(original.Position.Y, loaded.Position.Y);
            Assert.Equal(original.Position.Z, loaded.Position.Z);
            Assert.Equal(original.Mass, loaded.Mass);
            Assert.Equal(original.Radius, loaded.Radius);
        }

        // Cleanup
        if (Directory.Exists(baseDir))
        {
            Directory.Delete(baseDir, recursive: true);
        }
    }
}
