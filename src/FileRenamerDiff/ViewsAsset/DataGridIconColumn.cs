using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;

namespace FileRenamerDiff.Views
{
    /// <summary>
    /// PackIconを表示するDataGrid列
    /// </summary>
    public class DataGridIconColumn : DataGridTemplateColumn
    {
        #region PackIcon依存関係プロパティ
        public PackIconKind Kind
        {
            get => (PackIconKind)GetValue(PackIconProperty);
            set => SetValue(PackIconProperty, value);
        }
        public static readonly DependencyProperty PackIconProperty =
            DependencyProperty.Register(nameof(Kind), typeof(PackIconKind), typeof(DataGridIconColumn), new PropertyMetadata(default(PackIconKind), PropertyChanged));

        private static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is PackIconKind iconKind
                && d is DataGridIconColumn iconColumn)
            {
                var iconFactory = new FrameworkElementFactory(typeof(PackIcon));
                iconFactory.SetValue(PackIcon.KindProperty, iconKind);
                iconColumn.CellTemplate = new DataTemplate() { VisualTree = iconFactory };
            }
        }
        #endregion

        public DataGridIconColumn()
        {
            //選択もタブストップもできないセルにする
            if (App.Current.TryFindResource("IgnoreCell") is Style style)
            {
                this.CellStyle = style;
            }
        }
    }
}