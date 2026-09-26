using Kurs.Application.Models;

namespace Kurs.Application.Interfaces;

public interface ICourseMaterialStore
{
    Task<ImportedMaterial> ImportAsync(string htmlPath, string? mediaDirectory);
}
