using System;
using System.Collections.Generic;
using System.Text;

namespace Kurs.Application.Interfaces
{
    public interface IStoragePickerService
    {
        Task<string?> OpenFileAsync(string title, string? pattern = null);
        Task<List<string>> OpenMultipleFilesAsync(string title, string? pattern = null);
        Task<string?> SaveFileAsync(string title, string? defaultName = null, string? pattern = null);
        Task<string?> OpenFolderAsync(string title);
    }
}
