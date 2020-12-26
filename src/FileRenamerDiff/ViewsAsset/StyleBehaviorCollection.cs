using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace FileRenamerDiff.Views
{
    /// <summary>
    /// SytleでBehaviorを扱うためのBehaviorコレクション
    /// https://blog.okazuki.jp/entry/2016/07/19/192918
    /// </summary>
    public class StyleBehaviorCollection : FreezableCollection<Behavior>
    {

        #region StyleBehaviors添付プロパティ
        public static StyleBehaviorCollection GetStyleBehaviors(DependencyObject obj) => (StyleBehaviorCollection)obj.GetValue(StyleBehaviorsProperty);
        public static void SetStyleBehaviors(DependencyObject obj, StyleBehaviorCollection value) => obj.SetValue(StyleBehaviorsProperty, value);
        public static readonly DependencyProperty StyleBehaviorsProperty =
            DependencyProperty.RegisterAttached("StyleBehaviors", typeof(StyleBehaviorCollection), typeof(StyleBehaviorCollection),
                new PropertyMetadata(StyleBehaviors_Changed));

        private static void StyleBehaviors_Changed(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
                return;

            if (e.NewValue is not StyleBehaviorCollection value)
                return;

            var behaviors = Interaction.GetBehaviors(sender);
            //Cloneしないと複数のコントロールで使えない
            behaviors.Clear();

            foreach (var b in value.Select(x => (Behavior)x.Clone()))
                behaviors.Add(b);
        }
        #endregion


        protected override Freezable CreateInstanceCore() => new StyleBehaviorCollection();
    }
}
