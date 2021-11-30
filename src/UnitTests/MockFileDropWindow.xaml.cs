using System.Windows;

using Reactive.Bindings;

namespace UnitTests;
/// <summary>
/// MockFileDropWindow.xaml の相互作用ロジック
/// </summary>
public partial class MockFileDropWindow : Window
{
    public ReactiveCommand<IReadOnlyList<string>> AddFilePathsCommand { get; } = new();

    public MockFileDropWindow()
    {
        InitializeComponent();

        AddFilePathsCommand.Subscribe(x =>
            TargetTextBlock.Text = x.ConcatenateString(" | "));
    }
}
