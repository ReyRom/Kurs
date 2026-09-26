using Avalonia.Controls;
using Avalonia.Input;
using Kurs.Teacher.ViewModels;
using System;

namespace Kurs.Teacher.Views;

public partial class ScheduleEditorView : UserControl
{
    private static readonly DataFormat<ScheduleItemViewModel> ScheduleItemFormat =
        DataFormat.CreateInProcessFormat<ScheduleItemViewModel>("Kurs.ScheduleItem");
    private static readonly DataFormat<LessonViewModel> LessonFormat =
        DataFormat.CreateInProcessFormat<LessonViewModel>("Kurs.Lesson");

    public ScheduleEditorView() => InitializeComponent();

    private async void ScheduleItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: ScheduleItemViewModel item }) return;

        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.Create(ScheduleItemFormat, item));
        await DragDrop.DoDragDropAsync(e, transfer, DragDropEffects.Move);
    }

    private async void Lesson_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: LessonViewModel lesson }) return;

        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.Create(LessonFormat, lesson));
        await DragDrop.DoDragDropAsync(e, transfer, DragDropEffects.Copy);
    }

    private void ScheduleItem_DragOver(object? sender, DragEventArgs e)
    {
        var source = e.DataTransfer.TryGetValue(ScheduleItemFormat);
        var lesson = e.DataTransfer.TryGetValue(LessonFormat);
        e.DragEffects = source is not null
            ? DragDropEffects.Move
            : lesson is not null ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void ScheduleItem_Drop(object? sender, DragEventArgs e)
    {
        if (sender is not Control { DataContext: ScheduleItemViewModel target }) return;
        var source = e.DataTransfer.TryGetValue(ScheduleItemFormat);
        if (source is not null && source != target)
            ApplyDrop(source, target.DateTime, target);
        else
        {
            var lesson = e.DataTransfer.TryGetValue(LessonFormat);
            if (lesson is not null) ApplyLessonDrop(lesson, target.DateTime, target);
        }
        e.Handled = true;
    }

    private void ScheduleDay_DragOver(object? sender, DragEventArgs e)
    {
        var source = e.DataTransfer.TryGetValue(ScheduleItemFormat);
        var lesson = e.DataTransfer.TryGetValue(LessonFormat);
        e.DragEffects = source is not null
            ? DragDropEffects.Move
            : lesson is not null ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void ScheduleDay_Drop(object? sender, DragEventArgs e)
    {
        if (sender is not Control { DataContext: ScheduleDayViewModel day }) return;
        var source = e.DataTransfer.TryGetValue(ScheduleItemFormat);
        if (source is not null)
            ApplyDrop(source, day.Date);
        else
        {
            var lesson = e.DataTransfer.TryGetValue(LessonFormat);
            if (lesson is not null) ApplyLessonDrop(lesson, day.Date);
        }
        e.Handled = true;
    }

    private void ApplyDrop(
        ScheduleItemViewModel source,
        DateTimeOffset targetDate,
        ScheduleItemViewModel? target = null)
    {
        if (DataContext is MainWindowViewModel viewModel)
            viewModel.ApplyScheduleDropCommand.Execute(new ScheduleDropRequest(source, targetDate, target));
    }

    private void ApplyLessonDrop(
        LessonViewModel lesson,
        DateTimeOffset targetDate,
        ScheduleItemViewModel? target = null)
    {
        if (DataContext is MainWindowViewModel viewModel)
            viewModel.AddLessonToScheduleDropCommand.Execute(
                new ScheduleLessonDropRequest(lesson, targetDate, target));
    }
}
