using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using FileRenamerDiff.Models;

using FluentAssertions;

using Xunit;

namespace UnitTests;

public class FileElementModel_Test
{
    private const string dirName = "FileRenamerDiff_Test";
    private const string dirPath = @"D:\" + dirName + @"\";

    [Theory]
    [InlineData("coopy -copy.txt", " -copy", "XXX", "coopyXXX.txt", false)]
    [InlineData("abc.txt", "txt", "csv", "abc.csv", true)]
    [InlineData("abc.txt", "txt", "csv", "abc.txt", false)]
    [InlineData("xABCx_AxBC.txt", "ABC", "[$0]", "x[ABC]x_AxBC.txt", false)]
    [InlineData("xABCx_AxBC.txt", "ABC", "$$0", "x$0x_AxBC.txt", false)]
    [InlineData("abc ABC AnBC", "ABC", "X$0X", "abc XABCX AnBC", true)]
    [InlineData("A0012 34", "\\d*(\\d{3})", "$1", "A012 34", true)]
    [InlineData("A0012 34", "\\d*(\\d{3})", "$$1", "A$1 34", true)]
    //[InlineData("low UPP Pas", "[A-z]", "\\u$0", "LOW UPP PAS", true)] //System.IO.Abstractionsのバグ？で失敗する
    //[InlineData("low UPP Pas", "[A-z]", "\\l$0", "low upp pas", true)]
    [InlineData("Ha14 Ｆｕ１７", "[Ａ-ｚ]|[０-９]", "\\h$0", "Ha14 Fu17", true)]
    [InlineData("Ha14 Ｆｕ１７", "[A-z]|[0-9]", "\\f$0", "Ｈａ１４ Ｆｕ１７", true)]
    [InlineData("Ha14 Ｆｕ１７", "[A-z]|[0-9]", "\\f$$0", @"_f$0_f$0_f$0_f$0 Ｆｕ１７", true)]
    [InlineData("ｱﾝﾊﾟﾝ ﾊﾞｲｷﾝ", "[ｦ-ﾟ]+", "\\f$0", "アンパン バイキン", true)]
    [InlineData("ｱﾝﾊﾟﾝ ﾊﾞｲｷﾝ", "[ｦ-ﾟ]+", "\\f$$0", "_f$0 _f$0", true)]
    [InlineData("süß ÖL Ära", "\\w?[äöüßÄÖÜẞ]\\w?", "\\n$0", "suess OEL Aera", true)]
    [InlineData("süß ÖL Ära", "\\w?[äöüßÄÖÜẞ]\\w?", "\\n$$0", "_n$0 _n$0 _n$0a", true)]
    [InlineData("abc.txt", "^", "X", "Xabc.txt", true)]
    [InlineData("abc.txt", "$", "X", "abc.txtX", true)]
    [InlineData("abc.txt", "$", "X", "abcX.txt", false)]
    [InlineData("abc.txt", "^", "$d", dirName + "abc.txt", false)]
    [InlineData("abc.txt", "^", "$d_", dirName + "_abc.txt", false)]
    [InlineData("abc.txt", "abc", "$d", dirName + ".txt", false)]
    [InlineData("abc.txt", "abc", "$$d", "$d.txt", false)]
    [InlineData("abc.txt", "(.?)(\\.\\w*$)", "$1_$d$2", "abc_" + dirName + ".txt", true)]
    public void ReplacePatternSimple(string targetFileName, string regexPattern, string replaceText, string expectedRenamedFileName, bool isRenameExt)
        => Test_FileElementCore(targetFileName, new[] { regexPattern }, new[] { replaceText }, expectedRenamedFileName, isRenameExt);

    [Theory]
    [InlineData("LargeYChange.txt", "Y", "LargeChange.txt", false)]
    [InlineData("Gray,Sea,Green", "[ae]", "Gry,S,Grn", true)]
    //[InlineData("deleteBeforeExt.txt", @".*(?=\.\w+$)", ".txt", false)]
    [InlineData("deleteBeforeExt.txt", @".*(?=\.\w+$)", ".txt", true)]
    [InlineData("Rocky4", "[A-Z]", "ocky4", true)]
    [InlineData("water", "a.e", "wr", true)]
    [InlineData("A.a あ~ä-", "\\w", ". ~-", true)]
    [InlineData("A B　C", "\\s", "ABC", true)]
    [InlineData("Rocky4", "\\d", "Rocky", true)]
    [InlineData("rear rock", "^r", "ear rock", true)]
    [InlineData("rock rear", "r$", "rock rea", true)]
    [InlineData("door,or,o,lr", "o*r", "d,,o,l", true)]
    [InlineData("door,or,o,lr", "o+r", "d,,o,lr", true)]
    [InlineData("door,or,o,lr", "o?r", "do,,o,l", true)]
    [InlineData("door,or,o,lr", "[or]{2}", "dr,,o,lr", true)]
    [InlineData("1_2.3_45", "\\d\\.\\d", "1__45", true)]
    public void DeletePattern(string targetFileName, string regexPattern, string expectedRenamedFileName, bool isRenameExt)
        => Test_FileElementCore(targetFileName, new[] { regexPattern }, new[] { string.Empty }, expectedRenamedFileName, isRenameExt);

    [Theory]
    [InlineData("Sapmle-1.txt", new[] { "\\d+", "0*(\\d{3})" }, new[] { "00$0", "$1" }, "Sapmle-001.txt", false)]
    [InlineData("Sapmle-12.txt", new[] { "\\d+", "0*(\\d{3})" }, new[] { "00$0", "$1" }, "Sapmle-012.txt", false)]
    [InlineData("Sapmle-123.txt", new[] { "\\d+", "0*(\\d{3})" }, new[] { "00$0", "$1" }, "Sapmle-123.txt", false)]
    [InlineData("Sapmle-1234.txt", new[] { "\\d+", "0*(\\d{3})" }, new[] { "00$0", "$1" }, "Sapmle-1234.txt", false)]
    [InlineData("Sapmle-N.txt", new[] { "\\d+", "0*(\\d{3})" }, new[] { "00$0", "$1" }, "Sapmle-N.txt", false)]
    public void ReplacePatternComplex(string targetFileName, IReadOnlyList<string> regexPatterns, IReadOnlyList<string> replaceTexts, string expectedRenamedFileName, bool isRenameExt)
        => Test_FileElementCore(targetFileName, regexPatterns, replaceTexts, expectedRenamedFileName, isRenameExt);

    internal static void Test_FileElementCore(string targetFileName, IReadOnlyList<string> regexPatterns, IReadOnlyList<string> replaceTexts, string expectedRenamedFileName, bool isRenameExt)
    {
        string targetFilePath = dirPath + targetFileName;
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            [targetFilePath] = new MockFileData(targetFilePath)
        });

        var messageEvent = new Subject<AppMessage>();
        var fileElem = new FileElementModel(fileSystem, targetFilePath, messageEvent);
        var queuePropertyChanged = new Queue<string?>();
        fileElem.PropertyChanged += (o, e) => queuePropertyChanged.Enqueue(e.PropertyName);

        //TEST1 初期状態
        fileElem.OutputFileName
                .Should().Be(targetFileName, "まだ元のファイル名のまま");

        fileElem.IsReplaced
            .Should().BeFalse("まだリネーム変更されていないはず");

        fileElem.State
            .Should().Be(RenameState.None, "まだリネーム保存していない");

        queuePropertyChanged
            .Should().BeEmpty("まだ通知は来ていないはず");

        //TEST2 Replace
        //ファイル名の一部を変更する置換パターンを作成
        ReplaceRegexBase[] replaceRegexes = Enumerable
            .Zip(regexPatterns, replaceTexts,
                (regex, replaceText) => new ReplacePattern(regex, replaceText, true))
            .Select(x => x.ToReplaceRegex())
            .WhereNotNull()
            .ToArray();

        //リネームプレビュー実行
        fileElem.Replace(replaceRegexes, isRenameExt);

        fileElem.OutputFileName
            .Should().Be(expectedRenamedFileName, "リネーム変更後のファイル名になったはず");

        bool shouldRename = targetFileName != expectedRenamedFileName;
        fileElem.IsReplaced
            .Should().Be(shouldRename, "リネーム後の名前と前の名前が違うなら、リネーム変更されたはず");

        if (shouldRename)
            queuePropertyChanged
                .Should().Contain(new[] { nameof(FileElementModel.OutputFileName), nameof(FileElementModel.OutputFilePath), nameof(FileElementModel.IsReplaced) });
        else
            queuePropertyChanged
                .Should().BeEmpty();

        fileElem.State
            .Should().Be(RenameState.None, "リネーム変更はしたが、まだリネーム保存していない");

        fileSystem.Directory.GetFiles(Path.GetDirectoryName(targetFilePath))
            .Select(p => Path.GetFileName(p))
            .Should().BeEquivalentTo(new[] { targetFileName }, "ファイルシステム上はまだ前の名前のはず");

        //TEST3 Rename
        fileElem.Rename();

        fileElem.State
            .Should().Be(RenameState.Renamed, "リネーム保存されたはず");

        fileElem.InputFileName
            .Should().Be(expectedRenamedFileName, "リネーム保存後のファイル名になったはず");

        fileSystem.Directory.GetFiles(Path.GetDirectoryName(targetFilePath))
            .Select(p => Path.GetFileName(p))
            .Should().BeEquivalentTo(new[] { expectedRenamedFileName }, "ファイルシステム上も名前が変わったはず");
    }

    [Theory]
    [InlineData("coopy -copy", " -copy", "XXX", "coopyXXX")]
    [InlineData("abc.Dir", "Dir", "YYY", "abc.YYY")]
    internal static void Test_FileElementDirectory(string targetFileName, string regexPattern, string replaceText, string expectedRenamedFileName)
    {
        string targetFilePath = dirPath + targetFileName;
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            [targetFilePath] = new MockDirectoryData()
        });

        var messageEvent = new Subject<AppMessage>();
        var fileElem = new FileElementModel(fileSystem, targetFilePath, messageEvent);
        var queuePropertyChanged = new Queue<string?>();
        fileElem.PropertyChanged += (o, e) => queuePropertyChanged.Enqueue(e.PropertyName);

        //TEST1 初期状態
        fileElem.OutputFileName
                .Should().Be(targetFileName, "まだ元のファイル名のまま");

        fileElem.IsReplaced
            .Should().BeFalse("まだリネーム変更されていないはず");

        fileElem.State
            .Should().Be(RenameState.None, "まだリネーム保存していない");

        queuePropertyChanged
            .Should().BeEmpty("まだ通知は来ていないはず");

        //TEST2 Replace
        //ファイル名の一部を変更する置換パターンを作成
        var replaceRegex = new ReplaceRegex(new Regex(regexPattern, RegexOptions.Compiled), replaceText);
        replaceRegex.ToString()
            .Should().Contain(regexPattern, replaceText, "->");

        ReplaceRegex[] replaceRegexes = new[]
            {
                    replaceRegex
                };

        //リネームプレビュー実行
        fileElem.Replace(replaceRegexes, false);

        fileElem.OutputFileName
            .Should().Be(expectedRenamedFileName, "リネーム変更後のファイル名になったはず");

        fileElem.ToString()
            .Should().ContainAll(new[] { targetFileName, expectedRenamedFileName }, because: "リネーム前後のファイル名を含んでいるはず");

        bool shouldRename = targetFileName != expectedRenamedFileName;
        fileElem.IsReplaced
            .Should().Be(shouldRename, "リネーム後の名前と前の名前が違うなら、リネーム変更されたはず");

        if (shouldRename)
            queuePropertyChanged
                .Should().Contain(new[] { nameof(FileElementModel.OutputFileName), nameof(FileElementModel.OutputFilePath), nameof(FileElementModel.IsReplaced) });
        else
            queuePropertyChanged
                .Should().BeEmpty();

        fileElem.State
            .Should().Be(RenameState.None, "リネーム変更はしたが、まだリネーム保存していない");

        fileSystem.Directory.GetDirectories(Path.GetDirectoryName(targetFilePath))
            .Select(p => Path.GetFileName(p))
            .Should().BeEquivalentTo(new[] { targetFileName }, "ファイルシステム上はまだ前の名前のはず");

        //TEST3 Rename
        fileElem.Rename();

        fileElem.State
            .Should().Be(RenameState.Renamed, "リネーム保存されたはず");

        //System.IO.Abstractions のバグ？で反映されていない
        //fileElem.InputFileName
        //    .Should().Be(expectedRenamedFileName, "リネーム保存後のファイル名になったはず");

        fileSystem.Directory.GetDirectories(Path.GetDirectoryName(targetFilePath))
            .Select(p => Path.GetFileName(p))
            .Should().BeEquivalentTo(new[] { expectedRenamedFileName }, "ファイルシステム上も名前が変わったはず");
    }

    [Theory]
    [InlineData("_abc.txt", "^", (dirName + "_abc.txt"))]
    internal static void AddFolderName(string targetFileName, string regexPattern, string expectedRenamedFileName)
    {
        string targetFilePath = dirPath + targetFileName;
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            [targetFilePath] = new MockFileData(targetFileName)
        });

        var messageEvent = new Subject<AppMessage>();
        var fileElem = new FileElementModel(fileSystem, targetFilePath, messageEvent);
        var queuePropertyChanged = new Queue<string?>();
        fileElem.PropertyChanged += (o, e) => queuePropertyChanged.Enqueue(e.PropertyName);

        //TEST2 Replace
        //ファイル名の一部を変更する置換パターンを作成
        var replaceRegex = new ReplacePattern(regexPattern, "$d", true).ToReplaceRegex()!;

        //リネームプレビュー実行
        fileElem.Replace(new[] { replaceRegex }, false);

        fileElem.OutputFileName
            .Should().Be(expectedRenamedFileName, "リネーム変更後のファイル名になったはず");

        fileElem.ToString()
                .Should().ContainAll(new[] { targetFileName, expectedRenamedFileName
    }, because: "リネーム前後のファイル名を含んでいるはず");

        bool shouldRename = targetFileName != expectedRenamedFileName;
        fileElem.IsReplaced
            .Should().Be(shouldRename, "リネーム後の名前と前の名前が違うなら、リネーム変更されたはず");

        if (shouldRename)
            queuePropertyChanged
                .Should().Contain(new[] { nameof(FileElementModel.OutputFileName), nameof(FileElementModel.OutputFilePath), nameof(FileElementModel.IsReplaced) });
        else
            queuePropertyChanged
                .Should().BeEmpty();

        fileElem.State
            .Should().Be(RenameState.None, "リネーム変更はしたが、まだリネーム保存していない");

        fileSystem.Directory.GetFiles(Path.GetDirectoryName(targetFilePath))
            .Select(p => Path.GetFileName(p))
            .Should().BeEquivalentTo(new[] { targetFileName }, "ファイルシステム上はまだ前の名前のはず");

        //TEST3 Rename
        fileElem.Rename();

        fileElem.State
            .Should().Be(RenameState.Renamed, "リネーム保存されたはず");

        //System.IO.Abstractions のバグ？で反映されていない
        //fileElem.InputFileName
        //    .Should().Be(expectedRenamedFileName, "リネーム保存後のファイル名になったはず");

        fileSystem.Directory.GetFiles(Path.GetDirectoryName(targetFilePath))
            .Select(p => Path.GetFileName(p))
            .Should().BeEquivalentTo(new[] { expectedRenamedFileName }, "ファイルシステム上も名前が変わったはず");
    }

    [Fact]
    public void FileElement_WarningMessageInvalid()
    {
        string targetFilePath = dirPath + @"ABC.txt";
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            [targetFilePath] = new MockFileData("ABC")
        });

        var messageEvent = new Subject<AppMessage>();
        var messages = messageEvent.ToReadOnlyList();

        var fileElem = new FileElementModel(fileSystem, targetFilePath, messageEvent);

        //TEST1 初期状態
        messages
            .Should().BeEmpty("まだなんの警告もきていないはず");

        //TEST2 Replace
        //無効文字の置換パターン

        //リネームプレビュー実行
        fileElem.Replace(new[] { new ReplaceRegex(new Regex("A"), ":") }, false);

        const string expectedFileName = "_BC.txt";

        fileElem.OutputFileName
            .Should().Be(expectedFileName, "無効文字が[_]に置き換わった置換後文字列になっているはず");

        fileElem.IsReplaced
            .Should().BeTrue("リネーム変更されたはず");

        fileElem.State
            .Should().Be(RenameState.None, "リネーム変更はしたが、まだリネーム保存していない");

        messages
            .Should().HaveCount(1, "無効文字が含まれていた警告があるはず");

        //TEST3 Rename
        fileElem.Rename();

        fileElem.State
            .Should().Be(RenameState.Renamed, "リネーム保存されたはず");

        fileSystem.Directory.GetFiles(Path.GetDirectoryName(targetFilePath))
            .Select(p => Path.GetFileName(p))
            .Should().BeEquivalentTo(new[] { expectedFileName }, "ファイルシステム上も名前が変わったはず");
    }

    [Fact]
    public void FileElement_WarningMessageCannotChange()
    {
        string targetFilePath = dirPath + @"ABC.txt";
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            [targetFilePath] = new MockFileData("ABC") { AllowedFileShare = FileShare.None }
        });

        var messageEvent = new Subject<AppMessage>();
        var messages = messageEvent.ToReadOnlyList();

        var fileElem = new FileElementModel(fileSystem, targetFilePath, messageEvent);

        //TEST1 初期状態
        messages
            .Should().BeEmpty("まだなんの警告もきていないはず");

        //TEST2 Replace
        //無効文字の置換パターン

        //リネームプレビュー実行
        fileElem.Replace(new[] { new ReplaceRegex(new Regex("ABC"), "xyz") }, false);

        const string expectedFileName = "xyz.txt";

        fileElem.OutputFileName
            .Should().Be(expectedFileName, "置換後文字列になっているはず");

        fileElem.IsReplaced
            .Should().BeTrue("リネーム変更されたはず");

        fileElem.State
            .Should().Be(RenameState.None, "リネーム変更はしたが、まだリネーム保存していない");

        messages
            .Should().BeEmpty("まだなんの警告もきていないはず");

        //TEST3 Rename
        fileElem.Rename();

        fileElem.State
            .Should().Be(RenameState.FailedToRename, "リネーム失敗したはず");

        messages
            .Should().HaveCount(1, "ファイルがリネーム失敗した警告があるはず");

        fileSystem.Directory.GetFiles(Path.GetDirectoryName(targetFilePath))
            .Select(p => Path.GetFileName(p))
            .Should().NotContain(new[] { expectedFileName }, "ファイルシステム上では変わっていないはず");

        fileSystem.Directory.GetFiles(Path.GetDirectoryName(targetFilePath))
            .Should().Contain(new[] { targetFilePath }, "ファイルシステム上では変わっていないはず");
    }

    [Theory]
    [InlineData("test.abc", (FileAttributes.Normal), FileCategories.OtherFile)]
    [InlineData("test.abc", (FileAttributes.Hidden), FileCategories.HiddenFile)]
    [InlineData("test.abc", (FileAttributes.Hidden | FileAttributes.Archive), FileCategories.HiddenFile)]
    [InlineData("test", (FileAttributes.Hidden | FileAttributes.Directory), FileCategories.HiddenFolder)]
    [InlineData("test", (FileAttributes.Directory), FileCategories.Folder)]
    [InlineData("test", (FileAttributes.Directory | FileAttributes.ReadOnly), FileCategories.Folder)]
    [InlineData("test.png", (FileAttributes.Normal), FileCategories.Image)]
    [InlineData("test.bmp", (FileAttributes.Normal), FileCategories.Image)]
    [InlineData("test.bmp", (FileAttributes.Hidden), FileCategories.HiddenFile)]
    [InlineData("test.bmppp", (FileAttributes.Normal), FileCategories.OtherFile)]
    [InlineData("test.mp3", (FileAttributes.Normal), FileCategories.Audio)]
    [InlineData("test.mp4", (FileAttributes.Normal), FileCategories.Video)]
    [InlineData("test.zip", (FileAttributes.Normal), FileCategories.Compressed)]
    [InlineData("test.txt.lnk", (FileAttributes.Normal), FileCategories.Shortcut)]
    [InlineData("test.exe", (FileAttributes.Normal), FileCategories.Exe)]
    [InlineData("test.dll", (FileAttributes.Normal), FileCategories.Library)]
    [InlineData("test.txt", (FileAttributes.Normal), FileCategories.Text)]
    [InlineData("test.TXT", (FileAttributes.Normal), FileCategories.Text)]
    [InlineData("test.csv", (FileAttributes.Normal), FileCategories.Csv)]
    [InlineData("test.tsv", (FileAttributes.Normal), FileCategories.Csv)]
    [InlineData("test.doc", (FileAttributes.Normal), FileCategories.Word)]
    [InlineData("test.xlsx", (FileAttributes.Normal), FileCategories.Excel)]
    [InlineData("test.pptx", (FileAttributes.Normal), FileCategories.PowerPoint)]
    [InlineData("test.adp", (FileAttributes.Normal), FileCategories.Access)]
    [InlineData("test.one", (FileAttributes.Normal), FileCategories.OneNote)]
    [InlineData("test.pst", (FileAttributes.Normal), FileCategories.Outlook)]
    [InlineData("test.eml", (FileAttributes.Normal), FileCategories.Mail)]
    [InlineData("test.pdf", (FileAttributes.Normal), FileCategories.Pdf)]
    [InlineData("test.md", (FileAttributes.Normal), FileCategories.Markdown)]
    public void FileCategory(string targetFileName, FileAttributes attributes, FileCategories category)
    {
        string targetFilePath = dirPath + targetFileName;
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            [targetFilePath] = new MockFileData(targetFilePath) { Attributes = attributes }
        });

        var fileElem = new FileElementModel(fileSystem, targetFilePath, new Subject<AppMessage>());
        fileElem.Category
            .Should().Be(category);
    }

    [Fact]
    public void FileCategory_Unique()
    {
        Enum.GetValues<FileCategories>()
            .SelectMany(x => x.GetFileExtPattern())
            .Should().OnlyHaveUniqueItems();
    }
}
