#!/usr/bin/env bash

set -euo pipefail

proxied_authserver_domain="${CODESPACE_NAME}-7236.${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN}"

(cd dotnet-authserver/src/TeacherIdentity.TestClient && \
    DOTNET_URLS=https://localhost:7261 \
    OidcBackChannelAuthority="localhost:7236" \
    SignInAuthority=https://${proxied_authserver_domain} \
    dotnet watch)
