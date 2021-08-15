using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Anotar.Serilog;

using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace FileRenamerDiff.Models
{
    public static class AppExtention
    {
        /// <summary>
        /// コレクションのメンバーを連結します。各メンバーの間には、指定した区切り記号が挿入されます。
        /// </summary>
        public static string ConcatenateString<T>(this IEnumerable<T> values, string sepalator) => String.Join(sepalator, values);

        /// <summary>
        /// コレクションのメンバーを連結します。各メンバーの間には、指定した区切り記号が挿入されます。
        /// </summary>
        public static string ConcatenateString<T>(this IEnumerable<T> values, char sepalator = ' ') => String.Join(sepalator, values);

        /// <summary>
        /// Linesをパイプ'|'で連結する
        /// </summary>
        public static string ToRawText(this DiffPaneModel diffPane) => diffPane.Lines.Select(x => x.Text).ConcatenateString('|');

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
        public static IEnumerable<T> Do<T>(this IEnumerable<T> source, Action<T, int> onNext) =>
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
        ///nullの要素を取り除いてnullが含まれていないことが保証されたWhere
        /// </summary>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class =>
            source.OfType<T>();

        public static string? GetDirectoryPath(this IFileSystemInfo fsInfo) => fsInfo switch
        {
            FileInfoBase fi => fi.DirectoryName,
            DirectoryInfoBase di => di.Parent?.FullName,
            _ => string.Empty
        };

        public static bool GetExistWithReflesh(this IFileSystemInfo fsInfo)
        {
            fsInfo.Refresh();
            return fsInfo.Exists;
        }

        /// <summary>
        /// 確実にファイル／ディレクトリの名前を変更する
        /// </summary>
        /// <param name="fsInfo">変更元ファイル</param>
        /// <param name="outputFilePath">変更後ファイルパス</param>
        public static void Rename(this IFileSystemInfo fsInfo, string outputFilePath)
        {
            switch (fsInfo)
            {
                case FileInfoBase fi:
                    fi.MoveTo(outputFilePath);
                    return;
                case DirectoryInfoBase di:
                    di.RenameSafely(outputFilePath);
                    return;

            }
        }

        /// <summary>
        /// 確実にディレクトリの名前を変更する
        /// </summary>
        /// <param name="di">変更元ディレクトリ情報/param>
        /// <param name="outputFilePath">変更後ファイルパス</param>
        public static void RenameSafely(this DirectoryInfoBase di, string outputFilePath)
        {
            string sourceFilePath = di.FullName;
            //Directory.Moveはなぜか、大文字小文字だけの変更だとエラーする
            //なので、大文字小文字だけの変更の場合は一度別のファイル名に変更する
            if (string.Compare(sourceFilePath, outputFilePath, true) == 0)
            {
                var tempPath = GetSafeTempName(di.FileSystem, outputFilePath);
                di.MoveTo(tempPath);
            }

            di.MoveTo(outputFilePath);
        }

        /// <summary>
        /// 指定したファイルパスが他のファイルパスとかぶらなくなるまで"_"を足して返す
        /// </summary>
        internal static string GetSafeTempName(IFileSystem fileSystem, string filePath)
        {
            while (true)
            {
                filePath += "_";

                if (!fileSystem.File.Exists(filePath))
                    return filePath;
            }
        }

        /// <summary>
        /// 拡張子より前のファイルパスを返す（ex. "C:dirrr\\abc.txt"→"C:dirrr\\abc")
        /// </summary>
        internal static string GetFilePathWithoutExtension(string filePath) =>
            Path.Combine(Path.GetDirectoryName(filePath) ?? String.Empty, Path.GetFileNameWithoutExtension(filePath));

        /// <summary>
        /// 正規表現を生成する、失敗したらnullを返す
        /// </summary>
        public static Regex? CreateRegexOrNull(string pattern)
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
            ? Path.GetExtension(path)[1..]
            : string.Empty;

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

        /// <summary>
        /// ターゲットパターンが正しい形式か判定
        /// </summary>
        /// <param name="pattern">パターン</param>
        /// <param name="asExpression">正規表現パターンか</param>
        internal static bool IsValidRegexPattern(string pattern, bool asExpression)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                LogTo.Debug("TargetPattern '{@pattern}' is NOT valid. The pattern may not be empty or null.", pattern);
                return false;
            }

            if (!asExpression)
                return true;

            try
            {
                _ = new Regex(pattern);
                return true;
            }
            catch (ArgumentException ex)
            {
                LogTo.Debug("TargetPattern '{@pattern}' is NOT valid: {@msg}", pattern, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 一定時間内にTaskが終了しなければ、TimeoutExceptionを発生させる
        /// </summary>
        public static Task Timeout(this Task task, double millisec) => Timeout(task, TimeSpan.FromMilliseconds(millisec));

        /// <summary>
        /// 一定時間内にTaskが終了しなければ、TimeoutExceptionを発生させる
        /// </summary>
        public static async Task Timeout(this Task task, TimeSpan timeout)
        {
            var delay = Task.Delay(timeout);
            if (await Task.WhenAny(task, delay) == delay)
            {
                throw new TimeoutException();
            }
        }

        /// <summary>
        /// 一定時間内にTaskが終了しなければ、TimeoutExceptionを発生させる
        /// </summary>
        public static Task<T> Timeout<T>(this Task<T> task, double millisec) => Timeout(task, TimeSpan.FromMilliseconds(millisec));

        /// <summary>
        /// 一定時間内にTaskが終了しなければ、TimeoutExceptionを発生させる
        /// </summary>
        public static async Task<T> Timeout<T>(this Task<T> task, TimeSpan timeout)
        {
            await ((Task)task).Timeout(timeout);
            return await task;
        }
    }
}
