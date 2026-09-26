using System.IO.Compression;
using System.Text.Json;
using Kurs.Application.Interfaces;
using Kurs.Application.Models;
using Kurs.Domain.Models;

namespace Kurs.Infrastructure.Packaging;

public sealed class ZipCoursePackager : ICoursePackager
{
    private const string CourseFile = "course.json";
    private const string TestsFile = "tests.json";
    private const string ScheduleFile = "schedule.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task ExportAsync(CoursePackage package, string outputPath)
    {
        await using var stream = File.Create(outputPath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        await AddHtmlMaterialsAsync(archive, package.Course);
        await AddTestImagesAsync(archive, package.Tests);
        await WriteAsync(archive, CourseFile, package.Course);
        await WriteAsync(archive, TestsFile, package.Tests);
        await WriteAsync(archive, ScheduleFile, package.Schedule);
    }

    private static async Task AddTestImagesAsync(ZipArchive archive, IEnumerable<Test> tests)
    {
        foreach (var question in tests.SelectMany(test => test.Questions))
        {
            if (string.IsNullOrWhiteSpace(question.ImageFileName) || !File.Exists(question.ImageFileName)) continue;
            var archivePath = $"test-images/{question.Id}{Path.GetExtension(question.ImageFileName)}";
            await CopyFileToArchiveAsync(archive, question.ImageFileName, archivePath);
            question.ImageFileName = archivePath;
        }
    }

    private static async Task AddHtmlMaterialsAsync(ZipArchive archive, Course course)
    {
        foreach (var material in course.Disciplines.SelectMany(d => d.Lessons).SelectMany(l => l.Materials))
        {
            if (!File.Exists(material.HtmlFileName)) continue;

            var materialRoot = $"materials/{material.Id}";
            var archivePath = $"{materialRoot}/{Path.GetFileName(material.HtmlFileName)}";
            await CopyFileToArchiveAsync(archive, material.HtmlFileName, archivePath);
            if (!string.IsNullOrWhiteSpace(material.MediaDirectory) && Directory.Exists(material.MediaDirectory))
            {
                var mediaFolderName = Path.GetFileName(material.MediaDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                foreach (var file in Directory.GetFiles(material.MediaDirectory, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(material.MediaDirectory, file).Replace('\\', '/');
                    await CopyFileToArchiveAsync(archive, file, $"{materialRoot}/{mediaFolderName}/{relativePath}");
                }
                material.MediaDirectory = $"{materialRoot}/{mediaFolderName}";
            }
            material.HtmlFileName = archivePath;
        }
    }

    private static async Task CopyFileToArchiveAsync(ZipArchive archive, string sourcePath, string archivePath)
    {
        var entry = archive.CreateEntry(archivePath);
        await using var entryStream = entry.Open();
        await using var sourceStream = File.OpenRead(sourcePath);
        await sourceStream.CopyToAsync(entryStream);
    }

    public async Task<CoursePackage> ImportAsync(string archivePath)
    {
        await using var stream = File.OpenRead(archivePath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var course = await ReadAsync<Course>(archive, CourseFile);
        var tests = await ReadAsync<List<Test>>(archive, TestsFile);
        var schedule = await ReadAsync<List<ScheduleEntry>>(archive, ScheduleFile);
        ExtractMaterials(archive, course);
        return new CoursePackage(course, tests, schedule);
    }

    private static void ExtractMaterials(ZipArchive archive, Course course)
    {
        var importRoot = Path.Combine(Path.GetTempPath(), "Kurs", "imports", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(importRoot);
        archive.ExtractToDirectory(importRoot);

        foreach (var material in course.Disciplines.SelectMany(d => d.Lessons).SelectMany(l => l.Materials))
        {
            if (!string.IsNullOrWhiteSpace(material.HtmlFileName))
                material.HtmlFileName = Path.GetFullPath(Path.Combine(importRoot, material.HtmlFileName.Replace('/', Path.DirectorySeparatorChar)));
            if (!string.IsNullOrWhiteSpace(material.MediaDirectory))
                material.MediaDirectory = Path.GetFullPath(Path.Combine(importRoot, material.MediaDirectory.Replace('/', Path.DirectorySeparatorChar)));
        }
    }

    private static async Task WriteAsync<T>(ZipArchive archive, string name, T data)
    {
        var entry = archive.CreateEntry(name);
        await using var stream = entry.Open();
        await JsonSerializer.SerializeAsync(stream, data, JsonOptions);
    }

    private static async Task<T> ReadAsync<T>(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name) ?? throw new InvalidDataException($"В архиве отсутствует {name}.");
        await using var stream = entry.Open();
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions)
               ?? throw new InvalidDataException($"Не удалось прочитать {name}.");
    }
}
