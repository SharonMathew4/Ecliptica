using Ecliptica.Core.Enums;

namespace Ecliptica.Core.Models;

public class StellarProperties
{
    public StellarPhase Phase { get; set; }
    public double Luminosity { get; set; }         // W
    public double SurfaceTemperature { get; set; } // K
    public double Age { get; set; }                // seconds
    public double MainSequenceLifetime { get; set; } // seconds
    public double CoreTemperature { get; set; }    // K
    public double Metallicity { get; set; }        // Z (fraction)
}
