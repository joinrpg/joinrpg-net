<#
.SYNOPSIS
  Read-only доступ к API Selectel от сервисного пользователя (роль reader).

.DESCRIPTION
  Сам выпускает IAM-токен из пользовательских переменных окружения
  SELECTEL_ACCOUNT_ID / SELECTEL_USERNAME / SELECTEL_PASSWORD и делает один GET.
  Токен и пароль никуда не выводит и не сохраняет. Ходит только на хосты Selectel.

  Запускать из корня репозитория:
  pwsh -NoProfile -File .claude/skills/selectel-readonly/selectel-api.ps1 <параметры>

.EXAMPLE
  selectel-api.ps1 -Catalog                         # сервисы и их URL по регионам
  selectel-api.ps1 -Service managed-kubernetes -Path clusters     # GET во всех регионах сервиса
  selectel-api.ps1 -Service managed-kubernetes -Region ru-9 -Path clusters
  selectel-api.ps1 -Url https://cloud.api.selcloud.ru/identity/v3/auth/projects
  selectel-api.ps1 -Scope account -Url ...          # токен на аккаунт вместо проекта
#>
[CmdletBinding(DefaultParameterSetName = 'Service')]
param(
    [Parameter(ParameterSetName = 'Catalog', Mandatory)][switch]$Catalog,
    [Parameter(ParameterSetName = 'Service', Mandatory)][string]$Service,
    [Parameter(ParameterSetName = 'Service')][string]$Region,
    [Parameter(ParameterSetName = 'Service')][string]$Path = '',
    [Parameter(ParameterSetName = 'Url', Mandatory)][string]$Url,
    [ValidateSet('project', 'account')][string]$Scope = 'project',
    [string]$Project = 'JoinRpgCloud'
)

$ErrorActionPreference = 'Stop'

function Get-Var([string]$name) {
    $v = [Environment]::GetEnvironmentVariable($name, 'User')
    if (-not $v) { throw "Не задана пользовательская переменная окружения $name" }
    $v
}

function Test-SelectelHost([string]$u) {
    $h = ([Uri]$u).Host
    if (([Uri]$u).Scheme -ne 'https') { return $false }
    foreach ($suffix in @('selcloud.ru', 'selectel.ru', 'servercore.com')) {
        if ($h -eq $suffix -or $h.EndsWith(".$suffix")) { return $true }
    }
    $false
}

$acc = Get-Var 'SELECTEL_ACCOUNT_ID'
$user = @{ name = (Get-Var 'SELECTEL_USERNAME'); domain = @{ name = $acc }; password = (Get-Var 'SELECTEL_PASSWORD') }
$scopeBody = if ($Scope -eq 'account') { @{ domain = @{ name = $acc } } }
             else { @{ project = @{ name = $Project; domain = @{ name = $acc } } } }
$body = @{ auth = @{ identity = @{ methods = @('password'); password = @{ user = $user } }; scope = $scopeBody } } |
    ConvertTo-Json -Depth 10

$auth = Invoke-WebRequest -Uri 'https://cloud.api.selcloud.ru/identity/v3/auth/tokens' -Method Post `
    -ContentType 'application/json' -Body $body -UseBasicParsing
$token = @($auth.Headers['X-Subject-Token'])[0]
$serviceCatalog = ($auth.Content | ConvertFrom-Json).token.catalog
$headers = @{ 'X-Auth-Token' = $token }

function Invoke-Get([string]$u) {
    if (-not (Test-SelectelHost $u)) { throw "Отказ: $u — не хост Selectel" }
    try {
        Invoke-RestMethod -Uri $u -Headers $headers -Method Get | ConvertTo-Json -Depth 30
    } catch {
        $status = $_.Exception.Response.StatusCode
        "ERROR $([int]$status) $status — $u"
        if ($_.ErrorDetails.Message) { $_.ErrorDetails.Message }
    }
}

switch ($PSCmdlet.ParameterSetName) {
    'Catalog' {
        $serviceCatalog | ForEach-Object {
            $type = $_.type
            $_.endpoints | Where-Object interface -eq 'public' |
                ForEach-Object { "{0}`t{1}`t{2}" -f $type, $_.region, $_.url }
        } | Sort-Object
    }
    'Url' { Invoke-Get $Url }
    'Service' {
        $svc = $serviceCatalog | Where-Object type -eq $Service
        if (-not $svc) { throw "Сервиса '$Service' нет в каталоге. Список: -Catalog" }
        $endpoints = $svc.endpoints | Where-Object interface -eq 'public'
        if ($Region) { $endpoints = $endpoints | Where-Object region -eq $Region }
        foreach ($e in $endpoints) {
            "### $Service $($e.region)"
            # Путь пишется без ведущего '/', иначе Git Bash превращает его в путь Windows
            $suffix = if ($Path) { '/' + $Path.TrimStart('/') } else { '' }
            Invoke-Get ($e.url.TrimEnd('/') + $suffix)
        }
    }
}
