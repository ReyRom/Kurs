using Kurs.Teacher.ViewModels;
using Kurs.Teacher.Mappers;
using Kurs.Infrastructure.Packaging;
using System.Reflection;
using System.Text.Json;
using Kurs.Domain.Models;
using Kurs.Application.Models;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
var vm = new MainWindowViewModel();
vm.AddDisciplineCommand.Execute(null);
vm.AddTopicCommand.Execute(null);
var first = vm.SelectedTopic!;
first.Title = "Первая тема";
first.Description = "Описание";
vm.AddLessonCommand.Execute(null);
var lesson = vm.SelectedLesson!;
vm.AddTopicCommand.Execute(null);
var second = vm.SelectedTopic!;
lesson.Topic = second;
Check(first.Lessons.Count == 0 && second.Lessons.Single() == lesson, "Move between topics");
vm.MoveTopicUpCommand.Execute(null);
Check(vm.SelectedDiscipline!.Topics[0] == second, "Topic order");
var package = (CoursePackage)typeof(MainWindowViewModel).GetMethod("CreatePackage", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(vm, null)!;
var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".kurs");
try
{
    var packager = new ZipCoursePackager();
    await packager.ExportAsync(package, path);
    var imported = await packager.ImportAsync(path);
    Check(imported.Course.Disciplines[0].Lessons.Single().TopicId == second.Id, "Archive topic membership");
    Check(imported.Course.Disciplines[0].Topics[1].Description == "Описание", "Archive topic metadata");
    var editor = CoursePackageMapper.FromPackage(imported);
    Check(editor.Disciplines[0].Topics[0].Lessons.Single().Id == lesson.Id, "Mapper round trip");
    var restored = new MainWindowViewModel();
    typeof(MainWindowViewModel).GetMethod("LoadPackage", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(restored, [imported]);
    Check(restored.Disciplines[0].Topics[0].Lessons.Single().Id == lesson.Id, "Teacher load");
    var student = new Kurs.Student.ViewModels.MainWindowViewModel();
    var load = student.GetType().GetMethod("LoadPackage", BindingFlags.NonPublic | BindingFlags.Instance)!;
    load.Invoke(student, [imported]);
    Check(student.Disciplines[0].Topics[0].Lessons.Single().Id == lesson.Id, "Student grouping");
    var legacy = JsonSerializer.Deserialize<Course>("{\"Disciplines\":[{\"Title\":\"Старая дисциплина\",\"Lessons\":[{\"Title\":\"Занятие\"}]}]}")!;
    load.Invoke(student, [new CoursePackage(legacy, [], [])]);
    Check(student.Disciplines[0].Topics.Single().Lessons.Count == 1, "Legacy lessons remain visible");
}
finally { File.Delete(path); }
vm.ConfirmDeleteTopicCommand.Execute(null);
Check(vm.SelectedDiscipline.Topics.Contains(second), "Deletion requires confirmation");
vm.RequestDeleteTopicCommand.Execute(null);
vm.ConfirmDeleteTopicCommand.Execute(null);
Check(lesson.Topic is null && vm.SelectedDiscipline.UngroupedLessons.Contains(lesson), "Delete topic preserves lesson");
Check(vm.SelectedDiscipline.Lessons.Single().Id == lesson.Id, "Stable lesson identity");
var sidebar = new MainWindowViewModel();
sidebar.AddDisciplineCommand.Execute(null);
var targetDiscipline = sidebar.SelectedDiscipline!;
sidebar.AddTopicToDisciplineCommand.Execute(targetDiscipline);
var targetTopic = sidebar.SelectedTopic!;
sidebar.AddDisciplineCommand.Execute(null);
var otherDiscipline = sidebar.SelectedDiscipline!;
sidebar.AddLessonToTopicCommand.Execute(targetTopic);
var targetLesson = sidebar.SelectedLesson!;
Check(targetDiscipline.Lessons.Contains(targetLesson) && targetLesson.Topic == targetTopic
    && otherDiscipline.Lessons.Count == 0, "Inline add uses target topic instead of previous selection");
sidebar.AddTopicToDisciplineCommand.Execute(otherDiscipline);
var otherTopic = sidebar.SelectedTopic!;
sidebar.SelectLessonCommand.Execute(targetLesson);
var properties = new TopicPropertiesViewModel(otherTopic, otherDiscipline, sidebar);
properties.Topic.Title = "Изменено во всплывающей панели";
properties.RequestDeleteCommand.Execute(null);
properties.ConfirmDeleteCommand.Execute(null);
Check(!otherDiscipline.Topics.Contains(otherTopic) && sidebar.SelectedLesson == targetLesson
    && sidebar.SelectedDiscipline == targetDiscipline, "Properties leave active lesson and discipline unchanged");
Console.WriteLine("Topic checks passed: editing, ordering, archive round trip, both applications, legacy course, confirmed deletion, inline creation, independent properties.");

sidebar.AddLessonToTopicCommand.Execute(targetTopic);
var laterLesson = sidebar.SelectedLesson!;
sidebar.MoveLessonUpCommand.Execute(laterLesson);
Check(targetTopic.Lessons[0] == laterLesson, "Lesson moves within topic");
sidebar.MoveLessonUpCommand.Execute(laterLesson);
Check(targetTopic.Lessons[0] == laterLesson, "First lesson stays at boundary");
sidebar.MoveLessonDownCommand.Execute(laterLesson);
Check(targetTopic.Lessons[1] == laterLesson, "Lesson moves down");
sidebar.MoveDisciplineUpCommand.Execute(otherDiscipline);
Check(sidebar.Disciplines[0] == otherDiscipline, "Discipline moves up");
var reordered = (CoursePackage)typeof(MainWindowViewModel).GetMethod("CreatePackage", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sidebar, null)!;
var reload = new MainWindowViewModel();
typeof(MainWindowViewModel).GetMethod("LoadPackage", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(reload, [reordered]);
Check(reload.Disciplines[0].Id == otherDiscipline.Id
    && reload.Disciplines[1].Lessons[1].Id == laterLesson.Id, "Order survives package reload");
sidebar.SelectLessonCommand.Execute(targetLesson);
sidebar.AddTestCommand.Execute(null);
var deletingTest = sidebar.SelectedTest!;
sidebar.AddQuestionToSelectedTestCommand.Execute(null);
var deletingQuestion = deletingTest.Questions.Single();
var deletingAnswer = deletingQuestion.Options[0];
sidebar.RequestDeleteCommand.Execute(deletingAnswer);
sidebar.CancelDeleteCommand.Execute(null);
sidebar.ConfirmDeleteCommand.Execute(null);
Check(deletingQuestion.Options.Contains(deletingAnswer), "Cancel prevents deletion");
sidebar.RequestDeleteCommand.Execute(deletingAnswer);
sidebar.ConfirmDeleteCommand.Execute(null);
Check(!deletingQuestion.Options.Contains(deletingAnswer), "Answer removed");
sidebar.RequestDeleteCommand.Execute(deletingQuestion);
sidebar.ConfirmDeleteCommand.Execute(null);
Check(deletingTest.Questions.Count == 0, "Question removed");
var deletingMaterial = new MaterialViewModel("Material", "example.html");
targetLesson.Materials.Add(deletingMaterial);
sidebar.RequestDeleteCommand.Execute(deletingMaterial);
sidebar.ConfirmDeleteCommand.Execute(null);
Check(targetLesson.Materials.Count == 0, "Material removed");
sidebar.AddScheduleCommand.Execute(null);
sidebar.SelectLessonCommand.Execute(laterLesson);
sidebar.AddScheduleCommand.Execute(null);
sidebar.RequestDeleteCommand.Execute(targetLesson);
sidebar.ConfirmDeleteCommand.Execute(null);
Check(!targetDiscipline.Lessons.Contains(targetLesson) && !sidebar.Tests.Contains(deletingTest)
    && sidebar.SelectedTest is null && sidebar.Schedule.Single().LessonId == laterLesson.Id
    && sidebar.Schedule.Single().Order == 1, "Lesson deletion cleans tests and normalizes schedule");
Check(sidebar.SelectedLesson == laterLesson, "Unrelated active lesson preserved");
sidebar.AddTestCommand.Execute(null);
sidebar.RequestDeleteCommand.Execute(sidebar.SelectedTest);
sidebar.ConfirmDeleteCommand.Execute(null);
Check(sidebar.Tests.Count == 0 && laterLesson.Tests.Count == 0, "Test removed from both lists");
sidebar.AddTestCommand.Execute(null);
sidebar.RequestDeleteCommand.Execute(targetDiscipline);
sidebar.ConfirmDeleteCommand.Execute(null);
Check(!sidebar.Disciplines.Contains(targetDiscipline) && sidebar.Tests.Count == 0
    && sidebar.Schedule.Count == 0 && sidebar.SelectedLesson is null
    && sidebar.SelectedDiscipline == otherDiscipline, "Discipline cascade and selection cleanup");
sidebar.RequestDeleteCommand.Execute(otherDiscipline);
sidebar.ConfirmDeleteCommand.Execute(null);
Check(sidebar.Disciplines.Count == 0 && sidebar.SelectedDiscipline is null, "Last discipline deletion");
Console.WriteLine("Deletion and ordering checks passed.");