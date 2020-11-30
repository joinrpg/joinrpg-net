param(
    [Parameter(Mandatory=$true)]
    [string] $VaultName,

    [Parameter(Mandatory=$false)]
    [string] $Prefix = "",

    [Parameter(Mandatory=$false)]
    [string] $SecretName = "joinrpg-secrets",

    [Parameter(Mandatory=$false)]
    [string] $Namespace = "default",

    [Parameter(Mandatory=$false)]
    [string] $Path = "join-secrets.yaml"
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

$fileHead = @"
apiVersion: v1
kind: Secret
metadata:
  name: $SecretName
  namespace: $Namespace 
type: Opaque
stringData:
"@

$fileHead | Out-File -FilePath $Path -Encoding utf8 -Force

$data.Keys | %{ "  $($_): `"$($data[$_])`"" } | Out-File -FilePath $Path -Encoding utf8 -Append
