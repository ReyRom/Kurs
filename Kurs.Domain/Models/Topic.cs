using System;
using System.Collections.Generic;
using System.Text;

namespace Kurs.Domain.Models
{
    public class Topic
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
        public Guid DisciplineId { get; set; }

        public Discipline Discipline { get; set; } = null!;
        public List<Lesson> Lessons { get; set; } = [];
    }

}
