using System.Collections.Generic;

namespace Ecliptica.Core.Models;

public record BodySnapshot(string Id, string Name, Vector3d Position, Vector3d Velocity, double Mass, double Radius);

public record SimulationSnapshot(
    IReadOnlyList<BodySnapshot> Bodies,
    IReadOnlyList<string> EventLog,
    double ElapsedTime,
    double TimeScale,
    bool IsRunning,
    double PhysicsTickMs
);
