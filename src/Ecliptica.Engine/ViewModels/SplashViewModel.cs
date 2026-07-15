using Ecliptica.Core.Interfaces;

namespace Ecliptica.Engine.ViewModels;

public class SplashViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public SplashViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }
}
