using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kurs.Application.Models;
using Kurs.Application.Services;
using Kurs.Domain.Models;
using Kurs.Student.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Kurs.Student.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly CourseFileWorkflow? _courseFiles;
    private readonly StudentCourseStateStore? _stateStore;
    private CoursePackage? _package;
    private string? _currentCoursePath;
    private readonly HashSet<Guid> _openedLessonIds = [];
    private readonly Dictionary<Guid, StudentTestResultState> _testResults = [];

    [ObservableProperty] private string courseTitle = "Цифровая грамотность";
    [ObservableProperty] private string courseAuthor = "Кафедра информационных технологий";
    [ObservableProperty] private string statusText = "Демонстрационный курс готов к работе";
    [ObservableProperty] private string activePage = "overview";
    [ObservableProperty] private string selectedLessonTitle = "Безопасность в интернете";
    [ObservableProperty] private string selectedLessonDescription = "Научитесь защищать личные данные, распознавать угрозы и безопасно работать в сети.";
    [ObservableProperty] private string selectedDisciplineTitle = "Информационная безопасность";
    [ObservableProperty] private Uri? materialSource;
    [ObservableProperty] private StudentTestViewModel? selectedTest;
    [ObservableProperty] private DateTimeOffset scheduleWeekStart = GetMonday(DateTimeOffset.Now);

    public bool IsOverviewVisible => ActivePage == "overview";
    public bool IsScheduleVisible => ActivePage == "schedule";
    public bool IsLessonVisible => ActivePage == "lesson";
    public bool IsTestIntroVisible => ActivePage == "testIntro";
    public bool IsTestVisible => ActivePage == "test";
    public bool HasMaterial => MaterialSource is not null;
    public bool HasNoMaterial => MaterialSource is null;
    public int TotalDisciplines { get; private set; }
    public int TotalLessons { get; private set; }
    public int TotalMaterials { get; private set; }
    public int TotalTests { get; private set; }
    public int OpenedLessonsCount => _openedLessonIds.Count(id => Disciplines.SelectMany(d => d.Lessons).Any(l => l.Id == id));
    public double ProgressPercent => TotalLessons == 0 ? 0 : Math.Round(OpenedLessonsCount * 100d / TotalLessons);
    public string ProgressText => $"{OpenedLessonsCount} из {TotalLessons} занятий открыто";
    public string CourseCompositionText => $"{TotalDisciplines} дисциплин · {TotalLessons} занятий · {TotalTests} тестов";
    public string ScheduleModeText { get; private set; } = "Рекомендательный режим";
    public StudentLessonViewModel? NextLesson { get; private set; }
    public string NextLessonTitle => NextLesson?.Title ?? "Выберите занятие в содержании курса";
    public string NextLessonDiscipline => NextLesson?.DisciplineTitle ?? CourseTitle;
    public string NextLessonDateText => NextLesson?.AvailabilityText.ToUpperInvariant() ?? "КУРС ГОТОВ К РАБОТЕ";
    public ObservableCollection<ScheduleCardViewModel> UpcomingSchedule { get; } = [];
    public ObservableCollection<CourseSectionSummaryViewModel> CourseSections { get; } = [];
    public ObservableCollection<StudentScheduleDayViewModel> ScheduleWeekDays { get; } = [];
    public string ScheduleWeekTitle => $"{ScheduleWeekStart:dd MMMM} — {ScheduleWeekStart.AddDays(6):dd MMMM yyyy}";

    public ObservableCollection<StudentDisciplineViewModel> Disciplines { get; } = [];
    public ObservableCollection<ScheduleCardViewModel> Schedule { get; } = [];
    public ObservableCollection<StudentMaterialViewModel> CurrentMaterials { get; } = [];
    public ObservableCollection<StudentTestViewModel> CurrentTests { get; } = [];

    public MainWindowViewModel(CourseFileWorkflow? courseFiles = null, StudentCourseStateStore? stateStore = null)
    {
        _courseFiles = courseFiles;
        _stateStore = stateStore;
        LoadDemoCourse();
        RefreshSummary();
        RefreshScheduleWeek();
    }

    partial void OnActivePageChanged(string value)
    {
        OnPropertyChanged(nameof(IsOverviewVisible));
        OnPropertyChanged(nameof(IsScheduleVisible));
        OnPropertyChanged(nameof(IsLessonVisible));
        OnPropertyChanged(nameof(IsTestIntroVisible));
        OnPropertyChanged(nameof(IsTestVisible));
    }

    partial void OnMaterialSourceChanged(Uri? value)
    {
        OnPropertyChanged(nameof(HasMaterial));
        OnPropertyChanged(nameof(HasNoMaterial));
    }

    partial void OnScheduleWeekStartChanged(DateTimeOffset value)
    {
        var monday = GetMonday(value);
        if (monday != value)
        {
            ScheduleWeekStart = monday;
            return;
        }
        OnPropertyChanged(nameof(ScheduleWeekTitle));
        RefreshScheduleWeek();
    }

    [RelayCommand]
    private void ShowOverview() => ActivePage = "overview";

    [RelayCommand]
    private void ShowSchedule() => ActivePage = "schedule";

    [RelayCommand]
    private void PreviousScheduleWeek() => ScheduleWeekStart = ScheduleWeekStart.AddDays(-7);

    [RelayCommand]
    private void NextScheduleWeek() => ScheduleWeekStart = ScheduleWeekStart.AddDays(7);

    [RelayCommand]
    private void CurrentScheduleWeek() => ScheduleWeekStart = GetMonday(DateTimeOffset.Now);

    [RelayCommand]
    private async Task SelectLesson(StudentLessonViewModel? lesson)
    {
        if (lesson is null || lesson.IsLocked)
        {
            if (lesson?.IsLocked == true) StatusText = $"Занятие откроется {lesson.AvailabilityText.ToLowerInvariant()}";
            return;
        }

        SelectedLessonTitle = lesson.Title;
        SelectedLessonDescription = lesson.Description;
        SelectedDisciplineTitle = lesson.DisciplineTitle;
        CurrentMaterials.Clear();
        CurrentTests.Clear();

        if (_package is not null)
        {
            var domainLesson = _package.Course.Disciplines.SelectMany(d => d.Lessons).FirstOrDefault(l => l.Id == lesson.Id);
            if (domainLesson is not null)
            {
                foreach (var material in domainLesson.Materials)
                    CurrentMaterials.Add(new StudentMaterialViewModel(material.Title, ResolveMaterialUri(material.HtmlFileName)));
                foreach (var test in _package.Tests.Where(t => t.LessonId == lesson.Id))
                {
                    var testViewModel = StudentTestViewModel.FromDomain(test);
                    ApplySavedResult(testViewModel);
                    CurrentTests.Add(testViewModel);
                }
            }
        }

        if (_package is null && CurrentMaterials.Count == 0)
        {
            CurrentMaterials.Add(new StudentMaterialViewModel("Краткий конспект", null));
            CurrentMaterials.Add(new StudentMaterialViewModel("Практические рекомендации", null));
        }
        if (_package is null && CurrentTests.Count == 0)
            CurrentTests.Add(CreateDemoTest());

        MaterialSource = CurrentMaterials.FirstOrDefault()?.Source;
        ActivePage = "lesson";
        _openedLessonIds.Add(lesson.Id);
        lesson.IsOpened = true;
        RefreshSummary();
        await SaveStateAsync();
        StatusText = $"Открыто занятие «{lesson.Title}»";
    }

    [RelayCommand]
    private void SelectMaterial(StudentMaterialViewModel? material)
    {
        if (material is null) return;
        MaterialSource = material.Source;
        StatusText = $"Открыт материал «{material.Title}»";
    }

    [RelayCommand]
    private void OpenTest(StudentTestViewModel? test)
    {
        if (test is null) return;
        SelectedTest = test;
        ActivePage = "testIntro";
        StatusText = $"Открыта информация о тесте «{test.Title}»";
    }

    [RelayCommand]
    private void BeginTest()
    {
        if (SelectedTest is null) return;
        SelectedTest.Reset();
        ActivePage = "test";
        StatusText = $"Начат тест «{SelectedTest.Title}»";
    }

    [RelayCommand]
    private async Task FinishTest()
    {
        if (SelectedTest is null) return;
        SelectedTest.Finish();
        var result = new StudentTestResultState
        {
            TestId = SelectedTest.Id,
            Score = SelectedTest.Score,
            TotalPoints = SelectedTest.TotalPoints,
            CompletedAt = DateTimeOffset.Now
        };
        _testResults[SelectedTest.Id] = result;
        SelectedTest.ApplyLastResult(result);
        await SaveStateAsync();
        StatusText = $"Тест завершён: {SelectedTest.Score} из {SelectedTest.TotalPoints} баллов";
    }

    [RelayCommand]
    private void BackToLesson()
    {
        ActivePage = "lesson";
        StatusText = "Возврат к занятию";
    }

    [RelayCommand]
    private async Task OpenCourse()
    {
        if (_courseFiles is null)
        {
            StatusText = "Выбор файла доступен в запущенном приложении";
            return;
        }

        try
        {
            var opened = await _courseFiles.OpenWithPathAsync();
            if (opened is null) { StatusText = "Импорт курса отменён"; return; }
            _currentCoursePath = opened.Value.Path;
            _openedLessonIds.Clear();
            _testResults.Clear();
            LoadPackage(opened.Value.Package);
            await SaveStateAsync();
            StatusText = "Курс импортирован и готов к работе";
        }
        catch (Exception ex)
        {
            StatusText = $"Не удалось открыть курс: {ex.Message}";
        }
    }

    public async Task RestoreLastCourseAsync()
    {
        if (_courseFiles is null || _stateStore is null) return;
        try
        {
            var state = await _stateStore.LoadAsync();
            if (string.IsNullOrWhiteSpace(state?.LastCoursePath)) return;
            if (!File.Exists(state.LastCoursePath))
            {
                StatusText = "Последний открытый курс не найден. Выберите файл .kurs заново";
                return;
            }

            _currentCoursePath = state.LastCoursePath;
            _openedLessonIds.Clear();
            foreach (var id in state.OpenedLessonIds) _openedLessonIds.Add(id);
            _testResults.Clear();
            foreach (var result in state.TestResults) _testResults[result.TestId] = result;
            var package = await _courseFiles.ImportAsync(state.LastCoursePath);
            if (state.CourseId != package.Course.Id)
            {
                _openedLessonIds.Clear();
                _testResults.Clear();
            }
            LoadPackage(package);
            StatusText = "Последний открытый курс восстановлен";
        }
        catch (Exception ex)
        {
            StatusText = $"Не удалось восстановить последний курс: {ex.Message}";
        }
    }

    private Task SaveStateAsync()
    {
        if (_stateStore is null || _package is null || string.IsNullOrWhiteSpace(_currentCoursePath)) return Task.CompletedTask;
        return _stateStore.SaveAsync(new StudentCourseState
        {
            LastCoursePath = _currentCoursePath,
            CourseId = _package.Course.Id,
            OpenedLessonIds = _openedLessonIds.ToList(),
            TestResults = _testResults.Values.OrderBy(result => result.CompletedAt).ToList()
        });
    }

    private void ApplySavedResult(StudentTestViewModel test)
    {
        if (_testResults.TryGetValue(test.Id, out var result)) test.ApplyLastResult(result);
    }

    private void LoadPackage(CoursePackage package)
    {
        _package = package;
        CourseTitle = package.Course.Title;
        CourseAuthor = string.IsNullOrWhiteSpace(package.Course.Author) ? "Автор не указан" : package.Course.Author;
        Disciplines.Clear();
        Schedule.Clear();
        UpcomingSchedule.Clear();

        foreach (var discipline in package.Course.Disciplines.OrderBy(d => d.Order))
        {
            var disciplineVm = new StudentDisciplineViewModel(discipline.Title);
            foreach (var lesson in discipline.Lessons.OrderBy(l => l.Order))
            {
                var date = package.Schedule.FirstOrDefault(s => s.LessonId == lesson.Id)?.DateTime;
                var locked = package.Course.ScheduleAccessMode == ScheduleAccessMode.Strict && date is not null && date > DateTimeOffset.Now;
                var availability = GetAvailabilityText(date, locked, package.Course.ScheduleAccessMode);
                disciplineVm.Lessons.Add(new StudentLessonViewModel(lesson.Id, lesson.Title, lesson.Description, discipline.Title, locked,
                    availability) { IsOpened = _openedLessonIds.Contains(lesson.Id) });
            }
            foreach (var topic in discipline.Topics.OrderBy(t => t.Order))
            {
                var group = new StudentTopicViewModel(topic.Title);
                foreach (var lesson in discipline.Lessons.Where(l => l.TopicId == topic.Id).OrderBy(l => l.Order))
                    group.Lessons.Add(disciplineVm.Lessons.First(l => l.Id == lesson.Id));
                disciplineVm.Topics.Add(group);
            }
            var ungrouped = new StudentTopicViewModel("Без темы");
            foreach (var lesson in discipline.Lessons.Where(l => !discipline.Topics.Any(t => t.Id == l.TopicId)).OrderBy(l => l.Order))
                ungrouped.Lessons.Add(disciplineVm.Lessons.First(l => l.Id == lesson.Id));
            if (ungrouped.Lessons.Count > 0) disciplineVm.Topics.Add(ungrouped);
            Disciplines.Add(disciplineVm);
        }

        foreach (var item in package.Schedule.OrderBy(s => s.DateTime).ThenBy(s => s.Order))
        {
            var domainLesson = package.Course.Disciplines.SelectMany(d => d.Lessons).FirstOrDefault(l => l.Id == item.LessonId);
            var lessonVm = Disciplines.SelectMany(d => d.Lessons).FirstOrDefault(l => l.Id == item.LessonId);
            if (domainLesson is null || lessonVm is null) continue;
            var card = new ScheduleCardViewModel(item.DateTime, domainLesson.Title, lessonVm.DisciplineTitle, lessonVm);
            Schedule.Add(card);
        }
        foreach (var card in Schedule.Where(s => s.Date >= DateTimeOffset.Now.Date).Take(4)) UpcomingSchedule.Add(card);
        if (UpcomingSchedule.Count == 0) foreach (var card in Schedule.TakeLast(4)) UpcomingSchedule.Add(card);
        ScheduleModeText = package.Course.ScheduleAccessMode == ScheduleAccessMode.Strict ? "Строгий режим" : "Рекомендательный режим";
        var nearestSchedule = Schedule.FirstOrDefault(s => s.Date.Date >= DateTimeOffset.Now.Date) ?? Schedule.LastOrDefault();
        ScheduleWeekStart = GetMonday(nearestSchedule?.Date ?? DateTimeOffset.Now);
        RefreshScheduleWeek();
        RefreshSummary();
        ActivePage = "overview";
    }

    private static string GetAvailabilityText(DateTimeOffset? date, bool locked, ScheduleAccessMode mode)
    {
        if (date is null) return "Доступно без расписания";
        if (locked) return $"Откроется {date:dd MMM, HH:mm}";
        if (date.Value.Date == DateTimeOffset.Now.Date) return $"Сегодня, {date:HH:mm}";
        if (date > DateTimeOffset.Now && mode == ScheduleAccessMode.Recommended) return $"Рекомендовано {date:dd MMM, HH:mm}";
        return $"Доступно · {date:dd MMM, HH:mm}";
    }

    private void RefreshScheduleWeek()
    {
        ScheduleWeekDays.Clear();
        for (var offset = 0; offset < 7; offset++)
        {
            var date = ScheduleWeekStart.Date.AddDays(offset);
            var items = Schedule.Where(item => item.Date.Date == date).OrderBy(item => item.Date).ToList();
            ScheduleWeekDays.Add(new StudentScheduleDayViewModel(date, items));
        }
    }

    private static DateTimeOffset GetMonday(DateTimeOffset date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-offset);
    }

    private static Uri? ResolveMaterialUri(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        return new Uri(Path.GetFullPath(path));
    }

    private void LoadDemoCourse()
    {
        var first = new StudentDisciplineViewModel("Основы работы с данными");
        first.Lessons.Add(new(Guid.NewGuid(), "Введение в курс", "Как устроен курс и как с ним работать.", first.Title, false, "Пройдено"));
        first.Lessons.Add(new(Guid.NewGuid(), "Поиск и оценка информации", "Проверка источников и работа с информацией.", first.Title, false, "Доступно"));
        var second = new StudentDisciplineViewModel("Информационная безопасность");
        second.Lessons.Add(new(Guid.NewGuid(), "Безопасность в интернете", SelectedLessonDescription, second.Title, false, "Сегодня, 10:00"));
        second.Lessons.Add(new(Guid.NewGuid(), "Защита личных данных", "Пароли, двухфакторная аутентификация и приватность.", second.Title, true, "24 июля, 10:00"));
        var third = new StudentDisciplineViewModel("Совместная работа");
        third.Lessons.Add(new(Guid.NewGuid(), "Облачные документы", "Совместное редактирование и контроль версий.", third.Title, true, "28 июля, 12:30"));
        Disciplines.Add(first); Disciplines.Add(second); Disciplines.Add(third);
        foreach (var discipline in Disciplines)
        {
            var topic = new StudentTopicViewModel("Основные занятия");
            foreach (var lesson in discipline.Lessons) topic.Lessons.Add(lesson);
            discipline.Topics.Add(topic);
        }
        Schedule.Add(new(DateTimeOffset.Now.Date.AddHours(10), "Безопасность в интернете", second.Title, second.Lessons[0]));
        Schedule.Add(new(DateTimeOffset.Now.Date.AddDays(2).AddHours(10), "Защита личных данных", second.Title, second.Lessons[1]));
        Schedule.Add(new(DateTimeOffset.Now.Date.AddDays(6).AddHours(12.5), "Облачные документы", third.Title, third.Lessons[0]));
        foreach (var item in Schedule.Take(4)) UpcomingSchedule.Add(item);
    }

    private void RefreshSummary()
    {
        var lessons = Disciplines.SelectMany(d => d.Lessons).ToList();
        TotalDisciplines = Disciplines.Count;
        TotalLessons = lessons.Count;
        TotalMaterials = _package?.Course.Disciplines.SelectMany(d => d.Lessons).Sum(l => l.Materials.Count) ?? 0;
        TotalTests = _package?.Tests.Count ?? 0;

        CourseSections.Clear();
        var sectionNumber = 1;
        foreach (var discipline in Disciplines)
        {
            var opened = discipline.Lessons.Count(l => _openedLessonIds.Contains(l.Id));
            var percent = discipline.Lessons.Count == 0 ? 0 : opened * 100d / discipline.Lessons.Count;
            CourseSections.Add(new CourseSectionSummaryViewModel(
                sectionNumber++.ToString("00"), discipline.Title,
                $"{discipline.Lessons.Count} занятий · {opened} открыто", percent));
        }

        NextLesson = Schedule.Select(s => s.Lesson)
            .FirstOrDefault(l => !l.IsLocked && !_openedLessonIds.Contains(l.Id))
            ?? lessons.FirstOrDefault(l => !l.IsLocked && !_openedLessonIds.Contains(l.Id))
            ?? lessons.FirstOrDefault(l => !l.IsLocked);

        OnPropertyChanged(nameof(TotalDisciplines));
        OnPropertyChanged(nameof(TotalLessons));
        OnPropertyChanged(nameof(TotalMaterials));
        OnPropertyChanged(nameof(TotalTests));
        OnPropertyChanged(nameof(OpenedLessonsCount));
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(CourseCompositionText));
        OnPropertyChanged(nameof(ScheduleModeText));
        OnPropertyChanged(nameof(NextLesson));
        OnPropertyChanged(nameof(NextLessonTitle));
        OnPropertyChanged(nameof(NextLessonDiscipline));
        OnPropertyChanged(nameof(NextLessonDateText));
    }

    private static StudentTestViewModel CreateDemoTest()
    {
        var test = new StudentTestViewModel(Guid.NewGuid(), "Самопроверка", "2 вопроса · без ограничения времени");
        test.Questions.Add(new StudentQuestionViewModel("Какой пароль считается наиболее надёжным?", QuestionType.SingleChoice, 2,
            ["qwerty2026", "Длинная уникальная парольная фраза", "Дата рождения и фамилия"], [1]));
        test.Questions.Add(new StudentQuestionViewModel("Какие признаки могут указывать на фишинговое письмо?", QuestionType.MultipleChoice, 3,
            ["Неожиданная просьба срочно перейти по ссылке", "Адрес отправителя отличается одной буквой", "Письмо пришло в рабочее время"], [0, 1]));
        return test;
    }
}

public sealed class StudentTopicViewModel(string title) : ViewModelBase
{
    public string Title { get; } = title;
    public ObservableCollection<StudentLessonViewModel> Lessons { get; } = [];
}

public sealed class StudentDisciplineViewModel(string title) : ViewModelBase
{
    public ObservableCollection<StudentTopicViewModel> Topics { get; } = [];
    public string Title { get; } = title;
    public ObservableCollection<StudentLessonViewModel> Lessons { get; } = [];
}

public partial class StudentLessonViewModel(Guid id, string title, string description, string disciplineTitle, bool isLocked, string availabilityText) : ViewModelBase
{
    public Guid Id { get; } = id;
    public string Title { get; } = title;
    public string Description { get; } = description;
    public string DisciplineTitle { get; } = disciplineTitle;
    public bool IsLocked { get; } = isLocked;
    public string AvailabilityText { get; } = availabilityText;
    [ObservableProperty] private bool isOpened;
    public string StateIcon => IsLocked ? "○" : IsOpened ? "✓" : "●";
    partial void OnIsOpenedChanged(bool value) => OnPropertyChanged(nameof(StateIcon));
}

public sealed class StudentMaterialViewModel(string title, Uri? source) : ViewModelBase
{
    public string Title { get; } = title;
    public Uri? Source { get; } = source;
}

public sealed class ScheduleCardViewModel : ViewModelBase
{
    public ScheduleCardViewModel(DateTimeOffset date, string lessonTitle, string disciplineTitle, StudentLessonViewModel lesson)
    {
        Date = date;
        LessonTitle = lessonTitle;
        DisciplineTitle = disciplineTitle;
        Lesson = lesson;
    }

    public DateTimeOffset Date { get; }
    public string Day => Date.ToString("dd");
    public string Month => Date.ToString("MMM").ToUpperInvariant();
    public string Time => Date.ToString("HH:mm");
    public string LessonTitle { get; }
    public string DisciplineTitle { get; }
    public StudentLessonViewModel Lesson { get; }
    public bool CanOpen => !Lesson.IsLocked;
    public string StatusText => Lesson.IsLocked ? "Ещё недоступно" : Date.Date == DateTimeOffset.Now.Date ? "Сегодня" : Date < DateTimeOffset.Now ? "Доступно" : "Запланировано";
    public string DateLong => Date.ToString("dddd, dd MMMM");
    public bool IsToday => Date.Date == DateTimeOffset.Now.Date;
}

public sealed record CourseSectionSummaryViewModel(string Number, string Title, string Subtitle, double Progress);

public sealed class StudentScheduleDayViewModel
{
    public StudentScheduleDayViewModel(DateTimeOffset date, IReadOnlyList<ScheduleCardViewModel> items)
    {
        Date = date;
        Items = items;
    }

    public DateTimeOffset Date { get; }
    public string DayName => Date.ToString("ddd").ToUpperInvariant();
    public string DayNumber => Date.ToString("dd");
    public bool IsToday => Date.Date == DateTimeOffset.Now.Date;
    public string HeaderBackground => IsToday ? "#4255E8" : "#F4F6FA";
    public string HeaderForeground => IsToday ? "#FFFFFF" : "#687187";
    public IReadOnlyList<ScheduleCardViewModel> Items { get; }
    public bool HasNoItems => Items.Count == 0;
}

public partial class StudentTestViewModel(Guid id, string title, string subtitle) : ViewModelBase
{
    public Guid Id { get; } = id;
    public string Title { get; } = title;
    public string Subtitle { get; } = subtitle;
    public ObservableCollection<StudentQuestionViewModel> Questions { get; } = [];
    [ObservableProperty] private bool isFinished;
    [ObservableProperty] private int score;
    [ObservableProperty] private int? lastScore;
    [ObservableProperty] private int? lastTotalPoints;
    [ObservableProperty] private DateTimeOffset? lastCompletedAt;
    public int TotalPoints => Questions.Sum(q => q.Points);
    public int QuestionCount => Questions.Count;
    public bool IsNotFinished => !IsFinished;
    public bool HasLastResult => LastScore.HasValue;
    public string LastResultText => HasLastResult ? $"Последний результат: {LastScore} из {LastTotalPoints} баллов" : "Ещё не пройден";
    public string LastResultDateText => LastCompletedAt is null ? "" : $"{LastCompletedAt:dd MMMM yyyy, HH:mm}";

    partial void OnIsFinishedChanged(bool value) => OnPropertyChanged(nameof(IsNotFinished));

    public static StudentTestViewModel FromDomain(Test test)
    {
        var vm = new StudentTestViewModel(test.Id, test.Title, $"{test.Questions.Count} вопросов · без ограничения времени");
        foreach (var q in test.Questions) vm.Questions.Add(new(q.Text, q.Type, q.Points, q.Options, q.CorrectAnswers));
        return vm;
    }

    public void Reset()
    {
        IsFinished = false; Score = 0;
        foreach (var question in Questions) question.Reset();
    }

    public void Finish()
    {
        Score = Questions.Sum(q => q.IsCorrect ? q.Points : 0);
        IsFinished = true;
        OnPropertyChanged(nameof(TotalPoints));
    }

    public void ApplyLastResult(StudentTestResultState result)
    {
        LastScore = result.Score;
        LastTotalPoints = result.TotalPoints;
        LastCompletedAt = result.CompletedAt;
        OnPropertyChanged(nameof(HasLastResult));
        OnPropertyChanged(nameof(LastResultText));
        OnPropertyChanged(nameof(LastResultDateText));
    }
}

public sealed class StudentQuestionViewModel : ViewModelBase
{
    public string Text { get; }
    public int Points { get; }
    public string TypeLabel { get; }
    public ObservableCollection<StudentAnswerViewModel> Answers { get; } = [];
    private readonly HashSet<int> _correct;
    private readonly bool _singleChoice;
    public bool IsCorrect => Answers.Where(a => a.IsSelected).Select(a => a.Index).ToHashSet().SetEquals(_correct);

    public StudentQuestionViewModel(string text, QuestionType type, int points, IEnumerable<string> options, IEnumerable<int> correct)
    {
        Text = text; Points = points; _correct = correct.ToHashSet(); _singleChoice = type == QuestionType.SingleChoice;
        TypeLabel = _singleChoice ? "Один вариант" : "Несколько вариантов";
        var index = 0;
        foreach (var option in options) Answers.Add(new StudentAnswerViewModel(index++, option, SelectAnswer));
    }

    private void SelectAnswer(StudentAnswerViewModel answer)
    {
        if (_singleChoice) foreach (var option in Answers) option.IsSelected = false;
        answer.IsSelected = !answer.IsSelected;
    }

    public void Reset() { foreach (var answer in Answers) answer.IsSelected = false; }
}

public partial class StudentAnswerViewModel(int index, string text, Action<StudentAnswerViewModel> onSelect) : ViewModelBase
{
    public int Index { get; } = index;
    public string Text { get; } = text;
    [ObservableProperty] private bool isSelected;
    [RelayCommand] private void Select() => onSelect(this);
}


