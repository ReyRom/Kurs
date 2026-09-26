using System.Text.Json;
using Kurs.Application.Interfaces;
using Kurs.Application.Models;
using Kurs.Domain.Models;

namespace Kurs.Infrastructure.Storage;

public sealed class FileCourseDraftStore(string applicationDirectory) : ICourseDraftStore
{
    private const string DraftFolderName = "draft";
    private const string CourseFileName = "course.json";
    private const string TestsFileName = "tests.json";
    private const string ScheduleFileName = "schedule.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private string DraftDirectory => Path.Combine(applicationDirectory, DraftFolderName);

    public async Task SaveAsync(CoursePackage package)
    {
        Directory.CreateDirectory(DraftDirectory);
        await WriteAsync(Path.Combine(DraftDirectory, CourseFileName), package.Course);
        await WriteAsync(Path.Combine(DraftDirectory, TestsFileName), package.Tests);
        await WriteAsync(Path.Combine(DraftDirectory, ScheduleFileName), package.Schedule);
    }

    public async Task<CoursePackage?> LoadAsync()
    {
        var coursePath = Path.Combine(DraftDirectory, CourseFileName);
        var testsPath = Path.Combine(DraftDirectory, TestsFileName);
        var schedulePath = Path.Combine(DraftDirectory, ScheduleFileName);
        if (!File.Exists(coursePath) || !File.Exists(testsPath) || !File.Exists(schedulePath)) return null;

        var course = await ReadAsync<Course>(coursePath);
        var tests = await ReadAsync<List<Test>>(testsPath);
        var schedule = await ReadAsync<List<ScheduleEntry>>(schedulePath);
        return new CoursePackage(course, tests, schedule);
    }

    private static async Task WriteAsync<T>(string path, T data)
    {
        var temporaryPath = $"{path}.tmp";
        await using (var stream = File.Create(temporaryPath))
            await JsonSerializer.SerializeAsync(stream, data, JsonOptions);
        File.Move(temporaryPath, path, overwrite: true);
    }

    private static async Task<T> ReadAsync<T>(string path)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions)
            ?? throw new InvalidDataException($"Не удалось прочитать файл черновика {Path.GetFileName(path)}.");
    }
}
