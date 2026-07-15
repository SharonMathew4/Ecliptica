using Ecliptica.Core.Interfaces;

namespace Ecliptica.Engine.Services;

public static class SimulationControllerProvider
{
    private static ISimulationController? _instance;

    public static ISimulationController Instance
    {
        get => _instance ?? throw new InvalidOperationException("SimulationController has not been registered.");
        set => _instance = value;
    }
}
