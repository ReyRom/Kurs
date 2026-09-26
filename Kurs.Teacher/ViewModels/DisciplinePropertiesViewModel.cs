namespace Kurs.Teacher.ViewModels;

public sealed class DisciplinePropertiesViewModel(DisciplineViewModel discipline, MainWindowViewModel editor)
{
    public DisciplineViewModel Discipline { get; } = discipline;
    public MainWindowViewModel Editor { get; } = editor;
}
