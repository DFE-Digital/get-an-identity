variable "environment_name" {
  type = string
}
variable "resource_prefix" {
  type    = string
  default = ""
}
variable "app_suffix" {
  type    = string
  default = ""
}
variable "azure_sp_credentials_json" {
  type    = string
  default = null
}

variable "app_service_plan_sku" {
  type    = string
  default = "B1"
}

variable "postgres_flexible_server_sku" {
  type    = string
  default = "B_Standard_B1ms"
}

variable "postgres_flexible_server_storage_mb" {
  type    = number
  default = 32768
}

variable "key_vault_name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "redis_service_sku" {
  type    = string
  default = "Basic"
}

variable "redis_service_version" {
  type    = number
  default = 6
}


variable "redis_service_family" {
  type    = string
  default = "C"
}

variable "redis_service_capacity" {
  type    = number
  default = 1
}

variable "application_insights_daily_data_cap_mb" {
  type    = string
  default = "0.033"
}

variable "application_insights_retention_days" {
  type    = number
  default = 30
}

variable "data_protection_container_delete_retention_days" {
  default = 7
  type    = number
}

variable "data_protection_storage_account_name" {
  default = null
}

variable "deploy_test_server_app" {
  type    = bool
  default = false
}

variable "keyvault_logging_enabled" {
  type    = bool
  default = false
}

variable "storage_diagnostics_logging_enabled" {
  type    = bool
  default = false
}

variable "storage_log_categories" {
  type    = list(string)
  default = []
}

variable "log_analytics_sku" {
  type    = string
  default = "PerGB2018"
}

variable "docker_image" {
  type    = string
  default = "ghcr.io/dfe-digital/get-an-identity"
}

variable "authserver_tag" {
  type = string
}
variable "testclient_tag" {
  type = string
}


locals {
  hosting_environment              = var.environment_name
  app_service_plan_name            = "${var.resource_prefix}getanid-${var.environment_name}-plan"
  get_an_identity_app_name         = "${var.resource_prefix}getanid-${var.environment_name}${var.app_suffix}-auth-server-app"
  get_an_identity_test_client_name = "${var.resource_prefix}getanid-${var.environment_name}${var.app_suffix}-test-client-app"
  postgres_server_name             = "${var.resource_prefix}getanid-${var.environment_name}${var.app_suffix}-psql"
  postgres_database_name           = "${var.resource_prefix}getanid-${var.environment_name}${var.app_suffix}-psql-db"
  redis_database_name              = "${var.resource_prefix}getanid-${var.environment_name}${var.app_suffix}-redis"
  app_insights_name                = "${var.resource_prefix}getanid-${var.environment_name}${var.app_suffix}-appi"
  log_analytics_workspace_name     = "${var.resource_prefix}getanid-${var.environment_name}-log"

  keyvault_logging_enabled            = var.keyvault_logging_enabled
  storage_diagnostics_logging_enabled = length(var.storage_log_categories) > 0
  storage_log_categories              = var.storage_log_categories
  storage_log_retention_days          = var.environment_name == "prd" ? 365 : 30
  app_settings = {
    EnvironmentName                              = local.hosting_environment,
    ApplicationInsights__ConnectionString        = azurerm_application_insights.insights.connection_string
    ConnectionStrings__DefaultConnection         = "Server=${local.postgres_server_name}.postgres.database.azure.com;User Id=${local.infrastructure_secrets.POSTGRES_ADMIN_USERNAME};Password=${local.infrastructure_secrets.POSTGRES_ADMIN_PASSWORD};Database=${local.postgres_database_name};Port=5432;Trust Server Certificate=true;"
    ConnectionStrings__Redis                     = azurerm_redis_cache.redis.primary_connection_string,
    ConnectionStrings__DataProtectionBlobStorage = "DefaultEndpointsProtocol=https;AccountName=${azurerm_storage_account.data-protection.name};AccountKey=${azurerm_storage_account.data-protection.primary_access_key}"
    DataProtectionKeysContainerName              = azurerm_storage_container.keys.name,
    DOCKER_REGISTRY_SERVER_URL                   = "https://ghcr.io",
    EncryptionKey                                = local.infrastructure_secrets.ENCRYPTION_KEY1,
    SigningKey                                   = local.infrastructure_secrets.SIGNING_KEY1,
    NotifyApiKey                                 = local.infrastructure_secrets.NOTIFY_API_KEY,
    AdminCredentials__Username                   = local.infrastructure_secrets.ADMIN_CREDENTIALS_USERNAME,
    AdminCredentials__Password                   = local.infrastructure_secrets.ADMIN_CREDENTIALS_PASSWORD,
    Sentry__Dsn                                  = local.infrastructure_secrets.SENTRY_DSN,
    FindALostTrnIntegration__HandoverEndpoint    = "/FindALostTrn/Identity",
    FindALostTrnIntegration__EnableStubEndpoints = "true",
    FindALostTrnIntegration__SharedKey           = local.infrastructure_secrets.FIND_SHARED_KEY,
    DqtApi__ApiKey                               = local.infrastructure_secrets.DQT_API_KEY,
    DqtApi__BaseAddress                          = local.infrastructure_secrets.DQT_API_BASE_ADDRESS,
    ApiClients__Find__ApiKeys__0                 = local.infrastructure_secrets.API_CLIENTS_FIND_KEY
  }
}
