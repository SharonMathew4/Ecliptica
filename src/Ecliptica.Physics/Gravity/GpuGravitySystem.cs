using System;
using System.Linq;
using ILGPU;
using ILGPU.Runtime;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;
using Ecliptica.Core.Constants;

namespace Ecliptica.Physics.Gravity;

public class GpuGravitySystem : IPhysicsSystem, IDisposable
{
    public string Name => "ILGPU Parallel Gravity";
    public int Priority => 0;

    public double SofteningFactor { get; set; } = 1e5;

    private readonly Context _context;
    private readonly Accelerator _accelerator;
    private readonly Action<Index1D, ArrayView<double>, ArrayView<double>, ArrayView<double>, ArrayView<double>, ArrayView<double>, ArrayView<double>, ArrayView<double>, double, int> _kernel;

    public GpuGravitySystem()
    {
        _context = Context.Create(builder => builder.Default().AllAccelerators());
        _accelerator = _context.GetPreferredDevice(preferCPU: false)
                              .CreateAccelerator(_context);

        _kernel = _accelerator.LoadAutoGroupedStreamKernel<
            Index1D, ArrayView<double>, ArrayView<double>, ArrayView<double>, ArrayView<double>, ArrayView<double>, ArrayView<double>, ArrayView<double>, double, int>(
            GravityKernel);
    }

    public void Update(SimulationState state, double deltaTime)
    {
        int N = state.Bodies.Count;
        if (N < 2) return;

        double[] posX = new double[N];
        double[] posY = new double[N];
        double[] posZ = new double[N];
        double[] mass = new double[N];
        double[] accX = new double[N];
        double[] accY = new double[N];
        double[] accZ = new double[N];

        for (int i = 0; i < N; i++)
        {
            var body = state.Bodies[i];
            posX[i] = body.Position.X;
            posY[i] = body.Position.Y;
            posZ[i] = body.Position.Z;
            mass[i] = body.Mass;
        }

        using var d_posX = _accelerator.Allocate1D(posX);
        using var d_posY = _accelerator.Allocate1D(posY);
        using var d_posZ = _accelerator.Allocate1D(posZ);
        using var d_mass = _accelerator.Allocate1D(mass);
        using var d_accX = _accelerator.Allocate1D(accX);
        using var d_accY = _accelerator.Allocate1D(accY);
        using var d_accZ = _accelerator.Allocate1D(accZ);

        _kernel(N, d_posX.View, d_posY.View, d_posZ.View, d_mass.View, d_accX.View, d_accY.View, d_accZ.View, SofteningFactor, N);
        _accelerator.Synchronize();

        d_accX.CopyToCPU(accX);
        d_accY.CopyToCPU(accY);
        d_accZ.CopyToCPU(accZ);

        for (int i = 0; i < N; i++)
        {
            var body = state.Bodies[i];
            body.Velocity += new Vector3d(accX[i], accY[i], accZ[i]) * deltaTime;
            body.Position += body.Velocity * deltaTime;
        }
    }

    private static void GravityKernel(
        Index1D index,
        ArrayView<double> posX, ArrayView<double> posY, ArrayView<double> posZ,
        ArrayView<double> mass,
        ArrayView<double> accX, ArrayView<double> accY, ArrayView<double> accZ,
        double softening, int N)
    {
        double px = posX[index];
        double py = posY[index];
        double pz = posZ[index];

        double ax = 0.0;
        double ay = 0.0;
        double az = 0.0;

        double G = 6.67430e-11;

        for (int j = 0; j < N; j++)
        {
            if (index == j) continue;

            double dx = posX[j] - px;
            double dy = posY[j] - py;
            double dz = posZ[j] - pz;

            double distSq = dx * dx + dy * dy + dz * dz + softening * softening;
            double dist = Math.Sqrt(distSq);

            if (dist > 1e-10)
            {
                double factor = (G * mass[j]) / (distSq * dist);
                ax += dx * factor;
                ay += dy * factor;
                az += dz * factor;
            }
        }

        accX[index] = ax;
        accY[index] = ay;
        accZ[index] = az;
    }

    public void Dispose()
    {
        _accelerator.Dispose();
        _context.Dispose();
    }
}
