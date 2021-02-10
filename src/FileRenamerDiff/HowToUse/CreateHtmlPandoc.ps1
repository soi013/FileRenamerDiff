# HowToUse htmlファイル生成スクリプト
# MarkdownからHTMLファイルを生成する
cd 'HowToUse'
$nameHeader = 'how_to_use'
# 各言語ごとのコード
$langCodes = @('','.de','.ru','.zh','.ja')

# 各言語ごとに1つのHTMLファイルができる
foreach($langCode in $langCodes)
{
    $sourcePath = ".\$nameHeader$langCode.md"
    $sourceTime = $(Get-ItemProperty $sourcePath).LastWriteTime
    $targetPath = "..\Resources\$nameHeader$langCode.html"
    if (Test-Path $targetPath)
    {
        $targetTime = $(Get-ItemProperty $targetPath).LastWriteTime
    }
    else
    {
        $targetTime = 0
    }

    echo "Creation $nameHeader$langCode.html: sourceTime is $sourceTime, targetTime is $targetTime"

    # 生成したHTMLファイルが元のMarkdownファイルより更新日時が新しいときのみコンバートする
    if ( $sourceTime -gt   $targetTime )
    {
        echo "Start Create $nameHeader$langCode.html"
        # Pandocを使用してMarkdownからHTMLファイルを生成する。cssなどを指定する
        & 'C:\Program Files\Pandoc\pandoc' -s ./$nameHeader$langCode.md -o ../Resources/$nameHeader$langCode.html --toc --template=./elegant_bootstrap_menu.html --self-contained -t html5 -c github.css
        echo "Finished Create $nameHeader$langCode.html"
    }
} 
