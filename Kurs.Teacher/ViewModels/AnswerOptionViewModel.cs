using CommunityToolkit.Mvvm.ComponentModel;

namespace Kurs.Teacher.ViewModels;

public partial class AnswerOptionViewModel(string text, bool isCorrect = false) : ViewModelBase
{
    [ObservableProperty] private string text = text;
    [ObservableProperty] private bool isCorrect = isCorrect;
}
