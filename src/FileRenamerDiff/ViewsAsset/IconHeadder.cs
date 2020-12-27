using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MaterialDesignThemes.Wpf;

namespace FileRenamerDiff.Views
{
    static class IconHeadder
    {
        #region IconKind添付プロパティ
        public static PackIconKind GetIconKind(DependencyObject obj) => (PackIconKind)obj.GetValue(IconKindProperty);
        public static void SetIconKind(DependencyObject obj, PackIconKind value) => obj.SetValue(IconKindProperty, value);
        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.RegisterAttached("IconKind", typeof(PackIconKind), typeof(IconHeadder), new PropertyMetadata(default(PackIconKind)));

        #endregion
    }
}
