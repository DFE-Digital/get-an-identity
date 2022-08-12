output "get_an_identity_fqdn" {
  value = "https://${azurerm_linux_web_app.auth-server-app.default_hostname}"
}
