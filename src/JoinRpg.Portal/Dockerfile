# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.8
WORKDIR /app
COPY $SOURCE_PUBLISH_DIR .
ENTRYPOINT ["dotnet", "JoinRpg.Portal.dll"]
