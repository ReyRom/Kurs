using Kurs.Domain.Models;

namespace Kurs.Teacher.ViewModels;

public sealed record ScheduleModeOption(string Title, string Description, ScheduleAccessMode Mode);
