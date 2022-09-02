output "auth_server_fqdn" {
  value = azurerm_linux_web_app.auth-server-app.default_hostname
}
