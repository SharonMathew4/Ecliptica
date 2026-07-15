using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Ecliptica.Core.Enums;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;
using Ecliptica.Engine.Commands;
using Ecliptica.Engine.Services;

namespace Ecliptica.Engine.ViewModels;

public class SimulationViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IProjectService _projectService;
    private readonly ISimulationController _controller;
    private readonly Dispatcher _dispatcher;

    private double _timeScale = 1.0;
    private double _elapsedTime = 0.0;
    private bool _isPlaying;
    private bool _isControlsVisible = true;
    private bool _isEnergyVisible = true;
    private bool _isPerformanceVisible = true;
    private bool _isAddBodyVisible = true;
    private bool _isInspectorVisible = true;
    private bool _isInsightsVisible = true;

    private bool _showTrails = true;
    private bool _showGlow = true;
    private bool _showExplosions = true;

    private double _trailLength = 24;
    private double _trailSpacing = 0.0025;

    // --- Reference UI Custom Fields & Properties ---
    private bool _followSelectedBody = false;
    private bool _orbitReferenceBody = false;
    private bool _autoCentralBody = true;
    private string _selectedCentralBody = "[0] Star";
    private string _selectedReferenceFrame = "Inertial";
    private bool _showPredictedTrajectory = true;
    private bool _highPrecisionPrediction = false;
    private double _predictionStep = 1.0;
    private double _timeStepDt = 1.63e-003;
    private double _effectiveDt = 1.63e-002;
    private string _solverPath = "SoA SimSingleThreadBackend";
    private string _stabilityState = "Stable";

    private bool _showPersistentOrbitPaths = true;
    private bool _showAccretionDisks = false;
    private bool _showGravitationalWaves = false;
    private bool _showBloom = true;
    private bool _showHdr = true;
    private bool _showReflections = false;
    private bool _showDistanceGlowScaling = true;
    private bool _showSpaceBackground = true;

    private double _orbitPathLength = 720;
    private double _orbitPathSpacing = 0.0015;

    // Add Body Dropdowns & Settings
    private string _selectedAddCategory = "Planets and Planetary Bodies";
    private string _selectedCelestialObject = "Asteroid";
    private bool _placementModeInteractive = true;
    private double _speedScale = 0.500;
    private double _minSpeed = 0.000;
    private double _maxSpeed = 4.000;
    private double _vectorMagnitude = 0.000;
    private string _directionVectorString = "(0.000, 0.000, 0.000)";

    // Energy Monitor Additions
    private double _systemStabilityPercent = 100.0;
    private double _totalWavelength = 2.500e-001;
    private double _momentumDriftPercent = 0.000;
    private int _collisionsCount = 0;
    private int _explosionBurstsCount = 0;
    private string _totalEnergyHistoryString = "E = -2.5930E-001";
    private string _integratorDescription = "Verlet (Symplectic), excellent energy conservation. Recommended.";

    public bool FollowSelectedBody { get => _followSelectedBody; set => SetProperty(ref _followSelectedBody, value); }
    public bool OrbitReferenceBody { get => _orbitReferenceBody; set => SetProperty(ref _orbitReferenceBody, value); }
    public bool AutoCentralBody { get => _autoCentralBody; set => SetProperty(ref _autoCentralBody, value); }
    public string SelectedCentralBody { get => _selectedCentralBody; set => SetProperty(ref _selectedCentralBody, value); }
    public string SelectedReferenceFrame { get => _selectedReferenceFrame; set => SetProperty(ref _selectedReferenceFrame, value); }
    public bool ShowPredictedTrajectory { get => _showPredictedTrajectory; set => SetProperty(ref _showPredictedTrajectory, value); }
    public bool HighPrecisionPrediction { get => _highPrecisionPrediction; set => SetProperty(ref _highPrecisionPrediction, value); }
    public double PredictionStep { get => _predictionStep; set => SetProperty(ref _predictionStep, value); }
    public double TimeStepDt { get => _timeStepDt; set => SetProperty(ref _timeStepDt, value); }
    public double EffectiveDt { get => _effectiveDt; set => SetProperty(ref _effectiveDt, value); }
    public string SolverPath { get => _solverPath; set => SetProperty(ref _solverPath, value); }
    public string StabilityState { get => _stabilityState; set => SetProperty(ref _stabilityState, value); }

    public bool ShowPersistentOrbitPaths { get => _showPersistentOrbitPaths; set => SetProperty(ref _showPersistentOrbitPaths, value); }
    public bool ShowAccretionDisks { get => _showAccretionDisks; set => SetProperty(ref _showAccretionDisks, value); }
    public bool ShowGravitationalWaves { get => _showGravitationalWaves; set => SetProperty(ref _showGravitationalWaves, value); }
    public bool ShowBloom { get => _showBloom; set => SetProperty(ref _showBloom, value); }
    public bool ShowHdr { get => _showHdr; set => SetProperty(ref _showHdr, value); }
    public bool ShowReflections { get => _showReflections; set => SetProperty(ref _showReflections, value); }
    public bool ShowDistanceGlowScaling { get => _showDistanceGlowScaling; set => SetProperty(ref _showDistanceGlowScaling, value); }
    public bool ShowSpaceBackground { get => _showSpaceBackground; set => SetProperty(ref _showSpaceBackground, value); }

    public double OrbitPathLength { get => _orbitPathLength; set => SetProperty(ref _orbitPathLength, value); }
    public double OrbitPathSpacing { get => _orbitPathSpacing; set => SetProperty(ref _orbitPathSpacing, value); }

    public string SelectedAddCategory { get => _selectedAddCategory; set => SetProperty(ref _selectedAddCategory, value); }
    public string SelectedCelestialObject { get => _selectedCelestialObject; set => SetProperty(ref _selectedCelestialObject, value); }
    public bool PlacementModeInteractive { get => _placementModeInteractive; set => SetProperty(ref _placementModeInteractive, value); }
    public double SpeedScale { get => _speedScale; set => SetProperty(ref _speedScale, value); }
    public double MinSpeed { get => _minSpeed; set => SetProperty(ref _minSpeed, value); }
    public double MaxSpeed { get => _maxSpeed; set => SetProperty(ref _maxSpeed, value); }
    public double VectorMagnitude { get => _vectorMagnitude; set => SetProperty(ref _vectorMagnitude, value); }
    public string DirectionVectorString { get => _directionVectorString; set => SetProperty(ref _directionVectorString, value); }

    public double SystemStabilityPercent { get => _systemStabilityPercent; set => SetProperty(ref _systemStabilityPercent, value); }
    public double TotalWavelength { get => _totalWavelength; set => SetProperty(ref _totalWavelength, value); }
    public double MomentumDriftPercent { get => _momentumDriftPercent; set => SetProperty(ref _momentumDriftPercent, value); }
    public int CollisionsCount { get => _collisionsCount; set => SetProperty(ref _collisionsCount, value); }
    public int ExplosionBurstsCount { get => _explosionBurstsCount; set => SetProperty(ref _explosionBurstsCount, value); }
    public string TotalEnergyHistoryString { get => _totalEnergyHistoryString; set => SetProperty(ref _totalEnergyHistoryString, value); }
    public string IntegratorDescription { get => _integratorDescription; set => SetProperty(ref _integratorDescription, value); }

    public bool IsControlsVisible { get => _isControlsVisible; set => SetProperty(ref _isControlsVisible, value); }
    public bool IsEnergyVisible { get => _isEnergyVisible; set => SetProperty(ref _isEnergyVisible, value); }
    public bool IsPerformanceVisible { get => _isPerformanceVisible; set => SetProperty(ref _isPerformanceVisible, value); }
    public bool IsAddBodyVisible { get => _isAddBodyVisible; set => SetProperty(ref _isAddBodyVisible, value); }
    public bool IsInspectorVisible { get => _isInspectorVisible; set => SetProperty(ref _isInspectorVisible, value); }
    public bool IsInsightsVisible { get => _isInsightsVisible; set => SetProperty(ref _isInsightsVisible, value); }

    public bool ShowTrails { get => _showTrails; set => SetProperty(ref _showTrails, value); }
    public bool ShowGlow { get => _showGlow; set => SetProperty(ref _showGlow, value); }
    public bool ShowExplosions { get => _showExplosions; set => SetProperty(ref _showExplosions, value); }

    public double TrailLength { get => _trailLength; set => SetProperty(ref _trailLength, value); }
    public double TrailSpacing { get => _trailSpacing; set => SetProperty(ref _trailSpacing, value); }

    private double _kineticEnergy;
    private double _potentialEnergy;
    private double _totalEnergy;
    private double _energyDrift;

    public double KineticEnergy { get => _kineticEnergy; set => SetProperty(ref _kineticEnergy, value); }
    public double PotentialEnergy { get => _potentialEnergy; set => SetProperty(ref _potentialEnergy, value); }
    public double TotalEnergy { get => _totalEnergy; set => SetProperty(ref _totalEnergy, value); }
    public double EnergyDrift { get => _energyDrift; set => SetProperty(ref _energyDrift, value); }

    private double _physicsTimeMs;
    private double _renderTimeMs;
    private double _fps;

    public double PhysicsTimeMs { get => _physicsTimeMs; set => SetProperty(ref _physicsTimeMs, value); }
    public double RenderTimeMs { get => _renderTimeMs; set => SetProperty(ref _renderTimeMs, value); }
    public double Fps { get => _fps; set => SetProperty(ref _fps, value); }

    public double EscapeVelocity { get; private set; }
    public double HillSphere { get; private set; }

    public ICommand ToggleControlsCommand { get; }
    public ICommand ToggleEnergyCommand { get; }
    public ICommand TogglePerformanceCommand { get; }
    public ICommand ToggleAddBodyCommand { get; }
    public ICommand ToggleInspectorCommand { get; }
    public ICommand ToggleInsightsCommand { get; }

    public ICommand ResetSimulationCommand { get; }
    public ICommand SetTimeSpeedPresetCommand { get; }

    public ObservableCollection<string> CentralBodies { get; } = new() { "[0] Star" };
    public ObservableCollection<string> ReferenceFrames { get; } = new() { "Inertial", "Barycentric", "Body Centred" };
    public ObservableCollection<string> AddCategories { get; } = new() { "Planets and Planetary Bodies", "Stars and Stellar Remnants", "Remnants and Exotic Objects" };
    public ObservableCollection<string> CelestialObjects { get; } = new() { "Asteroid", "Planet", "Moon", "Star", "Black Hole", "Dark Matter Halo" };

    private ObservableCollection<BodySnapshot> _bodies = new();
    private ObservableCollection<string> _eventLog = new();

    public ProjectInfo? CurrentProject => _projectService.CurrentProject;

    public ObservableCollection<string> EventLog
    {
        get => _eventLog;
        set => SetProperty(ref _eventLog, value);
    }

    public double TimeScale
    {
        get => _timeScale;
        set
        {
            if (SetProperty(ref _timeScale, value))
            {
                _controller.TimeScale = value;
            }
        }
    }

    public double ElapsedTime
    {
        get => _elapsedTime;
        set => SetProperty(ref _elapsedTime, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => SetProperty(ref _isPlaying, value);
    }

    public ObservableCollection<BodySnapshot> Bodies
    {
        get => _bodies;
        set => SetProperty(ref _bodies, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand MarkDirtyCommand { get; }

    public ICommand PlayCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StepCommand { get; }

    private BodySnapshot? _selectedBody;
    private double _theta = 0.5;
    private double _softening = 1e5;

    public BodySnapshot? SelectedBody
    {
        get => _selectedBody;
        set => SetProperty(ref _selectedBody, value);
    }

    public double Theta
    {
        get => _theta;
        set => SetProperty(ref _theta, value);
    }

    public double Softening
    {
        get => _softening;
        set => SetProperty(ref _softening, value);
    }

    public ICommand SpawnStarCommand { get; }
    public ICommand SpawnPlanetCommand { get; }

    public event Action<Action<string>>? RequestExitConfirmation;

    // --- Phase 8 State Machine & Custom Interactions ---
    private UiMode _currentMode = UiMode.Idle;
    public UiMode CurrentMode
    {
        get => _currentMode;
        set
        {
            if (SetProperty(ref _currentMode, value))
            {
                OnPropertyChanged(nameof(IsAddMode));
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(IsAnalyseMode));
                OnPropertyChanged(nameof(IsSimulateMode));
                UpdateSimulationStateForMode(value);
            }
        }
    }

    public bool IsAddMode => CurrentMode == UiMode.AddPlacement;
    public bool IsEditMode => CurrentMode == UiMode.Edit;
    public bool IsAnalyseMode => CurrentMode == UiMode.Analyse;
    public bool IsSimulateMode => CurrentMode == UiMode.Simulate;

    public ICommand EnterAddModeCommand { get; }
    public ICommand EnterEditModeCommand { get; }
    public ICommand EnterAnalyseModeCommand { get; }
    public ICommand EnterSimulateModeCommand { get; }
    public ICommand EnterIdleModeCommand { get; }

    // --- Two-Step Placement Workflow ---
    private PlacementPhase _placementPhase = PlacementPhase.None;
    public PlacementPhase PlacementPhase
    {
        get => _placementPhase;
        set => SetProperty(ref _placementPhase, value);
    }

    private Vector3d _placedPosition;
    private Vector3d _ghostPosition;
    private Vector3d _velocityEndpoint;
    private List<Vector3d> _trajectoryPreview = new();

    public Vector3d PlacedPosition { get => _placedPosition; set => SetProperty(ref _placedPosition, value); }
    public Vector3d GhostPosition { get => _ghostPosition; set => SetProperty(ref _ghostPosition, value); }
    public Vector3d VelocityEndpoint { get => _velocityEndpoint; set => SetProperty(ref _velocityEndpoint, value); }
    public List<Vector3d> TrajectoryPreview { get => _trajectoryPreview; set => SetProperty(ref _trajectoryPreview, value); }

    public int MaxBodies { get; set; } = 15;
    public double VelocityScaleFactor { get; set; } = 0.5;

    // --- Undo/Redo System ---
    private readonly Stack<CelestialBody[]> _undoStack = new();
    private readonly Stack<CelestialBody[]> _redoStack = new();
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    // --- Body Deletion ---
    public ICommand DeleteBodyCommand { get; }

    // --- New Spawn Commands (Phase 9) ---
    public ICommand SpawnBlackHoleCommand { get; }
    public ICommand SpawnDarkMatterCommand { get; }

    // --- Pending Body Form Properties (Add Body toolbox inputs) ---
    private string _pendingBodyName = string.Empty;
    private double _pendingBodyMass = 5.972e24;
    private double _pendingBodyRadius = 6.371e6;
    private double _pendingBodyTemp = 288.0;
    private int _pendingBodyPhaseIndex = 1; // MainSequence
    private double _pendingHaloMass = 1e42;
    private double _pendingScaleRadius = 1e20;
    private double _pendingConcentration = 10.0;
    private bool _isEventLogVisible = true;

    public string PendingBodyName { get => _pendingBodyName; set => SetProperty(ref _pendingBodyName, value); }
    public double PendingBodyMass { get => _pendingBodyMass; set => SetProperty(ref _pendingBodyMass, value); }
    public double PendingBodyRadius { get => _pendingBodyRadius; set => SetProperty(ref _pendingBodyRadius, value); }
    public double PendingBodyTemp { get => _pendingBodyTemp; set => SetProperty(ref _pendingBodyTemp, value); }
    public int PendingBodyPhaseIndex { get => _pendingBodyPhaseIndex; set => SetProperty(ref _pendingBodyPhaseIndex, value); }
    public double PendingHaloMass { get => _pendingHaloMass; set => SetProperty(ref _pendingHaloMass, value); }
    public double PendingScaleRadius { get => _pendingScaleRadius; set => SetProperty(ref _pendingScaleRadius, value); }
    public double PendingConcentration { get => _pendingConcentration; set => SetProperty(ref _pendingConcentration, value); }
    public bool IsEventLogVisible { get => _isEventLogVisible; set => SetProperty(ref _isEventLogVisible, value); }
    public ICommand ToggleEventLogCommand { get; }

    // --- Computed Properties ---
    public int BodyCount => Bodies.Count;
    public bool IsAtBodyLimit => Bodies.Count >= MaxBodies;

    private string _elapsedTimeFormatted = "0s";
    public string ElapsedTimeFormatted
    {
        get => _elapsedTimeFormatted;
        set => SetProperty(ref _elapsedTimeFormatted, value);
    }

    private double _orbitalPeriodEstimate;
    private double _schwarzschildRadius;
    public double OrbitalPeriodEstimate { get => _orbitalPeriodEstimate; set => SetProperty(ref _orbitalPeriodEstimate, value); }
    public double SchwarzschildRadius { get => _schwarzschildRadius; set => SetProperty(ref _schwarzschildRadius, value); }

    // --- Diagnostics / Telemetry Helpers ---
    private double? _initialTotalEnergy;
    private double _totalMomentum;
    public double TotalMomentum { get => _totalMomentum; set => SetProperty(ref _totalMomentum, value); }

    private DateTime _lastSnapshotTime = DateTime.UtcNow;

    public SimulationViewModel(INavigationService navigationService, IProjectService projectService)
    {
        _navigationService = navigationService;
        _projectService = projectService;
        _controller = SimulationControllerProvider.Instance;
        _dispatcher = Dispatcher.CurrentDispatcher;

        SaveCommand = new RelayCommand(SaveProject);
        ExitCommand = new RelayCommand(ExitWorkspace);
        MarkDirtyCommand = new RelayCommand(() => _projectService.MarkDirty());

        PlayCommand = new RelayCommand(PlaySimulation);
        PauseCommand = new RelayCommand(PauseSimulation);
        StepCommand = new RelayCommand(StepSimulation);

        SpawnStarCommand = new RelayCommand(SpawnStar);
        SpawnPlanetCommand = new RelayCommand(SpawnPlanet);

        ToggleControlsCommand = new RelayCommand(() => IsControlsVisible = !IsControlsVisible);
        ToggleEnergyCommand = new RelayCommand(() => IsEnergyVisible = !IsEnergyVisible);
        TogglePerformanceCommand = new RelayCommand(() => IsPerformanceVisible = !IsPerformanceVisible);
        ToggleAddBodyCommand = new RelayCommand(() => IsAddBodyVisible = !IsAddBodyVisible);
        ToggleInspectorCommand = new RelayCommand(() => IsInspectorVisible = !IsInspectorVisible);
        ToggleInsightsCommand = new RelayCommand(() => IsInsightsVisible = !IsInsightsVisible);

        ResetSimulationCommand = new RelayCommand(ResetSimulation);
        SetTimeSpeedPresetCommand = new RelayCommand<double>(preset => TimeScale = preset);

        // State machine transitions
        EnterAddModeCommand = new RelayCommand(() => { CurrentMode = UiMode.AddPlacement; PlacementPhase = PlacementPhase.ChoosingPosition; });
        EnterEditModeCommand = new RelayCommand(() => CurrentMode = UiMode.Edit);
        EnterAnalyseModeCommand = new RelayCommand(() => CurrentMode = UiMode.Analyse);
        EnterSimulateModeCommand = new RelayCommand(() => CurrentMode = UiMode.Simulate);
        EnterIdleModeCommand = new RelayCommand(() => CurrentMode = UiMode.Idle);

        // Undo/Redo commands
        UndoCommand = new RelayCommand(Undo, () => CanUndo);
        RedoCommand = new RelayCommand(Redo, () => CanRedo);

        // Delete Command
        DeleteBodyCommand = new RelayCommand(DeleteBody);

        // New Phase 9 Spawn Commands
        SpawnBlackHoleCommand = new RelayCommand(SpawnBlackHole);
        SpawnDarkMatterCommand = new RelayCommand(SpawnDarkMatter);
        ToggleEventLogCommand = new RelayCommand(() => IsEventLogVisible = !IsEventLogVisible);

        _controller.SnapshotUpdated += OnSnapshotUpdated;
    }

    private void UpdateSimulationStateForMode(UiMode newMode)
    {
        if (newMode == UiMode.Simulate)
        {
            PlaySimulation();
        }
        else
        {
            PauseSimulation();
        }
    }

    private void ResetSimulation()
    {
        PauseSimulation();
        RecordUndoSnapshot();
        ElapsedTime = 0.0;
        _initialTotalEnergy = null;

        _controller.WithEngineLock(stateObj =>
        {
            stateObj.Bodies.Clear();
            stateObj.EventLog.Clear();
            stateObj.LogEvent("Simulation reset by user.");
        });

        Bodies.Clear();
        EventLog.Clear();
        _projectService.MarkDirty();
    }

    private void SpawnStar()
    {
        int currentCount = 0;
        _controller.WithEngineLock(engine => currentCount = engine.Bodies.Count);
        if (currentCount >= MaxBodies) return;

        RecordUndoSnapshot();

        var rand = new Random();
        var id = "star-" + Guid.NewGuid().ToString().Substring(0, 4);
        var body = new CelestialBody
        {
            Id = id,
            Name = $"Star {id.ToUpper()}",
            BodyType = CelestialBodyType.Star,
            ObjectType = AstrophysicalObjectType.Star,
            Position = new Vector3d((rand.NextDouble() - 0.5) * 1e9, (rand.NextDouble() - 0.5) * 1e9, 0.0),
            Velocity = new Vector3d((rand.NextDouble() - 0.5) * 1e5, (rand.NextDouble() - 0.5) * 1e5, 0.0),
            Mass = 1.989e30 * (0.5 + rand.NextDouble() * 3.0),
            Radius = 6.957e8 * (0.8 + rand.NextDouble() * 2.0)
        };
        body.Thermodynamics = new ThermodynamicState { Temperature = 5778.0, HeatCapacity = 1e12, InternalEnergy = 5778.0 * 1e12 };
        body.Stellar = new StellarProperties { Phase = StellarPhase.MainSequence, Age = 1e9 };

        _controller.AddBody(body);
        _controller.WithEngineLock(engine => engine.LogEvent($"Spawned Star '{body.Name}' dynamically."));
        _projectService.MarkDirty();
    }

    private void SpawnPlanet()
    {
        int currentCount = 0;
        _controller.WithEngineLock(engine => currentCount = engine.Bodies.Count);
        if (currentCount >= MaxBodies) return;

        RecordUndoSnapshot();

        var rand = new Random();
        var id = "planet-" + Guid.NewGuid().ToString().Substring(0, 4);
        var body = new CelestialBody
        {
            Id = id,
            Name = $"Planet {id.ToUpper()}",
            BodyType = CelestialBodyType.Planet,
            ObjectType = AstrophysicalObjectType.Planet,
            Position = new Vector3d((rand.NextDouble() - 0.5) * 1e9, (rand.NextDouble() - 0.5) * 1e9, 0.0),
            Velocity = new Vector3d((rand.NextDouble() - 0.5) * 1e5, (rand.NextDouble() - 0.5) * 1e5, 0.0),
            Mass = 5.972e24 * (0.5 + rand.NextDouble() * 10.0),
            Radius = 6.371e6 * (0.8 + rand.NextDouble() * 2.0)
        };
        body.Thermodynamics = new ThermodynamicState { Temperature = 288.0, HeatCapacity = 1e10, InternalEnergy = 288.0 * 1e10 };

        _controller.AddBody(body);
        _controller.WithEngineLock(engine => engine.LogEvent($"Spawned Planet '{body.Name}' dynamically."));
        _projectService.MarkDirty();
    }

    private void DeleteBody()
    {
        if (SelectedBody == null) return;

        RecordUndoSnapshot();
        _controller.RemoveBody(SelectedBody.Id);
        _controller.WithEngineLock(engine => engine.LogEvent($"Deleted body '{SelectedBody.Name}' dynamically."));
        SelectedBody = null;
        _projectService.MarkDirty();
    }

    private void SpawnBlackHole()
    {
        int currentCount = 0;
        _controller.WithEngineLock(engine => currentCount = engine.Bodies.Count);
        if (currentCount >= MaxBodies) return;

        RecordUndoSnapshot();

        var rand = new Random();
        var name = string.IsNullOrWhiteSpace(PendingBodyName) ? null : PendingBodyName;
        var id = "bh-" + Guid.NewGuid().ToString().Substring(0, 4);
        var body = new CelestialBody
        {
            Id = id,
            Name = name ?? $"Black Hole {id.ToUpper()}",
            BodyType = CelestialBodyType.BlackHole,
            ObjectType = AstrophysicalObjectType.BlackHole,
            Position = new Vector3d((rand.NextDouble() - 0.5) * 1e9, (rand.NextDouble() - 0.5) * 1e9, 0.0),
            Velocity = new Vector3d((rand.NextDouble() - 0.5) * 5e4, (rand.NextDouble() - 0.5) * 5e4, 0.0),
            Mass = 1.989e30 * (10.0 + rand.NextDouble() * 20.0),   // 10–30 solar masses
            Radius = 3.0e4 * (1.0 + rand.NextDouble() * 4.0)        // Schwarzschild-scale km
        };

        _controller.AddBody(body);
        _controller.WithEngineLock(engine => engine.LogEvent($"Spawned Black Hole '{body.Name}' dynamically."));
        PendingBodyName = string.Empty;
        _projectService.MarkDirty();
    }

    private void SpawnDarkMatter()
    {
        int currentCount = 0;
        _controller.WithEngineLock(engine => currentCount = engine.Bodies.Count);
        if (currentCount >= MaxBodies) return;

        RecordUndoSnapshot();

        var rand = new Random();
        var name = string.IsNullOrWhiteSpace(PendingBodyName) ? null : PendingBodyName;
        var id = "dm-" + Guid.NewGuid().ToString().Substring(0, 4);
        var body = new CelestialBody
        {
            Id = id,
            Name = name ?? $"DM Halo {id.ToUpper()}",
            BodyType = CelestialBodyType.DarkMatterHalo,
            ObjectType = AstrophysicalObjectType.DarkMatter,
            Position = new Vector3d((rand.NextDouble() - 0.5) * 2e9, (rand.NextDouble() - 0.5) * 2e9, 0.0),
            Velocity = new Vector3d((rand.NextDouble() - 0.5) * 2e4, (rand.NextDouble() - 0.5) * 2e4, 0.0),
            Mass = PendingHaloMass,
            Radius = PendingScaleRadius * 0.1
        };
        body.DarkMatter = new DarkMatterHalo
        {
            HaloMass = PendingHaloMass,
            ScaleRadius = PendingScaleRadius,
            ConcentrationParameter = PendingConcentration,
            LocalDensity = PendingHaloMass / (4.0 / 3.0 * Math.PI * Math.Pow(PendingScaleRadius, 3))
        };

        _controller.AddBody(body);
        _controller.WithEngineLock(engine => engine.LogEvent($"Spawned Dark Matter Halo '{body.Name}' dynamically."));
        PendingBodyName = string.Empty;
        _projectService.MarkDirty();
    }

    // --- Placement Workflow Methods ---
    public void UpdateGhostPosition(double x, double y, double z)
    {
        if (CurrentMode == UiMode.AddPlacement && PlacementPhase == PlacementPhase.ChoosingPosition)
        {
            GhostPosition = new Vector3d(x, y, z);
        }
    }

    public void ConfirmPosition()
    {
        if (CurrentMode == UiMode.AddPlacement && PlacementPhase == PlacementPhase.ChoosingPosition)
        {
            PlacedPosition = GhostPosition;
            VelocityEndpoint = GhostPosition;
            PlacementPhase = PlacementPhase.ChoosingVelocity;
            ComputeTrajectoryPreview();
        }
    }

    public void UpdateVelocityEndpoint(double x, double y, double z)
    {
        if (CurrentMode == UiMode.AddPlacement && PlacementPhase == PlacementPhase.ChoosingVelocity)
        {
            VelocityEndpoint = new Vector3d(x, y, z);
            ComputeTrajectoryPreview();
        }
    }

    public List<Vector3d> ComputeTrajectoryPreview()
    {
        var preview = new List<Vector3d>();
        if (PlacementPhase != PlacementPhase.ChoosingVelocity)
            return preview;

        Vector3d pos = PlacedPosition;
        Vector3d vel = (VelocityEndpoint - PlacedPosition) * VelocityScaleFactor;

        var otherBodies = new List<CelestialBody>();
        _controller.WithEngineLock(engine =>
        {
            otherBodies.AddRange(engine.Bodies);
        });

        double previewDt = 500.0;
        double softeningSq = Softening * Softening;
        double g = 6.67430e-11;

        for (int step = 0; step < 50; step++)
        {
            Vector3d acc = new Vector3d(0, 0, 0);
            foreach (var body in otherBodies)
            {
                Vector3d direction = body.Position - pos;
                double distSq = direction.LengthSquared();
                double dist = Math.Sqrt(distSq);
                if (dist > 1e-3)
                {
                    double forceMag = (g * body.Mass) / (distSq + softeningSq);
                    acc += (direction / dist) * forceMag;
                }
            }

            vel += acc * previewDt;
            pos += vel * previewDt;
            preview.Add(pos);
        }

        TrajectoryPreview = preview;
        return preview;
    }

    public void ConfirmVelocityAndPlace(string categoryName = "Planet", double mass = 5.972e24, double radius = 6.371e6)
    {
        if (CurrentMode == UiMode.AddPlacement && PlacementPhase == PlacementPhase.ChoosingVelocity)
        {
            int currentBodyCount = 0;
            _controller.WithEngineLock(engine => currentBodyCount = engine.Bodies.Count);
            if (currentBodyCount >= MaxBodies) return;

            RecordUndoSnapshot();

            var id = (categoryName.ToLower() == "star" ? "star-" : "planet-") + Guid.NewGuid().ToString().Substring(0, 4);
            var body = new CelestialBody
            {
                Id = id,
                Name = categoryName + " " + id.ToUpper(),
                BodyType = categoryName.ToLower() == "star" ? CelestialBodyType.Star : CelestialBodyType.Planet,
                ObjectType = categoryName.ToLower() == "star" ? AstrophysicalObjectType.Star : AstrophysicalObjectType.Planet,
                Position = PlacedPosition,
                Velocity = (VelocityEndpoint - PlacedPosition) * VelocityScaleFactor,
                Mass = mass,
                Radius = radius
            };

            if (body.BodyType == CelestialBodyType.Star)
            {
                body.Thermodynamics = new ThermodynamicState { Temperature = 5778.0, HeatCapacity = 1e12, InternalEnergy = 5778.0 * 1e12 };
                body.Stellar = new StellarProperties { Phase = StellarPhase.MainSequence, Age = 1e9 };
            }
            else
            {
                body.Thermodynamics = new ThermodynamicState { Temperature = 288.0, HeatCapacity = 1e10, InternalEnergy = 288.0 * 1e10 };
            }

            _controller.AddBody(body);
            _controller.WithEngineLock(engine => engine.LogEvent($"Placed body '{body.Name}' dynamically with drag velocity."));
            _projectService.MarkDirty();

            PlacementPhase = PlacementPhase.ChoosingPosition;
            TrajectoryPreview = new List<Vector3d>();
        }
    }

    public void PlaceWithZeroVelocity(string categoryName = "Planet", double mass = 5.972e24, double radius = 6.371e6)
    {
        if (CurrentMode == UiMode.AddPlacement && (PlacementPhase == PlacementPhase.ChoosingPosition || PlacementPhase == PlacementPhase.ChoosingVelocity))
        {
            int currentBodyCount = 0;
            _controller.WithEngineLock(engine => currentBodyCount = engine.Bodies.Count);
            if (currentBodyCount >= MaxBodies) return;

            RecordUndoSnapshot();

            var pos = PlacementPhase == PlacementPhase.ChoosingVelocity ? PlacedPosition : GhostPosition;
            var id = (categoryName.ToLower() == "star" ? "star-" : "planet-") + Guid.NewGuid().ToString().Substring(0, 4);
            var body = new CelestialBody
            {
                Id = id,
                Name = categoryName + " " + id.ToUpper(),
                BodyType = categoryName.ToLower() == "star" ? CelestialBodyType.Star : CelestialBodyType.Planet,
                ObjectType = categoryName.ToLower() == "star" ? AstrophysicalObjectType.Star : AstrophysicalObjectType.Planet,
                Position = pos,
                Velocity = new Vector3d(0, 0, 0),
                Mass = mass,
                Radius = radius
            };

            if (body.BodyType == CelestialBodyType.Star)
            {
                body.Thermodynamics = new ThermodynamicState { Temperature = 5778.0, HeatCapacity = 1e12, InternalEnergy = 5778.0 * 1e12 };
                body.Stellar = new StellarProperties { Phase = StellarPhase.MainSequence, Age = 1e9 };
            }
            else
            {
                body.Thermodynamics = new ThermodynamicState { Temperature = 288.0, HeatCapacity = 1e10, InternalEnergy = 288.0 * 1e10 };
            }

            _controller.AddBody(body);
            _controller.WithEngineLock(engine => engine.LogEvent($"Placed body '{body.Name}' dynamically with zero velocity."));
            _projectService.MarkDirty();

            PlacementPhase = PlacementPhase.ChoosingPosition;
            TrajectoryPreview = new List<Vector3d>();
        }
    }

    public void CancelPlacement()
    {
        CurrentMode = UiMode.Idle;
        PlacementPhase = PlacementPhase.None;
        TrajectoryPreview = new List<Vector3d>();
    }

    // --- Undo/Redo Methods ---
    private void RecordUndoSnapshot()
    {
        CelestialBody[]? snapshot = null;
        _controller.WithEngineLock(state =>
        {
            snapshot = state.Bodies.Select(b => new CelestialBody
            {
                Id = b.Id,
                Name = b.Name,
                BodyType = b.BodyType,
                ObjectType = b.ObjectType,
                Position = b.Position,
                Velocity = b.Velocity,
                Mass = b.Mass,
                Radius = b.Radius,
                Thermodynamics = b.Thermodynamics != null ? new ThermodynamicState
                {
                    Temperature = b.Thermodynamics.Temperature,
                    HeatCapacity = b.Thermodynamics.HeatCapacity,
                    InternalEnergy = b.Thermodynamics.InternalEnergy
                } : null,
                Stellar = b.Stellar != null ? new StellarProperties
                {
                    Phase = b.Stellar.Phase,
                    Age = b.Stellar.Age,
                    Luminosity = b.Stellar.Luminosity
                } : null,
                DarkMatter = b.DarkMatter != null ? new DarkMatterHalo
                {
                    HaloMass = b.DarkMatter.HaloMass,
                    ScaleRadius = b.DarkMatter.ScaleRadius,
                    ConcentrationParameter = b.DarkMatter.ConcentrationParameter,
                    LocalDensity = b.DarkMatter.LocalDensity
                } : null
            }).ToArray();
        });

        if (snapshot != null)
        {
            _undoStack.Push(snapshot);
            if (_undoStack.Count > 32)
            {
                var list = _undoStack.ToList();
                list.RemoveAt(list.Count - 1);
                _undoStack.Clear();
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    _undoStack.Push(list[i]);
                }
            }
            _redoStack.Clear();
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }
    }

    private void Undo()
    {
        if (_undoStack.Count == 0) return;

        CelestialBody[]? currentSnapshot = null;
        _controller.WithEngineLock(state =>
        {
            currentSnapshot = state.Bodies.Select(b => new CelestialBody
            {
                Id = b.Id,
                Name = b.Name,
                BodyType = b.BodyType,
                ObjectType = b.ObjectType,
                Position = b.Position,
                Velocity = b.Velocity,
                Mass = b.Mass,
                Radius = b.Radius,
                Thermodynamics = b.Thermodynamics != null ? new ThermodynamicState
                {
                    Temperature = b.Thermodynamics.Temperature,
                    HeatCapacity = b.Thermodynamics.HeatCapacity,
                    InternalEnergy = b.Thermodynamics.InternalEnergy
                } : null,
                Stellar = b.Stellar != null ? new StellarProperties
                {
                    Phase = b.Stellar.Phase,
                    Age = b.Stellar.Age,
                    Luminosity = b.Stellar.Luminosity
                } : null,
                DarkMatter = b.DarkMatter != null ? new DarkMatterHalo
                {
                    HaloMass = b.DarkMatter.HaloMass,
                    ScaleRadius = b.DarkMatter.ScaleRadius,
                    ConcentrationParameter = b.DarkMatter.ConcentrationParameter,
                    LocalDensity = b.DarkMatter.LocalDensity
                } : null
            }).ToArray();
        });

        if (currentSnapshot != null)
        {
            _redoStack.Push(currentSnapshot);
        }

        var undoState = _undoStack.Pop();
        _controller.ReplaceBodies(undoState);
        _projectService.MarkDirty();

        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    private void Redo()
    {
        if (_redoStack.Count == 0) return;

        CelestialBody[]? currentSnapshot = null;
        _controller.WithEngineLock(state =>
        {
            currentSnapshot = state.Bodies.Select(b => new CelestialBody
            {
                Id = b.Id,
                Name = b.Name,
                BodyType = b.BodyType,
                ObjectType = b.ObjectType,
                Position = b.Position,
                Velocity = b.Velocity,
                Mass = b.Mass,
                Radius = b.Radius,
                Thermodynamics = b.Thermodynamics != null ? new ThermodynamicState
                {
                    Temperature = b.Thermodynamics.Temperature,
                    HeatCapacity = b.Thermodynamics.HeatCapacity,
                    InternalEnergy = b.Thermodynamics.InternalEnergy
                } : null,
                Stellar = b.Stellar != null ? new StellarProperties
                {
                    Phase = b.Stellar.Phase,
                    Age = b.Stellar.Age,
                    Luminosity = b.Stellar.Luminosity
                } : null,
                DarkMatter = b.DarkMatter != null ? new DarkMatterHalo
                {
                    HaloMass = b.DarkMatter.HaloMass,
                    ScaleRadius = b.DarkMatter.ScaleRadius,
                    ConcentrationParameter = b.DarkMatter.ConcentrationParameter,
                    LocalDensity = b.DarkMatter.LocalDensity
                } : null
            }).ToArray();
        });

        if (currentSnapshot != null)
        {
            _undoStack.Push(currentSnapshot);
        }

        var redoState = _redoStack.Pop();
        _controller.ReplaceBodies(redoState);
        _projectService.MarkDirty();

        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    private void OnSnapshotUpdated(SimulationSnapshot snapshot)
    {
        _dispatcher.BeginInvoke(() =>
        {
            var dispSw = Stopwatch.StartNew();

            ElapsedTime = snapshot.ElapsedTime;
            IsPlaying = snapshot.IsRunning;

            if (snapshot.IsRunning)
            {
                _projectService.MarkDirty();
            }

            // Compute live energy statistics
            double ke = 0.0;
            double pe = 0.0;
            double g = 6.67430e-11;
            Vector3d totalMom = new Vector3d(0, 0, 0);

            var list = new List<BodySnapshot>(snapshot.Bodies);
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                double vSq = b.Velocity.X * b.Velocity.X + b.Velocity.Y * b.Velocity.Y + b.Velocity.Z * b.Velocity.Z;
                ke += 0.5 * b.Mass * vSq;
                totalMom += b.Velocity * b.Mass;

                for (int j = i + 1; j < list.Count; j++)
                {
                    var o = list[j];
                    double dx = o.Position.X - b.Position.X;
                    double dy = o.Position.Y - b.Position.Y;
                    double dz = o.Position.Z - b.Position.Z;
                    double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    if (dist > 1e-3)
                    {
                        pe -= (g * b.Mass * o.Mass) / dist;
                    }
                }
            }

            KineticEnergy = ke;
            PotentialEnergy = pe;
            TotalEnergy = ke + pe;
            TotalMomentum = totalMom.Length();

            if (list.Count > 0)
            {
                if (_initialTotalEnergy == null)
                {
                    _initialTotalEnergy = TotalEnergy;
                }

                double initialE = _initialTotalEnergy.Value;
                EnergyDrift = Math.Abs(initialE) > 1e-15 ? ((TotalEnergy - initialE) / Math.Abs(initialE)) * 100.0 : 0.0;
            }
            else
            {
                _initialTotalEnergy = null;
                EnergyDrift = 0.0;
            }

            // Performance diagnostics values
            PhysicsTimeMs = snapshot.PhysicsTickMs;

            var now = DateTime.UtcNow;
            double elapsedSeconds = (now - _lastSnapshotTime).TotalSeconds;
            _lastSnapshotTime = now;
            if (elapsedSeconds > 0)
            {
                Fps = 1.0 / elapsedSeconds;
                if (Fps > 60.0) Fps = 60.0; // clamp to 60 for presentation stability
            }

            // Update SelectedBody details if selected
            if (SelectedBody != null)
            {
                var matched = list.Find(b => b.Id == SelectedBody.Id);
                if (matched != null)
                {
                    SelectedBody = matched;

                    double escapeVel = Math.Sqrt(2 * g * matched.Mass / Math.Max(1.0, matched.Radius));
                    EscapeVelocity = escapeVel;
                    OnPropertyChanged(nameof(EscapeVelocity));

                    double hillSp = matched.Position.Length() * Math.Pow(matched.Mass / (3.0 * 1.989e30), 1.0 / 3.0);
                    HillSphere = hillSp;
                    OnPropertyChanged(nameof(HillSphere));

                    // Schwarzschild radius (rs = 2GM/c²)
                    double c = 2.998e8;
                    SchwarzschildRadius = 2.0 * g * matched.Mass / (c * c);

                    // Orbital period estimate — find nearest massive body and apply Kepler's 3rd law
                    double minDist = double.MaxValue;
                    double centralMass = 0;
                    foreach (var other in list)
                    {
                        if (other.Id == matched.Id) continue;
                        double dx = other.Position.X - matched.Position.X;
                        double dy = other.Position.Y - matched.Position.Y;
                        double dist = Math.Sqrt(dx * dx + dy * dy);
                        if (dist < minDist && other.Mass > matched.Mass)
                        {
                            minDist = dist;
                            centralMass = other.Mass;
                        }
                    }
                    if (centralMass > 0 && minDist < double.MaxValue)
                    {
                        OrbitalPeriodEstimate = 2.0 * Math.PI * Math.Sqrt(Math.Pow(minDist, 3) / (g * centralMass));
                    }
                    else
                    {
                        OrbitalPeriodEstimate = 0;
                    }
                }
            }

            // Repopulate active body snapshots
            Bodies.Clear();
            foreach (var body in snapshot.Bodies)
            {
                Bodies.Add(body);
            }
            OnPropertyChanged(nameof(BodyCount));
            OnPropertyChanged(nameof(IsAtBodyLimit));

            var selectedCentral = SelectedCentralBody;
            CentralBodies.Clear();
            for (int i = 0; i < snapshot.Bodies.Count; i++)
            {
                var bodyStr = $"[{i}] {snapshot.Bodies[i].Name}";
                CentralBodies.Add(bodyStr);
            }
            if (CentralBodies.Contains(selectedCentral))
            {
                SelectedCentralBody = selectedCentral;
            }
            else if (CentralBodies.Count > 0)
            {
                SelectedCentralBody = CentralBodies[0];
            }

            // Formatted elapsed time
            double totalSec = snapshot.ElapsedTime;
            double years = totalSec / 3.156e7;
            double days = (totalSec % 3.156e7) / 86400.0;
            if (years >= 1.0)
                ElapsedTimeFormatted = $"{years:F1} yrs {(int)days:D3} days";
            else if (days >= 1.0)
                ElapsedTimeFormatted = $"{days:F1} days";
            else
                ElapsedTimeFormatted = $"{totalSec:F0} s";

            // Sync Event Log items
            if (snapshot.EventLog.Count != EventLog.Count)
            {
                EventLog.Clear();
                foreach (var evt in snapshot.EventLog)
                {
                    EventLog.Add(evt);
                }
            }

            dispSw.Stop();
            RenderTimeMs = dispSw.Elapsed.TotalMilliseconds;
        });
    }

    private void PlaySimulation()
    {
        _controller.Play();
        IsPlaying = true;
    }

    private void PauseSimulation()
    {
        _controller.Pause();
        IsPlaying = false;
    }

    private void StepSimulation()
    {
        _controller.Step(1.0);
    }

    private void SaveProject()
    {
        _projectService.Save();
        OnPropertyChanged(nameof(CurrentProject));
    }

    private void ExitWorkspace()
    {
        PauseSimulation();

        if (_projectService.HasUnsavedChanges)
        {
            RequestExitConfirmation?.Invoke((confirmAction) =>
            {
                if (confirmAction == "save")
                {
                    _projectService.Save();
                    _navigationService.NavigateTo(NavigationTarget.ModeSelection);
                }
                else if (confirmAction == "discard")
                {
                    _navigationService.NavigateTo(NavigationTarget.ModeSelection);
                }
            });
        }
        else
        {
            _navigationService.NavigateTo(NavigationTarget.ModeSelection);
        }
    }
}
