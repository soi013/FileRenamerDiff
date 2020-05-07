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
    public class RemoveItemExt
    {
        #region RemoveItem添付プロパティ
        public static bool GetRemoveItem(DependencyObject obj) => (bool)obj.GetValue(RemoveItemProperty);
        public static void SetRemoveItem(DependencyObject obj, bool value) => obj.SetValue(RemoveItemProperty, value);
        public static readonly DependencyProperty RemoveItemProperty =
            DependencyProperty.RegisterAttached("RemoveItem", typeof(bool), typeof(RemoveItemExt), new PropertyMetadata(default(bool), OnRemoveItemChanged));

        private static void OnRemoveItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ButtonBase button))
                return;

            if (!(e.NewValue is bool b))
                return;

            if (b)
                button.Click += RemoveItem;
            else
                button.Click -= RemoveItem;
        }
        private static void RemoveItem(object sender, RoutedEventArgs e) => RemoveItemFromParent(sender as DependencyObject);
        #endregion

        public static void RemoveItemFromParent(DependencyObject elementInItem)
        {
            DependencyObject parent = elementInItem;
            var parentTree = new List<DependencyObject> { parent };
            while (parent != null && !(parent is ItemsControl) || parent is DataGridCellsPresenter)
            {
                parent = VisualTreeHelper.GetParent(parent);
                parentTree.Add(parent);
            }
            if (!(parent is ItemsControl itemsControl))
                return;

            var item = parentTree
                .LastOrDefault(x => itemsControl.IsItemItsOwnContainer(x));

            int? removeIndex = itemsControl.ItemContainerGenerator?.IndexFromContainer(item);

            if (removeIndex == null || removeIndex < 0)
                return;

            ((itemsControl.ItemsSource as IList) ?? itemsControl.Items)
                ?.RemoveAt(removeIndex.Value);
        }
    }
}
