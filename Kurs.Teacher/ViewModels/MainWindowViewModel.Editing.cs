using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace Kurs.Teacher.ViewModels;

public partial class MainWindowViewModel
{
    private object? deletionTarget;
    [ObservableProperty] private bool isDeletePending;
    [ObservableProperty] private string deleteMessage = "";

    [RelayCommand]
    private void RequestDelete(object? item)
    {
        DeleteMessage = item switch
        {
            DisciplineViewModel d => $"Удалить дисциплину «{d.Title}»? Все её темы, занятия, материалы, тесты и записи расписания будут удалены из курса.",
            LessonViewModel l => $"Удалить занятие «{l.Title}» вместе с материалами, тестами и записями расписания?",
            MaterialViewModel m => $"Удалить материал «{m.Title}» из занятия? Исходные файлы останутся на диске.",
            TestViewModel t => $"Удалить тест «{t.Title}» со всеми вопросами?",
            QuestionViewModel q => $"Удалить вопрос «{q.Text}» со всеми вариантами ответа?",
            AnswerOptionViewModel a => $"Удалить вариант ответа «{a.Text}»?",
            _ => ""
        };
        deletionTarget = item;
        IsDeletePending = DeleteMessage.Length > 0;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        deletionTarget = null;
        IsDeletePending = false;
    }

    [RelayCommand]
    private void ConfirmDelete()
    {
        if (!IsDeletePending) return;
        var item = deletionTarget;
        CancelDelete();
        switch (item)
        {
            case DisciplineViewModel d when Disciplines.Contains(d):
                foreach (var lesson in d.Lessons.ToList()) DeleteLesson(lesson);
                Disciplines.Remove(d);
                if (SelectedDiscipline == d)
                {
                    SelectedDiscipline = Disciplines.FirstOrDefault();
                    SelectedLesson = SelectedDiscipline?.Lessons.FirstOrDefault();
                    SelectedTopic = SelectedLesson?.Topic;
                }
                break;
            case LessonViewModel l:
                DeleteLesson(l);
                break;
            case MaterialViewModel m:
                foreach (var lesson in Disciplines.SelectMany(d => d.Lessons)) lesson.Materials.Remove(m);
                break;
            case TestViewModel t:
                DeleteTest(t);
                break;
            case QuestionViewModel q:
                foreach (var test in Tests) test.Questions.Remove(q);
                break;
            case AnswerOptionViewModel a:
                foreach (var question in Tests.SelectMany(t => t.Questions)) question.Options.Remove(a);
                break;
        }
        StatusText = "Элемент удалён из курса";
    }

    private void DeleteTest(TestViewModel test)
    {
        foreach (var lesson in Disciplines.SelectMany(d => d.Lessons)) lesson.Tests.Remove(test);
        Tests.Remove(test);
        if (SelectedTest == test) SelectedTest = null;
    }

    private void DeleteLesson(LessonViewModel lesson)
    {
        var discipline = Disciplines.FirstOrDefault(d => d.Lessons.Contains(lesson));
        if (discipline is null) return;
        foreach (var test in lesson.Tests.ToList()) DeleteTest(test);
        foreach (var entry in Schedule.Where(s => s.LessonId == lesson.Id).ToList()) RemoveSchedule(entry);
        discipline.Lessons.Remove(lesson);
        if (SelectedLesson == lesson)
        {
            SelectedLesson = discipline.Lessons.FirstOrDefault();
            SelectedDiscipline = discipline;
            SelectedTopic = SelectedLesson?.Topic;
        }
    }

    [RelayCommand]
    private void RemoveQuestionImage(QuestionViewModel? question)
    {
        if (question is not null) question.ImagePath = null;
    }

    [RelayCommand] private void MoveDisciplineUp(DisciplineViewModel? discipline) => MoveItem(Disciplines, discipline, -1);
    [RelayCommand] private void MoveDisciplineDown(DisciplineViewModel? discipline) => MoveItem(Disciplines, discipline, 1);
    [RelayCommand] private void MoveLessonUp(LessonViewModel? lesson) => MoveLesson(lesson, -1);
    [RelayCommand] private void MoveLessonDown(LessonViewModel? lesson) => MoveLesson(lesson, 1);

    private static void MoveItem<T>(ObservableCollection<T> items, T? item, int offset) where T : class
    {
        if (item is null) return;
        var index = items.IndexOf(item);
        var target = index + offset;
        if (index >= 0 && target >= 0 && target < items.Count) items.Move(index, target);
    }

    private void MoveLesson(LessonViewModel? lesson, int offset)
    {
        var discipline = Disciplines.FirstOrDefault(d => lesson is not null && d.Lessons.Contains(lesson));
        if (discipline is null || lesson is null) return;
        var siblings = discipline.Lessons.Where(l => l.Topic == lesson.Topic).ToList();
        var target = siblings.IndexOf(lesson) + offset;
        if (target >= 0 && target < siblings.Count)
            discipline.Lessons.Move(discipline.Lessons.IndexOf(lesson), discipline.Lessons.IndexOf(siblings[target]));
    }
}
