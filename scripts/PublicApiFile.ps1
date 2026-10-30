# Чтение и запись PublicAPI.*.txt — общие для Update-PublicApi.ps1 и Ship-PublicApi.ps1.

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
