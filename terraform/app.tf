resource "azurerm_service_plan" "service-plan" {
  name                = local.app_service_plan_name
  location            = data.azurerm_resource_group.group.location
  resource_group_name = data.azurerm_resource_group.group.name
  os_type             = "Linux"
  sku_name            = var.app_service_plan_sku

  lifecycle {
    ignore_changes = [
      tags
    ]
  }
}

resource "azurerm_linux_web_app" "auth-server-app" {
  name                = local.get_an_identity_app_name
  location            = data.azurerm_resource_group.group.location
  resource_group_name = data.azurerm_resource_group.group.name
  service_plan_id     = azurerm_service_plan.service-plan.id
  https_only          = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    http2_enabled       = true
    minimum_tls_version = "1.2"
    application_stack {
      docker_image     = var.docker_image
      docker_image_tag = var.authserver_tag
    }
    health_check_path = "/health"
  }

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

  lifecycle {
    ignore_changes = [
      tags
    ]
  }
}

resource "azurerm_linux_web_app" "test-server-app" {
  count               = var.deploy_test_server_app ? 1 : 0
  name                = local.get_an_identity_test_client_name
  location            = data.azurerm_resource_group.group.location
  resource_group_name = data.azurerm_resource_group.group.name
  service_plan_id     = azurerm_service_plan.service-plan.id
  https_only          = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    http2_enabled       = true
    minimum_tls_version = "1.2"
    application_stack {
      docker_image     = var.docker_image
      docker_image_tag = var.testclient_tag
    }
  }

  app_settings = {
    HOSTING_ENVIRONMENT                    = local.hosting_environment,
    APPLICATION_INSIGHTS_CONNECTION_STRING = azurerm_application_insights.insights.connection_string
    ConnectionStrings__DefaultConnection   = "Server=${local.postgres_server_name}.postgres.database.azure.com;User Id=${local.infrastructure_secrets.POSTGRES_ADMIN_USERNAME};Password=${local.infrastructure_secrets.POSTGRES_ADMIN_PASSWORD};Database=${local.postgres_database_name};Port=5432;Trust Server Certificate=true;"
    REDIS_URL                              = "${azurerm_redis_cache.redis.hostname}/${azurerm_redis_cache.redis.primary_access_key}",
    DOCKER_REGISTRY_SERVER_URL             = "https://ghcr.io",
    EncryptionKey                          = local.infrastructure_secrets.ENCRYPTION_KEY1,
    SigningKey                             = local.infrastructure_secrets.SIGNING_KEY1,
    NotifyApiKey                           = local.infrastructure_secrets.NOTIFY_API_KEY,
    AdminCredentials__Username             = local.infrastructure_secrets.ADMIN_CREDENTIALS_USERNAME,
    AdminCredentials__Password             = local.infrastructure_secrets.ADMIN_CREDENTIALS_PASSWORD
  }

  lifecycle {
    ignore_changes = [
      tags
    ]
  }
}

resource "azurerm_postgresql_flexible_server" "postgres-server" {
  name                   = local.postgres_server_name
  location               = data.azurerm_resource_group.group.location
  resource_group_name    = data.azurerm_resource_group.group.name
  version                = 12
  administrator_login    = local.infrastructure_secrets.POSTGRES_ADMIN_USERNAME
  administrator_password = local.infrastructure_secrets.POSTGRES_ADMIN_PASSWORD
  zone                   = 1
  create_mode            = "Default"
  storage_mb             = var.postgres_flexible_server_storage_mb
  sku_name               = var.postgres_flexible_server_sku

  lifecycle {
    ignore_changes = [
      tags
    ]
  }
}

resource "azurerm_postgresql_flexible_server_database" "postgres-database" {
  name      = local.postgres_database_name
  server_id = azurerm_postgresql_flexible_server.postgres-server.id
}

resource "azurerm_redis_cache" "redis" {
  name                = local.redis_database_name
  location            = data.azurerm_resource_group.group.location
  resource_group_name = data.azurerm_resource_group.group.name
  capacity            = var.redis_service_capacity
  family              = var.redis_service_family
  sku_name            = var.redis_service_sku
  enable_non_ssl_port = false
  minimum_tls_version = "1.2"
  redis_version       = var.redis_service_version

  lifecycle {
    ignore_changes = [
      tags
    ]
  }

}

resource "azurerm_log_analytics_workspace" "analytics" {
  name                = local.log_analytics_workspace_name
  location            = data.azurerm_resource_group.group.location
  resource_group_name = data.azurerm_resource_group.group.name
  sku                 = var.log_analytics_sku
  retention_in_days   = local.storage_log_retention_days

  lifecycle {
    ignore_changes = [
      tags
    ]
  }
}

resource "azurerm_application_insights" "insights" {
  name                 = local.app_insights_name
  location             = data.azurerm_resource_group.group.location
  resource_group_name  = data.azurerm_resource_group.group.name
  application_type     = "web"
  daily_data_cap_in_gb = var.application_insights_daily_data_cap_mb
  retention_in_days    = var.application_insights_retention_days

  lifecycle {
    ignore_changes = [
      tags
    ]
  }
}
