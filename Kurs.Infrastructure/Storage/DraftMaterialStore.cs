using Kurs.Application.Interfaces;
using Kurs.Application.Models;

namespace Kurs.Infrastructure.Storage;

public sealed class DraftMaterialStore(string applicationDirectory) : ICourseMaterialStore
{
    public Task<ImportedMaterial> ImportAsync(string htmlPath, string? mediaDirectory)
    {
        if (!File.Exists(htmlPath)) throw new FileNotFoundException("HTML-файл не найден.", htmlPath);

        var materialDirectory = Path.Combine(applicationDirectory, "draft", "materials", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(materialDirectory);
        var htmlDestination = Path.Combine(materialDirectory, Path.GetFileName(htmlPath));
        File.Copy(htmlPath, htmlDestination, overwrite: true);

        string? mediaDestination = null;
        if (!string.IsNullOrWhiteSpace(mediaDirectory))
        {
            if (!Directory.Exists(mediaDirectory)) throw new DirectoryNotFoundException("Папка с медиафайлами не найдена.");
            mediaDestination = Path.Combine(materialDirectory, Path.GetFileName(mediaDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
            CopyDirectory(mediaDirectory, mediaDestination);
        }

        return Task.FromResult(new ImportedMaterial(Path.GetFileNameWithoutExtension(htmlPath), htmlDestination, mediaDestination));
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(directory.Replace(sourceDirectory, destinationDirectory, StringComparison.Ordinal));
        Directory.CreateDirectory(destinationDirectory);
        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(sourceDirectory, destinationDirectory, StringComparison.Ordinal), overwrite: true);
    }
}
