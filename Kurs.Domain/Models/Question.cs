namespace Kurs.Domain.Models
{
    public class Question
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; }
        public List<string> Options { get; set; } = [];
        public List<int> CorrectAnswers { get; set; } = [];
        public int Points { get; set; } = 1;
        public Guid TestId { get; set; }
        public Test Test { get; set; } = null!;
    }


}