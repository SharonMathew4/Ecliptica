using System;
using Ecliptica.Core.Enums;
using Ecliptica.Core.Interfaces;

namespace Ecliptica.Engine.Services;

public class NavigationService : INavigationService
{
    private NavigationTarget _currentTarget = NavigationTarget.Splash;

    public NavigationTarget CurrentTarget => _currentTarget;

    public event Action<NavigationTarget>? Navigated;

    public void NavigateTo(NavigationTarget target)
    {
        if (_currentTarget == target) return;

        // Perform basic transition validation if needed.
        _currentTarget = target;
        Navigated?.Invoke(target);
    }
}
