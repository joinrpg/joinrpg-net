<#
.SYNOPSIS
Переносит опубликованные строки PublicAPI.Unshipped.txt в PublicAPI.Shipped.txt.

.DESCRIPTION
Запускается из nuget-publish.yml после публикации, в рабочей копии свежего master, после
Stamp-Changelog.ps1. Строки берутся из Unshipped на коммите, с которого публиковали
(-PublishedRef): добавленное в master позже не опубликовано и остаётся в Unshipped.
Со строками опубликованного Unshipped:
- обычная строка — в Shipped, из Unshipped убирается;
- `*REMOVED*<символ>` — символ убирается из Shipped, сама строка — из Unshipped; если в master
  отметку уже сняли (символ вернули), символ остаётся в Unshipped как новый;
- строка, которой в master уже нет в Unshipped (символ убрали или изменили после публикации), —
  в Shipped, а в Unshipped — `*REMOVED*<строка>`.
Подавления опубликованного коммита больше не нужны: CompatibilitySuppressions.xml удаляется,
если в master его не меняли. Повторный запуск ничего не меняет.

.PARAMETER PublishedRef
Коммит, с которого публиковали.
#>
param(
    [Parameter(Mandatory)] [string] $PublishedRef
)

$ErrorActionPreference = 'Stop'
# Коды git проверяются вручную: «файла нет на коммите» — не ошибка. nuget-publish.yml включает
# этот флаг, скрипт его наследует — выключаем.
$PSNativeCommandUseErrorActionPreference = $false
$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'PublicApiFile.ps1')
# Вывод git читается в кодировке консоли; без UTF-8 кириллица в значениях по умолчанию не совпадёт.
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

& git -C $repoRoot rev-parse --verify --quiet "${PublishedRef}^{commit}" > $null
if ($LASTEXITCODE -ne 0) { throw "Нет коммита $PublishedRef" }

function Get-Published([string] $relative) {
    $text = & git -C $repoRoot show "${PublishedRef}:$relative" 2>$null
    if ($LASTEXITCODE -ne 0) { return $null }
    return , @($text)
}

foreach ($file in Get-ChildItem -Path (Join-Path $repoRoot 'src') -Filter PublicAPI.Unshipped.txt -Recurse) {
    $dir = $file.DirectoryName
    $relative = [System.IO.Path]::GetRelativePath($repoRoot, $file.FullName).Replace('\', '/')
    $publishedLines = Get-Published $relative
    if ($null -eq $publishedLines) {
        Write-Host "${relative}: нет на $PublishedRef, пропускаю"
        continue
    }

    $shippedPath = Join-Path $dir 'PublicAPI.Shipped.txt'
    $shipped = Read-ApiFile $shippedPath
    $unshipped = Read-ApiFile $file.FullName
    $moved = 0
    foreach ($line in $publishedLines) {
        if ($line -eq '' -or $line -eq '#nullable enable') { continue }
        if ($line.StartsWith('*REMOVED*')) {
            $symbol = $line.Substring('*REMOVED*'.Length)
            $removedFromShipped = $shipped.Remove($symbol)
            if ($unshipped.Remove($line)) { $moved++; continue }
            # Отметку сняли в master после публикации: символ вернули, а в опубликованной версии его нет.
            if ($removedFromShipped) { $unshipped.Add($symbol); $moved++ }
            continue
        }
        # Повторный запуск: строка уже перенесена.
        if ($shipped.Contains($line)) {
            [void] $unshipped.Remove($line)
            continue
        }
        $shipped.Add($line)
        if (-not $unshipped.Remove($line)) {
            Write-Host "::notice file=$relative::Символ убран или изменён в master после публикации, в Unshipped добавлен *REMOVED*: $line"
            $unshipped.Add("*REMOVED*$line")
        }
        $moved++
    }
    Write-ApiFile $shippedPath $shipped
    Write-ApiFile $file.FullName $unshipped
    Write-Host "${relative}: перенесено $moved"

    $suppressions = Join-Path $dir 'CompatibilitySuppressions.xml'
    if (-not (Test-Path $suppressions)) { continue }
    $suppressionsRelative = [System.IO.Path]::GetRelativePath($repoRoot, $suppressions).Replace('\', '/')
    $published = Get-Published $suppressionsRelative
    if ($null -eq $published) { continue }
    $current = [System.IO.File]::ReadAllLines($suppressions)
    # Файл от GenerateCompatibilitySuppressionFile бывает с BOM: ReadAllLines его снимает, git show — нет.
    if ((@($published) -join "`n").TrimStart([char] 0xFEFF) -ceq ($current -join "`n")) {
        Remove-Item $suppressions
        Write-Host "${suppressionsRelative}: удалён"
    }
    else {
        Write-Host "::warning file=$suppressionsRelative::Изменён после публикации, оставлен — устаревшие подавления убрать руками"
    }
}
