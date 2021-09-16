FROM mcr.microsoft.com/dotnet/sdk:5.0.202 AS build-env
WORKDIR /app

COPY . ./
RUN dotnet publish src/Joinrpg.Dal.Migrate -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0.10
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["/app/Joinrpg.Dal.Migrate"]

EXPOSE 80/tcp
EXPOSE 443/tcp
