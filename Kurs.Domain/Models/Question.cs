namespace Kurs.Domain.Models;

public sealed class Test
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<Question> Questions { get; set; } = [];
}

public sealed class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public List<string> Options { get; set; } = [];
    public List<int> CorrectAnswers { get; set; } = [];
    public int Points { get; set; } = 1;
    public string? ImageFileName { get; set; }
}
