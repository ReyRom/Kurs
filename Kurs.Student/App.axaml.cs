using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Kurs.Student.ViewModels;
using Kurs.Student.Views;
using Kurs.Student.Services;
using Kurs.Application.Services;
using Kurs.Infrastructure.Packaging;
using System.Linq;
using System;
using System.IO;

namespace Kurs.Student
{
    public partial class App : Avalonia.Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = new MainWindow();
                var storagePicker = new AvaloniaStoragePickerService(window);
                var stateDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kurs");
                var viewModel = new MainWindowViewModel(
                    new CourseFileWorkflow(storagePicker, new ZipCoursePackager()),
                    new StudentCourseStateStore(stateDirectory));
                window.DataContext = viewModel;
                window.Opened += async (_, _) => await viewModel.RestoreLastCourseAsync();
                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
