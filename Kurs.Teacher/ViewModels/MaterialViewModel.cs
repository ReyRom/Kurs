using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Kurs.Teacher.ViewModels;

public partial class MaterialViewModel(string title, string source, string? mediaDirectory = null, Guid? id = null) : ViewModelBase
{
    public Guid Id { get; } = id ?? Guid.NewGuid();
    [ObservableProperty] private string title = title;
    [ObservableProperty] private string source = source;
    [ObservableProperty] private string? mediaDirectory = mediaDirectory;
}
