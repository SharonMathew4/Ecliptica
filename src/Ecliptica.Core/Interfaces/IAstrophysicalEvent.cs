using System;
using Ecliptica.Core.Models;

namespace Ecliptica.Core.Interfaces;

public interface IAstrophysicalEvent
{
    string Name { get; }
    bool ShouldTrigger(CelestialBody body, SimulationState state);
    void Execute(CelestialBody body, SimulationState state, Action<CelestialBody> spawnCallback);
}
