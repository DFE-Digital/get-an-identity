# syntax=docker/dockerfile:1
  FROM mcr.microsoft.com/dotnet/aspnet:8.0
  COPY bin/Release/net8.0/publish/ App/
  WORKDIR /App
  ENTRYPOINT ["dotnet", "TeacherIdentity.TestClient.dll"]
  EXPOSE 80
