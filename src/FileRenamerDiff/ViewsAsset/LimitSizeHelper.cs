using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FileRenamerDiff.Views
{
    class LimitSizeHelper
    {
        #region LimitMaxHeight添付プロパティ
        public static double GetLimitMaxHeight(DependencyObject obj) => (double)obj.GetValue(LimitMaxHeightProperty);
        public static void SetLimitMaxHeight(DependencyObject obj, double value) => obj.SetValue(LimitMaxHeightProperty, value);
        public static readonly DependencyProperty LimitMaxHeightProperty =
            DependencyProperty.RegisterAttached("LimitMaxHeight", typeof(double), typeof(LimitSizeHelper),
                    new PropertyMetadata(1d, (d, e) => AddLimitMaxSize(d, e, false)));
        #endregion

        #region LimitMaxWidth添付プロパティ
        public static double GetLimitMaxWidth(DependencyObject obj) => (double)obj.GetValue(LimitMaxWidthProperty);
        public static void SetLimitMaxWidth(DependencyObject obj, double value) => obj.SetValue(LimitMaxWidthProperty, value);
        public static readonly DependencyProperty LimitMaxWidthProperty =
            DependencyProperty.RegisterAttached("LimitMaxWidth", typeof(double), typeof(LimitSizeHelper),
                    new PropertyMetadata(-1d, (d, e) => AddLimitMaxSize(d, e, true)));
        #endregion


        private static void AddLimitMaxSize(DependencyObject d, DependencyPropertyChangedEventArgs e, bool isWidth)
        {
            if (d is FrameworkElement targetObj
                && targetObj.Parent is Panel panel
                && e.NewValue is double newValue && newValue > 0)
            {
                panel.SizeChanged += (o, _) =>
                    Parent_SizeChanged(targetObj, panel, isWidth, newValue);
            }
        }

        private static void Parent_SizeChanged(FrameworkElement targetObj, Panel panel, bool isWidth, double ratio)
        {
            var otherSumSize = panel.Children
                .Cast<FrameworkElement>()
                .Where(x => x != targetObj)
                .Sum(x => isWidth ? x.ActualWidth : x.ActualHeight);

            double maxSize = ((isWidth ? panel.ActualWidth : panel.ActualHeight) - otherSumSize) * ratio;

            if (isWidth)
                targetObj.MaxWidth = maxSize;
            else
                targetObj.MaxHeight = maxSize;
        }
    }
}
