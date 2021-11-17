using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Microsoft.Xaml.Behaviors;

namespace FileRenamerDiff.Views;

/// <summary>
/// スクロールイベントを親コントロールにも送るBehavior
/// https://stackoverflow.com/questions/31633680/how-to-prevent-listview-from-stealing-scroll-activity-on-datagrid-parent
/// </summary>
public sealed class BubbleScrollEvent : Behavior<UIElement>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
        base.OnDetaching();
    }

    void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent
        };
        AssociatedObject.RaiseEvent(e2);
    }
}
