namespace Ecliptica.Core.Models;

public class ThermodynamicState
{
    public double Temperature { get; set; }        // K
    public double InternalEnergy { get; set; }     // J
    public double Entropy { get; set; }            // J/K
    public double HeatCapacity { get; set; }       // J/K
    public double EnergyFluxIn { get; set; }       // W
    public double EnergyFluxOut { get; set; }      // W
}
