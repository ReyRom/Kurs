using Kurs.Application.Models;

namespace Kurs.Application.Interfaces;

public interface ICoursePackager
{
    Task ExportAsync(CoursePackage package, string outputPath);
    Task<CoursePackage> ImportAsync(string archivePath);
}
