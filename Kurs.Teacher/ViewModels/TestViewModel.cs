using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Kurs.Teacher.ViewModels;

public partial class TestViewModel(string title, Guid? id = null) : ViewModelBase
{
    public Guid Id { get; } = id ?? Guid.NewGuid();
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private string title = title;
    [ObservableProperty] private string lessonTitle = "";
    public int TotalPoints => Questions.Sum(q => q.Points);
    public ObservableCollection<QuestionViewModel> Questions { get; } = [];
}

