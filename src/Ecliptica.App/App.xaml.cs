using System.Windows;
using Ecliptica.Engine.Services;
using Ecliptica.Engine.ViewModels;

namespace Ecliptica.App;

public partial class App : System.Windows.Application
{
    private NavigationService? _navigationService;
    private ProjectService? _projectService;
    private MainShellViewModel? _mainShellVM;

    protected void OnStartup(object sender, StartupEventArgs e)
    {
        // 1. Initialize core services
        _navigationService = new NavigationService();
        _projectService = new ProjectService();

        // Initialize simulation loop, engine, and link them
        var simState = new Core.Models.SimulationState();
        var simEngine = new Ecliptica.Simulation.SimulationBuilder()
            .WithGravity()
            .WithStellarEvolution()
            .WithThermodynamics()
            .WithAstrophysicalEvents()
            .WithAccretionDisks()
            .WithInterstellarMedium()
            .WithCosmicStructure()
            .WithBinaryInteraction()
            .WithRemnantExpansion()
            .Build();

        var simLoop = new Ecliptica.Simulation.SimulationLoop();
        simLoop.Initialize(simEngine.State, (dt) => simEngine.Tick(dt));
        SimulationControllerProvider.Instance = simLoop;

        // 2. Initialize main view model
        _mainShellVM = new MainShellViewModel(_navigationService, _projectService);

        // 3. Create and show main window
        var mainWindow = new MainWindow
        {
            DataContext = _mainShellVM
        };

        // Handle App close requests from the home screen
        _mainShellVM.ModeSelectionVM.RequestClose += () => mainWindow.Close();

        mainWindow.Show();

        // 4. Run background splash auto-transition (2 seconds delay)
        StartSplashTimer();
    }

    private async void StartSplashTimer()
    {
        await Task.Delay(2000);
        // Navigate from Splash to ModeSelection
        _navigationService?.NavigateTo(Core.Enums.NavigationTarget.ModeSelection);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            SimulationControllerProvider.Instance.Shutdown();
        }
        catch { }
        base.OnExit(e);
    }
}
