using System;
using System.Collections.Generic;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;

namespace Ecliptica.Physics;

public class AstrophysicalEventSystem : IPhysicsSystem
{
    public string Name => "Astrophysical Event Evaluator";
    public int Priority => 8; // Evaluated after gravity and collisions

    private readonly List<IAstrophysicalEvent> _events = new();

    public void RegisterEvent(IAstrophysicalEvent astroEvent)
    {
        if (astroEvent != null) _events.Add(astroEvent);
    }

    public void Update(SimulationState state, double deltaTime)
    {
        if (state.Bodies.Count == 0) return;

        var bodiesToProcess = new List<CelestialBody>(state.Bodies);
        var spawnedBodies = new List<CelestialBody>();

        Action<CelestialBody> spawnCallback = (newBody) =>
        {
            lock (spawnedBodies)
            {
                spawnedBodies.Add(newBody);
            }
        };

        foreach (var body in bodiesToProcess)
        {
            foreach (var astroEvent in _events)
            {
                if (astroEvent.ShouldTrigger(body, state))
                {
                    astroEvent.Execute(body, state, spawnCallback);
                    break; // Execute at most one event transition per body per tick
                }
            }
        }

        if (spawnedBodies.Count > 0)
        {
            lock (state.Bodies)
            {
                state.Bodies.AddRange(spawnedBodies);
            }
        }
    }
}
