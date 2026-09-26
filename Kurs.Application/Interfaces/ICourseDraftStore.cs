using Kurs.Application.Models;

namespace Kurs.Application.Interfaces;

public interface ICourseDraftStore
{
    Task SaveAsync(CoursePackage package);
    Task<CoursePackage?> LoadAsync();
}
