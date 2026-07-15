using System;
using Ecliptica.Core.Constants;

namespace Ecliptica.Physics.Thermodynamics;

public static class EnergyTransferHelper
{
    // Stefan-Boltzmann Law: P = sigma * T^4 * A, where A = 4 * pi * R^2
    public static double ComputeRadiativeLoss(double temperature, double radius)
    {
        if (temperature < 0.0) return 0.0;
        double area = 4.0 * Math.PI * radius * radius;
        return PhysicalConstants.StefanBoltzmann * Math.Pow(temperature, 4) * area;
    }

    // Irradiance at a distance: I = L / (4 * pi * d^2)
    public static double ComputeIrradiance(double luminosity, double distance)
    {
        if (distance < 1.0) distance = 1.0;
        return luminosity / (4.0 * Math.PI * distance * distance);
    }

    // Absorbed Power: P_abs = Irradiance * CrossSectionalArea * (1 - Albedo)
    // CrossSection = pi * R^2
    public static double ComputeAbsorbedPower(double irradiance, double radius, double albedo)
    {
        if (albedo < 0.0) albedo = 0.0;
        if (albedo > 1.0) albedo = 1.0;
        double crossSection = Math.PI * radius * radius;
        return irradiance * crossSection * (1.0 - albedo);
    }

    // Clausius Entropy Relation: dS = dQ / T
    public static double ComputeEntropyChange(double netHeatTransfer, double temperature)
    {
        if (temperature < 1.0) temperature = 1.0; // Prevent division by zero / negative temperature entropy singularity
        return netHeatTransfer / temperature;
    }
}
