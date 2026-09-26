namespace Kurs.Application.Interfaces;

public interface ITestImageStore
{
    Task<string> ImportAsync(string imagePath);
}
