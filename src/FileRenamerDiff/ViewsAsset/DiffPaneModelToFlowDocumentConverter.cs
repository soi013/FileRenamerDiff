using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

using Anotar.Serilog;

using DiffPlex.DiffBuilder.Model;

using FileRenamerDiff.Models;

namespace FileRenamerDiff.Views
{
    public class DiffPaneModelToFlowDocumentConverter : IValueConverter
    {
        private static readonly Brush unchangeBrush = Colors.Transparent.ToSolidColorBrush(true);
        private static readonly Brush deletedBrush = AppExtention.ToColorOrDefault($"#FFAFD1").ToSolidColorBrush(true);
        private static readonly Brush insertedBrush = AppExtention.ToColorOrDefault($"#88E6A7").ToSolidColorBrush(true);
        private static readonly Brush imaginaryBrush = Colors.SkyBlue.ToSolidColorBrush(true);
        private static readonly Brush modifiedBrush = Colors.Orange.ToSolidColorBrush(true);
        private static readonly Brush changedTextBrush = Colors.Black.ToSolidColorBrush(true);
        private static readonly Brush normalTextBrush = (SolidColorBrush)App.Current.Resources["MaterialDesignBody"];

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not DiffPaneModel diffVM)
                return Binding.DoNothing;

            if (diffVM.Lines.IsEmpty())
                return Binding.DoNothing;

            if (diffVM.Lines.Count > 1)
                LogTo.Warning("Lines Count is over. {@LinesCount}", diffVM.Lines.Count);

            List<Run> lineView = ConvertLinveVmToRuns(diffVM.Lines.First());

            var paragraph = new Paragraph();
            paragraph.Inlines.AddRange(lineView);
            return new FlowDocument(paragraph);
        }

        private static List<Run> ConvertLinveVmToRuns(DiffPiece lineVM) =>
            lineVM.Type switch
            {
                //ChangeType.Modifiedだったら変更された部分だけハイライトしたいのでSubPieceからいろいろやる
                ChangeType.Modified => lineVM
                    .SubPieces
                    .Select(x => ConvertPieceVmToRun(x))
                    .ToList(),

                //ChangeType.Modified以外は行全体で同じ書式
                _ => new() { ConvertPieceVmToRun(lineVM) },
            };

        private static Run ConvertPieceVmToRun(DiffPiece pieceVM) =>
            new()
            {
                Text = pieceVM.Text,
                Foreground = (pieceVM.Type == ChangeType.Unchanged)
                    ? normalTextBrush
                    : changedTextBrush,
                //差分タイプによって、背景色を決定
                Background = (pieceVM.Type switch
                {
                    ChangeType.Deleted => deletedBrush,
                    ChangeType.Inserted => insertedBrush,
                    ChangeType.Imaginary => imaginaryBrush,
                    ChangeType.Modified => modifiedBrush,
                    _ => unchangeBrush
                }),
            };

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            LogTo.Error("Not Implemented");
            return Binding.DoNothing;
        }
    }
}
