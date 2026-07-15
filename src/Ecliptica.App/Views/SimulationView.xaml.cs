using System.Windows;
using System.Windows.Controls;
using Ecliptica.App.Views.Dialogs;
using Ecliptica.Engine.ViewModels;
using Ecliptica.Core.Enums;

namespace Ecliptica.App.Views;

public partial class SimulationView : System.Windows.Controls.UserControl
{
    private Window? _parentWindow;

    public SimulationView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Hook parent window events to keep Popups positioned correctly when window is moved/resized
        _parentWindow = Window.GetWindow(this);
        if (_parentWindow != null)
        {
            _parentWindow.LocationChanged -= OnWindowLocationOrSizeChanged;
            _parentWindow.LocationChanged += OnWindowLocationOrSizeChanged;
            _parentWindow.SizeChanged -= OnWindowLocationOrSizeChanged;
            _parentWindow.SizeChanged += OnWindowLocationOrSizeChanged;
        }

        // Subscribe to singleton loop updates when control is loaded in visual tree
        Ecliptica.Engine.Services.SimulationControllerProvider.Instance.SnapshotUpdated -= OnSnapshotUpdated;
        Ecliptica.Engine.Services.SimulationControllerProvider.Instance.SnapshotUpdated += OnSnapshotUpdated;

        if (DataContext is SimulationViewModel vm)
        {
            vm.RequestExitConfirmation -= OnRequestExitConfirmation;
            vm.RequestExitConfirmation += OnRequestExitConfirmation;
        }

        GLViewport.MouseDown -= OnViewportMouseDown;
        GLViewport.MouseDown += OnViewportMouseDown;
        GLViewport.MouseMove -= OnViewportMouseMove;
        GLViewport.MouseMove += OnViewportMouseMove;
        GLViewport.MouseUp -= OnViewportMouseUp;
        GLViewport.MouseUp += OnViewportMouseUp;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_parentWindow != null)
        {
            _parentWindow.LocationChanged -= OnWindowLocationOrSizeChanged;
            _parentWindow.SizeChanged -= OnWindowLocationOrSizeChanged;
        }

        // Unsubscribe from all updates to prevent memory leaks when control is discarded
        Ecliptica.Engine.Services.SimulationControllerProvider.Instance.SnapshotUpdated -= OnSnapshotUpdated;

        if (DataContext is SimulationViewModel vm)
        {
            vm.RequestExitConfirmation -= OnRequestExitConfirmation;
        }

        GLViewport.MouseDown -= OnViewportMouseDown;
        GLViewport.MouseMove -= OnViewportMouseMove;
        GLViewport.MouseUp -= OnViewportMouseUp;
    }

    private void OnWindowLocationOrSizeChanged(object? sender, EventArgs e)
    {
        // Simple force-repositioning offset trick to sync floating popups
        var popups = new[] { ControlsPopup, EnergyPopup, InsightsPopup, AddBodyPopup, InspectorPopup, PerformancePopup };
        foreach (var popup in popups)
        {
            if (popup != null && popup.IsOpen)
            {
                var offset = popup.HorizontalOffset;
                popup.HorizontalOffset = offset + 0.01;
                popup.HorizontalOffset = offset;
            }
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is SimulationViewModel oldVm)
        {
            oldVm.RequestExitConfirmation -= OnRequestExitConfirmation;
        }
        if (e.NewValue is SimulationViewModel newVm && IsLoaded)
        {
            newVm.RequestExitConfirmation -= OnRequestExitConfirmation;
            newVm.RequestExitConfirmation += OnRequestExitConfirmation;
        }
    }

    private void OnSnapshotUpdated(Core.Models.SimulationSnapshot snapshot)
    {
        // Safe cross-thread invoke to update WinForms graphics panel
        Dispatcher.BeginInvoke(() =>
        {
            GLViewport.UpdateSnapshot(snapshot);
        });
    }

    private void OnRequestExitConfirmation(Action<string> callback)
    {
        var parentWindow = Window.GetWindow(this);
        var dialog = new UnsavedChangesDialog
        {
            Owner = parentWindow
        };

        if (dialog.ShowDialog() == true)
        {
            callback(dialog.Result);
        }
        else
        {
            callback("cancel");
        }
    }

    private void OnViewportMouseDown(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (DataContext is SimulationViewModel vm && vm.CurrentMode == UiMode.AddPlacement)
        {
            var worldPos = GLViewport.ScreenToWorld(e.X, e.Y);
            if (vm.PlacementPhase == PlacementPhase.ChoosingPosition)
            {
                vm.GhostPosition = worldPos;
                vm.ConfirmPosition();
            }
        }
    }

    private void OnViewportMouseMove(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (DataContext is SimulationViewModel vm && vm.CurrentMode == UiMode.AddPlacement)
        {
            var worldPos = GLViewport.ScreenToWorld(e.X, e.Y);
            if (vm.PlacementPhase == PlacementPhase.ChoosingPosition)
            {
                vm.UpdateGhostPosition(worldPos.X, worldPos.Y, worldPos.Z);
            }
            else if (vm.PlacementPhase == PlacementPhase.ChoosingVelocity)
            {
                vm.UpdateVelocityEndpoint(worldPos.X, worldPos.Y, worldPos.Z);
                var velocityVec = (worldPos - vm.PlacedPosition) * vm.VelocityScaleFactor;
                vm.VectorMagnitude = velocityVec.Length();
                vm.DirectionVectorString = $"({velocityVec.X:E2}, {velocityVec.Y:E2}, {velocityVec.Z:E2})";
            }
        }
    }

    private void OnViewportMouseUp(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (DataContext is SimulationViewModel vm && vm.CurrentMode == UiMode.AddPlacement)
        {
            if (vm.PlacementPhase == PlacementPhase.ChoosingVelocity)
            {
                var worldPos = GLViewport.ScreenToWorld(e.X, e.Y);
                vm.UpdateVelocityEndpoint(worldPos.X, worldPos.Y, worldPos.Z);

                double mass = 5.972e24;
                double radius = 6.371e6;
                string category = vm.SelectedCelestialObject;

                if (category == "Star")
                {
                    mass = 1.989e30;
                    radius = 6.957e8;
                }
                else if (category == "Black Hole")
                {
                    mass = 1.989e30 * 10;
                    radius = 3.0e4;
                }
                else if (category == "Dark Matter Halo")
                {
                    mass = vm.PendingHaloMass;
                    radius = vm.PendingScaleRadius;
                }
                else if (category == "Asteroid")
                {
                    mass = 1e12;
                    radius = 1000.0;
                }
                else if (category == "Moon")
                {
                    mass = 7.347e22;
                    radius = 1.737e6;
                }

                vm.ConfirmVelocityAndPlace(category, mass, radius);
            }
        }
    }
}
// Note: dialog logic gets bound properly via code-behind event handling.
