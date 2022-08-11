
.DEFAULT_GOAL		:=help
SHELL				:=/bin/bash

.PHONY: help # default target
help: ## Show this help
	@grep -E '^[a-zA-Z\.\-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-30s\033[0m %s\n", $$1, $$2}'
    $(eval AZURE_BACKUP_STORAGE_ACCOUNT_NAME=false)
    $(eval AZURE_BACKUP_STORAGE_CONTAINER_NAME=false)

.PHONY: dev
dev:
	$(eval DEPLOY_ENV=dev)
	$(eval AZURE_SUBSCRIPTION=s165-teachingqualificationsservice-development)
	$(eval RESOURCE_NAME_PREFIX=s165d01)
	$(eval ENV_SHORT=dv)
	$(eval ENV_TAG=dev)

.PHONY: preprod
preprod:
	$(eval DEPLOY_ENV=preprod)
	$(eval AZURE_SUBSCRIPTION=s165-teachingqualificationsservice-test)
	$(eval RESOURCE_NAME_PREFIX=s165t01)
	$(eval ENV_SHORT=pp)
	$(eval ENV_TAG=pre-prod)

.PHONY: production
production:
	$(eval DEPLOY_ENV=production)
	$(eval AZURE_SUBSCRIPTION=s165-teachingqualificationsservice-production)
	$(eval RESOURCE_NAME_PREFIX=s165p01)
	$(eval ENV_SHORT=pd)
	$(eval ENV_TAG=prod)

set-azure-account: ${environment}
	echo "Logging on to ${AZURE_SUBSCRIPTION}"
	az account set -s ${AZURE_SUBSCRIPTION}

tags: ##Tags that will be added to resource group on it's creation in ARM template
	$(eval RG_TAGS=$(shell echo '{"Portfolio": "Early years and Schools Group", "Parent Business":"Teaching Regulation Agency", "Product" : "Get an Identity", "Service Line": "Teaching Workforce", "Service": "Teacher Services", "Service Offering": "Get an Identity", "Environment" : "$(ENV_TAG)"}' | jq . ))

deploy-azure-resources: set-azure-account tags # make dev deploy-azure-resources CONFIRM_DEPLOY=1
	$(if $(CONFIRM_DEPLOY), , $(error can only run with CONFIRM_DEPLOY))
	az deployment sub create -l "West Europe" --template-uri "https://raw.githubusercontent.com/DFE-Digital/tra-shared-services/main/azure/resourcedeploy.json" --parameters "resourceGroupName=${RESOURCE_NAME_PREFIX}-getanid-${ENV_SHORT}-rg" 'tags=${RG_TAGS}' "environment=${DEPLOY_ENV}" "tfStorageAccountName=${RESOURCE_NAME_PREFIX}getanidtfstate${ENV_SHORT}" "tfStorageContainerName=getanid-tfstate" "dbBackupStorageAccountName=${AZURE_BACKUP_STORAGE_ACCOUNT_NAME}" "dbBackupStorageContainerName=${AZURE_BACKUP_STORAGE_CONTAINER_NAME}" "keyVaultName=${RESOURCE_NAME_PREFIX}-getanid-${ENV_SHORT}-kv"

validate-azure-resources: set-azure-account  tags# make dev validate-azure-resources
	az deployment sub create -l "West Europe" --template-uri "https://raw.githubusercontent.com/DFE-Digital/tra-shared-services/main/azure/resourcedeploy.json" --parameters "resourceGroupName=${RESOURCE_NAME_PREFIX}-getanid-${ENV_SHORT}-rg" 'tags=${RG_TAGS}' "environment=${DEPLOY_ENV}" "tfStorageAccountName=${RESOURCE_NAME_PREFIX}getanidtfstate${ENV_SHORT}" "tfStorageContainerName=getanid-tfstate" "dbBackupStorageAccountName=${AZURE_BACKUP_STORAGE_ACCOUNT_NAME}" "dbBackupStorageContainerName=${AZURE_BACKUP_STORAGE_CONTAINER_NAME}" "keyVaultName=${RESOURCE_NAME_PREFIX}-getanid-${ENV_SHORT}-kv" --what-if
