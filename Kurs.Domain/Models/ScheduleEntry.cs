namespace Kurs.Domain.Models;

public sealed class ScheduleEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Guid LessonId { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public int Order { get; set; }
}
