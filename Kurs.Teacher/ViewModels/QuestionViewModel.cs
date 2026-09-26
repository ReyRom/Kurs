using CommunityToolkit.Mvvm.ComponentModel;
using Kurs.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Kurs.Teacher.ViewModels;

public partial class QuestionViewModel(
    string text,
    QuestionType type,
    int points,
    Guid? id = null) : ViewModelBase
{
    public static IReadOnlyList<QuestionTypeOption> AvailableTypes { get; } =
    [
        new("Один правильный ответ", QuestionType.SingleChoice),
        new("Несколько правильных ответов", QuestionType.MultipleChoice)
    ];

    public Guid Id { get; } = id ?? Guid.NewGuid();
    [ObservableProperty] private string text = text;
    [ObservableProperty] private QuestionTypeOption selectedType =
        AvailableTypes.First(option => option.Value == type);
    [ObservableProperty] private int points = points;
    [ObservableProperty] private string? imagePath;
    public ObservableCollection<AnswerOptionViewModel> Options { get; } = [];
}
