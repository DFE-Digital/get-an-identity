resource "azurerm_storage_account" "data-protection" {
  name                              = var.data_protection_storage_account_name
  location                          = data.azurerm_resource_group.group.location
  resource_group_name               = data.azurerm_resource_group.group.name
  account_replication_type          = var.environment_name != "production" ? "LRS" : "GRS"
  account_tier                      = "Standard"
  account_kind                      = "StorageV2"
  min_tls_version                   = "TLS1_2"
  infrastructure_encryption_enabled = true

  blob_properties {
    last_access_time_enabled = true

    container_delete_retention_policy {
      days = var.data_protection_container_delete_retention_days
    }
  }

  lifecycle {
    ignore_changes = [
      tags
    ]
  }
}


resource "azurerm_storage_encryption_scope" "forms-encryption" {
  name                               = "microsoftmanaged"
  storage_account_id                 = azurerm_storage_account.data-protection.id
  source                             = "Microsoft.Storage"
  infrastructure_encryption_required = true
}


resource "azurerm_storage_container" "uploads" {
  name                  = "uploads"
  storage_account_name  = azurerm_storage_account.data-protection.name
  container_access_type = "private"
}
