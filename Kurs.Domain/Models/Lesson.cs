using static System.Net.Mime.MediaTypeNames;

namespace Kurs.Domain.Models
{
    public class Lesson
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
        public string HtmlFileName { get; set; } = string.Empty;
        public Guid TopicId { get; set; }
        public Topic Topic { get; set; } = null!;
    }
}