FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build-env
WORKDIR /app

COPY . ./
RUN dotnet publish src/JoinRpg.Portal -c Release -o out -p:langversion=latest -p:GenerateDocumentationFile=true

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "JoinRpg.Portal.dll"]

EXPOSE 80/tcp
EXPOSE 443/tcp
