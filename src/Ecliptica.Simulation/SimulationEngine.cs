using System.Collections.Generic;
using System.Linq;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;

namespace Ecliptica.Simulation;

public class SimulationEngine
{
    private readonly List<IPhysicsSystem> _systems = new();
    private readonly SimulationState _state;

    public SimulationState State => _state;
    public IReadOnlyList<IPhysicsSystem> Systems => _systems;

    public SimulationEngine(SimulationState state)
    {
        _state = state ?? new SimulationState();
    }

    public void RegisterSystem(IPhysicsSystem system)
    {
        if (system == null) return;
        _systems.Add(system);
        // Sort systems based on Priority (lower priority values run earlier)
        _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public void Tick(double deltaTime)
    {
        if (deltaTime <= 0.0) return;

        // Apply timescale factor
        double delta = deltaTime * _state.TimeScale;

        // Execute each physics system in prioritized order
        foreach (var system in _systems)
        {
            system.Update(_state, delta);
        }

        // Advance total elapsed simulation time
        _state.ElapsedTime += delta;
    }
}
