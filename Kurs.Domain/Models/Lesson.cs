using static System.Net.Mime.MediaTypeNames;

namespace Kurs.Domain.Models
{
    public abstract class Lesson
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
        public Guid TopicId { get; set; }
        public Topic Topic { get; set; } = null!;
    }

    public class Test: Lesson
    {
        public int PassingScore { get; set; } = 70;
        public List<Question> Questions { get; set; } = [];
    }

    public class Theory : Lesson
    {
        public string HtmlFileName { get; set; } = string.Empty;
    }
}