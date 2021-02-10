FROM mcr.microsoft.com/dotnet/sdk:5.0.101 AS build-env
WORKDIR /app

COPY . ./
RUN dotnet publish src/JoinRpg.Portal -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0.3
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "JoinRpg.Portal.dll"]

EXPOSE 80/tcp
EXPOSE 443/tcp
