using Ecliptica.Core.Enums;

namespace Ecliptica.Core.Models;

public class CelestialBody
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public CelestialBodyType BodyType { get; set; }
    public AstrophysicalObjectType ObjectType { get; set; } = AstrophysicalObjectType.Planet;

    public Vector3d Position { get; set; }
    public Vector3d Velocity { get; set; }
    public double Mass { get; set; }       // kg
    public double Radius { get; set; }     // m

    public StellarProperties? Stellar { get; set; }
    public ThermodynamicState? Thermodynamics { get; set; }
    public DarkMatterHalo? DarkMatter { get; set; }
}
