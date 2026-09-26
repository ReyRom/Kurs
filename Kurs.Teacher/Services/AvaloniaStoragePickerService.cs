using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Kurs.Application.Interfaces;

namespace Kurs.Teacher.Services;

public sealed class AvaloniaStoragePickerService(Window owner) : IStoragePickerService
{
    public async Task<string?> OpenFileAsync(string title, string? pattern = null)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [CreateFileType(pattern)]
        });
        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<List<string>> OpenMultipleFilesAsync(string title, string? pattern = null)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = true,
            FileTypeFilter = [CreateFileType(pattern)]
        });
        return files.Select(f => f.TryGetLocalPath()).OfType<string>().ToList();
    }

    public async Task<string?> SaveFileAsync(string title, string? defaultName = null, string? pattern = null)
    {
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = defaultName,
            FileTypeChoices = [CreateFileType(pattern)]
        });
        return file?.TryGetLocalPath();
    }

    public async Task<string?> OpenFolderAsync(string title)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title });
        return folders.FirstOrDefault()?.TryGetLocalPath();
    }

    private static FilePickerFileType CreateFileType(string? pattern) => new("Файлы курса")
    {
        Patterns = string.IsNullOrWhiteSpace(pattern) ? ["*.*"] : [pattern]
    };
}
