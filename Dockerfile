# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.7
WORKDIR /app
COPY ./JoinRpg.Portal/bin/Release/netcoreapp3.1/publish .
ENTRYPOINT ["dotnet", "JoinRpg.Portal.dll"]
