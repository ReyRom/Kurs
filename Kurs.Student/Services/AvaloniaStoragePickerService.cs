using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Kurs.Application.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kurs.Student.Services;

public sealed class AvaloniaStoragePickerService(Window owner) : IStoragePickerService
{
    public async Task<string?> OpenFileAsync(string title, string? pattern = null)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [CreateType(pattern)]
        });
        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<List<string>> OpenMultipleFilesAsync(string title, string? pattern = null) => [];
    public Task<string?> SaveFileAsync(string title, string? defaultName = null, string? pattern = null) => Task.FromResult<string?>(null);
    public Task<string?> OpenFolderAsync(string title) => Task.FromResult<string?>(null);

    private static FilePickerFileType CreateType(string? pattern) => new("Курсы Kurs")
    {
        Patterns = string.IsNullOrWhiteSpace(pattern) ? ["*.kurs"] : [pattern]
    };
}
