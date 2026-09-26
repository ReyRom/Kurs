using Kurs.Application.Interfaces;
using Kurs.Application.Models;

namespace Kurs.Application.Services;

public sealed class CourseDraftWorkflow(ICourseDraftStore draftStore)
{
    public Task SaveAsync(CoursePackage package) => draftStore.SaveAsync(package);
    public Task<CoursePackage?> RestoreAsync() => draftStore.LoadAsync();
}
