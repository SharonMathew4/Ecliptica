using System;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;

namespace Ecliptica.Physics.Thermodynamics;

public class ThermodynamicsSystem : IPhysicsSystem
{
    public string Name => "Thermodynamics and Entropy";
    public int Priority => 20;

    // Standard Albedo fallback (e.g. 0.3 similar to Earth)
    public double DefaultAlbedo { get; set; } = 0.3;

    public void Update(SimulationState state, double deltaTime)
    {
        // 1. Calculate irradiance and incoming fluxes from all active luminous stars
        var stars = state.Bodies.Where(b => b.Stellar != null && b.Stellar.Luminosity > 0.0).ToList();

        // 2. Process each body with thermodynamic properties
        foreach (var body in state.Bodies)
        {
            if (body.Thermodynamics == null) continue;

            var thermo = body.Thermodynamics;

            // Reset flux accumulations for this tick
            thermo.EnergyFluxIn = 0.0;
            thermo.EnergyFluxOut = 0.0;

            // Radiative output (Stefan-Boltzmann)
            double radiationLoss = EnergyTransferHelper.ComputeRadiativeLoss(thermo.Temperature, body.Radius);
            thermo.EnergyFluxOut = radiationLoss;

            // Irradiance input from stars
            double totalAbsorbedPower = 0.0;
            foreach (var star in stars)
            {
                if (star == body) continue;

                double distance = Vector3d.Distance(body.Position, star.Position);
                double irradiance = EnergyTransferHelper.ComputeIrradiance(star.Stellar!.Luminosity, distance);
                double absorbed = EnergyTransferHelper.ComputeAbsorbedPower(irradiance, body.Radius, DefaultAlbedo);
                totalAbsorbedPower += absorbed;
            }

            // Core internal heat contribution (for stars, or geothermally active planets)
            if (body.Stellar != null)
            {
                // Star internal core energy production (simplified scaling based on core temperature)
                // L_produced is matched to its current Stellar luminosity representation
                totalAbsorbedPower += body.Stellar.Luminosity;
            }

            thermo.EnergyFluxIn = totalAbsorbedPower;

            // Net heat energy change: dQ = (FluxIn - FluxOut) * dt
            double netFlux = thermo.EnergyFluxIn - thermo.EnergyFluxOut;
            double heatEnergyTransfer = netFlux * deltaTime;

            // Update internal energy: U = U + dQ
            thermo.InternalEnergy += heatEnergyTransfer;

            // Update temperature: T = U / C (HeatCapacity)
            if (thermo.HeatCapacity < 1.0) thermo.HeatCapacity = 1e6; // Safe minimum
            thermo.Temperature = thermo.InternalEnergy / thermo.HeatCapacity;

            // Clamp temperature to prevent absolute zero violation
            if (thermo.Temperature < 2.7) // Space background temperature limit
            {
                thermo.Temperature = 2.7;
                thermo.InternalEnergy = thermo.Temperature * thermo.HeatCapacity;
            }

            // Update entropy: dS = dQ / T
            double entropyChange = EnergyTransferHelper.ComputeEntropyChange(heatEnergyTransfer, thermo.Temperature);
            thermo.Entropy += entropyChange;
        }
    }
}
