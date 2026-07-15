using Ecliptica.Core.Models;

namespace Ecliptica.Core.Interfaces;

public interface IPhysicsSystem
{
    string Name { get; }
    int Priority { get; }
    void Update(SimulationState state, double deltaTime);
}
