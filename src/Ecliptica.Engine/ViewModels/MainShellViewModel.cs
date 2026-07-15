using Ecliptica.Core.Enums;
using Ecliptica.Core.Interfaces;

namespace Ecliptica.Engine.ViewModels;

public class MainShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IProjectService _projectService;
    private object? _currentView;

    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public SplashViewModel SplashVM { get; }
    public ModeSelectionViewModel ModeSelectionVM { get; }
    public ProjectPickerViewModel ProjectPickerVM { get; }
    public SimulationViewModel SimulationVM { get; }
    public ObservationViewModel ObservationVM { get; }

    public MainShellViewModel(INavigationService navigationService, IProjectService projectService)
    {
        _navigationService = navigationService;
        _projectService = projectService;

        SplashVM = new SplashViewModel(_navigationService);
        ModeSelectionVM = new ModeSelectionViewModel(_navigationService);
        ProjectPickerVM = new ProjectPickerViewModel(_navigationService, _projectService);
        SimulationVM = new SimulationViewModel(_navigationService, _projectService);
        ObservationVM = new ObservationViewModel(_navigationService);

        _navigationService.Navigated += OnNavigated;

        // Start at Splash
        OnNavigated(_navigationService.CurrentTarget);
    }

    private bool _isSimulationActive;
    public bool IsSimulationActive
    {
        get => _isSimulationActive;
        set => SetProperty(ref _isSimulationActive, value);
    }

    private void OnNavigated(NavigationTarget target)
    {
        CurrentView = target switch
        {
            NavigationTarget.Splash => SplashVM,
            NavigationTarget.ModeSelection => ModeSelectionVM,
            NavigationTarget.SimulationProjectPicker => ProjectPickerVM,
            NavigationTarget.SimulationWorkspace => SimulationVM,
            NavigationTarget.ObservationWorkspace => ObservationVM,
            _ => null
        };

        IsSimulationActive = target == NavigationTarget.SimulationWorkspace;
    }
}
