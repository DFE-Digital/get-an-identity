# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0
ARG GIT_SHA

COPY bin/Release/net8.0/publish/ App/

ENV GitSha ${GIT_SHA}
WORKDIR /App
ENTRYPOINT ["dotnet", "/App/TeacherIdentity.AuthServer.dll"]

EXPOSE 80
