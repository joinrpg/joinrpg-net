param(
    [Parameter(Mandatory=$true)]
    [string] $VaultName,

    [Parameter(Mandatory=$false)]
    [string] $Prefix = "",

    [Parameter(Mandatory=$true)]
    [string] $Path,

    [Parameter(Mandatory=$true)]
    [string] $IngressHost,

    [Parameter(Mandatory=$false)]
    [string] $IngressPath = "/"
)

$data = @{}

$secrets = Get-AzKeyVaultSecret -VaultName $VaultName -WarningAction SilentlyContinue

Write-Host "Read secrets from KeyVault: $VaultName, prefix: $Prefix"

$secrets | ?{ $_.Name.StartsWith($Prefix) } |
%{
    $name = $_.Name

    $s = Get-AzKeyVaultSecret -VaultName $VaultName -Name $name -WarningAction SilentlyContinue
    $value = ConvertFrom-SecureString -SecureString $s.SecretValue -AsPlainText

    $name = $name.Substring($Prefix.Length).Replace("--","__");

    $data.Add($name, $value);

    Write-Host "  Found Secret: $name"
}

"secrets:" | Out-File -FilePath $Path -Encoding utf8 -Force -Append

$data.Keys | %{ "  $($_): `"$($data[$_])`"" } | Out-File -FilePath $Path -Encoding utf8 -Append

Write-Host "Set ingress hostname: $IngressHost"
Write-Host "Set ingress path: $IngressPath"

"ingress:" | Out-File -FilePath $Path -Encoding utf8 -Append
"  host: $IngressHost" | Out-File -FilePath $Path -Encoding utf8 -Append
"  path: $IngressPath" | Out-File -FilePath $Path -Encoding utf8 -Append