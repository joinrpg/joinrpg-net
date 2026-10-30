<#
.SYNOPSIS
Переносит записи «Не выпущено» CHANGELOG.md пакетов в раздел опубликованной версии.

.DESCRIPTION
Запускается из nuget-publish.yml после публикации, в рабочей копии свежего master. Записи берутся
из CHANGELOG на коммите, с которого публиковали (-PublishedRef): то, что влили в master позже,
в пакет не попало и остаётся в «Не выпущено». В master эти записи убираются из «Не выпущено»,
а под ним появляется раздел `## <версия> — <дата>`. Если у пакета не было записей — раздел с
«Видимых изменений нет.»: пакеты выходят одной версией.

Запись, исправленная или удалённая в master после публикации, выводится предупреждением: в
разделе версии остаётся опубликованный текст, а правку надо разобрать руками.

.PARAMETER Version
Опубликованная версия.

.PARAMETER PublishedRef
Коммит, с которого публиковали.
#>
param(
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string] $PublishedRef,
    [string] $Date = (Get-Date -Format 'yyyy-MM-dd')
)

$ErrorActionPreference = 'Stop'
# Коды git проверяются вручную: «файла нет на коммите» — не ошибка. Вызывающий (nuget-publish.yml)
# включает этот флаг, а скрипт наследует его.
$PSNativeCommandUseErrorActionPreference = $false
$repoRoot = Split-Path -Parent $PSScriptRoot
$unreleasedHeader = '## Не выпущено'
# Вывод git читается в кодировке консоли; без UTF-8 кириллица заголовков не совпадёт.
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

& git -C $repoRoot rev-parse --verify --quiet "${PublishedRef}^{commit}" > $null
if ($LASTEXITCODE -ne 0) { throw "Нет коммита $PublishedRef" }

# Раздел «Не выпущено»: подраздел → записи в исходном порядке. Всё, что не подраздел и не
# запись, — ошибка: при перезаписи раздела такая строка потерялась бы.
function Read-Unreleased([string[]] $lines, [string] $relative) {
    $sections = [ordered]@{}
    $inUnreleased = $false
    $current = $null
    foreach ($line in $lines) {
        if ($line.StartsWith('## ')) {
            $inUnreleased = $line -eq $unreleasedHeader
            continue
        }
        if (-not $inUnreleased -or $line.Trim() -eq '') { continue }
        if ($line.StartsWith('### ')) {
            $current = $line
            if (-not $sections.Contains($current)) { $sections[$current] = [System.Collections.Generic.List[string]]::new() }
        }
        elseif ($line.StartsWith('- ') -and $current) {
            $sections[$current].Add($line)
        }
        else {
            throw "${relative}: в «Не выпущено» строка не подраздел и не запись (продолжение записи? подвал под последним разделом?): $line"
        }
    }
    return $sections
}

$changelogs = Get-ChildItem -Path (Join-Path $repoRoot 'src') -Filter CHANGELOG.md -Recurse
foreach ($file in $changelogs) {
    $relative = [System.IO.Path]::GetRelativePath($repoRoot, $file.FullName).Replace('\', '/')
    $lines = [System.IO.File]::ReadAllLines($file.FullName)
    if ($lines | Where-Object { $_ -eq "## $Version" -or $_.StartsWith("## $Version ") }) {
        Write-Host "${relative}: $Version уже проставлена, пропускаю"
        continue
    }

    $publishedText = & git -C $repoRoot show "${PublishedRef}:$relative" 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "${relative}: нет на $PublishedRef, пропускаю"
        continue
    }
    $published = Read-Unreleased @($publishedText) $relative
    $released = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($entries in $published.Values) { foreach ($e in $entries) { [void] $released.Add($e) } }

    $remaining = Read-Unreleased $lines $relative
    $inMaster = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($entries in $remaining.Values) { foreach ($e in $entries) { [void] $inMaster.Add($e) } }
    foreach ($e in $released) {
        if (-not $inMaster.Contains($e)) {
            Write-Host "::warning file=$relative::Запись исправлена или удалена в master после публикации: $e"
        }
    }

    $result = [System.Collections.Generic.List[string]]::new()
    $skip = $false
    foreach ($line in $lines) {
        if ($line.StartsWith('## ')) {
            $skip = $false
            if ($line -eq $unreleasedHeader) {
                $result.Add($line)
                $result.Add('')
                foreach ($section in $remaining.Keys) {
                    $left = @($remaining[$section] | Where-Object { -not $released.Contains($_) })
                    if ($left.Count -eq 0) { continue }
                    $result.Add($section)
                    foreach ($e in $left) { $result.Add($e) }
                    $result.Add('')
                }
                $result.Add("## $Version — $Date")
                $result.Add('')
                if ($released.Count -eq 0) {
                    $result.Add('Видимых изменений нет.')
                    $result.Add('')
                }
                else {
                    foreach ($section in $published.Keys) {
                        if ($published[$section].Count -eq 0) { continue }
                        $result.Add($section)
                        foreach ($e in $published[$section]) { $result.Add($e) }
                        $result.Add('')
                    }
                }
                $skip = $true
                continue
            }
        }
        if ($skip) { continue }
        $result.Add($line)
    }

    $text = ($result -join "`r`n").TrimEnd() + "`r`n"
    [System.IO.File]::WriteAllText($file.FullName, $text, [System.Text.UTF8Encoding]::new($false))
    Write-Host "${relative}: $Version, записей: $($released.Count)"
}
