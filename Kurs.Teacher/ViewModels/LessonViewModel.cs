using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace Kurs.Teacher.ViewModels;

public partial class LessonViewModel(string title, string description, Guid? id = null) : ViewModelBase
{
    public Guid Id { get; } = id ?? Guid.NewGuid();
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private TopicViewModel? topic;
    [ObservableProperty] private string title = title;
    [ObservableProperty] private string description = description;
    public ObservableCollection<MaterialViewModel> Materials { get; } = [];
    public ObservableCollection<TestViewModel> Tests { get; } = [];
}


