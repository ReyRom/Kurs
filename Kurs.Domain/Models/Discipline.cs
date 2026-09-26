using System;
using System.Collections.Generic;
using System.Text;

namespace Kurs.Domain.Models
{
    public class Discipline
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
        public Guid CourseId { get; set; }

        public List<Topic> Topics { get; set; } = [];
        public List<Lesson> Lessons { get; set; } = [];
    }
}

