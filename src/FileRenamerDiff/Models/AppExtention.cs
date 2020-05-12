using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

using Anotar.Serilog;
using DiffPlex.DiffBuilder.Model;

namespace FileRenamerDiff.Models
{
    public static class AppExtention
    {
        /// <summary>
        /// 指定したコレクションからコピーされた要素を格納するObservableCollectionを生成
        /// </summary>
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source) => new ObservableCollection<T>(source);

        /// <summary>
        /// コレクションのメンバーを連結します。各メンバーの間には、指定した区切り記号が挿入されます。
        /// </summary>
        public static string ConcatenateString<T>(this IEnumerable<T> values, string sepalator) => String.Join(sepalator, values);

        /// <summary>
        /// コレクションのメンバーを連結します。各メンバーの間には、指定した区切り記号が挿入されます。
        /// </summary>
        public static string ConcatenateString<T>(this IEnumerable<T> values, char sepalator = ' ') => String.Join(sepalator, values);

        /// <summary>
        /// Linesを改行で連結する
        /// </summary>
        public static string ToRawText(this DiffPaneModel diffPane) => $"{diffPane.Lines.ConcatenateString(Environment.NewLine)}";

        /// <summary>
        /// 差分前後の文字を比較表示
        /// </summary>
        public static string ToDisplayString(this SideBySideDiffModel ssDiff) => $"{ssDiff.OldText.ToRawText()}->{ssDiff.NewText.ToRawText()}";

        /// <summary>
        /// オブジェクトを変更不可能にし、その System.Windows.Freezable.IsFrozen プロパティを true に設定します。
        /// </summary>
        public static T WithFreeze<T>(this T obj) where T : Freezable
        {
            obj.Freeze();
            return obj;
        }

        /// <summary>
        ///　指定した色のSolidColorBrushに変換します。
        /// </summary>
        public static SolidColorBrush ToSolidColorBrush(this Color mColor, bool isFreeze = false) =>
           isFreeze
           ? new SolidColorBrush(mColor)
           : new SolidColorBrush(mColor).WithFreeze();

        /// <summary>
        /// 色を文字列化 ex. #CC10D0
        /// </summary>
        public static string ToCode(this Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        /// <summary>
        /// 指定した文字列を色に変換する、できなかったら透明色を返す
        /// </summary>
        public static Color ToColorOrDefault(string code) => CodeToColorOrNull(code) ?? default;

        /// <summary>
        /// 指定した文字列を色に変換する、できなかったらnullを返す
        /// </summary>
        public static Color? CodeToColorOrNull(string code)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(code);
            }
            catch (FormatException ex)
            {
                LogTo.Error(ex, "Fail to Convert Color");
                return null;
            }
        }

        public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action<TSource, int> onNext) =>
            source.Select((x, i) =>
            {
                onNext(x, i);
                return x;
            });

        public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> ts) => ts.Select((t, i) => (t, i));
    }
}