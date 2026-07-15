using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;

namespace Ecliptica.Simulation;

public class SimulationLoop : ISimulationController
{
    private Thread? _thread;
    internal readonly object _lock = new();
    private bool _isShutdown;
    private bool _isRunning;
    private double _timeScale = 1.0;
    private double _targetTickRate = 60.0; // ticks per second

    internal SimulationState? _state;
    private Action<double>? _tickCallback;
    private double _lastPhysicsTickMs;

    public bool IsRunning
    {
        get { lock (_lock) return _isRunning; }
        private set { lock (_lock) _isRunning = value; }
    }

    public double TimeScale
    {
        get { lock (_lock) return _timeScale; }
        set { lock (_lock) _timeScale = value; }
    }

    public double TargetTickRate
    {
        get { lock (_lock) return _targetTickRate; }
        set { lock (_lock) _targetTickRate = value; }
    }

    public event Action<SimulationSnapshot>? SnapshotUpdated;

    public void Initialize(SimulationState state, Action<double> tickCallback)
    {
        _state = state;
        _tickCallback = tickCallback;
        _isShutdown = false;

        _thread = new Thread(Loop)
        {
            Name = "Ecliptica Simulation Loop Thread",
            IsBackground = true
        };
        _thread.Start();
    }

    public void Play() => IsRunning = true;

    public void Pause() => IsRunning = false;

    public void Step(double stepSizeSeconds)
    {
        if (IsRunning) return;

        lock (_lock)
        {
            var tickSw = Stopwatch.StartNew();
            _tickCallback?.Invoke(stepSizeSeconds);
            tickSw.Stop();
            _lastPhysicsTickMs = tickSw.Elapsed.TotalMilliseconds;
            RaiseSnapshot();
        }
    }

    public void Shutdown()
    {
        lock (_lock)
        {
            _isShutdown = true;
            _isRunning = false;
        }
        _thread?.Join(1000);
    }

    private void Loop()
    {
        var sw = Stopwatch.StartNew();
        double lastTime = sw.Elapsed.TotalSeconds;

        while (!_isShutdown)
        {
            double currentTime = sw.Elapsed.TotalSeconds;
            double dt = currentTime - lastTime;
            lastTime = currentTime;

            // Cap dt to prevent massive jumps during debug pauses
            if (dt > 0.1) dt = 0.1;

            bool active;
            double scale;
            double rate;

            lock (_lock)
            {
                active = _isRunning;
                scale = _timeScale;
                rate = _targetTickRate;
            }

            if (active && dt > 0)
            {
                var tickSw = Stopwatch.StartNew();
                lock (_lock)
                {
                    _tickCallback?.Invoke(dt * scale);
                }
                tickSw.Stop();
                lock (_lock)
                {
                    _lastPhysicsTickMs = tickSw.Elapsed.TotalMilliseconds;
                }
                RaiseSnapshot();
            }

            // Simple spin/sleep throttling matching target tickrate
            double targetPeriod = 1.0 / rate;
            double elapsed = sw.Elapsed.TotalSeconds - currentTime;
            double remaining = targetPeriod - elapsed;
            if (remaining > 0.001)
            {
                Thread.Sleep((int)(remaining * 1000));
            }
        }
    }

    public void WithEngineLock(Action<SimulationState> action)
    {
        lock (_lock)
        {
            if (_state != null)
            {
                action(_state);
            }
        }
    }

    public void AddBody(CelestialBody body)
    {
        lock (_lock)
        {
            if (_state != null)
            {
                _state.Bodies.Add(body);
            }
        }
    }

    public void RemoveBody(string bodyId)
    {
        lock (_lock)
        {
            if (_state != null)
            {
                _state.Bodies.RemoveAll(b => b.Id == bodyId);
            }
        }
    }

    public void ReplaceBodies(IEnumerable<CelestialBody> bodies)
    {
        lock (_lock)
        {
            if (_state != null)
            {
                _state.Bodies.Clear();
                _state.Bodies.AddRange(bodies);
            }
        }
    }

    private void RaiseSnapshot()
    {
        if (_state == null) return;

        var list = new List<BodySnapshot>();
        var eventsList = new List<string>();
        double tickMs;
        lock (_lock)
        {
            foreach (var body in _state.Bodies)
            {
                list.Add(new BodySnapshot(body.Id, body.Name, body.Position, body.Velocity, body.Mass, body.Radius));
            }
            lock (_state.EventLog)
            {
                eventsList.AddRange(_state.EventLog);
            }
            tickMs = _lastPhysicsTickMs;
        }

        var snap = new SimulationSnapshot(list, eventsList, _state.ElapsedTime, TimeScale, IsRunning, tickMs);
        SnapshotUpdated?.Invoke(snap);
    }
}
