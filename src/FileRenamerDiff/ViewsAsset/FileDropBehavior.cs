using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

using Microsoft.Xaml.Behaviors;

namespace FileRenamerDiff.Views
{
    public class FileDropBehavior : Behavior<FrameworkElement>
    {
        #region Command依存関係プロパティ
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(FileDropBehavior), new PropertyMetadata(null));
        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AllowDrop = true;
            AssociatedObject.PreviewDragOver += OnPreviewDragOver;
            AssociatedObject.Drop += OnDrop;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewDragOver -= OnPreviewDragOver;
            AssociatedObject.Drop -= OnDrop;
        }

        private void OnPreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop, true)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private static string[]? ToFilePaths(IDataObject data) =>
            !data.GetDataPresent(DataFormats.FileDrop)
            ? null
            : (data.GetData(DataFormats.FileDrop) as string[]);

        private void OnDrop(object sender, System.Windows.DragEventArgs e)
        {
            if (!Command.CanExecute(e))
                return;

            string[]? paths = ToFilePaths(e.Data)
                ?.Where(x => !String.IsNullOrWhiteSpace(x))
                .ToArray();

            if (paths is not null)
                Command.Execute(paths);
        }
    }
}
