namespace Kurs.Domain.Models;

public sealed class Topic
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public Guid DisciplineId { get; set; }
}
