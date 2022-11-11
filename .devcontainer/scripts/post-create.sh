#!/usr/bin/env bash

set -euo pipefail

dotnet dev-certs https --trust

(cd dotnet-authserver && dotnet build)

(cd dotnet-authserver/src/TeacherIdentity.AuthServer && \
    dotnet ef database update)
