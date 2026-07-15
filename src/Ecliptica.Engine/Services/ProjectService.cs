using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;
using Ecliptica.Core.Enums;
using Ecliptica.Infrastructure;

namespace Ecliptica.Engine.Services;

public class ProjectService : IProjectService
{
    private readonly List<ProjectInfo> _projects = new();
    private ProjectInfo? _currentProject;
    private bool _hasUnsavedChanges;
    private readonly ProjectStorageManager _storageManager;

    public ProjectService()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string baseDir = Path.Combine(localAppData, "Ecliptica", "Projects");
        _storageManager = new ProjectStorageManager(baseDir);

        // Pre-populate with a demo/placeholder project
        _projects.Add(new ProjectInfo(
            Id: "solar_system_sandbox",
            Name: "Solar System Sandbox",
            FilePath: Path.Combine(baseDir, "solar_system_sandbox"),
            CreatedAt: DateTime.Now.AddDays(-2),
            LastModifiedAt: DateTime.Now.AddDays(-1)
        ));

        // Create empty folders for it so we can load it
        string demoDir = Path.Combine(baseDir, "solar_system_sandbox");
        if (!Directory.Exists(demoDir))
        {
            Directory.CreateDirectory(demoDir);
            var defaultState = new SimulationState();
            // Add a mock sun
            defaultState.Bodies.Add(new CelestialBody
            {
                Id = "sun",
                Name = "Sun",
                BodyType = CelestialBodyType.Star,
                ObjectType = AstrophysicalObjectType.Star,
                Mass = 1.989e30,
                Radius = 6.957e8
            });
            _storageManager.SaveProject(_projects[0], defaultState);
        }
    }

    public IReadOnlyList<ProjectInfo> GetProjects() => _projects;

    public ProjectInfo CreateProject(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty", nameof(name));

        var id = name.ToLower().Replace(" ", "_") + "_" + Guid.NewGuid().ToString().Substring(0, 8);
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string baseDir = Path.Combine(localAppData, "Ecliptica", "Projects");
        string filePath = Path.Combine(baseDir, id);

        var project = new ProjectInfo(
            Id: id,
            Name: name,
            FilePath: filePath,
            CreatedAt: DateTime.Now,
            LastModifiedAt: DateTime.Now
        );

        _projects.Add(project);
        LoadProject(id);
        
        // Save initial empty state immediately
        _storageManager.SaveProject(project, new SimulationState());
        _hasUnsavedChanges = true;
        return project;
    }

    public void DeleteProject(string projectId)
    {
        var project = _projects.FirstOrDefault(p => p.Id == projectId);
        if (project != null)
        {
            _projects.Remove(project);
            if (_currentProject?.Id == projectId)
            {
                _currentProject = null;
                _hasUnsavedChanges = false;
            }
            
            // Delete folder on disk
            try
            {
                if (Directory.Exists(project.FilePath))
                {
                    Directory.Delete(project.FilePath, recursive: true);
                }
            }
            catch { }
        }
    }

    public void LoadProject(string projectId)
    {
        var project = _projects.FirstOrDefault(p => p.Id == projectId);
        if (project != null)
        {
            _currentProject = project;
            _hasUnsavedChanges = false;
        }
    }

    public ProjectInfo? CurrentProject => _currentProject;

    public bool HasUnsavedChanges => _hasUnsavedChanges;

    public void MarkDirty()
    {
        if (_currentProject != null)
        {
            _hasUnsavedChanges = true;
        }
    }

    public void Save()
    {
        if (_currentProject != null)
        {
            var idx = _projects.FindIndex(p => p.Id == _currentProject.Id);
            if (idx >= 0)
            {
                var updated = _currentProject with { LastModifiedAt = DateTime.Now };
                _projects[idx] = updated;
                _currentProject = updated;
                
                // Get simulation state reference and persist it (mock/empty if not currently running or from loop)
                var state = new SimulationState();
                try
                {
                    // Sync active loop body state before save if registered
                    var snap = SimulationControllerProvider.Instance;
                }
                catch { }
                
                _storageManager.SaveProject(updated, state);
            }
            _hasUnsavedChanges = false;
        }
    }
}
