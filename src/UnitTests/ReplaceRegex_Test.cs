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
    [InlineData("abc.txt", "^", "$n<5>_", $"5_abc.txt")]
    [InlineData("abc.txt", "^", "$n<,10>_", $"1_abc.txt")]
    [InlineData("abc.txt", "^", "$n<10,10>_", $"10_abc.txt")]
    [InlineData("abc.txt", "^", "$n<,,000>_", $"001_abc.txt")]
    [InlineData("abc.txt", "^", "$n<12,,000>_", $"012_abc.txt")]
    [InlineData("abc.txt", "^", "$n<10,10,000>_", $"010_abc.txt")]
    [InlineData("abc.txt", "^", "$n<99,,00>_", $"99_abc.txt")]
    [InlineData("abc.txt", "^", "$n<999,,00>_", $"999_abc.txt")]
    [InlineData("abc.txt", "^", "$n<,,,r>_", $"1_abc.txt")]
    [InlineData("abc.txt", "^", "$n<5,,,r>_", $"5_abc.txt")]
    [InlineData("abc.txt", "^", "$n<,10,,r>_", $"1_abc.txt")]
    [InlineData("abc.txt", "^", "$n<10,10,,r>_", $"10_abc.txt")]
    [InlineData("abc.txt", "^", "$n<,,000,r>_", $"001_abc.txt")]
    [InlineData("abc.txt", "^", "$n<12,,000,r>_", $"012_abc.txt")]
    [InlineData("abc.txt", "^", "$n<10,10,000,r>_", $"010_abc.txt")]
    [InlineData("abc.txt", "^", "$n<99,,00,r>_", $"99_abc.txt")]
    [InlineData("abc.txt", "^", "$n<999,,00,r>_", $"999_abc.txt")]
    [InlineData("abc.txt", "^", "$n<999,,00,r,i>_", $"999_abc.txt")]
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
    [InlineData("aaa.txt,bbb.txt,ccc.txt", "^", "$n<100>_", $"100_aaa.txt")]
    [InlineData("bbb.txt,aaa.txt,ccc.txt", "^", "$n<100>_", $"101_bbb.txt")]
    [InlineData("ccc.txt,aaa.txt,bbb.txt", "^", "$n<100>_", $"102_ccc.txt")]
    [InlineData("aaa.txt,bbb.txt,ccc.txt", "^", "$n<,10>_", $"1_aaa.txt")]
    [InlineData("bbb.txt,aaa.txt,ccc.txt", "^", "$n<,10>_", $"11_bbb.txt")]
    [InlineData("ccc.txt,aaa.txt,bbb.txt", "^", "$n<,10>_", $"21_ccc.txt")]
    [InlineData("aaa.txt,bbb.txt,ccc.txt", "^", "$n<10,10>_", $"10_aaa.txt")]
    [InlineData("bbb.txt,aaa.txt,ccc.txt", "^", "$n<10,10>_", $"20_bbb.txt")]
    [InlineData("ccc.txt,aaa.txt,bbb.txt", "^", "$n<10,10>_", $"30_ccc.txt")]
    [InlineData("aaa.txt,bbb.txt,ccc.txt", "^", "$n<,,000>_", $"001_aaa.txt")]
    [InlineData("bbb.txt,aaa.txt,ccc.txt", "^", "$n<,,000>_", $"002_bbb.txt")]
    [InlineData("ccc.txt,aaa.txt,bbb.txt", "^", "$n<,,000>_", $"003_ccc.txt")]
    [InlineData("aaa.txt,bbb.txt,ccc.txt", "^", "$n<11,,000>_", $"011_aaa.txt")]
    [InlineData("bbb.txt,aaa.txt,ccc.txt", "^", "$n<11,,000>_", $"012_bbb.txt")]
    [InlineData("ccc.txt,aaa.txt,bbb.txt", "^", "$n<11,,000>_", $"013_ccc.txt")]
    [InlineData("aaa.txt,bbb.txt,ccc.txt", "^", "$n<,10,000>_", $"001_aaa.txt")]
    [InlineData("bbb.txt,aaa.txt,ccc.txt", "^", "$n<,10,000>_", $"011_bbb.txt")]
    [InlineData("ccc.txt,aaa.txt,bbb.txt", "^", "$n<,10,000>_", $"021_ccc.txt")]
    [InlineData("aaa.txt,bbb.txt,ccc.txt", "^", "$n<91,10,000>_", $"091_aaa.txt")]
    [InlineData("bbb.txt,aaa.txt,ccc.txt", "^", "$n<91,10,000>_", $"101_bbb.txt")]
    [InlineData("ccc.txt,aaa.txt,bbb.txt", "^", "$n<91,10,000>_", $"111_ccc.txt")]
    [InlineData(@"Dir1\aaa.txt,Dir1\bbb.txt,Dir2\ccc.txt", "^", "$n_", $"1_aaa.txt")]
    [InlineData(@"Dir1\bbb.txt,Dir1\aaa.txt,Dir2\ccc.txt", "^", "$n_", $"2_bbb.txt")]
    [InlineData(@"Dir2\ccc.txt,Dir1\aaa.txt,Dir1\bbb.txt", "^", "$n_", $"3_ccc.txt")]
    [InlineData(@"Dir1\aaa.txt,Dir1\bbb.txt,Dir2\ccc.txt", "^", "$n<,,,r>_", $"1_aaa.txt")]
    [InlineData(@"Dir1\bbb.txt,Dir1\aaa.txt,Dir2\ccc.txt", "^", "$n<,,,r>_", $"2_bbb.txt")]
    [InlineData(@"Dir2\ccc.txt,Dir1\aaa.txt,Dir1\bbb.txt", "^", "$n<,,,r>_", $"1_ccc.txt")]
    [InlineData(@"Dir1\aaa.txt,Dir1\bbb.txt,Dir2\ccc.txt", "^", "$n<10,,,r>_", $"10_aaa.txt")]
    [InlineData(@"Dir1\bbb.txt,Dir1\aaa.txt,Dir2\ccc.txt", "^", "$n<10,,,r>_", $"11_bbb.txt")]
    [InlineData(@"Dir2\ccc.txt,Dir1\aaa.txt,Dir1\bbb.txt", "^", "$n<10,,,r>_", $"10_ccc.txt")]
    [InlineData(@"Dir1\aaa.txt,Dir1\bbb.txt,Dir2\ccc.txt", "^", "$n<,5,,r>_", $"1_aaa.txt")]
    [InlineData(@"Dir1\bbb.txt,Dir1\aaa.txt,Dir2\ccc.txt", "^", "$n<,5,,r>_", $"6_bbb.txt")]
    [InlineData(@"Dir2\ccc.txt,Dir1\aaa.txt,Dir1\bbb.txt", "^", "$n<,5,,r>_", $"1_ccc.txt")]
    [InlineData(@"Dir1\aaa.txt,Dir1\bbb.txt,Dir2\ccc.txt", "^", "$n<,,00,r>_", $"01_aaa.txt")]
    [InlineData(@"Dir1\bbb.txt,Dir1\aaa.txt,Dir2\ccc.txt", "^", "$n<,,00,r>_", $"02_bbb.txt")]
    [InlineData(@"Dir2\ccc.txt,Dir1\aaa.txt,Dir1\bbb.txt", "^", "$n<,,00,r>_", $"01_ccc.txt")]
    [InlineData(@"Dir1\aaa.txt,Dir1\bbb.txt,Dir2\ccc.txt", "^", "$n<10,5,000,r>_", $"010_aaa.txt")]
    [InlineData(@"Dir1\bbb.txt,Dir1\aaa.txt,Dir2\ccc.txt", "^", "$n<10,5,000,r>_", $"015_bbb.txt")]
    [InlineData(@"Dir2\ccc.txt,Dir1\aaa.txt,Dir1\bbb.txt", "^", "$n<10,5,000,r>_", $"010_ccc.txt")]
    [InlineData(@"Dir1\aaa.txt,Dir1\bbb.txt,Dir2\ccc.txt", "^", "$n<,,,,i>_", $"3_aaa.txt")]
    [InlineData(@"Dir1\bbb.txt,Dir1\aaa.txt,Dir2\ccc.txt", "^", "$n<,,,,i>_", $"2_bbb.txt")]
    [InlineData(@"Dir2\ccc.txt,Dir1\aaa.txt,Dir1\bbb.txt", "^", "$n<,,,,i>_", $"1_ccc.txt")]
    [InlineData(@"Dir1\aaa.txt,Dir1\bbb.txt,Dir2\ccc.txt", "^", "$n<10,,,,i>_", $"12_aaa.txt")]
    [InlineData(@"Dir1\bbb.txt,Dir1\aaa.txt,Dir2\ccc.txt", "^", "$n<10,,,,i>_", $"11_bbb.txt")]
    [InlineData(@"Dir2\ccc.txt,Dir1\aaa.txt,Dir1\bbb.txt", "^", "$n<10,,,,i>_", $"10_ccc.txt")]
    [InlineData(@"Dir1\aaa.txt,Dir1\bbb.txt,Dir2\ccc.txt", "^", "$n<,5,,,i>_", $"11_aaa.txt")]
    [InlineData(@"Dir1\bbb.txt,Dir1\aaa.txt,Dir2\ccc.txt", "^", "$n<,5,,,i>_", $"6_bbb.txt")]
    [InlineData(@"Dir2\ccc.txt,Dir1\aaa.txt,Dir1\bbb.txt", "^", "$n<,5,,,i>_", $"1_ccc.txt")]
    [InlineData(@"Dir1\aaa.txt,Dir1\bbb.txt,Dir2\ccc.txt", "^", "$n<,,00,,i>_", $"03_aaa.txt")]
    [InlineData(@"Dir1\bbb.txt,Dir1\aaa.txt,Dir2\ccc.txt", "^", "$n<,,00,,i>_", $"02_bbb.txt")]
    [InlineData(@"Dir2\ccc.txt,Dir1\aaa.txt,Dir1\bbb.txt", "^", "$n<,,00,,i>_", $"01_ccc.txt")]
    [InlineData(@"Dir1\aaa.txt,Dir1\bbb.txt,Dir2\ccc.txt", "^", "$n<,,,r,i>_", $"2_aaa.txt")]
    [InlineData(@"Dir1\bbb.txt,Dir1\aaa.txt,Dir2\ccc.txt", "^", "$n<,,,r,i>_", $"1_bbb.txt")]
    [InlineData(@"Dir2\ccc.txt,Dir1\aaa.txt,Dir1\bbb.txt", "^", "$n<,,,r,i>_", $"1_ccc.txt")]
    [InlineData(@"Dir1\aaa.txt,Dir1\bbb.txt,Dir2\ccc.txt", "^", "$n<10,50,000,r,i>_", $"060_aaa.txt")]
    [InlineData(@"Dir1\bbb.txt,Dir1\aaa.txt,Dir2\ccc.txt", "^", "$n<10,50,000,r,i>_", $"010_bbb.txt")]
    [InlineData(@"Dir2\ccc.txt,Dir1\aaa.txt,Dir1\bbb.txt", "^", "$n<10,50,000,r,i>_", $"010_ccc.txt")]
    public void AddSerialNumber_Multi(string concatedTargetFilePaths, string regexPattern, string replaceText, string expectedRenamedFileName)
    {
        string[] targetFilePathTails = concatedTargetFilePaths
                    .Split(',');
        string targetFileName = Path.GetFileName(targetFilePathTails.First());
        string[] targetFilePaths = targetFilePathTails
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

    [Theory]
    [InlineData("abc.txt", "def", "$n_")]
    [InlineData("abc.txt", "def", "_$n")]
    public void AddSerialNumber_NotChange(string targetFileName, string regexPattern, string replaceText)
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
            .Should().Be(targetFileName);
    }
}
