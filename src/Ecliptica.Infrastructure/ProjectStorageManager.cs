using System;
using System.IO;
using System.Text.Json;
using Ecliptica.Core.Enums;
using Ecliptica.Core.Models;

namespace Ecliptica.Infrastructure;

public class ProjectStorageManager
{
    private readonly string _baseDirectory;

    public ProjectStorageManager(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
        if (!Directory.Exists(_baseDirectory))
        {
            Directory.CreateDirectory(_baseDirectory);
        }
    }

    public void SaveProject(ProjectInfo info, SimulationState state)
    {
        string projectDir = Path.Combine(_baseDirectory, info.Id);
        if (!Directory.Exists(projectDir))
        {
            Directory.CreateDirectory(projectDir);
        }

        // 1. Save JSON metadata
        string metaPath = Path.Combine(projectDir, "project.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(info, options);
        File.WriteAllText(metaPath, json);

        // 2. Save binary bodies
        string binaryPath = Path.Combine(projectDir, "bodies.bin");
        using var fs = new FileStream(binaryPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(fs);

        int N = state.Bodies.Count;
        writer.Write(N);

        for (int i = 0; i < N; i++)
        {
            var b = state.Bodies[i];
            writer.Write(b.Id);
            writer.Write(b.Name);
            writer.Write((int)b.BodyType);
            writer.Write((int)b.ObjectType);
            writer.Write(b.Position.X);
            writer.Write(b.Position.Y);
            writer.Write(b.Position.Z);
            writer.Write(b.Velocity.X);
            writer.Write(b.Velocity.Y);
            writer.Write(b.Velocity.Z);
            writer.Write(b.Mass);
            writer.Write(b.Radius);
        }
    }

    public SimulationState LoadProjectBodies(string projectId, out ProjectInfo info)
    {
        string projectDir = Path.Combine(_baseDirectory, projectId);
        string metaPath = Path.Combine(projectDir, "project.json");
        string binaryPath = Path.Combine(projectDir, "bodies.bin");

        if (!File.Exists(metaPath) || !File.Exists(binaryPath))
        {
            throw new FileNotFoundException($"Project files not found for ID: {projectId}");
        }

        // 1. Read metadata
        string json = File.ReadAllText(metaPath);
        info = JsonSerializer.Deserialize<ProjectInfo>(json) 
               ?? throw new InvalidOperationException("Failed to deserialize project metadata.");

        // 2. Read binary bodies
        var state = new SimulationState();
        using var fs = new FileStream(binaryPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(fs);

        int N = reader.ReadInt32();
        for (int i = 0; i < N; i++)
        {
            var id = reader.ReadString();
            var name = reader.ReadString();
            var bodyType = (CelestialBodyType)reader.ReadInt32();
            var objectType = (AstrophysicalObjectType)reader.ReadInt32();
            var px = reader.ReadDouble();
            var py = reader.ReadDouble();
            var pz = reader.ReadDouble();
            var vx = reader.ReadDouble();
            var vy = reader.ReadDouble();
            var vz = reader.ReadDouble();
            var mass = reader.ReadDouble();
            var radius = reader.ReadDouble();

            state.Bodies.Add(new CelestialBody
            {
                Id = id,
                Name = name,
                BodyType = bodyType,
                ObjectType = objectType,
                Position = new Vector3d(px, py, pz),
                Velocity = new Vector3d(vx, vy, vz),
                Mass = mass,
                Radius = radius
            });
        }

        return state;
    }
}
