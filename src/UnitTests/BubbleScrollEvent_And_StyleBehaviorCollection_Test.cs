using System.Windows.Input;

using FileRenamerDiff.Views;

namespace UnitTests;

public class BubbleScrollEvent_And_StyleBehaviorCollection_Test
{
    [WpfFact]
    public async Task BubbleScrollEvent_NoBubble()
    {
        MockBubbleWindow window = SetupWindow();

        //ステージ1スクロール前
        window.TargetScroll.VerticalOffset
            .Should().Be(0, "スクロール前なので0のはず");

        //ステージ2 Bublleしないスクロール後
        var mouseWheelEventArgs1 = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, -500)
        {
            RoutedEvent = Mouse.PreviewMouseWheelEvent,
        };
        window.mockDataGridNotBubble.RaiseEvent(mouseWheelEventArgs1);
        await Task.Delay(100);

        window.TargetScroll.VerticalOffset
            .Should().Be(0, "スクロールが伝わらないので0のはず");
    }

    [WpfFact]
    public async Task BubbleScrollEvent_Bubble()
    {
        MockBubbleWindow window = SetupWindow();

        //ステージ1スクロール前
        window.TargetScroll.VerticalOffset
            .Should().Be(0, "スクロール前なので0のはず");

        //ステージ2 Bublleするスクロール後
        var mouseWheelEventArgs1 = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, -500)
        {
            RoutedEvent = Mouse.PreviewMouseWheelEvent,
        };
        window.mockDataGridBubble.RaiseEvent(mouseWheelEventArgs1);
        await Task.Delay(100);

        window.TargetScroll.VerticalOffset
            .Should().BeGreaterThan(1, "スクロールが伝わったので動いたはず");
    }

    [WpfFact]
    public async Task BubbleScrollEvent_BubbleByStyleBehaviorCollection()
    {
        MockBubbleWindow window = SetupWindow();

        //ステージ1スクロール前
        window.TargetScroll.VerticalOffset
            .Should().Be(0, "スクロール前なので0のはず");

        //ステージ2 Bublleするスクロール後
        var mouseWheelEventArgs1 = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, -500)
        {
            RoutedEvent = Mouse.PreviewMouseWheelEvent,
        };
        window.mockDataGridBubbleStyle1.RaiseEvent(mouseWheelEventArgs1);
        await Task.Delay(100);

        window.TargetScroll.VerticalOffset
            .Should().BeGreaterThan(1, "スクロールが伝わったので動いたはず");

        window.TargetScroll.ScrollToTop();
        await Task.Delay(100);

        var mouseWheelEventArgs2 = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, -500)
        {
            RoutedEvent = Mouse.PreviewMouseWheelEvent,
        };
        window.mockDataGridBubbleStyle2.RaiseEvent(mouseWheelEventArgs2);
        await Task.Delay(100);

        window.TargetScroll.VerticalOffset
            .Should().BeGreaterThan(1, "スクロールが伝わったので動いたはず");
    }

    [WpfFact]
    public void StyleBehaviorCollection_GetSet()
    {
        MockBubbleWindow window = SetupWindow();

        StyleBehaviorCollection.GetStyleBehaviors(window.mockDataGridBubbleStyle1)
            .Should().HaveCount(1);
        StyleBehaviorCollection.GetStyleBehaviors(window.mockDataGridBubbleStyle2)
            .Should().HaveCount(1);

        StyleBehaviorCollection.GetStyleBehaviors(window.mockDataGridNotBubble)
            .Should().BeNull();

        StyleBehaviorCollection.GetStyleBehaviors(window.mockDataGridBubble)
            .Should().BeNull();
    }

    private static MockBubbleWindow SetupWindow()
    {
        var window = new MockBubbleWindow()
        {
            Top = -10000,
        };
        //ウインドウ表示
        window.Show();
        return window;
    }
}
