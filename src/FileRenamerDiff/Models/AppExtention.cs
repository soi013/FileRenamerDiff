using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Text.RegularExpressions;

using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using Anotar.Serilog;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.DiffBuilder;
using DiffPlex;

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

        /// <summary>
        /// 要素とインデックスを使用して、なんらかの処理を付随的に行う。
        /// </summary>
        /// <param name="source">入力</param>
        /// <param name="onNext">付随的処理</param>
        /// <returns>sourceと同じ</returns>
        public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action<TSource, int> onNext) =>
            source.Select((x, i) =>
            {
                onNext(x, i);
                return x;
            });

        /// <summary>
        /// インデックスを付与して列挙する
        /// </summary>
        public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> ts) => ts.Select((t, i) => (t, i));

        /// <summary>
        /// CountプロパティをIObservableとして購読する
        /// </summary>
        public static IObservable<int> ObserveCount<T>(this ObservableCollection<T> source) =>
            source.ObserveProperty(x => x.Count);

        /// <summary>
        /// シークエンスに要素が含まれているかをIObservableとして購読する
        /// </summary>
        public static IObservable<bool> ObserveIsAny<T>(this ObservableCollection<T> source) =>
            source.ObserveCount().Select(x => x >= 1);

        /// <summary>
        /// シークエンスが空かをIObservableとして購読する
        /// </summary>
        public static IObservable<bool> ObserveIsEmpty<T>(this ObservableCollection<T> source) =>
            source.ObserveCount().Select(x => x <= 0);


        /// <summary>
        /// 確実にファイル／ディレクトリの名前を変更する
        /// </summary>
        /// <param name="fileInfo">変更元ファイル</param>
        /// <param name="outputFilePath">変更後ファイルパス</param>
        public static void Rename(this FileInfo fileInfo, string outputFilePath)
        {
            if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
            {
                //Directory.Moveはなぜか、大文字小文字だけの変更だとエラーする
                //なので、大文字小文字だけの変更の場合は一度別のファイル名に変更する
                RenameDirectory(fileInfo.FullName, outputFilePath);
            }
            else
            {
                fileInfo.MoveTo(outputFilePath);
            }
        }

        /// <summary>
        /// 確実にディレクトリの名前を変更する
        /// </summary>
        /// <param name="sourceFilePath">変更元ファイルパス</param>
        /// <param name="outputFilePath">変更後ファイルパス</param>
        public static void RenameDirectory(string sourceFilePath, string outputFilePath)
        {
            //Directory.Moveはなぜか、大文字小文字だけの変更だとエラーする
            //なので、大文字小文字だけの変更の場合は一度別のファイル名に変更する
            if ((String.Compare(sourceFilePath, outputFilePath, true) == 0))
            {
                var tempPath = GetSafeTempName(outputFilePath);

                Directory.Move(sourceFilePath, tempPath);
                Directory.Move(tempPath, outputFilePath);
            }
            else
            {
                Directory.Move(sourceFilePath, outputFilePath);
            }
        }

        /// <summary>
        /// 指定したファイルパスが他のファイルパスとかぶらなくなるまで"_"を足して返す
        /// </summary>
        private static string GetSafeTempName(string outputFilePath)
        {
            outputFilePath += "_";
            while (File.Exists(outputFilePath))
            {
                outputFilePath += "_";
            }
            return outputFilePath;
        }

        /// <summary>
        /// 正規表現を生成する、失敗したらnullを返す
        /// </summary>
        public static Regex CreateRegexOrNull(string pattern)
        {
            try
            {
                //\\lなどの無効なパターンが入力されると例外が発生する
                return new Regex(pattern, RegexOptions.Compiled);
            }
            catch (Exception ex)
            {
                LogTo.Warning(ex, "Fail to Create Regex {@pattern}", pattern);
                return null;
            }
        }

        public static string GetExtentionCoreFromPath(string path) =>
            Path.HasExtension(path)
            ? Path.GetExtension(path).Substring(1)
            : String.Empty;


        /// <summary>
        /// 差分比較情報を作成
        /// </summary>
        public static SideBySideDiffModel CreateDiff(string inputText, string outText)
        {
            char[] wordSeparaters =
            {
                ' ', '\t', '.', '(', ')', '{', '}', ',', '!', '?', ';', //MarkDiffデフォルトからコピー
                '_','-','[',']','~','+','=','^',    //半角系
                '　','、','。','「','」','（','）','｛','｝','・','！','？','；','：','＿','ー','－','～','‐','＋','＊','／','＝','＾',    //全角系
            };
            var diff = new SideBySideDiffBuilder(new Differ(), wordSeparaters);
            return diff.BuildDiffModel(inputText, outText);
        }
    }
}