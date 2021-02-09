cd 'HowToUse'

$sourceTime = $(Get-ItemProperty .\how_to_use.md).LastWriteTime
$targetTime = $(Get-ItemProperty ..\Resources\how_to_use.html).LastWriteTime

echo "Creation how_to_use: sourceTime is $sourceTime, targetTime is $targetTime"

if ( $sourceTime -gt   $targetTime )
{
    echo "Start Create how_to_use.html"
    & 'C:\Program Files\Pandoc\pandoc' -s ./how_to_use.md -o ../Resources/how_to_use.html --toc --template=./elegant_bootstrap_menu.html --self-contained -t html5 -c github.css
    echo "Finished Create how_to_use.html"
}

