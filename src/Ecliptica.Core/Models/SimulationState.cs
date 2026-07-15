using System.Collections.Generic;

namespace Ecliptica.Core.Models;

public class SimulationState
{
    public List<CelestialBody> Bodies { get; } = new();
    public List<string> EventLog { get; } = new();
    public double ElapsedTime { get; set; }     // total simulated seconds
    public double TimeScale { get; set; } = 1.0; // multiplier for time advancement

    public void LogEvent(string description)
    {
        lock (EventLog)
        {
            EventLog.Add(description);
        }
    }
}
