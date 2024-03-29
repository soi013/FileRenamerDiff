﻿using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

using FileRenamerDiff.Views;

namespace UnitTests;

public class DataGridOperation_Test
{
    class BindingSorceTest
    {
        public ObservableCollection<ValueHolder<string>> Values { get; } = new[] { "A", "B", "C" }
            .Select(x => ValueHolderFactory.Create(x))
            .ToObservableCollection();
    }

    [WpfFact]
    public void DataGridOperation_RemoveItem_Binding()
    {
        var window = new Window()
        {
            Top = -10000,
        };
        var dataGrid = new DataGrid();
        window.Content = dataGrid;
        dataGrid.AutoGenerateColumns = false;

        //DataGridにBindingをコードで指定
        var bindingSource = new BindingSorceTest();
        dataGrid.SetBinding(DataGrid.ItemsSourceProperty, new Binding(nameof(BindingSorceTest.Values)) { Source = bindingSource });

        //削除ボタンのある列を作成、ボタンコントロールを直接作成するのではなく、そのFrameworkElementFactoryを作成する
        var removeButtonFactory = new FrameworkElementFactory(typeof(Button));
        removeButtonFactory.SetValue(DataGridOperation.RemoveItemProperty, true);
        removeButtonFactory.SetValue(Button.ContentProperty, "BUTTON");
        dataGrid.Columns.Add(new DataGridTemplateColumn()
        {
            CellTemplate = new DataTemplate() { VisualTree = removeButtonFactory }
        });

        //ウインドウ表示
        window.Show();

        bindingSource.Values
            .Should().HaveCount(3, "削除前は全行あるはず");
        dataGrid.Items.Count
            .Should().Be(3, "削除前は全行あるはず");

        var row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(1);
        var cell = dataGrid.Columns[0].GetCellContent(row);
        var child = VisualTreeHelper.GetChild(cell, 0);

        //削除ボタン押下
        ((Button)child).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        bindingSource.Values
            .Should().HaveCount(2, because: "1つ減ったはず");
        bindingSource.Values.Select(x => x.Value)
            .Should().BeEquivalentTo(expectation: new[] { "A", "C" }, because: "1つ減ったはず");
        dataGrid.Items.Count
            .Should().Be(2, "1つ減ったはず");
    }

    [WpfFact]
    public void DataGridOperation_RemoveItem_NoBinding()
    {
        var window = new Window()
        {
            Top = -10000,
        };
        var dataGrid = new DataGrid();
        window.Content = dataGrid;
        dataGrid.AutoGenerateColumns = false;

        //DataGridにBindingせず直接指定
        var bindingSource = new BindingSorceTest();
        dataGrid.ItemsSource = bindingSource.Values;

        //削除ボタンのある列を作成、ボタンコントロールを直接作成するのではなく、そのFrameworkElementFactoryを作成する
        var removeButtonFactory = new FrameworkElementFactory(typeof(Button));
        removeButtonFactory.SetValue(DataGridOperation.RemoveItemProperty, true);
        removeButtonFactory.SetValue(Button.ContentProperty, "BUTTON");
        dataGrid.Columns.Add(new DataGridTemplateColumn()
        {
            CellTemplate = new DataTemplate() { VisualTree = removeButtonFactory }
        });

        //ウインドウ表示
        window.Show();

        bindingSource.Values
            .Should().HaveCount(3, "削除前は全行あるはず");
        dataGrid.Items.Count
            .Should().Be(3, "削除前は全行あるはず");

        var row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(1);
        var cell = dataGrid.Columns[0].GetCellContent(row);
        var child = VisualTreeHelper.GetChild(cell, 0);

        //削除ボタン押下
        ((Button)child).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        bindingSource.Values
            .Should().HaveCount(2, because: "1つ減ったはず");
        bindingSource.Values.Select(x => x.Value)
            .Should().BeEquivalentTo(expectation: new[] { "A", "C" }, because: "1つ減ったはず");
        dataGrid.Items.Count
            .Should().Be(2, "1つ減ったはず");
    }
}
