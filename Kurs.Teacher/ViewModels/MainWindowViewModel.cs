using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kurs.Application.Models;
using Kurs.Application.Services;
using Kurs.Domain.Models;
using DomainMaterial = Kurs.Domain.Models.Material;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Kurs.Teacher.ViewModels;

public partial class MainWindowViewModel(
    CourseFileWorkflow? courseFiles = null,
    HtmlMaterialImportWorkflow? materialImport = null,
    CourseDraftWorkflow? draftWorkflow = null,
    TestImageImportWorkflow? testImageImport = null) : ViewModelBase
{
    private readonly CourseFileWorkflow? _courseFiles = courseFiles;
    private readonly HtmlMaterialImportWorkflow? _materialImport = materialImport;
    private readonly CourseDraftWorkflow? _draftWorkflow = draftWorkflow;
    private readonly TestImageImportWorkflow? _testImageImport = testImageImport;
    private Guid _courseId = Guid.NewGuid();

    [ObservableProperty] private string courseTitle = "Новый курс";
    [ObservableProperty] private string author = "";
    [ObservableProperty] private string courseDescription = "";
    [ObservableProperty] private DateTime createdAt = DateTime.Today;
    [ObservableProperty] private string statusText = "Создайте структуру курса";
    [ObservableProperty] private TopicViewModel? selectedTopic;
    [ObservableProperty] private bool isTopicDeletePending;
    partial void OnSelectedDisciplineChanged(DisciplineViewModel? value)
    {
        SelectedTopic = null;
        IsTopicDeletePending = false;
    }
    partial void OnSelectedTopicChanged(TopicViewModel? value) => IsTopicDeletePending = false;

    [RelayCommand]
    private void AddTopicToDiscipline(DisciplineViewModel? discipline)
    {
        if (discipline is null || !Disciplines.Contains(discipline)) return;
        SelectedDiscipline = discipline;
        AddTopic();
    }

    [RelayCommand]
    private void AddLessonToTopic(TopicViewModel? topic)
    {
        var discipline = Disciplines.FirstOrDefault(d => topic is not null && d.Topics.Contains(topic));
        if (discipline is null) return;
        SelectedDiscipline = discipline;
        SelectedTopic = topic;
        AddLesson();
    }

    [RelayCommand]
    private void AddTopic()
    {
        if (SelectedDiscipline is null) AddDiscipline();
        var topic = new TopicViewModel($"Тема {SelectedDiscipline!.Topics.Count + 1}");
        SelectedDiscipline.Topics.Add(topic);
        SelectedTopic = topic;
        SelectedLesson = null;
    }
    [RelayCommand]
    private void SelectTopic(TopicViewModel? topic)
    {
        if (topic is null) return;
        SelectedDiscipline = Disciplines.FirstOrDefault(d => d.Topics.Contains(topic));
        SelectedTopic = topic;
        SelectedLesson = topic.Lessons.FirstOrDefault();
    }
    [RelayCommand]
    private void MoveTopicUp() => MoveTopic(-1);
    [RelayCommand]
    private void MoveTopicDown() => MoveTopic(1);
    private void MoveTopic(int offset)
    {
        if (SelectedTopic is null || SelectedDiscipline is null) return;
        var index = SelectedDiscipline.Topics.IndexOf(SelectedTopic);
        var target = index + offset;
        if (index >= 0 && target >= 0 && target < SelectedDiscipline.Topics.Count)
            SelectedDiscipline.Topics.Move(index, target);
    }
    [RelayCommand]
    private void RequestDeleteTopic() => IsTopicDeletePending = SelectedTopic is not null;
    [RelayCommand]
    private void CancelDeleteTopic() => IsTopicDeletePending = false;
    [RelayCommand]
    private void ConfirmDeleteTopic()
    {
        if (!IsTopicDeletePending || SelectedTopic is null || SelectedDiscipline is null) return;
        foreach (var lesson in SelectedDiscipline.Lessons.Where(l => l.Topic == SelectedTopic)) lesson.Topic = null;
        SelectedDiscipline.Topics.Remove(SelectedTopic);
        SelectedTopic = null;
        IsTopicDeletePending = false;
    }
    [RelayCommand]
    private void UngroupLesson()
    {
        if (SelectedLesson is not null) SelectedLesson.Topic = null;
    }
    [ObservableProperty] private LessonViewModel? selectedLesson;
    [ObservableProperty] private DisciplineViewModel? selectedDiscipline;
    [ObservableProperty] private string selectedSection = "Содержание";
    [ObservableProperty] private DateTimeOffset scheduleDate = DateTimeOffset.Now;
    [ObservableProperty] private TestViewModel? selectedTest;
    [ObservableProperty] private bool isTestEditorVisible;
    [ObservableProperty] private bool isScheduleEditorVisible;
    [ObservableProperty] private bool isCourseEditorVisible = true;
    [ObservableProperty] private ScheduleModeOption? selectedScheduleMode;
    [ObservableProperty] private DateTimeOffset scheduleWeekStart = GetMonday(DateTimeOffset.Now);

    public ObservableCollection<DisciplineViewModel> Disciplines { get; } = [];
    public ObservableCollection<ScheduleItemViewModel> Schedule { get; } = [];
    public ObservableCollection<TestViewModel> Tests { get; } = [];
    public ObservableCollection<ScheduleModeOption> ScheduleModes { get; } =
    [new("Рекомендательный", "Показывает порядок занятий, но не ограничивает доступ.", ScheduleAccessMode.Recommended),
     new("Строгий", "Открывает занятия в назначенную дату и в порядке этого дня.", ScheduleAccessMode.Strict)];
    public ObservableCollection<ScheduleDayViewModel> ScheduleDays { get; } = CreateEmptyWeek(GetMonday(DateTimeOffset.Now));
    public ScheduleAccessMode ScheduleAccessMode { get; private set; } = ScheduleAccessMode.Recommended;

    partial void OnSelectedScheduleModeChanged(ScheduleModeOption? value)
    {
        if (value is not null) ScheduleAccessMode = value.Mode;
    }

    partial void OnScheduleWeekStartChanged(DateTimeOffset value)
    {
        ScheduleWeekStart = GetMonday(value);
        RefreshScheduleDays();
    }

    [RelayCommand]
    private void NewCourse()
    {
        CancelDelete();
        SelectedTest = null;
        CourseTitle = "Новый курс";
        Author = "";
        CourseDescription = "";
        CreatedAt = DateTime.Today;
        _courseId = Guid.NewGuid();
        Disciplines.Clear();
        Schedule.Clear();
        Tests.Clear();
        SelectedScheduleMode = ScheduleModes[0];
        ScheduleAccessMode = ScheduleAccessMode.Recommended;
        RefreshScheduleDays();
        SelectedDiscipline = null;
        SelectedLesson = null;
        StatusText = "Создан новый пустой курс";
    }

    [RelayCommand]
    private void AddDiscipline()
    {
        var discipline = new DisciplineViewModel(
            $"Дисциплина {Disciplines.Count + 1}",
            color: GetDisciplineColor(Disciplines.Count));
        Disciplines.Add(discipline);
        SelectedDiscipline = discipline;
        SelectedLesson = null;
        StatusText = "Добавлена дисциплина";
    }

    [RelayCommand]
    private void SelectDiscipline(DisciplineViewModel? discipline)
    {
        if (discipline is null) return;
        SelectedDiscipline = discipline;
        SelectedLesson = discipline.Lessons.FirstOrDefault();
        SelectedTopic = SelectedLesson?.Topic;
    }

    [RelayCommand]
    private void AddLesson()
    {
        if (SelectedDiscipline is null)
        {
            AddDiscipline();
        }
        var discipline = SelectedDiscipline!;
        var lesson = new LessonViewModel($"Занятие {discipline.Lessons.Count + 1}", "Добавьте описание занятия.");
        lesson.Topic = SelectedTopic;
        discipline.Lessons.Add(lesson);
        SelectedLesson = lesson;
        StatusText = "Добавлено занятие";
    }

    partial void OnSelectedTestChanged(TestViewModel? oldValue, TestViewModel? newValue)
    {
        if (oldValue is not null) oldValue.IsSelected = false;
        if (newValue is not null) newValue.IsSelected = true;
    }
    partial void OnSelectedLessonChanged(LessonViewModel? oldValue, LessonViewModel? newValue)
    {
        if (oldValue is not null) oldValue.IsSelected = false;
        if (newValue is not null) newValue.IsSelected = true;
    }
    [RelayCommand]
    private void SelectLesson(LessonViewModel? lesson)
    {
        if (lesson is null) return;
        SelectedDiscipline = Disciplines.FirstOrDefault(d => d.Lessons.Contains(lesson));
        SelectedLesson = lesson;
        SelectedTopic = lesson.Topic;
    }

    [RelayCommand]
    private Task AddMaterial() => ImportMaterialAsync(includeMedia: false);

    [RelayCommand]
    private Task AddMaterialWithMedia() => ImportMaterialAsync(includeMedia: true);

    private async Task ImportMaterialAsync(bool includeMedia)
    {
        var lesson = SelectedLesson;
        if (lesson is null)
        {
            StatusText = "Сначала выберите или создайте занятие, затем добавьте HTML-материал";
            return;
        }
        if (_materialImport is null) { StatusText = "Импорт доступен в запущенном приложении"; return; }
        try
        {
            StatusText = "Выберите HTML-файл в окне выбора файлов";
            var imported = includeMedia
                ? await _materialImport.ImportHtmlWithMediaAsync()
                : await _materialImport.ImportHtmlAsync();
            if (imported is null) { StatusText = "Импорт материала отменён"; return; }
            lesson.Materials.Add(new MaterialViewModel(imported.Title, imported.HtmlPath, imported.MediaDirectory));
            StatusText = includeMedia ? "HTML-материал и медиафайлы добавлены" : "HTML-материал добавлен";
        }
        catch (Exception ex)
        {
            StatusText = $"Не удалось добавить HTML-материал: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddTest()
    {
        if (SelectedLesson is null) return;
        var test = new TestViewModel("Новый тест") { LessonTitle = SelectedLesson.Title };
        SelectedLesson.Tests.Add(test);
        Tests.Add(test);
        SelectedTest = test;
        StatusText = "Добавлен тест";
    }

    [RelayCommand]
    private void AddQuestion(TestViewModel? test)
    {
        if (test is null) return;
        var question = new QuestionViewModel("Новый вопрос", QuestionType.SingleChoice, 1);
        question.Options.Add(new AnswerOptionViewModel("Вариант 1", true));
        question.Options.Add(new AnswerOptionViewModel("Вариант 2"));
        test.Questions.Add(question);
        StatusText = "Добавлен вопрос";
    }

    [RelayCommand]
    private void SelectTest(TestViewModel? test)
    {
        if (test is null) return;
        SelectedTest = test;
        SelectedLesson = Disciplines.SelectMany(d => d.Lessons).FirstOrDefault(l => l.Tests.Contains(test));
        if (SelectedLesson is not null)
            SelectedDiscipline = Disciplines.FirstOrDefault(d => d.Lessons.Contains(SelectedLesson));
        SetSelectedSection("Тесты");
    }

    [RelayCommand]
    private void AddQuestionToSelectedTest() => AddQuestion(SelectedTest);

    [RelayCommand]
    private void AddAnswerOption(QuestionViewModel? question)
    {
        if (question is null) return;
        question.Options.Add(new AnswerOptionViewModel($"Вариант {question.Options.Count + 1}"));
    }

    [RelayCommand]
    private async Task AddQuestionImage(QuestionViewModel? question)
    {
        if (question is null || _testImageImport is null) return;
        try
        {
            var path = await _testImageImport.ImportAsync();
            if (path is null) { StatusText = "Выбор изображения отменён"; return; }
            question.ImagePath = path;
            StatusText = "Изображение вопроса добавлено";
        }
        catch (Exception ex) { StatusText = $"Не удалось добавить изображение: {ex.Message}"; }
    }

    [RelayCommand]
    private void AddSchedule()
    {
        if (SelectedLesson is null)
        {
            StatusText = "Сначала выберите занятие для расписания";
            return;
        }
        if (Schedule.Any(item => item.LessonId == SelectedLesson.Id))
        {
            StatusText = "Занятие уже есть в расписании";
            return;
        }
        var selectedDate = ScheduleDate.Date;
        var order = Schedule.Count(item => item.DateTime.Date == selectedDate) + 1;
        var discipline = SelectedDiscipline
            ?? Disciplines.FirstOrDefault(item => item.Lessons.Contains(SelectedLesson));
        Schedule.Add(new ScheduleItemViewModel(
            selectedDate,
            order,
            SelectedLesson.Id,
            SelectedLesson.Title,
            discipline?.Title ?? "Без дисциплины",
            discipline?.Color ?? "#EEF0F5"));
        ScheduleWeekStart = GetMonday(ScheduleDate);
        RefreshScheduleDays();
        StatusText = "Занятие добавлено в расписание";
    }

    [RelayCommand]
    private void MoveScheduleUp(ScheduleItemViewModel? item) => MoveSchedule(item, -1);

    [RelayCommand]
    private void MoveScheduleDown(ScheduleItemViewModel? item) => MoveSchedule(item, 1);

    private void MoveSchedule(ScheduleItemViewModel? item, int offset)
    {
        if (item is null) return;
        var sameDay = Schedule.Where(entry => entry.DateTime.Date == item.DateTime.Date).OrderBy(entry => entry.Order).ToList();
        var index = sameDay.IndexOf(item);
        var destination = index + offset;
        if (destination < 0 || destination >= sameDay.Count) return;
        (sameDay[index].Order, sameDay[destination].Order) = (sameDay[destination].Order, sameDay[index].Order);
        RefreshScheduleDays();
    }

    [RelayCommand]
    private void RemoveSchedule(ScheduleItemViewModel? item)
    {
        if (item is null) return;
        var date = item.DateTime.Date;
        Schedule.Remove(item);
        var sameDay = Schedule
            .Where(entry => entry.DateTime.Date == date)
            .OrderBy(entry => entry.Order)
            .ToList();
        for (var index = 0; index < sameDay.Count; index++) sameDay[index].Order = index + 1;
        RefreshScheduleDays();
        StatusText = "Занятие удалено из расписания";
    }

    [RelayCommand]
    private void ApplyScheduleDrop(ScheduleDropRequest? request)
    {
        if (request is null) return;

        var source = request.Source;
        var previousDate = source.DateTime.Date;
        var targetDate = request.TargetDate.Date;
        var destinationItems = Schedule
            .Where(item => item != source && item.DateTime.Date == targetDate)
            .OrderBy(item => item.Order)
            .ToList();
        var insertionIndex = request.TargetItem is null
            ? destinationItems.Count
            : Math.Max(0, destinationItems.IndexOf(request.TargetItem));

        source.DateTime = targetDate;
        destinationItems.Insert(insertionIndex, source);
        for (var index = 0; index < destinationItems.Count; index++)
            destinationItems[index].Order = index + 1;

        var previousDayItems = Schedule
            .Where(item => item.DateTime.Date == previousDate)
            .OrderBy(item => item.Order)
            .ToList();
        for (var index = 0; index < previousDayItems.Count; index++)
            previousDayItems[index].Order = index + 1;

        ScheduleWeekStart = GetMonday(targetDate);
        RefreshScheduleDays();
        StatusText = "Расписание обновлено перетаскиванием";
    }

    [RelayCommand]
    private void AddLessonToScheduleDrop(ScheduleLessonDropRequest? request)
    {
        if (request is null) return;
        if (Schedule.Any(item => item.LessonId == request.Lesson.Id))
        {
            StatusText = "Занятие уже есть в расписании";
            return;
        }

        var discipline = Disciplines.FirstOrDefault(item => item.Lessons.Contains(request.Lesson));
        var scheduleItem = new ScheduleItemViewModel(
            request.TargetDate.Date,
            1,
            request.Lesson.Id,
            request.Lesson.Title,
            discipline?.Title ?? "Без дисциплины",
            discipline?.Color ?? "#EEF0F5");
        Schedule.Add(scheduleItem);
        ApplyScheduleDrop(new ScheduleDropRequest(
            scheduleItem,
            request.TargetDate,
            request.TargetItem));
        SelectedLesson = request.Lesson;
        SelectedDiscipline = discipline;
        StatusText = "Занятие добавлено в расписание перетаскиванием";
    }

    [RelayCommand]
    private void PreviousWeek() => ScheduleWeekStart = ScheduleWeekStart.AddDays(-7);

    [RelayCommand]
    private void NextWeek() => ScheduleWeekStart = ScheduleWeekStart.AddDays(7);

    private void RefreshScheduleDays()
    {
        ScheduleDays.Clear();
        for (var index = 0; index < 7; index++)
        {
            var date = ScheduleWeekStart.Date.AddDays(index);
            var entries = Schedule.Where(item => item.DateTime.Date == date).OrderBy(item => item.Order).ToList();
            ScheduleDays.Add(new ScheduleDayViewModel(date, entries));
        }
    }

    private static DateTimeOffset GetMonday(DateTimeOffset date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-offset);
    }

    private static ObservableCollection<ScheduleDayViewModel> CreateEmptyWeek(DateTimeOffset weekStart) =>
        new(Enumerable.Range(0, 7).Select(index => new ScheduleDayViewModel(weekStart.AddDays(index), [])));

    [RelayCommand]
    private async Task ExportCourse()
    {
        if (_courseFiles is null) { StatusText = "Экспорт доступен в запущенном приложении"; return; }
        try
        {
            var path = await _courseFiles.ExportAsync(CreatePackage());
            StatusText = path is null ? "Экспорт отменён" : $"Курс сохранён: {path}";
        }
        catch (Exception ex) { StatusText = $"Не удалось сохранить курс: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task SaveDraft()
    {
        if (_draftWorkflow is null) { StatusText = "Сохранение доступно в запущенном приложении"; return; }
        try
        {
            await _draftWorkflow.SaveAsync(CreatePackage());
            StatusText = "Изменения сохранены в черновик";
        }
        catch (Exception ex) { StatusText = $"Не удалось сохранить черновик: {ex.Message}"; }
    }

    public async Task RestoreDraftAsync()
    {
        if (_draftWorkflow is null) return;
        try
        {
            var package = await _draftWorkflow.RestoreAsync();
            if (package is null) return;
            LoadPackage(package);
            StatusText = "Восстановлен сохранённый черновик";
        }
        catch (Exception ex) { StatusText = $"Не удалось восстановить черновик: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task OpenCourse()
    {
        if (_courseFiles is null) { StatusText = "Открытие доступно в запущенном приложении"; return; }
        try
        {
            var package = await _courseFiles.OpenAsync();
            if (package is null) { StatusText = "Открытие отменено"; return; }
            LoadPackage(package);
            StatusText = "Курс открыт";
        }
        catch (Exception ex) { StatusText = $"Не удалось открыть курс: {ex.Message}"; }
    }

    [RelayCommand]
    private void SetSelectedSection(string? section)
    {
        if (string.IsNullOrWhiteSpace(section)) return;
        SelectedSection = section;
        IsTestEditorVisible = section == "Тесты";
        IsScheduleEditorVisible = section == "Расписание";
        IsCourseEditorVisible = !IsTestEditorVisible && !IsScheduleEditorVisible;
    }

    private CoursePackage CreatePackage() => Kurs.Teacher.Mappers.CoursePackageMapper.ToPackage(
        _courseId, CourseTitle, Author, CourseDescription, CreatedAt, ScheduleAccessMode, Disciplines, Schedule);
    private void LoadPackage(CoursePackage package)
    {
        CancelDelete();
        _courseId = package.Course.Id;
        ScheduleAccessMode = package.Course.ScheduleAccessMode;
        SelectedScheduleMode = ScheduleModes.First(mode => mode.Mode == ScheduleAccessMode);
        CourseTitle = package.Course.Title; Author = package.Course.Author; CourseDescription = package.Course.Description; CreatedAt = package.Course.CreatedAt;
        Disciplines.Clear(); Schedule.Clear(); Tests.Clear();
        for (var disciplineIndex = 0; disciplineIndex < package.Course.Disciplines.Count; disciplineIndex++)
        {
            var d = package.Course.Disciplines[disciplineIndex];
            var discipline = new DisciplineViewModel(
                d.Title,
                d.Id,
                GetDisciplineColor(disciplineIndex),
                d.Description);
            foreach (var topic in d.Topics.OrderBy(t => t.Order))
                discipline.Topics.Add(new TopicViewModel(topic.Title, topic.Id, topic.Description));
            foreach (var l in d.Lessons.OrderBy(l => l.Order))
            {
                var lesson = new LessonViewModel(l.Title, l.Description, l.Id);
                foreach (var m in l.Materials) lesson.Materials.Add(new MaterialViewModel(m.Title, m.HtmlFileName, m.MediaDirectory, m.Id));
                foreach (var t in package.Tests.Where(t => t.LessonId == l.Id))
                {
                    var test = new TestViewModel(t.Title, t.Id);
                    test.LessonTitle = lesson.Title;
                    foreach (var q in t.Questions)
                    {
                        var question = new QuestionViewModel(q.Text, q.Type, q.Points, q.Id) { ImagePath = q.ImageFileName };
                        for (var index = 0; index < q.Options.Count; index++)
                            question.Options.Add(new AnswerOptionViewModel(q.Options[index], q.CorrectAnswers.Contains(index + 1)));
                        test.Questions.Add(question);
                    }
                    lesson.Tests.Add(test);
                    Tests.Add(test);
                }
                lesson.Topic = discipline.Topics.FirstOrDefault(t => t.Id == l.TopicId);
        discipline.Lessons.Add(lesson);
            }
            Disciplines.Add(discipline);
        }
        foreach (var s in package.Schedule.OrderBy(s => s.Order))
        {
            var discipline = Disciplines.FirstOrDefault(d => d.Lessons.Any(l => l.Id == s.LessonId));
            var title = discipline?.Lessons.FirstOrDefault(l => l.Id == s.LessonId)?.Title ?? "Удалённое занятие";
            Schedule.Add(new ScheduleItemViewModel(
                s.DateTime,
                s.Order,
                s.LessonId,
                title,
                discipline?.Title ?? "Без дисциплины",
                discipline?.Color ?? "#EEF0F5",
                s.Id));
        }
        RefreshScheduleDays();
        SelectedDiscipline = Disciplines.FirstOrDefault();
        SelectedLesson = SelectedDiscipline?.Lessons.FirstOrDefault();
        SelectedTest = Tests.FirstOrDefault();
    }

    private static string GetDisciplineColor(int index)
    {
        string[] palette = ["#E8ECFF", "#E4F6ED", "#FFF0DF", "#F5E8FF", "#E2F4FA", "#FFE7ED"];
        return palette[index % palette.Length];
    }
}





