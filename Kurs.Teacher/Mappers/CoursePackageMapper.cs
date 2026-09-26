using Kurs.Application.Models;
using Kurs.Domain.Models;
using Kurs.Teacher.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using DomainMaterial = Kurs.Domain.Models.Material;

namespace Kurs.Teacher.Mappers;

/// <summary>
/// Presentation adapter between editable MVVM state and the application package.
/// Domain and application layers remain independent of Avalonia and UI types.
/// </summary>
public static class CoursePackageMapper
{
    public static CoursePackage ToPackage(
        Guid courseId,
        string title,
        string author,
        string description,
        DateTime createdAt,
        ScheduleAccessMode scheduleAccessMode,
        IEnumerable<DisciplineViewModel> disciplineViewModels,
        IEnumerable<ScheduleItemViewModel> scheduleViewModels)
    {
        var course = new Course
        {
            Id = courseId,
            Title = title,
            Author = author,
            Description = description,
            CreatedAt = createdAt,
            ScheduleAccessMode = scheduleAccessMode
        };
        var tests = new List<Test>();

        foreach (var disciplineViewModel in disciplineViewModels)
        {
            var discipline = new Discipline
            {
                Id = disciplineViewModel.Id,
                CourseId = course.Id,
                Title = disciplineViewModel.Title,
                Description = disciplineViewModel.Description,
                Topics = disciplineViewModel.Topics.Select((topic, index) => new Topic { Id = topic.Id, Title = topic.Title, Description = topic.Description, Order = index, DisciplineId = disciplineViewModel.Id }).ToList(),
                Order = course.Disciplines.Count
            };

            foreach (var lessonViewModel in disciplineViewModel.Lessons)
            {
                var lesson = new Lesson
                {
                    Id = lessonViewModel.Id,
                    DisciplineId = discipline.Id,
                    TopicId = lessonViewModel.Topic?.Id,
                    Title = lessonViewModel.Title,
                    Description = lessonViewModel.Description,
                    Order = discipline.Lessons.Count,
                    Materials = lessonViewModel.Materials
                        .Select(material => new DomainMaterial
                        {
                            Id = material.Id,
                            Title = material.Title,
                            HtmlFileName = material.Source,
                            MediaDirectory = material.MediaDirectory
                        })
                        .ToList()
                };
                discipline.Lessons.Add(lesson);

                tests.AddRange(lessonViewModel.Tests.Select(test => new Test
                {
                    Id = test.Id,
                    LessonId = lesson.Id,
                    Title = test.Title,
                    Questions = test.Questions.Select(MapQuestion).ToList()
                }));
            }

            course.Disciplines.Add(discipline);
        }

        var schedule = scheduleViewModels.Select(item => new ScheduleEntry
        {
            Id = item.Id,
            CourseId = course.Id,
            LessonId = item.LessonId,
            DateTime = item.DateTime,
            Order = item.Order
        }).ToList();

        return new CoursePackage(course, tests, schedule);
    }

    public static CourseEditorState FromPackage(CoursePackage package)
    {
        var disciplines = new List<DisciplineViewModel>();
        var tests = new List<TestViewModel>();

        for (var disciplineIndex = 0; disciplineIndex < package.Course.Disciplines.Count; disciplineIndex++)
        {
            var domainDiscipline = package.Course.Disciplines[disciplineIndex];
            var discipline = new DisciplineViewModel(
                domainDiscipline.Title,
                domainDiscipline.Id,
                GetDisciplineColor(disciplineIndex),
                domainDiscipline.Description);

            foreach (var topic in domainDiscipline.Topics.OrderBy(t => t.Order))
                discipline.Topics.Add(new TopicViewModel(topic.Title, topic.Id, topic.Description));

            foreach (var domainLesson in domainDiscipline.Lessons.OrderBy(lesson => lesson.Order))
            {
                var lesson = new LessonViewModel(
                    domainLesson.Title,
                    domainLesson.Description,
                    domainLesson.Id) { Topic = discipline.Topics.FirstOrDefault(t => t.Id == domainLesson.TopicId) };

                foreach (var material in domainLesson.Materials)
                    lesson.Materials.Add(new MaterialViewModel(
                        material.Title,
                        material.HtmlFileName,
                        material.MediaDirectory,
                        material.Id));

                foreach (var domainTest in package.Tests.Where(test => test.LessonId == domainLesson.Id))
                {
                    var test = new TestViewModel(domainTest.Title, domainTest.Id)
                    {
                        LessonTitle = lesson.Title
                    };
                    foreach (var domainQuestion in domainTest.Questions)
                        test.Questions.Add(MapQuestion(domainQuestion));
                    lesson.Tests.Add(test);
                    tests.Add(test);
                }

                discipline.Lessons.Add(lesson);
            }

            discipline.RefreshGroups();
            disciplines.Add(discipline);
        }

        var schedule = package.Schedule
            .OrderBy(entry => entry.DateTime.Date)
            .ThenBy(entry => entry.Order)
            .Select(entry =>
            {
                var discipline = disciplines.FirstOrDefault(item =>
                    item.Lessons.Any(lesson => lesson.Id == entry.LessonId));
                var lessonTitle = discipline?.Lessons
                    .FirstOrDefault(lesson => lesson.Id == entry.LessonId)?.Title
                    ?? "Удалённое занятие";
                return new ScheduleItemViewModel(
                    entry.DateTime,
                    entry.Order,
                    entry.LessonId,
                    lessonTitle,
                    discipline?.Title ?? "Без дисциплины",
                    discipline?.Color ?? "#EEF0F5",
                    entry.Id);
            })
            .ToList();

        return new CourseEditorState(
            package.Course.Id,
            package.Course.Title,
            package.Course.Author,
            package.Course.Description,
            package.Course.CreatedAt,
            package.Course.ScheduleAccessMode,
            disciplines,
            tests,
            schedule);
    }

    public static string GetDisciplineColor(int index)
    {
        string[] palette = ["#E8ECFF", "#E4F6ED", "#FFF0DF", "#F5E8FF", "#E2F4FA", "#FFE7ED"];
        return palette[index % palette.Length];
    }

    private static Question MapQuestion(QuestionViewModel question) => new()
    {
        Id = question.Id,
        Text = question.Text,
        Type = question.SelectedType.Value,
        Points = question.Points,
        ImageFileName = question.ImagePath,
        Options = question.Options.Select(option => option.Text).ToList(),
        CorrectAnswers = question.Options
            .Select((option, index) => (option, index))
            .Where(value => value.option.IsCorrect)
            .Select(value => value.index + 1)
            .ToList()
    };

    private static QuestionViewModel MapQuestion(Question question)
    {
        var viewModel = new QuestionViewModel(
            question.Text,
            question.Type,
            question.Points,
            question.Id)
        {
            ImagePath = question.ImageFileName
        };
        for (var index = 0; index < question.Options.Count; index++)
            viewModel.Options.Add(new AnswerOptionViewModel(
                question.Options[index],
                question.CorrectAnswers.Contains(index + 1)));
        return viewModel;
    }
}

public sealed record CourseEditorState(
    Guid CourseId,
    string Title,
    string Author,
    string Description,
    DateTime CreatedAt,
    ScheduleAccessMode ScheduleAccessMode,
    IReadOnlyList<DisciplineViewModel> Disciplines,
    IReadOnlyList<TestViewModel> Tests,
    IReadOnlyList<ScheduleItemViewModel> Schedule);

