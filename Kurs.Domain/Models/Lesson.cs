namespace Kurs.Domain.Models;

public sealed class Lesson
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TopicId { get; set; }
    public Guid DisciplineId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<Material> Materials { get; set; } = [];
}

public sealed class Material
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string HtmlFileName { get; set; } = string.Empty;
    public string? MediaDirectory { get; set; }
}

