using System;
using System.Collections.Generic;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;
using Ecliptica.Core.Enums;

namespace Ecliptica.Physics.Remnants;

public class AccretionDiskSystem : IPhysicsSystem
{
    public string Name => "Accretion Disk and Relativistic Jet Emitter";
    public int Priority => 15; // After basic gravity and events systems

    public double AccretionLimitFactor { get; set; } = 3.0; // Factor * Radius of remnant

    public void Update(SimulationState state, double deltaTime)
    {
        var remnants = state.Bodies.Where(b => b.ObjectType == AstrophysicalObjectType.BlackHole || 
                                               b.ObjectType == AstrophysicalObjectType.NeutronStar ||
                                               b.ObjectType == AstrophysicalObjectType.Pulsar ||
                                               b.ObjectType == AstrophysicalObjectType.Magnetar).ToList();
        
        if (remnants.Count == 0) return;

        var particlesToSpawn = new List<CelestialBody>();
        var consumedBodies = new HashSet<CelestialBody>();

        foreach (var remnant in remnants)
        {
            double RocheLimit = remnant.Radius * AccretionLimitFactor;

            foreach (var body in state.Bodies)
            {
                if (body == remnant || consumedBodies.Contains(body)) continue;

                // Skip processing other remnants
                if (body.ObjectType == AstrophysicalObjectType.BlackHole || 
                    body.ObjectType == AstrophysicalObjectType.NeutronStar) continue;

                double dist = Vector3d.Distance(remnant.Position, body.Position);
                
                // If a small object gets inside Roche Limit, shred it into accretion disc particles
                if (dist < RocheLimit && body.Mass < remnant.Mass * 0.1)
                {
                    state.LogEvent($"Accretion shredded body '{body.Name}' into the accretion disk of '{remnant.Name}'!");
                    consumedBodies.Add(body);

                    int shredCount = 6;
                    double diskOrbitalSpeed = Math.Sqrt((Ecliptica.Core.Constants.PhysicalConstants.G * remnant.Mass) / dist);

                    for (int i = 0; i < shredCount; i++)
                    {
                        double angle = (2.0 * Math.PI / shredCount) * i;
                        Vector3d tangent = new Vector3d(-Math.Sin(angle), Math.Cos(angle), 0.0);
                        Vector3d posOffset = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0.0) * dist;

                        // Accretion disk particles
                        particlesToSpawn.Add(new CelestialBody
                        {
                            Id = $"{remnant.Id}-disk-{body.Id}-{i}",
                            Name = $"Accretion Disk Particle {i}",
                            BodyType = CelestialBodyType.Asteroid,
                            ObjectType = AstrophysicalObjectType.AccretionParticle,
                            Mass = body.Mass / (shredCount * 2.0),
                            Radius = body.Radius * 0.1,
                            Position = remnant.Position + posOffset,
                            Velocity = remnant.Velocity + tangent * diskOrbitalSpeed
                        });
                    }

                    // Relativistic jet particles ejected perpendicularly
                    Vector3d jetDirectionUp = new Vector3d(0, 0, 1);
                    Vector3d jetDirectionDown = new Vector3d(0, 0, -1);
                    double jetVelocity = Ecliptica.Core.Constants.PhysicalConstants.c * 0.1; // ~10% speed of light

                    particlesToSpawn.Add(new CelestialBody
                    {
                        Id = $"{remnant.Id}-jet-up-{body.Id}",
                        Name = $"Relativistic Jet Particle Up",
                        BodyType = CelestialBodyType.GasCloud,
                        ObjectType = AstrophysicalObjectType.JetParticle,
                        Mass = body.Mass * 0.1,
                        Radius = body.Radius * 0.2,
                        Position = remnant.Position + jetDirectionUp * (remnant.Radius * 1.5),
                        Velocity = remnant.Velocity + jetDirectionUp * jetVelocity
                    });

                    particlesToSpawn.Add(new CelestialBody
                    {
                        Id = $"{remnant.Id}-jet-down-{body.Id}",
                        Name = $"Relativistic Jet Particle Down",
                        BodyType = CelestialBodyType.GasCloud,
                        ObjectType = AstrophysicalObjectType.JetParticle,
                        Mass = body.Mass * 0.1,
                        Radius = body.Radius * 0.2,
                        Position = remnant.Position + jetDirectionDown * (remnant.Radius * 1.5),
                        Velocity = remnant.Velocity + jetDirectionDown * jetVelocity
                    });
                }
            }
        }

        // Apply state updates
        if (consumedBodies.Count > 0)
        {
            state.Bodies.RemoveAll(b => consumedBodies.Contains(b));
        }

        if (particlesToSpawn.Count > 0)
        {
            state.Bodies.AddRange(particlesToSpawn);
        }
    }
}
