using System;
using System.Collections.Generic;

namespace Kurs.Teacher.ViewModels;

public sealed class ScheduleDayViewModel(DateTimeOffset date, IReadOnlyList<ScheduleItemViewModel> items)
{
    public DateTimeOffset Date { get; } = date;
    public string Title => Date.ToString("ddd, dd MMM");
    public IReadOnlyList<ScheduleItemViewModel> Items { get; } = items;
}

public sealed record ScheduleDropRequest(
    ScheduleItemViewModel Source,
    DateTimeOffset TargetDate,
    ScheduleItemViewModel? TargetItem = null);

public sealed record ScheduleLessonDropRequest(
    LessonViewModel Lesson,
    DateTimeOffset TargetDate,
    ScheduleItemViewModel? TargetItem = null);