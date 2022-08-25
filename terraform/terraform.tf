terraform {
  required_version = "~> 1.0"

  backend "azurerm" {
    container_name = "getanid-tfstate"
  }

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.17.0"
    }

    azapi = {
      source  = "Azure/azapi"
      version = "0.5.0"
    }
  }
}