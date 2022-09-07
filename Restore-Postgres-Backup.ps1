[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [String]$BackupStorageAccountName,
    [Parameter(Mandatory = $true)]
    [String]$BackupStorageContainerName,
    [Parameter(Mandatory = $true)]
    [String]$VaultName,
    [Parameter(Mandatory = $true)]
    [String]$Subscription,
    [Parameter(Mandatory = $true)]
    [String]$SecretName,
    [Parameter(Mandatory = $true)]
    [String]$BackupFileName,
    [Parameter(Mandatory = $true)]
    [String]$PostgresDatabaseName,
    [Parameter(Mandatory = $true)]
    [String]$PostgresServerName,
    [Parameter(Mandatory = $true)]
    [String]$ConfirmRestore
)

$ErrorActionPreference = "Stop"


If (-not(Get-InstalledModule powershell-yaml -ErrorAction silentlycontinue)) {
    Install-Module powershell-yaml -Confirm:$False -Force -Scope CurrentUser -AllowClobber
}

If (-not(Get-InstalledModule Az.Accounts -ErrorAction silentlycontinue)) {
    Install-Module Az.Accounts -Confirm:$False -Force -Scope CurrentUser -AllowClobber
}

If (-not(Get-InstalledModule Az.KeyVault -ErrorAction silentlycontinue)) {
    Install-Module Az.KeyVault -Confirm:$False -Force -Scope CurrentUser -AllowClobber
}

Connect-AzAccount

Select-AzSubscription -Subscription $Subscription

#Download backup file and unzip it. These sh scripts might need execute permissions. so run  chmod +x ./bin/download-db-backup ./bin/restore-db
./bin/download-db-backup $BackupStorageAccountName $BackupStorageContainerName "$BackupFileName.sql.gz"

$secret = Get-AzKeyVaultSecret -VaultName $VaultName -Name $SecretName -AsPlainText | ConvertFrom-Yaml

#Restore backup into azure postgres instance
./bin/restore-db "$ConfirmRestore" $PostgresServerName $secret.POSTGRES_ADMIN_USERNAME $PostgresDatabaseName $secret.POSTGRES_ADMIN_PASSWORD "$BackupFileName.sql"
