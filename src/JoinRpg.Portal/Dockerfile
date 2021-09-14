FROM mcr.microsoft.com/dotnet/sdk:5.0.202 AS build-env
WORKDIR /app

COPY . ./
RUN dotnet publish src/JoinRpg.Portal -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .

#We need this to ClosedXML to work
RUN apt-get update && apt-get install -y apt-utils libgdiplus libc6-dev
ENTRYPOINT ["dotnet", "JoinRpg.Portal.dll"]

EXPOSE 80/tcp
EXPOSE 443/tcp
