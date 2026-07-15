using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;

namespace Ecliptica.Physics.Gravity;

public class GravitySystemSelector : IPhysicsSystem
{
    public string Name => "Gravity Accelerator Selector";
    public int Priority => 0;

    private readonly IPhysicsSystem _fallbackSystem;
    private IPhysicsSystem? _gpuSystem;
    private bool _useGpu = true;

    public GravitySystemSelector(IPhysicsSystem fallbackSystem)
    {
        _fallbackSystem = fallbackSystem;
        try
        {
            _gpuSystem = new GpuGravitySystem();
        }
        catch
        {
            _gpuSystem = null; // Fall back cleanly if drivers or GPU hardware are missing
        }
    }

    public bool IsGpuActive => _useGpu && _gpuSystem != null;

    public void SetGpuEnabled(bool enabled)
    {
        _useGpu = enabled;
    }

    public void Update(SimulationState state, double deltaTime)
    {
        if (IsGpuActive)
        {
            _gpuSystem!.Update(state, deltaTime);
        }
        else
        {
            _fallbackSystem.Update(state, deltaTime);
        }
    }
}
