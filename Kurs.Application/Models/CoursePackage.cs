using Kurs.Domain.Models;

namespace Kurs.Application.Models;

public sealed record CoursePackage(
    Course Course,
    List<Test> Tests,
    List<ScheduleEntry> Schedule);
