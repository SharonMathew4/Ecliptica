using System;
using Ecliptica.Core.Constants;
using Ecliptica.Core.Enums;

namespace Ecliptica.Physics.StellarEvolution;

public static class StellarEvolutionHelper
{
    // t_ms ≈ t_sun * (M_sun / M)^2.5 in seconds
    public static double ComputeMainSequenceLifetime(double mass)
    {
        double massInSolar = mass / PhysicalConstants.SolarMass;
        if (massInSolar < 0.08) return double.MaxValue; // brown dwarfs don't evolve on normal scales
        
        // 10 billion years in seconds is approximately 3.1536e17 seconds
        double t_sun = 10.0e9 * 365.25 * 24 * 3600;
        return t_sun * Math.Pow(1.0 / massInSolar, 2.5);
    }

    public static double ComputeLuminosity(StellarPhase phase, double mass)
    {
        double massInSolar = mass / PhysicalConstants.SolarMass;

        return phase switch
        {
            StellarPhase.Protostar => 0.1 * PhysicalConstants.SolarLuminosity * Math.Pow(massInSolar, 1.5),
            StellarPhase.MainSequence => PhysicalConstants.SolarLuminosity * Math.Pow(massInSolar, 3.5),
            StellarPhase.SubGiant => 2.0 * PhysicalConstants.SolarLuminosity * Math.Pow(massInSolar, 3.5),
            StellarPhase.RedGiant => 100.0 * PhysicalConstants.SolarLuminosity * Math.Pow(massInSolar, 2.0),
            StellarPhase.HorizontalBranch => 50.0 * PhysicalConstants.SolarLuminosity * Math.Pow(massInSolar, 2.0),
            StellarPhase.AsymptoticGiantBranch => 1000.0 * PhysicalConstants.SolarLuminosity * Math.Pow(massInSolar, 2.0),
            StellarPhase.PlanetaryNebula => 100.0 * PhysicalConstants.SolarLuminosity,
            StellarPhase.WhiteDwarf => 1e-4 * PhysicalConstants.SolarLuminosity * Math.Pow(massInSolar, 0.5),
            StellarPhase.Supernova => 1e10 * PhysicalConstants.SolarLuminosity,
            StellarPhase.NeutronStar => 1e-6 * PhysicalConstants.SolarLuminosity,
            StellarPhase.BlackHole => 0.0,
            _ => 0.0
        };
    }

    public static double ComputeRadius(StellarPhase phase, double mass)
    {
        double massInSolar = mass / PhysicalConstants.SolarMass;

        return phase switch
        {
            StellarPhase.Protostar => 5.0 * PhysicalConstants.SolarRadius * Math.Pow(massInSolar, 0.5),
            StellarPhase.MainSequence => PhysicalConstants.SolarRadius * Math.Pow(massInSolar, 0.8),
            StellarPhase.SubGiant => 3.0 * PhysicalConstants.SolarRadius * Math.Pow(massInSolar, 0.8),
            StellarPhase.RedGiant => 100.0 * PhysicalConstants.SolarRadius * Math.Pow(massInSolar, 0.8),
            StellarPhase.HorizontalBranch => 10.0 * PhysicalConstants.SolarRadius * Math.Pow(massInSolar, 0.8),
            StellarPhase.AsymptoticGiantBranch => 300.0 * PhysicalConstants.SolarRadius * Math.Pow(massInSolar, 0.8),
            StellarPhase.PlanetaryNebula => 500.0 * PhysicalConstants.SolarRadius,
            StellarPhase.WhiteDwarf => 0.01 * PhysicalConstants.SolarRadius, // Earth sized
            StellarPhase.Supernova => 1000.0 * PhysicalConstants.SolarRadius,
            StellarPhase.NeutronStar => 12000.0, // ~12 km radius
            StellarPhase.BlackHole => 2.0 * PhysicalConstants.G * mass / (PhysicalConstants.c * PhysicalConstants.c), // Schwarzschild radius
            _ => 1.0
        };
    }

    public static double ComputeSurfaceTemperature(double luminosity, double radius)
    {
        if (radius < 1e-5) return 0.0;
        
        // L = 4 * pi * R^2 * sigma * T^4  =>  T = (L / (4 * pi * R^2 * sigma)) ^ 0.25
        double area = 4.0 * Math.PI * radius * radius;
        double val = luminosity / (area * PhysicalConstants.StefanBoltzmann);
        if (val < 0.0) return 0.0;
        return Math.Pow(val, 0.25);
    }

    public static StellarPhase DetermineNextPhase(StellarPhase current, double mass, double age, double msLifetime)
    {
        double massInSolar = mass / PhysicalConstants.SolarMass;

        switch (current)
        {
            case StellarPhase.Protostar:
                if (age > 1e6 * 365.25 * 24 * 3600) return StellarPhase.MainSequence; // ~1 million years
                return StellarPhase.Protostar;

            case StellarPhase.MainSequence:
                if (age > msLifetime)
                {
                    return massInSolar >= 8.0 ? StellarPhase.Supernova : StellarPhase.SubGiant;
                }
                return StellarPhase.MainSequence;

            case StellarPhase.SubGiant:
                if (age > msLifetime + 0.1 * msLifetime) return StellarPhase.RedGiant;
                return StellarPhase.SubGiant;

            case StellarPhase.RedGiant:
                if (age > msLifetime + 0.15 * msLifetime) return StellarPhase.HorizontalBranch;
                return StellarPhase.RedGiant;

            case StellarPhase.HorizontalBranch:
                if (age > msLifetime + 0.18 * msLifetime) return StellarPhase.AsymptoticGiantBranch;
                return StellarPhase.HorizontalBranch;

            case StellarPhase.AsymptoticGiantBranch:
                if (age > msLifetime + 0.2 * msLifetime)
                {
                    return StellarPhase.PlanetaryNebula;
                }
                return StellarPhase.AsymptoticGiantBranch;

            case StellarPhase.PlanetaryNebula:
                if (age > msLifetime + 0.22 * msLifetime) return StellarPhase.WhiteDwarf;
                return StellarPhase.PlanetaryNebula;

            case StellarPhase.Supernova:
                // Supernova is highly transient (simulated as 1 tick or a few weeks/months)
                // Let's transition after ~0.01 years equivalent simulation time
                double transientDuration = 0.01 * 365.25 * 24 * 3600;
                if (age > msLifetime + transientDuration)
                {
                    return massInSolar >= 20.0 ? StellarPhase.BlackHole : StellarPhase.NeutronStar;
                }
                return StellarPhase.Supernova;

            case StellarPhase.WhiteDwarf:
            case StellarPhase.NeutronStar:
            case StellarPhase.BlackHole:
            default:
                return current; // Remnants remain stable
        }
    }
}
