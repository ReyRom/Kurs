using Kurs.Application.Interfaces;

namespace Kurs.Application.Services;

public sealed class TestImageImportWorkflow(IStoragePickerService storagePicker, ITestImageStore imageStore)
{
    private static readonly HashSet<string> SupportedExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp"];

    public async Task<string?> ImportAsync()
    {
        var path = await storagePicker.OpenFileAsync("Выбрать изображение к вопросу", "*.*");
        if (path is null) return null;
        if (!SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Выберите изображение в формате PNG, JPG, GIF, WEBP или BMP.");
        return await imageStore.ImportAsync(path);
    }
}
