variable "environment_name" {
  type = string
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

locals {
  get_an_identity_app_name         = "get-an-identity-${var.environment_name}${var.app_suffix}-auth-server"
  get_an_identity_test_client_name = "get-an-identity-${var.environment_name}${var.app_suffix}-test-client"
  postgres_server_name             = "get-an-identity-${var.environment_name}${var.app_suffix}-pg-svr"
  postgres_database_name           = "get-an-identity-${var.environment_name}${var.app_suffix}-pg-database"
  redis_database_name              = "get-an-identity-${var.environment_name}${var.app_suffix}-redis-svc"
  app_insights_name                = "get-an-identity-${var.environment_name}${var.app_suffix}-app-ins-svc"
}
