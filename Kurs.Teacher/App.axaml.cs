using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Kurs.Teacher.ViewModels;
using Kurs.Teacher.Views;
using Kurs.Teacher.Services;
using Kurs.Application.Services;
using Kurs.Infrastructure.Packaging;
using Kurs.Infrastructure.Storage;
using System.Linq;
using System;

namespace Kurs.Teacher
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
                var workflow = new CourseFileWorkflow(storagePicker, new ZipCoursePackager());
                var viewModel = new MainWindowViewModel(
                    workflow,
                    new HtmlMaterialImportWorkflow(storagePicker, new DraftMaterialStore(AppContext.BaseDirectory)),
                    new CourseDraftWorkflow(new FileCourseDraftStore(AppContext.BaseDirectory)),
                    new TestImageImportWorkflow(storagePicker, new DraftTestImageStore(AppContext.BaseDirectory)));
                window.DataContext = viewModel;
                window.Opened += async (_, _) => await viewModel.RestoreDraftAsync();
                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
