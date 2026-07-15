using System.Collections.Generic;
using Ecliptica.Core.Models;

namespace Ecliptica.Core.Interfaces;

public interface IProjectService
{
    IReadOnlyList<ProjectInfo> GetProjects();
    ProjectInfo CreateProject(string name);
    void DeleteProject(string projectId);
    void LoadProject(string projectId);
    ProjectInfo? CurrentProject { get; }
    bool HasUnsavedChanges { get; }
    void MarkDirty();
    void Save();
}
