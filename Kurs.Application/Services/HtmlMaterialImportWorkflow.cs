using Kurs.Application.Interfaces;
using Kurs.Application.Models;

namespace Kurs.Application.Services;

public sealed class HtmlMaterialImportWorkflow(IStoragePickerService storagePicker, ICourseMaterialStore materialStore)
{
    public Task<ImportedMaterial?> ImportHtmlAsync() => ImportAsync(includeMedia: false);

    public Task<ImportedMaterial?> ImportHtmlWithMediaAsync() => ImportAsync(includeMedia: true);

    private async Task<ImportedMaterial?> ImportAsync(bool includeMedia)
    {
        var htmlPath = await storagePicker.OpenFileAsync("Выбрать HTML-материал", "*.html");
        if (htmlPath is null) return null;
        var mediaDirectory = includeMedia
            ? await storagePicker.OpenFolderAsync("Выбрать папку с медиафайлами")
            : null;
        return await materialStore.ImportAsync(htmlPath, mediaDirectory);
    }
}
