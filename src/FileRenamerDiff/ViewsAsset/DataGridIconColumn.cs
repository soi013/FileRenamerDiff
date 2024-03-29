﻿using System.Windows;
using System.Windows.Controls;

using MaterialDesignThemes.Wpf;

namespace FileRenamerDiff.Views;

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
        if (e.NewValue is not PackIconKind iconKind || d is not DataGridIconColumn iconColumn)
            return;

        var iconFactory = new FrameworkElementFactory(typeof(PackIcon));
        iconFactory.SetValue(PackIcon.KindProperty, iconKind);
        iconColumn.CellTemplate = new DataTemplate() { VisualTree = iconFactory };
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
