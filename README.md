# Ecliptica

Ecliptica is a professional desktop application for astronomical simulation and real-universe observation. It is designed as a modular, scalable, high-performance scientific platform rather than a game engine.

## Modes of Operation

- **Simulation Mode**: A sandbox where users can build and explore fictional or scientifically inspired universes. Features include N-body simulations, procedural generation of celestial bodies (stars, planets, black holes, nebulae), and timeline management.
- **Observation Mode**: A mode for exploring and analyzing the real universe using grounded astronomical catalogs (e.g., Gaia, Hipparcos, NASA data) and ephemeris data. 

## Tech Stack

- **Language**: C#
- **Runtime**: .NET 10
- **UI**: WPF
- **Rendering API**: OpenGL through Silk.NET (with future Vulkan/DirectX considerations)
- **Serialization**: System.Text.Json
- **Testing**: xUnit
- **Architecture Style**: Clean Architecture with strict Dependency Injection

## Architecture and Modules

Ecliptica uses a strict multi-project solution structure to enforce separation of concerns.

- **`Ecliptica.App`**: WPF presentation layer. Orchestrates the visible shell. Depends only on `Ecliptica.Engine`.
- **`Ecliptica.Engine`**: Application orchestrator, mode switching, and lifecycle coordinator. Depends only on `Ecliptica.Core`.
- **`Ecliptica.Core`**: Shared foundational code (math primitives, interfaces, constants). Depends on nothing.
- **`Ecliptica.Physics`**: Physics logic (gravity, N-body computations, orbital mechanics). Depends only on `Ecliptica.Core`.
- **`Ecliptica.Simulation`**: Sandbox fictional universe creation workflows. Depends on `Ecliptica.Core`, `Ecliptica.Physics`, and `Ecliptica.Renderer`.
- **`Ecliptica.Observation`**: Real astronomical data ingestion and representation. Depends on `Ecliptica.Core`, `Ecliptica.Renderer`, and `Ecliptica.Infrastructure`.
- **`Ecliptica.Renderer`**: Mode-agnostic rendering pipeline translating abstract primitives to GPU resources. Depends on `Ecliptica.Core` and `Ecliptica.Infrastructure`.
- **`Ecliptica.Infrastructure`**: External cross-cutting concerns (File IO, JSON serialization, HTTP clients, DB access). Depends only on `Ecliptica.Core`.

### Dependency Rules
The dependency graph must always point toward **Core**. No circular dependencies are allowed.

## Core Design Principles
- Single Responsibility Principle & Separation of Concerns
- Composition over inheritance
- Interface-based design
- Thread-safe, data-oriented design where beneficial
- GPU-first rendering pipeline with mode-agnostic abstractions

