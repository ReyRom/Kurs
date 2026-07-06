namespace Kurs.Application.Interfaces
{
    public interface ICoursePackager
    {
        Task<string> ExportCourseAsync(string coursePath, string outputPath, string courseTitle);
        Task<string> ImportCourseAsync(string zipPath, string extractPath);
    }
}
