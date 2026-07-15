using System;
using System.Collections.Generic;
using Ecliptica.Core.Models;

namespace Ecliptica.Core.Interfaces;

public interface ISimulationController
{
    bool IsRunning { get; }
    double TimeScale { get; set; }
    double TargetTickRate { get; set; }
    
    event Action<SimulationSnapshot>? SnapshotUpdated;
    
    void Initialize(SimulationState state, Action<double> tickCallback);
    void Play();
    void Pause();
    void Step(double stepSizeSeconds);
    void Shutdown();

    void WithEngineLock(Action<SimulationState> action);
    void AddBody(CelestialBody body);
    void RemoveBody(string bodyId);
    void ReplaceBodies(IEnumerable<CelestialBody> bodies);
}
