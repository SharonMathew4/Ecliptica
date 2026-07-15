# Contributing to Ecliptica

Thank you for your interest in contributing to Ecliptica! This project is designed as a highly structured, modular, and high-performance scientific sandbox for astronomical simulations and universe observation.

---

## Code Architecture & Modular Layer Boundaries

To keep the project clean, testable, and robust, Ecliptica enforces strict architectural separation. When writing code, place it in the appropriate assembly layers:

1. **`Ecliptica.Core`**: 
   - Contains all shared contracts, interfaces, enums, models, and physical constants.
   - Depends on nothing. Do not add references to other projects here.
2. **`Ecliptica.Physics`**: 
   - Contains pure scientific math, astrophysics, gravity solvers (N-body, Barnes-Hut, NFW dark matter profiles), collisions, and stellar evolution.
   - Depends only on `Ecliptica.Core`. No references to UI, networking, or file persistence.
3. **`Ecliptica.Simulation`**: 
   - Contains background ticking loops, builders, and engine coordinators.
   - Depends on `Ecliptica.Core`, `Ecliptica.Physics`, and `Ecliptica.Renderer`.
4. **`Ecliptica.Engine`**: 
   - MVVM orchestrator (ViewModels, commands, navigation service).
   - Depends only on `Ecliptica.Core` and other abstract contracts.
5. **`Ecliptica.Renderer`**: 
   - Mode-agnostic OpenGL execution context via Silk.NET.
6. **`Ecliptica.App`**: 
   - WPF presentation view shell (WPF views, visual assets, XAML style files).

---

## Guidelines for Adding New Physics Modules

If you want to implement a new physical behavior, event, or interaction system (e.g. Phase 10's galaxy clustering, stellar binary mass transfer, or supernova remnant expansion):

1. **Implement `IPhysicsSystem`**:
   Define your new system inside the `Ecliptica.Physics` project (e.g. inside `Ecliptica.Physics/Structure` or `Ecliptica.Physics/Remnants`):
   ```csharp
   public class MyNewPhysicsSystem : IPhysicsSystem
   {
       public string Name => "My New Physics Evaluator";
       public int Priority => 10; // set execution precedence

       public void Update(SimulationState state, double deltaTime)
       {
           // Apply astrophysical adjustments to body positions, velocities, or variables here
       }
   }
   ```

2. **Add Builder Method in `SimulationBuilder.cs`**:
   Expose a configuration chain hook inside `Ecliptica.Simulation.SimulationBuilder`:
   ```csharp
   public SimulationBuilder WithMyNewSystem()
   {
       _systemsToRegister.Add(new MyNewPhysicsSystem());
       return this;
   }
   ```

3. **Register in Application Startup**:
   Chain the method in `App.xaml.cs` to enable the module by default in the runtime environment.

---

## Coding Rules & Standards

* **No UI Logic in Physics:** All calculations must remain pure, math-driven, and testable without active views.
* **WPF View Layer Thinness:** Keep Views strictly layout/binding-based. Do not execute heavy simulation updates or calculations inside WPF code-behind files.
* **Generic Commands Safety:** When designing XAML command triggers, use generic `RelayCommand<T>` types and verify that command parameters are parsed safely using robust conversions (like `Convert.ChangeType`) to prevent runtime casting crashes.

---

## Verification & Testing

Every new feature or physical system must include unit tests. Place test suites in the `tests/` directory matching the module name (e.g. `tests/Ecliptica.Physics.Tests`).

Run tests using:
```powershell
dotnet test
```
All pull requests must pass all tests with 0 failures prior to merges.
