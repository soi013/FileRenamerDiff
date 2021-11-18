using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace FileRenamerDiff.Views;

public class DataGridOperation
{
    #region RemoveItem添付プロパティ
    public static bool GetRemoveItem(DependencyObject obj) => (bool)obj.GetValue(RemoveItemProperty);
    public static void SetRemoveItem(DependencyObject obj, bool value) => obj.SetValue(RemoveItemProperty, value);
    public static readonly DependencyProperty RemoveItemProperty =
        DependencyProperty.RegisterAttached("RemoveItem", typeof(bool), typeof(DataGridOperation),
            new PropertyMetadata(false, (d, e) => OnPropertyChanged(d, e, RemoveItem)));
    private static void RemoveItem(object sender, RoutedEventArgs e)
    {
        if (sender is DependencyObject dObj)
            RemoveItemFromParent(dObj);
    }
    #endregion

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e, RoutedEventHandler actionClick)
    {
        if (d is not ButtonBase button)
            return;

        if (e.NewValue is not bool b)
            return;

        if (b)
            button.Click += actionClick;
        else
            button.Click -= actionClick;
    }

    /// <summary>
    /// 指定されたオブジェクトを含む行を親のItemsControlから削除する
    /// </summary>
    public static void RemoveItemFromParent(DependencyObject elementInItem)
    {
        (IEnumerable? targetList, int index) = GetParentListAndIndex(elementInItem);

        if (targetList == null || index < 0)
            return;

        switch (targetList)
        {
            case IList x:
                x.RemoveAt(index);
                return;
            case IEditableCollectionView x:
                if (x.CanRemove)
                    x.RemoveAt(index);
                return;
        }
    }

    /// <summary>
    /// 指定されたオブジェクトを含む親コレクションとインデックスを返す
    /// </summary>
    private static (IEnumerable?, int) GetParentListAndIndex(DependencyObject elementInItem)
    {
        DependencyObject parent = elementInItem;
        var parentTree = new List<DependencyObject> { parent };

        //指定されたオブジェクトのVisualTree上の親を順番に探索し、ItemsControlを探す。
        //ただし、DataGridは中間にいるDataGridCellsPresenterは無視する
        while (parent is (not null and not ItemsControl) or DataGridCellsPresenter)
        {
            parent = VisualTreeHelper.GetParent(parent);
            parentTree.Add(parent);
        }
        if (parent is not ItemsControl itemsControl)
            return (null, -1);

        //ItemsControlの行にあたるオブジェクトを探索履歴の後ろから検索
        DependencyObject? item = parentTree
            .LastOrDefault(x => itemsControl.IsItemItsOwnContainer(x));

        //削除するIndexを取得
        int removeIndex = itemsControl.ItemContainerGenerator?.IndexFromContainer(item)
            ?? -1;

        //Bindingしていた場合はItemsSource、違うならItemsから削除する
        IEnumerable targetList = itemsControl.ItemsSource ?? itemsControl.Items;

        return (targetList, removeIndex);
    }
}
