using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileRenamerDiff.Views;

/// <summary>
/// InformationPage.xaml の相互作用ロジック
/// </summary>
public partial class InformationPage : UserControl
{
    public InformationPage()
    {
        InitializeComponent();
    }

    private void OpenHyperlink(object sender, ExecutedRoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("cmd", $"/c start {e.Parameter}") { CreateNoWindow = true });
    }
}
