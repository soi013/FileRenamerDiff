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

namespace FileRenamerDiff.Views
{
    public class DataGridOperation
    {
        #region RemoveItem添付プロパティ
        public static bool GetRemoveItem(DependencyObject obj) => (bool)obj.GetValue(RemoveItemProperty);
        public static void SetRemoveItem(DependencyObject obj, bool value) => obj.SetValue(RemoveItemProperty, value);
        public static readonly DependencyProperty RemoveItemProperty =
            DependencyProperty.RegisterAttached("RemoveItem", typeof(bool), typeof(DataGridOperation),
                new PropertyMetadata(false, (d, e) => OnPropertyChanged(d, e, RemoveItem)));
        private static void RemoveItem(object sender, RoutedEventArgs e) => RemoveItemFromParent(sender as DependencyObject);
        #endregion

        #region IncrementItem添付プロパティ
        public static bool GetIncrementItem(DependencyObject obj) => (bool)obj.GetValue(IncrementItemProperty);
        public static void SetIncrementItem(DependencyObject obj, bool value) => obj.SetValue(IncrementItemProperty, value);
        public static readonly DependencyProperty IncrementItemProperty =
            DependencyProperty.RegisterAttached("IncrementItem", typeof(bool), typeof(DataGridOperation),
                new PropertyMetadata(false, (d, e) => OnPropertyChanged(d, e, IncrementItem)));

        private static void IncrementItem(object sender, RoutedEventArgs e) => IncrementItemFromParent(sender as DependencyObject);
        #endregion

        #region DecrementItem添付プロパティ
        public static bool GetDecrementItem(DependencyObject obj) => (bool)obj.GetValue(DecrementItemProperty);
        public static void SetDecrementItem(DependencyObject obj, bool value) => obj.SetValue(DecrementItemProperty, value);
        public static readonly DependencyProperty DecrementItemProperty =
            DependencyProperty.RegisterAttached("DecrementItem", typeof(bool), typeof(DataGridOperation),
                new PropertyMetadata(false, (d, e) => OnPropertyChanged(d, e, DecrementItem)));
        private static void DecrementItem(object sender, RoutedEventArgs e) => DecrementItemFromParent(sender as DependencyObject);
        #endregion

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e, RoutedEventHandler actionClick)
        {
            if (!(d is ButtonBase button))
                return;

            if (!(e.NewValue is bool b))
                return;

            if (b)
                button.Click += actionClick;
            else
                button.Click -= actionClick;
        }

        /// <summary>
        /// 指定されたオブジェクトを含む行のIndexを増やす = 下の行に移動する
        /// </summary>
        public static void IncrementItemFromParent(DependencyObject elementInItem)
        {
            var (targetList, index) = GetParentListAndIndex(elementInItem);

            if (targetList == null || index < 0)
                return;

            //最後の行だったら何もしない
            if ((index + 1) >= targetList.Count)
                return;

            //一度削除して、1つ大きいIndexに入れ直す
            var targetElement = targetList[index];
            targetList?.RemoveAt(index);
            targetList?.Insert(index + 1, targetElement);
        }

        /// <summary>
        /// 指定されたオブジェクトを含む行のIndexを減らす = 上の行に移動する
        /// </summary>
        public static void DecrementItemFromParent(DependencyObject elementInItem)
        {
            var (targetList, index) = GetParentListAndIndex(elementInItem);

            if (targetList == null || index < 0)
                return;

            //最初の行だったら何もしない
            if (index <= 0)
                return;

            //一度削除して、1つ少ないIndexに入れ直す
            var targetElement = targetList[index];
            targetList?.RemoveAt(index);
            targetList?.Insert(index - 1, targetElement);
        }

        /// <summary>
        /// 指定されたオブジェクトを含む行を親のItemsControlから削除する
        /// </summary>
        public static void RemoveItemFromParent(DependencyObject elementInItem)
        {
            var (targetList, index) = GetParentListAndIndex(elementInItem);

            if (targetList == null || index < 0)
                return;

            targetList?.RemoveAt(index);
        }

        /// <summary>
        /// 指定されたオブジェクトを含む親コレクションとインデックスを返す
        /// </summary>
        private static (IList, int) GetParentListAndIndex(DependencyObject elementInItem)
        {
            DependencyObject parent = elementInItem;
            var parentTree = new List<DependencyObject> { parent };

            //指定されたオブジェクトのVisualTree上の親を順番に探索し、ItemsControlを探す。
            //ただし、DataGridは中間にいるDataGridCellsPresenterは無視する
            while (parent != null && !(parent is ItemsControl) || parent is DataGridCellsPresenter)
            {
                parent = VisualTreeHelper.GetParent(parent);
                parentTree.Add(parent);
            }
            if (!(parent is ItemsControl itemsControl))
                return (null, -1);

            //ItemsControlの行にあたるオブジェクトを探索履歴の後ろから検索
            var item = parentTree
                .LastOrDefault(x => itemsControl.IsItemItsOwnContainer(x));

            //削除するIndexを取得
            int removeIndex = itemsControl.ItemContainerGenerator?.IndexFromContainer(item)
                ?? -1;

            //Bindingしていた場合はItemsSource、違うならItemsから削除する
            IList targetList = ((itemsControl.ItemsSource as IList) ?? itemsControl.Items);

            return (targetList, removeIndex);
        }
    }
}
