using System.Collections.Generic;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;
using Ecliptica.Physics.Gravity;
using Ecliptica.Physics.Collision;
using Ecliptica.Physics.StellarEvolution;
using Ecliptica.Physics.Thermodynamics;
using Ecliptica.Physics.Events;
using Ecliptica.Physics.Remnants;
using Ecliptica.Physics.Medium;
using Ecliptica.Physics.Structure;
using Ecliptica.Physics;

namespace Ecliptica.Simulation;

public class SimulationBuilder
{
    private readonly SimulationState _state = new();
    private readonly List<IPhysicsSystem> _systemsToRegister = new();

    public SimulationBuilder WithGravity(double softeningFactor = 1e5)
    {
        var rawGravity = new GravitySystem { SofteningFactor = softeningFactor };
        var selector = new GravitySystemSelector(rawGravity);
        _systemsToRegister.Add(selector);
        return this;
    }

    public SimulationBuilder WithBarnesHutGravity(double theta = 0.5, double softeningFactor = 1e5)
    {
        var rawGravity = new BarnesHutGravitySystem 
        { 
            Theta = theta, 
            SofteningFactor = softeningFactor 
        };
        var selector = new GravitySystemSelector(rawGravity);
        _systemsToRegister.Add(selector);
        return this;
    }

    public SimulationBuilder WithCollisions()
    {
        _systemsToRegister.Add(new CollisionSystem());
        return this;
    }

    public SimulationBuilder WithAstrophysicalEvents()
    {
        var evSystem = new AstrophysicalEventSystem();
        evSystem.RegisterEvent(new SupernovaEvent());
        evSystem.RegisterEvent(new StarBirthEvent());
        _systemsToRegister.Add(evSystem);
        return this;
    }

    public SimulationBuilder WithAccretionDisks(double limitFactor = 3.0)
    {
        _systemsToRegister.Add(new AccretionDiskSystem { AccretionLimitFactor = limitFactor });
        return this;
    }

    public SimulationBuilder WithInterstellarMedium(double density = 1e-18)
    {
        _systemsToRegister.Add(new InterstellarMediumSystem { MediumDensity = density });
        return this;
    }

    public SimulationBuilder WithDarkMatterGravity()
    {
        _systemsToRegister.Add(new DarkMatterGravitySystem());
        return this;
    }

    public SimulationBuilder WithStellarEvolution()
    {
        _systemsToRegister.Add(new StellarEvolutionSystem());
        return this;
    }

    public SimulationBuilder WithThermodynamics(double defaultAlbedo = 0.3)
    {
        _systemsToRegister.Add(new ThermodynamicsSystem { DefaultAlbedo = defaultAlbedo });
        return this;
    }

    public SimulationBuilder WithCosmicStructure()
    {
        _systemsToRegister.Add(new CosmicStructureSystem());
        return this;
    }

    public SimulationBuilder WithBinaryInteraction()
    {
        _systemsToRegister.Add(new BinarySystemInteractionSystem());
        return this;
    }

    public SimulationBuilder WithRemnantExpansion()
    {
        _systemsToRegister.Add(new RemnantExpansionSystem());
        return this;
    }

    public SimulationBuilder WithCustomSystem(IPhysicsSystem system)
    {
        _systemsToRegister.Add(system);
        return this;
    }

    public SimulationBuilder WithBody(CelestialBody body)
    {
        _state.Bodies.Add(body);
        return this;
    }

    public SimulationEngine Build()
    {
        var engine = new SimulationEngine(_state);
        foreach (var system in _systemsToRegister)
        {
            engine.RegisterSystem(system);
        }
        return engine;
    }
}
