namespace Kurs.Domain.Models
{
    public class Test
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PassingScore { get; set; } = 70;
        public Guid TopicId { get; set; }

        public Topic Topic { get; set; } = null!;
        public List<Question> Questions { get; set; } = [];
    }
}