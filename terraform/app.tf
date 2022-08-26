locals {
  auth_server_clients_app_env_vars = merge([
    for i, v in local.infrastructure_secrets.CLIENTS : merge({
      "Clients__${i}__ClientId"     = v.CLIENT_ID,
      "Clients__${i}__ClientSecret" = v.CLIENT_SECRET,
      "Clients__${i}__DisplayName"  = v.DISPLAY_NAME,
      "Clients__${i}__ServiceUrl"   = v.SERVICE_URL
      }, merge([
        for k, x in v.REDIRECT_URIS : {
          "Clients__${i}__RedirectUris__${k}" = x
        }
    ]...))
  ]...)

  auth_server_api_clients_app_env_vars = merge([
    for i, v in local.infrastructure_secrets.API_CLIENTS : merge({
      "ApiClients__${i}__ClientId" = v.CLIENT_ID
      }, merge([
        for k, x in v.API_KEYS : {
          "ApiClients__${i}__ApiKeys__${k}" = x
        }
    ]...))
  ]...)

  auth_server_env_vars = merge(
    local.auth_server_clients_app_env_vars,
    local.auth_server_api_clients_app_env_vars,
    {
      EnvironmentName                              = local.hosting_environment,
      ApplicationInsights__ConnectionString        = azurerm_application_insights.insights.connection_string
      ConnectionStrings__DefaultConnection         = "Server=${local.postgres_server_name}.postgres.database.azure.com;User Id=${local.infrastructure_secrets.POSTGRES_ADMIN_USERNAME};Password=${local.infrastructure_secrets.POSTGRES_ADMIN_PASSWORD};Database=${local.postgres_database_name};Port=5432;Trust Server Certificate=true;"
      ConnectionStrings__Redis                     = azurerm_redis_cache.redis.primary_connection_string,
      ConnectionStrings__DataProtectionBlobStorage = "DefaultEndpointsProtocol=https;AccountName=${azurerm_storage_account.data-protection.name};AccountKey=${azurerm_storage_account.data-protection.primary_access_key}"
      DataProtectionKeysContainerName              = azurerm_storage_container.keys.name,
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
    }
  )

  test_client_env_vars = {
    ClientId        = "testclient",
    ClientSecret    = local.infrastructure_secrets.TESTCLIENT_SECRET,
    SignInAuthority = "https://${module.auth_server_container_app.container_app_fqdn}"
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

resource "azapi_resource" "container-apps-environment" {
  type      = "Microsoft.App/managedEnvironments@2022-03-01"
  parent_id = data.azurerm_resource_group.group.id
  location  = data.azurerm_resource_group.group.location
  name      = local.container_apps_environment_name

  body = jsonencode({
    properties = {
      appLogsConfiguration = {
        destination = "log-analytics"
        logAnalyticsConfiguration = {
          customerId = azurerm_log_analytics_workspace.analytics.workspace_id
          sharedKey  = azurerm_log_analytics_workspace.analytics.primary_shared_key
        }
      }
    }
  })

  lifecycle {
    ignore_changes = [
      tags
    ]
  }
}

module "auth_server_container_app" {
  source = "./modules/container-app"

  app_definition_yaml = <<EOT
kind: containerapp
location: ${data.azurerm_resource_group.group.location}
name: ${local.auth_server_app_name}
resourcegroup: "${data.azurerm_resource_group.group.name}"
type: Microsoft.App/containerApps
properties:
  managedEnvironmentId: ${azapi_resource.container-apps-environment.id}
  configuration:
    activeRevisionsMode: "Multiple"
    ingress:
      external: true
      targetPort: 80
      allowInsecure: false
      traffic:
        - latestRevision: true
          weight: 100
    secrets:
      - name: "ghcr-password"
        value: ${local.infrastructure_secrets.GHCR_PASSWORD}
    registries:
      - server: "ghcr.io"
        username: local.infrastructure_secrets.GHCR_USERNAME
        passwordSecretRef: "ghcr-password"
  template:
    containers:
      - name: "main"
        image: "${var.docker_image}:${var.authserver_tag}"
        env:
          ${indent(10, yamlencode([for k, v in local.auth_server_env_vars : {
  name  = k,
  value = v
}]))}
        resources:
          cpu: 0.5
          memory: 1Gi
        probes:
          - type: "Startup"
            tcpSocket:
              port: 80
  scale:
    minReplicas: 1
    maxReplicas: 1
EOT
}

module "test_client_container_app" {
  source = "./modules/container-app"

  app_definition_yaml = <<EOT
kind: containerapp
location: ${data.azurerm_resource_group.group.location}
name: ${local.test_client_app_name}
resourcegroup: "${data.azurerm_resource_group.group.name}"
type: Microsoft.App/containerApps
properties:
  managedEnvironmentId: ${azapi_resource.container-apps-environment.id}
  configuration:
    activeRevisionsMode: "Multiple"
    ingress:
      external: true
      targetPort: 80
      allowInsecure: false
      traffic:
        - latestRevision: true
          weight: 100
    secrets:
      - name: "ghcr-password"
        value: ${local.infrastructure_secrets.GHCR_PASSWORD}
    registries:
      - server: "ghcr.io"
        username: local.infrastructure_secrets.GHCR_USERNAME
        passwordSecretRef: "ghcr-password"
  template:
    containers:
      - name: "main"
        image: "${var.docker_image}:${var.testclient_tag}"
        env:
          ${indent(10, yamlencode([for k, v in local.test_client_env_vars : {
  name  = k,
  value = v
}]))}
        resources:
          cpu: 0.5
          memory: 1Gi
        probes:
          - type: "Startup"
            tcpSocket:
              port: 80
  scale:
    minReplicas: 1
    maxReplicas: 1
EOT
}