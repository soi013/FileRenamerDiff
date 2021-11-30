using System.Globalization;
using System.Reflection;
using System.Windows;

namespace UnitTests;
public class FileDropBehavior_Test
{
    [WpfFact]
    public async Task Drop_OneFile()
    {
        MockFileDropWindow window = SetupWindow();

        //ステージ1 初期状態
        window.TargetTextBlock.Text
            .Should().BeEmpty("初期状態なので空のはず");

        //ステージ2 DropPreview
        const string dropFilePath = @"D:\Documents";

        string[] dropFilePaths = new[] { dropFilePath };

        DragEventArgs dragEventArgs = CreateDragEventArgs(dropFilePaths, window, DragDrop.PreviewDragOverEvent);

        dragEventArgs.Handled
            .Should().BeFalse("プレビュー前なので、まだハンドルされてない");

        dragEventArgs.Effects
            .Should().NotBe(DragDropEffects.Copy, "プレビュー前なので、定まっていない");

        window.RaiseEvent(dragEventArgs);

        dragEventArgs.Handled
            .Should().BeTrue("プレビュー後なので、ハンドルされている");

        dragEventArgs.Effects
            .Should().Be(DragDropEffects.Copy, "プレビュー後なので、定まっている");

        //ステージ3 Drop後
        DragEventArgs dragEventArgs2 = CreateDragEventArgs(dropFilePaths, window, DragDrop.DropEvent);

        window.RaiseEvent(dragEventArgs2);

        await Task.Delay(100);

        window.TargetTextBlock.Text
            .Should().Contain(dropFilePath, because: "ドロップされたフィルパスがUIに反映されているはず");
    }

    [WpfFact]
    public async Task Drop_MultiFile()
    {
        MockFileDropWindow window = SetupWindow();

        //ステージ1 初期状態
        window.TargetTextBlock.Text
            .Should().BeEmpty("初期状態なので空のはず");

        //ステージ2 DropPreview
        string[] dropFilePaths = new[] { @"D:\Documents", @"D:\Downloads" };

        DragEventArgs dragEventArgs = CreateDragEventArgs(dropFilePaths, window, DragDrop.PreviewDragOverEvent);

        dragEventArgs.Handled
            .Should().BeFalse("プレビュー前なので、まだハンドルされてない");

        dragEventArgs.Effects
            .Should().NotBe(DragDropEffects.Copy, "プレビュー前なので、定まっていない");

        window.RaiseEvent(dragEventArgs);

        dragEventArgs.Handled
            .Should().BeTrue("プレビュー後なので、ハンドルされている");

        dragEventArgs.Effects
            .Should().Be(DragDropEffects.Copy, "プレビュー後なので、定まっている");

        //ステージ3 Drop後
        DragEventArgs dragEventArgs2 = CreateDragEventArgs(dropFilePaths, window, DragDrop.DropEvent);

        window.RaiseEvent(dragEventArgs2);

        await Task.Delay(100);

        window.TargetTextBlock.Text
            .Should().Contain(dropFilePaths[0], because: "ドロップされたフィルパスがUIに反映されているはず");
        window.TargetTextBlock.Text
            .Should().Contain(dropFilePaths[1], because: "ドロップされたフィルパスがUIに反映されているはず");
    }

    private static DragEventArgs CreateDragEventArgs(string[] dropFilePaths, DependencyObject dropTarget, RoutedEvent rEvent)
    {
        IDataObject data = new DataObject(DataFormats.FileDrop, dropFilePaths);
        CultureInfo culture = CultureInfo.InvariantCulture;
        const DragDropEffects effect = DragDropEffects.Copy | DragDropEffects.Link | DragDropEffects.Move;

        //DragEventArgsのコンストラクタがinternalなので、代わりにActivatorを使用して無理やり生成する
        //var dragEventArgs = new DragEventArgs(data, DragDropKeyStates.LeftMouseButton,,,)

        var parameter = new object[] { data, DragDropKeyStates.LeftMouseButton, effect, dropTarget, new Point() };
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var dragEventArgs = (DragEventArgs)Activator.CreateInstance(
            typeof(DragEventArgs), flags, null, parameter, culture)!;

        dragEventArgs!.RoutedEvent = rEvent;

        return dragEventArgs!;
    }

    private static MockFileDropWindow SetupWindow()
    {
        var window = new MockFileDropWindow()
        {
            Top = -10000,
        };
        //ウインドウ表示
        window.Show();
        return window;
    }
}

