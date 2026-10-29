<#
.SYNOPSIS
Дописывает PublicAPI.Unshipped.txt NuGet-пакетов по предупреждениям RS0016/RS0017.

.DESCRIPTION
Код-фикс анализатора (и `dotnet format analyzers`) не трогает символы из сгенерированного кода —
Razor-компоненты и типы с [TypedEntityId]/[TypedStringValue]. Скрипт собирает пакет, берёт символы
из текста предупреждений и правит файлы сам:
- RS0016 (публичный символ не объявлен) — строка добавляется в PublicAPI.Unshipped.txt;
- RS0017 (объявленного символа нет) — строка убирается из Unshipped, а если она была в Shipped,
  в Unshipped добавляется `*REMOVED*<символ>`.

Без параметров обрабатывает все проекты с <IsPackable>true</IsPackable>. Порядок работы с
файлами — docs/nuget-packages.md.

.PARAMETER Project
Пути к .csproj. По умолчанию — все пакеты в src.
#>
param(
    [string[]] $Project
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not $Project) {
    $Project = Get-ChildItem -Path (Join-Path $repoRoot 'src') -Filter *.csproj -Recurse |
        Where-Object {
            $text = Get-Content $_.FullName -Raw
            # Генератор API не учитывает, см. Directory.Build.targets.
            $text.Contains('<IsPackable>true</IsPackable>') -and -not $text.Contains('<IsRoslynComponent>true</IsRoslynComponent>')
        } |
        ForEach-Object FullName
}

# Текст предупреждений нужен по-английски: по нему выделяется символ.
$env:DOTNET_CLI_UI_LANGUAGE = 'en'
# Без UTF-8 кириллица в значениях по умолчанию (`= "Ошибка…"`) приходит кракозябрами и не
# совпадает с символом анализатора.
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

function Get-QuotedSymbol([string] $line, [string] $prefix, [string] $suffix) {
    $start = $line.IndexOf($prefix)
    if ($start -lt 0) { return $null }
    $start += $prefix.Length
    $end = $line.IndexOf($suffix, $start)
    if ($end -lt 0) { return $null }
    return $line.Substring($start, $end - $start)
}

function Read-ApiFile([string] $path) {
    $list = [System.Collections.Generic.List[string]]::new()
    foreach ($line in [System.IO.File]::ReadAllLines($path)) {
        if ($line -ne '' -and $line -ne '#nullable enable') { $list.Add($line) }
    }
    # Запятая не даёт PowerShell развернуть пустой список в $null.
    return , $list
}

function Write-ApiFile([string] $path, [System.Collections.Generic.List[string]] $lines) {
    # Порядок ordinal: от культуры машины он не зависит, и дифф не шумит.
    $sorted = [System.Linq.Enumerable]::ToArray([System.Linq.Enumerable]::Distinct([string[]] $lines))
    [Array]::Sort($sorted, [StringComparer]::Ordinal)
    $content = (@('#nullable enable') + @($sorted)) -join "`r`n"
    [System.IO.File]::WriteAllText($path, $content + "`r`n", [System.Text.UTF8Encoding]::new($false))
}

foreach ($csproj in $Project) {
    $csprojPath = (Resolve-Path $csproj).Path
    $dir = Split-Path -Parent $csprojPath
    Write-Host "== $csprojPath"

    $shippedPath = Join-Path $dir 'PublicAPI.Shipped.txt'
    $unshippedPath = Join-Path $dir 'PublicAPI.Unshipped.txt'
    # Без `#nullable enable` анализатор выдаёт символы без аннотаций `!`/`?`.
    foreach ($path in $shippedPath, $unshippedPath) {
        if (-not (Test-Path $path)) {
            [System.IO.File]::WriteAllText($path, "#nullable enable`r`n", [System.Text.UTF8Encoding]::new($false))
        }
    }

    # ForceNoAlign: строки не переносятся по ширине консоли, путь к проекту в конце строки цел.
    $output = & dotnet build $csprojPath -c Release --no-incremental -tl:off '-clp:ForceNoAlign;NoSummary' -p:JoinPublicApiAsWarnings=true 2>&1
    if ($LASTEXITCODE -ne 0) {
        $output | Write-Host
        throw "Сборка $csprojPath упала"
    }

    $added = [System.Collections.Generic.HashSet[string]]::new()
    $removed = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($item in $output) {
        $line = "$item"
        # Предупреждения одного проекта; ссылки на другие проекты обрабатываются их собственным запуском.
        if (-not $line.Contains("[$csprojPath]")) { continue }
        if ($line.Contains('RS0016')) {
            $symbol = Get-QuotedSymbol $line "Symbol '" "' is not part of the declared public API"
            if ($symbol) { [void] $added.Add($symbol) }
        }
        elseif ($line.Contains('RS0017')) {
            $symbol = Get-QuotedSymbol $line "Symbol '" "' is part of the declared API"
            if ($symbol) { [void] $removed.Add($symbol) }
        }
    }

    $shippedLines = Read-ApiFile $shippedPath
    $unshippedLines = Read-ApiFile $unshippedPath

    foreach ($symbol in $removed) {
        if ($unshippedLines.Remove($symbol)) { continue }
        if ($shippedLines.Contains($symbol)) { $unshippedLines.Add("*REMOVED*$symbol") }
    }
    foreach ($symbol in $added) {
        # Символ вернули после удаления — снимаем отметку об удалении.
        if ($unshippedLines.Remove("*REMOVED*$symbol")) { continue }
        $unshippedLines.Add($symbol)
    }

    Write-ApiFile $shippedPath $shippedLines
    Write-ApiFile $unshippedPath $unshippedLines
    Write-Host "   добавлено $($added.Count), убрано $($removed.Count)"
}

Write-Host 'Проверить: dotnet build --no-incremental — инкрементальная сборка после скрипта может не показать RS0016.'
