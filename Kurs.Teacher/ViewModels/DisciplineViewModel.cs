using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;

namespace Kurs.Teacher.ViewModels;

public partial class DisciplineViewModel : ViewModelBase
{
    public DisciplineViewModel(string title, Guid? id = null, string color = "#E8ECFF", string description = "")
    {
        this.title = title;
        this.description = description;
        Id = id ?? Guid.NewGuid();
        Color = color;
        Lessons.CollectionChanged += (_, _) => RefreshGroups();
        Topics.CollectionChanged += (_, _) => RefreshGroups();
    }
    public Guid Id { get; }
    public string Color { get; }
    [ObservableProperty] private string title;
    [ObservableProperty] private string description;
    public ObservableCollection<LessonViewModel> Lessons { get; } = [];
    public ObservableCollection<TopicViewModel> Topics { get; } = [];
    public ObservableCollection<LessonViewModel> UngroupedLessons { get; } = [];
    private readonly HashSet<LessonViewModel> observed = [];

    private void LessonChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LessonViewModel.Topic)) RefreshGroups();
    }

    public void RefreshGroups()
    {
        foreach (var lesson in new List<LessonViewModel>(observed))
            if (!Lessons.Contains(lesson)) { lesson.PropertyChanged -= LessonChanged; observed.Remove(lesson); }
        UngroupedLessons.Clear();
        foreach (var topic in Topics) topic.Lessons.Clear();
        foreach (var lesson in Lessons)
        {
            if (observed.Add(lesson)) lesson.PropertyChanged += LessonChanged;
            if (lesson.Topic is not null && Topics.Contains(lesson.Topic)) lesson.Topic.Lessons.Add(lesson);
            else UngroupedLessons.Add(lesson);
        }
    }
}
