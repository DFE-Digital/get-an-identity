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

  app_settings = local.app_settings

  lifecycle {
    ignore_changes = [
      tags
    ]
  }
}

resource "azurerm_linux_web_app_slot" "auth-server-stage" {
  count          = var.environment_name != "dev" ? 1 : 0
  name           = "staging"
  app_service_id = azurerm_linux_web_app.auth-server-app.id
  site_config {
    http2_enabled       = true
    minimum_tls_version = "1.2"
    application_stack {
      docker_image     = var.docker_image
      docker_image_tag = var.authserver_tag
    }
  }
  app_settings = local.app_settings
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
      docker_image_tag = var.authserver_tag
    }
  }

  app_settings = local.app_settings
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
