#Requires -Version 7.0
<#
.SYNOPSIS
    Стриггерить concurrency rate-limit анонимных запросов (#6232) до первого 429.

.DESCRIPTION
    Лимитер — глобальный concurrency-лимитер анонимных запросов (см.
    AnonymousRateLimiterExtensions в JoinRpg.Common.WebInfrastructure):
    лимит AnonymousRateLimit:PermitLimit одновременных запросов на под
    (по умолчанию 10), очередь 0, сверх лимита — сразу 429.
    Последовательные запросы его не стриггерят, поэтому скрипт шлёт волны
    параллельных анонимных GET-запросов (без кук, без авторизации), пока
    не придёт первый 429. Авторизованные пользователи лимитером не режутся.

.PARAMETER Uri
    Анонимно доступный URL для нагрузки. По умолчанию главная страница dev.

.PARAMETER Concurrency
    Сколько запросов запускается одновременно в одной волне.

.PARAMETER MaxWaves
    Максимум волн до признания провала (лимитер не сработал).

.PARAMETER RequestTimeoutSec
    Таймаут одного запроса.

.EXAMPLE
    ./Test-RateLimit.ps1
    ./Test-RateLimit.ps1 -Uri 'https://dev.joinrpg.ru/' -Concurrency 40
#>
[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$Uri = 'https://dev.joinrpg.ru/',

    [ValidateRange(1, 1000)]
    [int]$Concurrency = 60,

    [ValidateRange(1, 1000)]
    [int]$MaxWaves = 30,

    [ValidateRange(1, 300)]
    [int]$RequestTimeoutSec = 30
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)

$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.UseCookies = $false
$client = [System.Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds($RequestTimeoutSec)
$client.DefaultRequestHeaders.UserAgent.ParseAdd('JoinRpg-RateLimit-Test/1.0')

$found429 = $false
$wave = 0
$totalRequests = 0
$elapsed = [System.Diagnostics.Stopwatch]::StartNew()

try {
    while (-not $found429 -and $wave -lt $MaxWaves) {
        $wave++
        Write-Host ("Волна {0}: {1} параллельных запросов к {2} ..." -f $wave, $Concurrency, $Uri)

        $tasks = @()
        for ($i = 0; $i -lt $Concurrency; $i++) {
            $tasks += $client.GetAsync($Uri)
        }
        try {
            $responses = [System.Threading.Tasks.Task]::WhenAll(
                [System.Threading.Tasks.Task[System.Net.Http.HttpResponseMessage][]]$tasks
            ).GetAwaiter().GetResult()
        } catch {
            foreach ($task in $tasks) {
                if ($task.IsCompletedSuccessfully) { $task.Result.Dispose() }
            }
            Write-Host ("  -> ОШИБКА волны: {0}" -f $_.Exception.ToString()) -ForegroundColor Red
            throw
        }

        $statusCounts = @{}
        foreach ($response in $responses) {
            $totalRequests++
            $code = [int]$response.StatusCode
            if ($statusCounts.ContainsKey($code)) {
                $statusCounts[$code]++
            } else {
                $statusCounts[$code] = 1
            }
            if ($code -eq 429 -and -not $found429) {
                $found429 = $true
                # Диагностика 429: лимитер отдаёт голый статус, но pipeline перезапускает
                # ответ на страницу /error/429 (UseStatusCodePagesWithReExecute) — смотрим,
                # что именно пришло, чтобы отличить наш лимитер от 429 CDN/WAF.
                $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                Write-Host ("  -> Первый 429: Content-Type: {0}, длина тела: {1}" -f `
                        $response.Content.Headers.ContentType, $body.Length)
                Write-Host ("     Начало тела: {0}" -f ($body.Substring(0, [Math]::Min(200, $body.Length)) -replace "`r?`n", ' '))
            }
            $response.Dispose()
        }

        $summary = ($statusCounts.GetEnumerator() | Sort-Object Name | ForEach-Object {
            "{0} × {1}" -f $_.Value, $_.Name
        }) -join ', '
        Write-Host ("  -> {0}" -f $summary)

        if (-not $found429) {
            Start-Sleep -Milliseconds 200
        }
    }

    Write-Host ''
    if ($found429) {
        Write-Host ("УСПЕХ: лимитер сработал — первый 429 получен на волне {0}, всего запросов {1}, за {2} сек." -f `
                $wave, $totalRequests, [int]$elapsed.Elapsed.TotalSeconds) -ForegroundColor Green
        Write-Host "Окончательное подтверждение — предупреждения ""Анонимный запрос ... отклонён лимитером"" в логах пода." -ForegroundColor Green
        exit 0
    } else {
        Write-Host ("НЕ СРАБОТАЛО: за {0} волн ({1} запросов, {2} сек.) ни одного 429." -f `
                $wave, $totalRequests, [int]$elapsed.Elapsed.TotalSeconds) -ForegroundColor Red
        Write-Host "Проверь: задеплоен ли код с лимитером (#6232) на цель, верный ли Uri, не мешает ли кеш/CDN." -ForegroundColor Red
        Write-Host "Бакет лимитера — per-pod: если реплик несколько, балансировщик мог раскидать волну так," -ForegroundColor Red
        Write-Host "что на каждом поде оказалось меньше лимита одновременных запросов. Подними -Concurrency." -ForegroundColor Red
        exit 1
    }
} finally {
    $client.Dispose()
}
