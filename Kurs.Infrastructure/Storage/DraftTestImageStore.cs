using Kurs.Application.Interfaces;

namespace Kurs.Infrastructure.Storage;

public sealed class DraftTestImageStore(string applicationDirectory) : ITestImageStore
{
    public Task<string> ImportAsync(string imagePath)
    {
        if (!File.Exists(imagePath)) throw new FileNotFoundException("Изображение не найдено.", imagePath);
        var directory = Path.Combine(applicationDirectory, "draft", "test-images");
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, $"{Guid.NewGuid():N}{Path.GetExtension(imagePath)}");
        File.Copy(imagePath, destination);
        return Task.FromResult(destination);
    }
}
