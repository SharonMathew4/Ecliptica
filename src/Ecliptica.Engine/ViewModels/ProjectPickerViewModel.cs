using System.Collections.ObjectModel;
using System.Windows.Input;
using Ecliptica.Core.Enums;
using Ecliptica.Core.Interfaces;
using Ecliptica.Core.Models;
using Ecliptica.Engine.Commands;

namespace Ecliptica.Engine.ViewModels;

public class ProjectPickerViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IProjectService _projectService;
    private ObservableCollection<ProjectInfo> _projects = new();
    private ProjectInfo? _selectedProject;
    private string _newProjectName = string.Empty;

    public ObservableCollection<ProjectInfo> Projects
    {
        get => _projects;
        set => SetProperty(ref _projects, value);
    }

    public ProjectInfo? SelectedProject
    {
        get => _selectedProject;
        set => SetProperty(ref _selectedProject, value);
    }

    public string NewProjectName
    {
        get => _newProjectName;
        set => SetProperty(ref _newProjectName, value);
    }

    public ICommand CreateProjectCommand { get; }
    public ICommand DeleteProjectCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand BackCommand { get; }

    public ProjectPickerViewModel(INavigationService navigationService, IProjectService projectService)
    {
        _navigationService = navigationService;
        _projectService = projectService;

        CreateProjectCommand = new RelayCommand(CreateProject, () => !string.IsNullOrWhiteSpace(NewProjectName));
        DeleteProjectCommand = new RelayCommand(DeleteProject, () => SelectedProject != null);
        NextCommand = new RelayCommand(Next, () => SelectedProject != null);
        BackCommand = new RelayCommand(() => _navigationService.NavigateTo(NavigationTarget.ModeSelection));

        RefreshProjectsList();
    }

    private void RefreshProjectsList()
    {
        Projects = new ObservableCollection<ProjectInfo>(_projectService.GetProjects());
    }

    private void CreateProject()
    {
        if (string.IsNullOrWhiteSpace(NewProjectName)) return;

        var proj = _projectService.CreateProject(NewProjectName);
        RefreshProjectsList();
        SelectedProject = Projects.FirstOrDefault(p => p.Id == proj.Id);
        NewProjectName = string.Empty;
    }

    private void DeleteProject()
    {
        if (SelectedProject == null) return;

        _projectService.DeleteProject(SelectedProject.Id);
        RefreshProjectsList();
        SelectedProject = Projects.FirstOrDefault();
    }

    private void Next()
    {
        if (SelectedProject == null) return;

        _projectService.LoadProject(SelectedProject.Id);
        _navigationService.NavigateTo(NavigationTarget.SimulationWorkspace);
    }
}
