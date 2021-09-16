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
    [string] $IngressPath = "/",

    [Parameter(Mandatory=$false)]
    [string] $ConnectionStringSecretName = "ConnectionStrings--DefaultConnection"
)

##=================================================================================
## Script for preparing values.yaml for deployment chart
## Source: Azure Key Vault (secrets) and parameters (host & path)
##=================================================================================

$data = @{}

$secrets = Get-AzKeyVaultSecret -VaultName $VaultName -WarningAction SilentlyContinue

Write-Host "Read secrets from KeyVault: $VaultName, prefix: $Prefix"

$secrets | ?{ $_.Name.StartsWith($Prefix) } |
%{
    $name = $_.Name

    $s = Get-AzKeyVaultSecret -VaultName $VaultName -Name $name -WarningAction SilentlyContinue
    $value = ConvertFrom-SecureString -SecureString $s.SecretValue -AsPlainText

    #Save DB Connection String as variable for next pipeline steps
    if( $name -eq ($Prefix + $ConnectionStringSecretName) )
    {
        Write-Host "Found the database connections string secret with name $name, save to pipeline variable 'DefaultConnection'"   
        Write-Host "##vso[task.setvariable variable=DefaultConnection;issecret=true]$value"        
    }
    
    #Save to the secrets array
    $name = $name.Substring($Prefix.Length).Replace("--","__");

    $data.Add($name, $value);

    Write-Host "  Found Secret: $name"
}

#Write found secrets to the values file

"secrets:" | Out-File -FilePath $Path -Encoding utf8 -Force -Append

$data.Keys | %{ "  $($_): `"$($data[$_])`"" } | Out-File -FilePath $Path -Encoding utf8 -Append

Write-Host "Set ingress hostname: $IngressHost"
Write-Host "Set ingress path: $IngressPath"

"ingress:" | Out-File -FilePath $Path -Encoding utf8 -Append
"  host: $IngressHost" | Out-File -FilePath $Path -Encoding utf8 -Append
"  path: $IngressPath" | Out-File -FilePath $Path -Encoding utf8 -Append
