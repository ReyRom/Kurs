using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Kurs.Student.Services;

public sealed class StudentCourseStateStore
{
    private readonly string _stateFile;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public StudentCourseStateStore(string applicationDataDirectory)
    {
        _stateFile = Path.Combine(applicationDataDirectory, "student-state.json");
    }

    public async Task<StudentCourseState?> LoadAsync()
    {
        if (!File.Exists(_stateFile)) return null;
        await using var stream = File.OpenRead(_stateFile);
        return await JsonSerializer.DeserializeAsync<StudentCourseState>(stream, JsonOptions);
    }

    public async Task SaveAsync(StudentCourseState state)
    {
        var directory = Path.GetDirectoryName(_stateFile)!;
        Directory.CreateDirectory(directory);
        var temporaryFile = _stateFile + ".tmp";
        await using (var stream = File.Create(temporaryFile))
            await JsonSerializer.SerializeAsync(stream, state, JsonOptions);
        File.Move(temporaryFile, _stateFile, true);
    }
}

public sealed class StudentCourseState
{
    public string? LastCoursePath { get; set; }
    public Guid? CourseId { get; set; }
    public List<Guid> OpenedLessonIds { get; set; } = [];
    public List<StudentTestResultState> TestResults { get; set; } = [];
}

public sealed class StudentTestResultState
{
    public Guid TestId { get; set; }
    public int Score { get; set; }
    public int TotalPoints { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}
