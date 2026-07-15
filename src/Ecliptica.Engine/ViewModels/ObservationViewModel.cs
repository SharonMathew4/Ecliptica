using System.Windows.Input;
using Ecliptica.Core.Enums;
using Ecliptica.Core.Interfaces;
using Ecliptica.Engine.Commands;

namespace Ecliptica.Engine.ViewModels;

public class ObservationViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public ICommand ExitCommand { get; }

    public ObservationViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        ExitCommand = new RelayCommand(() => _navigationService.NavigateTo(NavigationTarget.ModeSelection));
    }
}
