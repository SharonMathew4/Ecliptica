using System;
using Ecliptica.Core.Models;

namespace Ecliptica.Physics.Collision;

public static class CollisionHelper
{
    public static void Merge(CelestialBody survivor, CelestialBody absorbed)
    {
        double totalMass = survivor.Mass + absorbed.Mass;
        if (totalMass <= 0.0) return;

        // 1. Center of mass position conservation
        survivor.Position = (survivor.Position * survivor.Mass + absorbed.Position * absorbed.Mass) / totalMass;

        // 2. Momentum conservation
        survivor.Velocity = (survivor.Velocity * survivor.Mass + absorbed.Velocity * absorbed.Mass) / totalMass;

        // 3. Mass conservation
        survivor.Mass = totalMass;

        // 4. Volume conservation: V = 4/3 * pi * R^3 => R = cbrt(R1^3 + R2^3)
        double r1 = survivor.Radius;
        double r2 = absorbed.Radius;
        survivor.Radius = Math.Pow(r1 * r1 * r1 + r2 * r2 * r2, 1.0 / 3.0);

        // 5. Scientific properties merging
        if (absorbed.Stellar != null)
        {
            if (survivor.Stellar == null)
            {
                survivor.Stellar = absorbed.Stellar;
            }
            else if (absorbed.Mass > survivor.Mass)
            {
                // Take structural attributes from more massive parent star
                survivor.Stellar.Phase = absorbed.Stellar.Phase;
                survivor.Stellar.Age = absorbed.Stellar.Age;
                survivor.Stellar.Luminosity = absorbed.Stellar.Luminosity;
                survivor.Stellar.MainSequenceLifetime = absorbed.Stellar.MainSequenceLifetime;
            }
        }

        if (absorbed.Thermodynamics != null)
        {
            if (survivor.Thermodynamics == null)
            {
                survivor.Thermodynamics = absorbed.Thermodynamics;
            }
            else
            {
                // Sum heat properties
                survivor.Thermodynamics.InternalEnergy += absorbed.Thermodynamics.InternalEnergy;
                survivor.Thermodynamics.HeatCapacity += absorbed.Thermodynamics.HeatCapacity;
                survivor.Thermodynamics.Temperature = survivor.Thermodynamics.InternalEnergy / survivor.Thermodynamics.HeatCapacity;
                survivor.Thermodynamics.Entropy += absorbed.Thermodynamics.Entropy;
            }
        }

        if (absorbed.DarkMatter != null)
        {
            if (survivor.DarkMatter == null)
            {
                survivor.DarkMatter = absorbed.DarkMatter;
            }
            else
            {
                survivor.DarkMatter.HaloMass += absorbed.DarkMatter.HaloMass;
                survivor.DarkMatter.ScaleRadius = Math.Max(survivor.DarkMatter.ScaleRadius, absorbed.DarkMatter.ScaleRadius);
            }
        }
    }
}
