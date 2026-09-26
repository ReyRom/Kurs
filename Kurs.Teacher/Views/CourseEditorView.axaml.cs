using Avalonia.Controls;
using Avalonia.Interactivity;
using Kurs.Teacher.ViewModels;
using System.Linq;

namespace Kurs.Teacher.Views;

public partial class CourseEditorView : UserControl
{
    private Flyout? propertiesFlyout;
    public CourseEditorView() => InitializeComponent();

    private void OpenProperties(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Flyout: Flyout { Content: Control content } } button
            || DataContext is not MainWindowViewModel editor) return;
        propertiesFlyout = (Flyout)button.Flyout;
        if (button.DataContext is DisciplineViewModel discipline)
            content.DataContext = new DisciplinePropertiesViewModel(discipline, editor);
        else if (button.DataContext is TopicViewModel topic
            && editor.Disciplines.FirstOrDefault(d => d.Topics.Contains(topic)) is { } owner)
            content.DataContext = new TopicPropertiesViewModel(topic, owner, editor);
    }

    private void CloseProperties(object? sender, RoutedEventArgs e) => propertiesFlyout?.Hide();
}
