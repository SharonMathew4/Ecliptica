using Xunit;
using Ecliptica.Core.Constants;
using Ecliptica.Core.Enums;
using Ecliptica.Physics.StellarEvolution;
using Ecliptica.Physics.Thermodynamics;

namespace Ecliptica.Physics.Tests;

public class ScientificModulesTests
{
    [Fact]
    public void Vector3d_MathOps_ShouldBeCorrect()
    {
        var v1 = new Core.Models.Vector3d(1, 2, 3);
        var v2 = new Core.Models.Vector3d(4, 5, 6);

        var sum = v1 + v2;
        var diff = v2 - v1;
        var scaled = v1 * 2.0;

        Assert.Equal(5.0, sum.X);
        Assert.Equal(7.0, sum.Y);
        Assert.Equal(9.0, sum.Z);

        Assert.Equal(3.0, diff.X);
        Assert.Equal(2.0, scaled.X);
        Assert.Equal(4.0, scaled.Y);
        Assert.Equal(6.0, scaled.Z);

        Assert.Equal(32.0, Core.Models.Vector3d.Dot(v1, v2));
    }

    [Fact]
    public void StellarEvolutionHelper_MSLifetime_ShouldScaleWithMass()
    {
        double massLow = PhysicalConstants.SolarMass * 0.5;
        double massHigh = PhysicalConstants.SolarMass * 2.0;

        double lifetimeLow = StellarEvolutionHelper.ComputeMainSequenceLifetime(massLow);
        double lifetimeHigh = StellarEvolutionHelper.ComputeMainSequenceLifetime(massHigh);

        // Lower mass stars live longer
        Assert.True(lifetimeLow > lifetimeHigh);
    }

    [Fact]
    public void EnergyTransferHelper_StefanBoltzmann_ShouldCalculateRadiativeLoss()
    {
        double temp = 5778.0; // Sun surface temp
        double rad = PhysicalConstants.SolarRadius;
        
        double expectedLoss = PhysicalConstants.StefanBoltzmann * Math.Pow(temp, 4) * (4.0 * Math.PI * rad * rad);
        double actualLoss = EnergyTransferHelper.ComputeRadiativeLoss(temp, rad);

        Assert.Equal(expectedLoss, actualLoss, precision: 1);
        Assert.True(actualLoss > 3.8e26 && actualLoss < 3.9e26); // roughly solar luminosity
    }

    [Fact]
    public void EnergyTransferHelper_EntropyChange_ShouldBeCorrect()
    {
        double heat = 10000.0;
        double temp = 300.0;

        double expectedEntropy = heat / temp;
        double actualEntropy = EnergyTransferHelper.ComputeEntropyChange(heat, temp);

        Assert.Equal(expectedEntropy, actualEntropy);
    }
}
