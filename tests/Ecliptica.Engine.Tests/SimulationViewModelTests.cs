using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Ecliptica.Core.Enums;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;
using Ecliptica.Engine.Services;
using Ecliptica.Engine.ViewModels;

namespace Ecliptica.Engine.Tests;

public class SimulationViewModelTests
{
    private readonly MockNavigationService _navigationService;
    private readonly MockProjectService _projectService;
    private readonly MockSimulationController _controller;

    public SimulationViewModelTests()
    {
        _navigationService = new MockNavigationService();
        _projectService = new MockProjectService();
        _controller = new MockSimulationController();

        SimulationControllerProvider.Instance = _controller;
    }

    [Fact]
    public void TestUiModeTransitions()
    {
        var vm = new SimulationViewModel(_navigationService, _projectService);

        Assert.Equal(UiMode.Idle, vm.CurrentMode);
        Assert.False(vm.IsAddMode);
        Assert.True(vm.EnterAddModeCommand.CanExecute(null));

        vm.EnterAddModeCommand.Execute(null);
        Assert.Equal(UiMode.AddPlacement, vm.CurrentMode);
        Assert.True(vm.IsAddMode);
        Assert.Equal(PlacementPhase.ChoosingPosition, vm.PlacementPhase);
        Assert.False(_controller.IsRunning); // Entering non-simulate mode auto-pauses

        vm.EnterSimulateModeCommand.Execute(null);
        Assert.Equal(UiMode.Simulate, vm.CurrentMode);
        Assert.True(vm.IsSimulateMode);
        Assert.True(_controller.IsRunning); // Entering simulate mode auto-plays
    }

    [Fact]
    public void TestTwoStepPlacementWorkflow()
    {
        var vm = new SimulationViewModel(_navigationService, _projectService);

        vm.EnterAddModeCommand.Execute(null);
        vm.UpdateGhostPosition(10.0, 20.0, 30.0);
        Assert.Equal(new Vector3d(10, 20, 30), vm.GhostPosition);

        vm.ConfirmPosition();
        Assert.Equal(PlacementPhase.ChoosingVelocity, vm.PlacementPhase);
        Assert.Equal(new Vector3d(10, 20, 30), vm.PlacedPosition);

        vm.UpdateVelocityEndpoint(15.0, 25.0, 35.0);
        Assert.Equal(new Vector3d(15, 25, 35), vm.VelocityEndpoint);

        // Trajectory preview
        var preview = vm.TrajectoryPreview;
        Assert.NotNull(preview);
        Assert.Equal(50, preview.Count);

        // Finalize placement
        vm.ConfirmVelocityAndPlace("Planet", 1e24, 1e6);
        Assert.Single(_controller.State.Bodies);

        var placedBody = _controller.State.Bodies.First();
        Assert.Equal(new Vector3d(10, 20, 30), placedBody.Position);
        // v = (endpoint - placed) * scale = (15 - 10, 25 - 20, 35 - 30) * 0.5 = (2.5, 2.5, 2.5)
        Assert.Equal(new Vector3d(2.5, 2.5, 2.5), placedBody.Velocity);
        Assert.Equal(1e24, placedBody.Mass);
        Assert.Equal(1e6, placedBody.Radius);

        // Phase reset to ChoosingPosition
        Assert.Equal(PlacementPhase.ChoosingPosition, vm.PlacementPhase);
    }

    [Fact]
    public void TestMaxBodiesLimit()
    {
        var vm = new SimulationViewModel(_navigationService, _projectService)
        {
            MaxBodies = 2
        };

        vm.EnterAddModeCommand.Execute(null);
        vm.UpdateGhostPosition(1, 1, 1);
        vm.ConfirmPosition();
        vm.UpdateVelocityEndpoint(2, 2, 2);
        vm.ConfirmVelocityAndPlace("Planet", 100, 10);

        Assert.Single(_controller.State.Bodies);

        // Add second
        vm.UpdateGhostPosition(3, 3, 3);
        vm.ConfirmPosition();
        vm.UpdateVelocityEndpoint(4, 4, 4);
        vm.ConfirmVelocityAndPlace("Planet", 200, 20);

        Assert.Equal(2, _controller.State.Bodies.Count);

        // Try adding third - should be blocked
        vm.UpdateGhostPosition(5, 5, 5);
        vm.ConfirmPosition();
        vm.UpdateVelocityEndpoint(6, 6, 6);
        vm.ConfirmVelocityAndPlace("Planet", 300, 30);

        Assert.Equal(2, _controller.State.Bodies.Count);
    }

    [Fact]
    public void TestUndoRedoSystem()
    {
        var vm = new SimulationViewModel(_navigationService, _projectService);

        Assert.False(vm.CanUndo);
        Assert.False(vm.CanRedo);

        // Spawning star creates undo snapshot
        vm.SpawnStarCommand.Execute(null);
        Assert.True(vm.CanUndo);
        Assert.False(vm.CanRedo);

        // Undo
        vm.UndoCommand.Execute(null);
        Assert.Empty(_controller.State.Bodies);
        Assert.False(vm.CanUndo);
        Assert.True(vm.CanRedo);

        // Redo
        vm.RedoCommand.Execute(null);
        Assert.Single(_controller.State.Bodies);
        Assert.True(vm.CanUndo);
        Assert.False(vm.CanRedo);
    }

    [Fact]
    public void TestBodyDeletion()
    {
        var vm = new SimulationViewModel(_navigationService, _projectService);
        vm.SpawnPlanetCommand.Execute(null);
        Assert.Single(_controller.State.Bodies);

        var body = _controller.State.Bodies.First();
        vm.SelectedBody = new BodySnapshot(body.Id, body.Name, body.Position, body.Velocity, body.Mass, body.Radius);

        vm.DeleteBodyCommand.Execute(null);
        Assert.Empty(_controller.State.Bodies);
        Assert.Null(vm.SelectedBody);
        Assert.True(vm.CanUndo);
    }

    // --- Mock Implementations ---

    private class MockNavigationService : INavigationService
    {
        public NavigationTarget CurrentTarget { get; set; }
        public event Action<NavigationTarget>? Navigated;
        public void NavigateTo(NavigationTarget target)
        {
            CurrentTarget = target;
            Navigated?.Invoke(target);
        }
    }

    private class MockProjectService : IProjectService
    {
        public IReadOnlyList<ProjectInfo> GetProjects() => new List<ProjectInfo>();
        public ProjectInfo CreateProject(string name) => new ProjectInfo("test-id", name, "path", DateTime.UtcNow, DateTime.UtcNow);
        public void DeleteProject(string projectId) { }
        public void LoadProject(string projectId) { }
        public ProjectInfo? CurrentProject => new ProjectInfo("test-id", "Test Project", "path", DateTime.UtcNow, DateTime.UtcNow);
        public bool HasUnsavedChanges { get; private set; }
        public void MarkDirty() { HasUnsavedChanges = true; }
        public void Save() { HasUnsavedChanges = false; }
    }

    private class MockSimulationController : ISimulationController
    {
        public bool IsRunning { get; private set; }
        public double TimeScale { get; set; } = 1.0;
        public double TargetTickRate { get; set; } = 60.0;

        public event Action<SimulationSnapshot>? SnapshotUpdated;

        public SimulationState State { get; } = new SimulationState();

        public void Initialize(SimulationState state, Action<double> tickCallback) { }

        public void Play() { IsRunning = true; }
        public void Pause() { IsRunning = false; }
        public void Step(double stepSizeSeconds) { }
        public void Shutdown() { }

        public void WithEngineLock(Action<SimulationState> action)
        {
            lock (State)
            {
                action(State);
            }
        }

        public void AddBody(CelestialBody body)
        {
            lock (State)
            {
                State.Bodies.Add(body);
            }
        }

        public void RemoveBody(string bodyId)
        {
            lock (State)
            {
                State.Bodies.RemoveAll(b => b.Id == bodyId);
            }
        }

        public void ReplaceBodies(IEnumerable<CelestialBody> bodies)
        {
            lock (State)
            {
                State.Bodies.Clear();
                State.Bodies.AddRange(bodies);
            }
        }
    }
}
