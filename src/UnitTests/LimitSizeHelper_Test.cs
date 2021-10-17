using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Anotar.Serilog;

using FileRenamerDiff.Models;
using FileRenamerDiff.ViewModels;
using FileRenamerDiff.Views;

using FluentAssertions;
using FluentAssertions.Extensions;
using FluentAssertions.Primitives;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Schedulers;

using Xunit;

namespace UnitTests
{
    public class LimitSizeHelper_Test
    {
        [WpfFact]
        public void LimitWidth()
        {
            var window = new Window() { Width = 200, Height = 200, SizeToContent = SizeToContent.Manual, WindowStyle = WindowStyle.None };
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
        }

        [WpfFact]
        public void LimitHeight()
        {
            var window = new Window() { Width = 200, Height = 200, SizeToContent = SizeToContent.Manual, WindowStyle = WindowStyle.None };
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
        }
    }
}
