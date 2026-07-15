using Ecliptica.Core.Enums;

namespace Ecliptica.Core.Interfaces;

public interface INavigationService
{
    NavigationTarget CurrentTarget { get; }
    event Action<NavigationTarget>? Navigated;
    void NavigateTo(NavigationTarget target);
}
