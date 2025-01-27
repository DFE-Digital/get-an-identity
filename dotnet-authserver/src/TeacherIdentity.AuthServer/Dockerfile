# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:7.0
ARG GIT_SHA

COPY bin/Release/net7.0/publish/ App/

ENV GitSha ${GIT_SHA}
WORKDIR /App
ENTRYPOINT ["dotnet", "/App/TeacherIdentity.AuthServer.dll"]

EXPOSE 80
