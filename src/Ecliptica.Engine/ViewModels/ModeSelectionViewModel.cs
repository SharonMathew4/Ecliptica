using System.Windows.Input;
using Ecliptica.Core.Enums;
using Ecliptica.Core.Interfaces;
using Ecliptica.Engine.Commands;

namespace Ecliptica.Engine.ViewModels;

public class ModeSelectionViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public ICommand EnterSimulationCommand { get; }
    public ICommand EnterObservationCommand { get; }
    public ICommand ExitApplicationCommand { get; }

    public event Action? RequestClose;

    public ModeSelectionViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        EnterSimulationCommand = new RelayCommand(() => _navigationService.NavigateTo(NavigationTarget.SimulationProjectPicker));
        EnterObservationCommand = new RelayCommand(() => _navigationService.NavigateTo(NavigationTarget.ObservationWorkspace));
        ExitApplicationCommand = new RelayCommand(() => RequestClose?.Invoke());
    }
}
