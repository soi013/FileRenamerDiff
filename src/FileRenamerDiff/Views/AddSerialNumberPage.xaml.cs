using System.Windows;
using System.Windows.Controls;

namespace FileRenamerDiff.Views;
/// <summary>
/// AddSerialNumberPage.xaml の相互作用ロジック
/// </summary>
public partial class AddSerialNumberPage : UserControl
{
    public AddSerialNumberPage()
    {
        InitializeComponent();
        this.Loaded += AddSerialNumberPage_Loaded;
        ;

    }

    private void AddSerialNumberPage_Loaded(object sender, RoutedEventArgs e)
    {

    }
}
