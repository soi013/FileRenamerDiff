global using System;
global using System.Collections.Generic;
global using System.ComponentModel;
global using System.IO;
global using System.Linq;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

using Anotar.Serilog;

using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace FileRenamerDiff.Models;

public static class AppExtension
{
    private static readonly IReadOnlyList<string> mockFilePaths = new[]
    {
        $@"D:\mydir\abc.txt",
        $@"D:\mydir\def.txt",
    };
    private static readonly IFileSystem mockFileSystem = AppExtension.CreateMockFileSystem(mockFilePaths);
    private static readonly IFileSystemInfo mockFileInfo = mockFileSystem.FileInfo.FromFileName(mockFilePaths[0]);

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
       ? new SolidColorBrush(mColor).WithFreeze()
       : new SolidColorBrush(mColor);

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
    /// 要素を使用して、なんらかの処理を付随的に行う。
    /// </summary>
    /// <param name="source">入力</param>
    /// <param name="onNext">付随的処理</param>
    /// <returns>sourceと同じ</returns>
    public static IEnumerable<T> Do<T>(this IEnumerable<T> source, Action<T> onNext) =>
        source.Select(x =>
        {
            onNext(x);
            return x;
        });

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
    public static IEnumerable<(T element, int index)> WithIndex<T>(this IEnumerable<T> ts) => ts.Select((t, i) => (t, i));

    /// <summary>
    ///nullの要素を取り除いてnullが含まれていないことが保証されたWhere
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class =>
        source.OfType<T>();

    /// <summary>
    ///nullの要素を取り除いてnullが含まれていないことが保証されたWhere
    /// </summary>
    public static IObservable<T> WhereNotNull<T>(this IObservable<T?> source) where T : class =>
        source.Where(x => x is not null)!;

    /// <summary>
    /// ファイルのあるディレクトリの名前を取得
    /// </summary>
    public static string? GetDirectoryName(this IFileSystemInfo fsInfo) =>
        Path.GetFileName(Path.GetDirectoryName(fsInfo.FullName));
    /// <summary>
    /// ファイルのあるディレクトリのパスを取得
    /// </summary>
    public static string? GetDirectoryPath(this IFileSystemInfo fsInfo) =>
       Path.GetDirectoryName(fsInfo.FullName);

    /// <summary>
    /// ファイルを更新して存在しているか取得
    /// </summary>
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
    private static void RenameSafely(this DirectoryInfoBase di, string outputFilePath)
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
    private static string GetSafeTempName(IFileSystem fileSystem, string filePath)
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
    /// ファイルパスから拡張子を取得する（ex. "C:dirrr\\abc.txt"→"txt")
    /// </summary>
    public static string GetExtentionCoreFromPath(string path) =>
        Path.HasExtension(path)
        ? Path.GetExtension(path)[1..]
        : string.Empty;

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
        if (pattern.IsNullOrEmpty())
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
    /// リプレイスパターンが正しい形式か判定
    /// </summary>
    /// <param name="pattern">パターン</param>
    /// <param name="asExpression">正規表現パターンか</param>
    internal static bool IsValidReplacePattern(string pattern, bool asExpression)
    {
        if (pattern is null)
        {
            LogTo.Debug("TargetPattern '{@pattern}' is NOT valid. The pattern may not be null.", pattern);
            return false;
        }

        if (!asExpression)
            return true;

        try
        {
            var rp = new ReplacePattern("a", pattern, asExpression)
                .ToReplaceRegex();

            rp?.Replace("sample.txt", mockFilePaths, mockFileInfo);

            return true;
        }
        catch (Exception ex)
        {
            if (!(ex is ArgumentException or FormatException))
                throw;

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

    /// <summary>
    ///属性ごとにキャッシュを作るためのジェネリッククラス
    /// </summary>
    /// <typeparam name="TAttribute">属性型</typeparam>
    private static class EnumAttributeCache<TEnum, TAttribute> where TAttribute : Attribute where TEnum : Enum
    {
        private static readonly ConcurrentDictionary<TEnum, TAttribute?> body = new();

        /// <summary>
        /// ConcurrentDictionaryのGetOrAddを呼び出す
        /// </summary>
        internal static TAttribute? GetOrAdd(TEnum enumKey, Func<TEnum, TAttribute?> valueFactory)
            => body.GetOrAdd(enumKey, valueFactory);
    }

    /// <summary>
    /// 特定の属性を取得する
    /// </summary>
    /// <typeparam name="TAttribute">属性型</typeparam>
    public static TAttribute? GetAttribute<TEnum, TAttribute>(this TEnum enumKey) where TAttribute : Attribute where TEnum : Enum =>
        //キャッシュに無かったら、リフレクションを用いて取得、キャッシュへの追加をして返す
        EnumAttributeCache<TEnum, TAttribute>.GetOrAdd(enumKey, _ => enumKey.GetAttributeCore<TEnum, TAttribute>());

    /// <summary>
    /// リフレクションを使用して特定の属性を取得する
    /// </summary>
    /// <typeparam name="TAttribute">属性型</typeparam>
    public static TAttribute? GetAttributeCore<TEnum, TAttribute>(this TEnum enumKey) where TAttribute : Attribute where TEnum : Enum
    {
        //リフレクションを用いて列挙体の型から情報を取得
        FieldInfo? fieldInfo = enumKey.GetType().GetField(enumKey.ToString());
        //指定した属性のリスト
        IEnumerable<TAttribute> attributes
            = fieldInfo?.GetCustomAttributes(typeof(TAttribute), false)
            .Cast<TAttribute>()
            ?? Array.Empty<TAttribute>();
        //属性がなかった場合、nullを返す
        if (!attributes.Any())
            return null;

        //同じ属性が複数含まれていても、最初のみ返す
        return attributes.First();
    }

    /// <summary>
    /// IsNullOrEmpty の簡易版。 (string.IsNullOrEmpty(value) が value.IsNullOrEmpty() で呼び出せます。)
    /// </summary>
    /// <param name="value">対象文字列</param>
    /// <returns>Null または Empty の場合に true を返します。</returns>
    public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);

    /// <summary>
    /// IsNullOrWhiteSpace の簡易版。 (string.IsNullOrWhiteSpace(value) が value.IsNullOrWhiteSpace() で呼び出せます。)
    /// </summary>
    /// <param name="value">対象文字列</param>
    /// <returns>Null または WhiteSpace の場合に true を返します。</returns>
    public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// IsNullOrWhiteSpace の簡易版。 (string.IsNullOrWhiteSpace(value) が value.IsNullOrWhiteSpace() で呼び出せます。)
    /// </summary>
    /// <param name="value">対象文字列</param>
    /// <returns>Null または WhiteSpace の場合に false を返します。</returns>
    public static bool HasText(this string? value) => !string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// 指定したファイルパスのファイルを含むファイルシステムの生成
    /// </summary>
    /// <param name="filePaths">内部に含むファイルパスコレクション</param>
    /// <returns>指定したファイルを含んだファイルシステム</returns>
    public static IFileSystem CreateMockFileSystem(IEnumerable<string> filePaths)
    {
        IDictionary<string, MockFileData> files = filePaths
            .ToDictionaryDirectKey(path => new MockFileData(path));

        return new MockFileSystem(files);
    }

    /// <summary>
    /// Keyに指定して、辞書へ変換
    /// </summary>
    /// <param name="source">入力</param>
    /// <param name="elementSelector">入力から値への変換デリゲート</param>
    public static Dictionary<TKey, TValue> ToDictionaryDirectKey<TKey, TValue>(this IEnumerable<TKey> source, Func<TKey, TValue> elementSelector) where TKey : notnull
        => source.ToDictionary(
            keySelector: key => key,
            elementSelector: elementSelector);

    /// <summary>
    /// コレクションが空かどうか
    /// </summary>
    public static bool IsEmpty<T>(this IEnumerable<T> source) => !source.Any();

    public static int? ToIntOrNull(this string value) => int.TryParse(value, out int result)
        ? result
        : null;

}
