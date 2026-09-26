using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace Kurs.Teacher.ViewModels;

public partial class TopicViewModel(string title, Guid? id = null, string description = "") : ViewModelBase
{
    public Guid Id { get; } = id ?? Guid.NewGuid();
    [ObservableProperty] private string title = title;
    [ObservableProperty] private string description = description;
    public ObservableCollection<LessonViewModel> Lessons { get; } = [];
}
