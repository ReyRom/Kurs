using Kurs.Application.Interfaces;
using Kurs.Application.Models;

namespace Kurs.Application.Services;

public sealed class CourseFileWorkflow(IStoragePickerService storagePicker, ICoursePackager packager)
{
    public async Task<string?> ExportAsync(CoursePackage package)
    {
        var path = await storagePicker.SaveFileAsync("Экспортировать курс", CreateFileName(package.Course.Title), "*.kurs");
        if (path is null) return null;
        await packager.ExportAsync(package, path);
        return path;
    }

    public async Task<CoursePackage?> OpenAsync()
    {
        var opened = await OpenWithPathAsync();
        return opened?.Package;
    }

    public async Task<(CoursePackage Package, string Path)?> OpenWithPathAsync()
    {
        var path = await storagePicker.OpenFileAsync("Открыть курс", "*.kurs");
        return path is null ? null : (await packager.ImportAsync(path), path);
    }

    public Task<CoursePackage> ImportAsync(string path) => packager.ImportAsync(path);

    private static string CreateFileName(string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safeTitle = string.Concat(title.Select(c => invalid.Contains(c) ? '_' : c)).Trim();
        return $"{(string.IsNullOrWhiteSpace(safeTitle) ? "course" : safeTitle)}.kurs";
    }
}
