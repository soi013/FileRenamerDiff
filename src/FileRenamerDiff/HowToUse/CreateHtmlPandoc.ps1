# HowToUse htmlファイル生成スクリプト
# MarkdownからHTMLファイルを生成する
cd 'HowToUse'
$nameHeader = 'how_to_use'
# 各言語ごとのコード
$langCodes = @('', '.de', '.ru', '.zh', '.ja')

function GetCommandPath {
    $commandName = 'pandoc'

    $commandPathSpare1 = 'C:\Users\user\AppData\Local\Pandoc\pandoc.exe'
    $commandPathSpare2 = 'C:\Program Files\Pandoc\pandoc.exe'

    if (Get-Command $commandName -ea SilentlyContinue) {
        return $commandName
    }
    elseif (Test-Path $commandPathSpare1) {
        return $commandPathSpare1
    }
    elseif (Test-Path $commandPathSpare2) {
        return $commandPathSpare2
    }
    else {
        echo "WARNING cannnot found command [$commandName]."
        Exit 1
    }
}

$commandPath = ''

# 各言語ごとに1つのHTMLファイルができる
foreach ($langCode in $langCodes) {
    $sourcePath = ".\$nameHeader$langCode.md"
    $sourceTime = $(Get-ItemProperty $sourcePath).LastWriteTime
    $targetPath = "..\Resources\$nameHeader$langCode.html"
    if (Test-Path $targetPath) {
        $targetTime = $(Get-ItemProperty $targetPath).LastWriteTime
    }
    else {
        $targetTime = 0
    }

    echo "Creation $nameHeader$langCode.html: sourceTime is $sourceTime, targetTime is $targetTime"

    # 生成したHTMLファイルが元のMarkdownファイルより更新日時が新しいときのみコンバートする
    if ( $sourceTime -gt $targetTime ) {
        echo "Start Create $nameHeader$langCode.html"

        if (!$commandPath) {
            $commandPath = GetCommandPath
        } 
        
        echo "pandoc path is $commandPath"
        # Pandocを使用してMarkdownからHTMLファイルを生成する。cssなどを指定する
        & $commandPath -s ./$nameHeader$langCode.md -o ../Resources/$nameHeader$langCode.html --toc --template=./elegant_bootstrap_menu.html --self-contained -t html5 -c my_markdown.css
        echo "Finished Create $nameHeader$langCode.html"
    }
} 
