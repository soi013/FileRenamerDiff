using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

using FileRenamerDiff.Views;

using Reactive.Bindings;

namespace UnitTests;

public class RichTextBoxHelper_Test
{
    public class RichTextViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public FlowDocument DocSource { get; private set; } = new();

        public void UpdateFlowDoc(string innerText, Color color)
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run("FixText_"));
            paragraph.Inlines.Add(new Run(innerText) { Foreground = new SolidColorBrush(color) });
            this.DocSource = new FlowDocument(paragraph);
            RaisePropertyChanged(nameof(DocSource));
        }
    }

    [WpfFact]
    public async Task RichTextBoxHelper_Binding()
    {
        var window = new Window()
        {
            Top = -10000,
        };
        var tb = new RichTextBox() { };

        var firstColor = Colors.Red;
        string firstText = "TEXT1";
        var bindingSource = new RichTextViewModel();
        bindingSource.UpdateFlowDoc(firstText, firstColor);

        //RichTextBoxにBindingをコードで指定
        tb.SetBinding(RichTextBoxHelper.DocumentProperty, new Binding(nameof(RichTextViewModel.DocSource)) { Source = bindingSource });

        window.Content = tb;

        //ウインドウ表示
        window.Show();

        //ステージ 初期状態
        var richTbRuns = ((Paragraph)tb.Document.Blocks.FirstBlock).Inlines.Select(x => (Run)x).ToArray();

        richTbRuns.Select(x => x.Text)
            .Should().BeEquivalentTo("FixText_", firstText);
        richTbRuns.Select(x => x.Foreground as SolidColorBrush).Select(x => x!.Color)
            .Should().Contain(firstColor);

        //ステージ VM変更
        string secondText = "TEXT2";
        var secondColor = Colors.YellowGreen;

        bindingSource.UpdateFlowDoc(secondText, secondColor);

        await Task.Delay(100);

        richTbRuns = ((Paragraph)tb.Document.Blocks.FirstBlock).Inlines.Select(x => (Run)x).ToArray();
        richTbRuns.Select(x => x.Text)
            .Should().BeEquivalentTo("FixText_", secondText);
        richTbRuns.Select(x => x.Foreground as SolidColorBrush).Select(x => x!.Color)
            .Should().Contain(secondColor);
    }

    [WpfFact]
    public async Task DoubleRichTextBox_ChangeSame()
    {
        var window = new Window()
        {
            Top = -10000,
        };
        var tb1 = new RichTextBox() { };
        var tb2 = new RichTextBox() { };

        var firstColor = Colors.Red;
        string firstText = "TEXT1";
        var bindingSource = new RichTextViewModel();
        bindingSource.UpdateFlowDoc(firstText, firstColor);
        //２つのRichTextBoxに同じBindingをコードで指定
        tb1.SetBinding(RichTextBoxHelper.DocumentProperty, new Binding(nameof(RichTextViewModel.DocSource)) { Source = bindingSource });
        tb2.SetBinding(RichTextBoxHelper.DocumentProperty, new Binding(nameof(RichTextViewModel.DocSource)) { Source = bindingSource });

        var stackPanel = new StackPanel();
        stackPanel.Children.Add(tb1);
        stackPanel.Children.Add(tb2);
        window.Content = stackPanel;

        //ウインドウ表示
        window.Show();

        //ステージ 初期状態
        var richTbRuns = ((Paragraph)tb1.Document.Blocks.FirstBlock).Inlines.Select(x => (Run)x).ToArray();

        richTbRuns.Select(x => x.Text)
            .Should().BeEquivalentTo("FixText_", firstText);
        richTbRuns.Select(x => x.Foreground as SolidColorBrush).Select(x => x!.Color)
            .Should().Contain(firstColor);

        //ステージ VM変更
        string secondText = "TEXT2";
        var secondColor = Colors.YellowGreen;

        bindingSource.UpdateFlowDoc(secondText, secondColor);

        await Task.Delay(100);

        richTbRuns = ((Paragraph)tb2.Document.Blocks.FirstBlock).Inlines.Select(x => (Run)x).ToArray();
        richTbRuns.Select(x => x.Text)
            .Should().BeEquivalentTo("FixText_", secondText);
        richTbRuns.Select(x => x.Foreground as SolidColorBrush).Select(x => x!.Color)
            .Should().Contain(secondColor);
    }
}
