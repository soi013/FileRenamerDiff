cd 'HowToUse'

$nameHeader = 'how_to_use'
$langCodes = @('','.de','.ru','.zh','.ja')

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

    if ( $sourceTime -gt   $targetTime )
    {
        echo "Start Create $nameHeader$langCode.html"
        & 'C:\Program Files\Pandoc\pandoc' -s ./$nameHeader$langCode.md -o ../Resources/$nameHeader$langCode.html --toc --template=./elegant_bootstrap_menu.html --self-contained -t html5 -c github.css
        echo "Finished Create $nameHeader$langCode.html"
    }
} 
