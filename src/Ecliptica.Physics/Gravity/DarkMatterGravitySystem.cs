using System;
using Ecliptica.Core.Constants;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;

namespace Ecliptica.Physics.Gravity;

public class DarkMatterGravitySystem : IPhysicsSystem
{
    public string Name => "Dark Matter Gravity";
    public int Priority => 1; // Run after main gravity or synchronously

    public void Update(SimulationState state, double deltaTime)
    {
        // Find all bodies that act as Dark Matter Halos (e.g. Galaxy centers)
        var halos = state.Bodies.Where(b => b.DarkMatter != null).ToList();
        if (!halos.Any()) return;

        foreach (var body in state.Bodies)
        {
            // Skip applying self-interaction if it is a halo, or process ordinary bodies
            Vector3d totalDMAcceleration = Vector3d.Zero;

            foreach (var halo in halos)
            {
                if (body == halo) continue;

                Vector3d direction = halo.Position - body.Position;
                double distance = direction.Length();

                if (distance < 1e-10) continue;

                var dm = halo.DarkMatter!;
                
                // NFW Enclosed Mass: M(r) = 4 * pi * rho_0 * r_s^3 * [ln((r_s + r)/r_s) - r/(r_s + r)]
                // To compute rho_0 (characteristic density) from concentration c and halo mass M_vir:
                // For simplicity, we assume the HaloMass represents the total virial mass and scale radius rs is preset.
                // Let's compute the characteristic scale density rho_0 if it is not explicitly configured
                double rs = dm.ScaleRadius;
                if (rs < 1.0) rs = 1.0; // Avoid divide by zero

                // Enclosed NFW mass at distance
                double r_div_rs = distance / rs;
                double nfwEnclosedMassFactor = Math.Log(1.0 + r_div_rs) - (distance / (rs + distance));
                
                // Virial mass denominator factor
                double c_param = dm.ConcentrationParameter;
                if (c_param < 1.0) c_param = 10.0; // default concentration
                double virialFactor = Math.Log(1.0 + c_param) - (c_param / (1.0 + c_param));
                
                // M_enclosed = M_virial * [ln(1 + r/rs) - (r/(rs+r))] / [ln(1 + c) - c/(1+c)]
                double enclosedMass = dm.HaloMass * (nfwEnclosedMassFactor / virialFactor);

                // Gravitational acceleration from enclosed DM: a = G * M(r) / r^2
                double accelerationMagnitude = (PhysicalConstants.G * enclosedMass) / (distance * distance);

                totalDMAcceleration += direction.Normalize() * accelerationMagnitude;
            }

            // Apply acceleration directly to velocity
            body.Velocity += totalDMAcceleration * deltaTime;
        }
    }
}
