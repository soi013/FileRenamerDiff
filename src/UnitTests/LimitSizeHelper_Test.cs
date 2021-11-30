using System.Windows;
using System.Windows.Controls;

using FileRenamerDiff.Views;

namespace UnitTests;

public class LimitSizeHelper_Test
{
    [WpfFact]
    public void LimitWidth()
    {
        var window = new Window()
        {
            Top = -1000,
            Width = 200,
            Height = 200,
            SizeToContent = SizeToContent.Manual,
            WindowStyle = WindowStyle.None
        };
        var tb = new TextBox() { Text = "short", TextWrapping = TextWrapping.Wrap };

        //StackPanelに含めることで、サイズがWindowいっぱいにならなくなる
        var stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };
        stackPanel.Children.Add(tb);
        stackPanel.Children.Add(new Button());
        window.Content = stackPanel;

        //親パネルの半分サイズまでに制限する
        tb.SetValue(LimitSizeHelper.LimitMaxWidthProperty, 0.5);

        //ウインドウ表示
        window.Show();

        tb.ActualWidth
            .Should().BeLessThan(100, "まだ短いコンテンツなので、サイズが小さい");

        //大きいコンテンツに変更
        tb.Text = "SUUUUUUUUUUUUUUUUUUUUUUUUUPEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEER____________LOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOG_____________TEEEEEEEEEEEEEEEEXXXXXT";

        window.UpdateLayout();
        tb.ActualWidth
            .Should().BeInRange(90, 110, "大きいコンテンツなので、サイズが制限ギリギリまで増えているはず");

        LimitSizeHelper.GetLimitMaxHeight(tb)
            .Should().Be(0d);
        LimitSizeHelper.GetLimitMaxWidth(tb)
            .Should().Be(0.5d);
    }

    [WpfFact]
    public void LimitHeight()
    {
        var window = new Window()
        {
            Top = -1000,
            Width = 200,
            Height = 200,
            SizeToContent = SizeToContent.Manual,
            WindowStyle = WindowStyle.None
        };
        var tb = new TextBox() { Text = "short", TextWrapping = TextWrapping.Wrap };

        //StackPanelに含めることで、サイズがWindowいっぱいにならなくなる
        var stackPanel = new StackPanel() { Orientation = Orientation.Vertical };
        stackPanel.Children.Add(tb);
        stackPanel.Children.Add(new Button());
        window.Content = stackPanel;

        //親パネルの半分サイズまでに制限する
        tb.SetValue(LimitSizeHelper.LimitMaxHeightProperty, 0.5);

        //ウインドウ表示
        window.Show();

        tb.ActualHeight
            .Should().BeLessThan(80, "まだ短いコンテンツなので、サイズが小さい");

        //大きいコンテンツに変更
        tb.Text = "SUUUUUUUUUUUUUUUUUUUUUUUUUPEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEER____________LOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOG_____________TEEEEEEEEEEEEEEEEXXXXXT";

        window.UpdateLayout();
        tb.ActualHeight
            .Should().BeInRange(90, 110, "大きいコンテンツなので、サイズが制限ギリギリまで増えているはず");

        LimitSizeHelper.GetLimitMaxHeight(tb)
            .Should().Be(0.5d);
        LimitSizeHelper.GetLimitMaxWidth(tb)
            .Should().Be(0d);
    }
}
