using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;

namespace Kurs.Teacher.ViewModels;

public partial class TopicPropertiesViewModel(
    TopicViewModel topic, DisciplineViewModel discipline, MainWindowViewModel editor) : ViewModelBase
{
    public TopicViewModel Topic { get; } = topic;
    [ObservableProperty] private bool isDeletePending;

    [RelayCommand]
    private void MoveUp() => Move(-1);

    [RelayCommand]
    private void MoveDown() => Move(1);

    private void Move(int offset)
    {
        var index = discipline.Topics.IndexOf(Topic);
        var target = index + offset;
        if (index >= 0 && target >= 0 && target < discipline.Topics.Count)
            discipline.Topics.Move(index, target);
    }

    [RelayCommand]
    private void RequestDelete() => IsDeletePending = true;

    [RelayCommand]
    private void CancelDelete() => IsDeletePending = false;

    [RelayCommand]
    private void ConfirmDelete()
    {
        if (!IsDeletePending || !discipline.Topics.Contains(Topic)) return;
        foreach (var lesson in discipline.Lessons.Where(l => l.Topic == Topic))
            lesson.Topic = null;
        discipline.Topics.Remove(Topic);
        if (editor.SelectedTopic == Topic) editor.SelectedTopic = null;
        IsDeletePending = false;
    }
}
