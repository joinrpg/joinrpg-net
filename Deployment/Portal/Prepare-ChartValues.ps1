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

$secrets | ?{ $_.Name.StartsWith($Prefix) } |
%{
    $name = $_.Name

    $s = Get-AzKeyVaultSecret -VaultName $VaultName -Name $name -WarningAction SilentlyContinue
    $value = ConvertFrom-SecureString -SecureString $s.SecretValue -AsPlainText

    $name = $name.Substring($Prefix.Length).Replace("--","__");

    $data.Add($name, $value);
}

"secrets:" | Out-File -FilePath $Path -Encoding utf8 -Force -Append

$data.Keys | %{ "  $($_): `"$($data[$_])`"" } | Out-File -FilePath $Path -Encoding utf8 -Append

"ingress:" | Out-File -FilePath $Path -Encoding utf8 -Append
"  host: $IngressHost" | Out-File -FilePath $Path -Encoding utf8 -Append
"  path: $IngressPath" | Out-File -FilePath $Path -Encoding utf8 -Append