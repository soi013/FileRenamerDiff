namespace UnitTests;

public class ReplaceRegex_Test
{
    private const string dirName = "FileRenamerDiff_Test";
    private const string dirPath = $@"D:\{dirName}\";

    [Theory]
    [InlineData("coopy -copy.txt", " -copy", "XXX", "coopyXXX.txt")]
    [InlineData("abc.txt", "txt", "csv", "abc.csv")]
    [InlineData("xABCx_AxBC.txt", "ABC", "[$0]", "x[ABC]x_AxBC.txt")]
    [InlineData("xABCx_AxBC.txt", "ABC", "$$0", "x$0x_AxBC.txt")]
    [InlineData("abc ABC AnBC", "ABC", "X$0X", "abc XABCX AnBC")]
    [InlineData("A0012 34", "\\d*(\\d{3})", "$1", "A012 34")]
    [InlineData("A0012 34", "\\d*(\\d{3})", "$$1", "A$1 34")]
    [InlineData("low UPP Pas", "[A-z]", "\\u$0", "LOW UPP PAS")] //System.IO.Abstractionsのバグ？で失敗する
    [InlineData("low UPP Pas", "[A-z]", "\\l$0", "low upp pas")]
    [InlineData("Ha14 Ｆｕ１７", "[Ａ-ｚ]|[０-９]", "\\h$0", "Ha14 Fu17")]
    [InlineData("Ha14 Ｆｕ１７", "[A-z]|[0-9]", "\\f$0", "Ｈａ１４ Ｆｕ１７")]
    [InlineData("Ha14 Ｆｕ１７", "[A-z]|[0-9]", "\\f$$0", @"\f$0\f$0\f$0\f$0 Ｆｕ１７")]//ReplaceRegexではまだファイル名禁止文字(\)のことは気にしない
    [InlineData("ｱﾝﾊﾟﾝ ﾊﾞｲｷﾝ", "[ｦ-ﾟ]+", "\\f$0", "アンパン バイキン")]
    [InlineData("ｱﾝﾊﾟﾝ ﾊﾞｲｷﾝ", "[ｦ-ﾟ]+", "\\f$$0", @"\f$0 \f$0")]//ReplaceRegexではまだファイル名禁止文字(\)のことは気にしない
    [InlineData("süß ÖL Ära", "\\w?[äöüßÄÖÜẞ]\\w?", "\\n$0", "suess OEL Aera")]
    [InlineData("süß ÖL Ära", "\\w?[äöüßÄÖÜẞ]\\w?", "\\n$$0", @"\n$0 \n$0 \n$0a")]//ReplaceRegexではまだファイル名禁止文字(\)のことは気にしない
    [InlineData("abc.txt", "^", "X", "Xabc.txt")]
    [InlineData("abc.txt", "$", "X", "abc.txtX")]
    [InlineData("LargeYChange.txt", "Y", "", "LargeChange.txt")]
    [InlineData("Gray,Sea,Green", "[ae]", "", "Gry,S,Grn")]
    //[InlineData("deleteBeforeExt.txt", @".*(?=\.\w+$)", ".txt")]
    [InlineData("deleteBeforeExt.txt", @".*(?=\.\w+$)", "", ".txt")]
    [InlineData("Rocky4", "[A-Z]", "", "ocky4")]
    [InlineData("water", "a.e", "", "wr")]
    [InlineData("A.a あ~ä-", "\\w", "", ". ~-")]
    [InlineData("A B　C", "\\s", "", "ABC")]
    [InlineData("Rocky4", "\\d", "", "Rocky")]
    [InlineData("rear rock", "^r", "", "ear rock")]
    [InlineData("rock rear", "r$", "", "rock rea")]
    [InlineData("door,or,o,lr", "o*r", "", "d,,o,l")]
    [InlineData("door,or,o,lr", "o+r", "", "d,,o,lr")]
    [InlineData("door,or,o,lr", "o?r", "", "do,,o,l")]
    [InlineData("door,or,o,lr", "[or]{2}", "", "dr,,o,lr")]
    [InlineData("1_2.3_45", "\\d\\.\\d", "", "1__45")]
    public void Replace_Normal(string targetFileName, string regexPattern, string replaceText, string expectedRenamedFileName)
    {
        var regex = new Regex(regexPattern);
        var rpRegex = new ReplaceRegex(regex, replaceText);
        string replacedFileName = rpRegex.Replace(targetFileName);

        replacedFileName
            .Should().Be(expectedRenamedFileName);
    }

    [Theory]
    [InlineData("coopy -copy.txt", " -coopy")]
    [InlineData("coopy -copy.txt", " copy")]
    [InlineData("abc.txt", "xyz")]
    [InlineData("XXX.txt", "XXX")]
    public void Replace_NotChange(string targetFileName, string regexPattern)
    {
        var regex = new Regex(regexPattern);
        var rpRegex = new ReplaceRegex(regex, "XXX");
        string replacedFileName = rpRegex.Replace(targetFileName);

        replacedFileName
            .Should().Be(targetFileName);
    }

    [Theory]
    [InlineData("abc.txt", "^", "$d_", $"{dirName}_abc.txt")]
    [InlineData("abc.txt", "$", "_$d", $"abc.txt_{dirName}")]
    public void AddFolderName_Normal(string targetFileName, string regexPattern, string replaceText, string expectedRenamedFileName)
    {
        string targetFilePath = Path.Combine(dirPath, targetFileName);
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            [targetFilePath] = new MockFileData(targetFileName)
        });

        var fileInfo = fileSystem.FileInfo.FromFileName(targetFilePath);

        var regex = new Regex(regexPattern);
        var rpRegex = new AddDirectoryNameRegex(regex, replaceText);
        string replacedFileName = rpRegex.Replace(targetFileName, fsInfo: fileInfo);

        replacedFileName
            .Should().Be(expectedRenamedFileName);
    }

    [Theory]
    [InlineData("abc.txt", "def", "$d_")]
    [InlineData("abc.txt", "def", "_$d")]
    public void AddFolderName_NotChange(string targetFileName, string regexPattern, string replaceText)
    {
        string targetFilePath = Path.Combine(dirPath, targetFileName);
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            [targetFilePath] = new MockFileData(targetFileName)
        });

        var fileInfo = fileSystem.FileInfo.FromFileName(targetFilePath);

        var regex = new Regex(regexPattern);
        var rpRegex = new AddDirectoryNameRegex(regex, replaceText);
        string replacedFileName = rpRegex.Replace(targetFileName, fsInfo: fileInfo);

        replacedFileName
            .Should().Be(targetFileName);
    }

    [Theory]
    [InlineData("abc.txt", "^", "$n_", $"1_abc.txt")]
    [InlineData("abc.txt", "$", "_$n", $"abc.txt_1")]
    public void AddSerialNumber_Single(string targetFileName, string regexPattern, string replaceText, string expectedRenamedFileName)
    {
        string targetFilePath = Path.Combine(dirPath, targetFileName);
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            [targetFilePath] = new MockFileData(targetFileName)
        });

        var fileInfo = fileSystem.FileInfo.FromFileName(targetFilePath);

        var regex = new Regex(regexPattern);
        var rpRegex = new AddSerialNumberRegex(regex, replaceText);
        string replacedFileName = rpRegex.Replace(targetFileName, new[] { targetFilePath }, fileInfo);

        replacedFileName
            .Should().Be(expectedRenamedFileName);
    }

    [Theory]
    [InlineData("aaa.txt,bbb.txt,ccc.txt", "^", "$n_", $"1_aaa.txt")]
    [InlineData("bbb.txt,aaa.txt,ccc.txt", "^", "$n_", $"2_bbb.txt")]
    [InlineData("ccc.txt,aaa.txt,bbb.txt", "^", "$n_", $"3_ccc.txt")]
    public void AddSerialNumber_Multi(string concatedTargetFileNames, string regexPattern, string replaceText, string expectedRenamedFileName)
    {
        string[] targetFileNames = concatedTargetFileNames
                    .Split(',');
        string targetFileName = targetFileNames.First();
        string[] targetFilePaths = targetFileNames
            .Select(x => Path.Combine(dirPath, x))
            .ToArray();

        var fileSystem = AppExtension.CreateMockFileSystem(targetFilePaths);

        string targetFilePath = targetFilePaths.First();
        var fileInfo = fileSystem.FileInfo.FromFileName(targetFilePath);

        string[] orderdTargetFilePaths = targetFilePaths.OrderBy(x => x).ToArray();
        var regex = new Regex(regexPattern);
        var rpRegex = new AddSerialNumberRegex(regex, replaceText);
        string replacedFileName = rpRegex.Replace(targetFileName, orderdTargetFilePaths, fileInfo);

        replacedFileName
            .Should().Be(expectedRenamedFileName);
    }
}
