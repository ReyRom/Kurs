using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Kurs.Teacher.ViewModels;

public partial class ScheduleItemViewModel(
    DateTimeOffset dateTime,
    int order,
    Guid lessonId,
    string lessonTitle,
    string disciplineTitle,
    string disciplineColor,
    Guid? id = null) : ViewModelBase
{
    public Guid Id { get; } = id ?? Guid.NewGuid();
    [ObservableProperty] private DateTimeOffset dateTime = dateTime;
    [ObservableProperty] private int order = order;
    public Guid LessonId { get; } = lessonId;
    public string Day => DateTime.ToString("dd MMMM");
    public string LessonTitle { get; } = lessonTitle;
    public string DisciplineTitle { get; } = disciplineTitle;
    public string DisciplineColor { get; } = disciplineColor;
}
