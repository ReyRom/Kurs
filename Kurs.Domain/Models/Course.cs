using System;
using System.Collections.Generic;
using System.Text;

namespace Kurs.Domain.Models
{
    public class Course
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ScheduleAccessMode ScheduleAccessMode { get; set; } = ScheduleAccessMode.Recommended;

        public List<Discipline> Disciplines { get; set; } = [];
    }
}
