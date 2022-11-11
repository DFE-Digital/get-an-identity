# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:7.0

RUN export DEBIAN_FRONTEND=noninteractive && \
    curl -fsSL https://deb.nodesource.com/setup_18.x | bash - && \
    apt-get install -y nodejs postgresql-client

RUN npm install -g sass

RUN dotnet tool install --global dotnet-ef