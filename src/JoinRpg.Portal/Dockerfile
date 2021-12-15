FROM mcr.microsoft.com/dotnet/sdk:6.0.100 AS build-env
WORKDIR /app

COPY . ./
#We have to build separedly to workaround https://github.com/aspnet/LibraryManager/issues/643
RUN dotnet build src/JoinRpg.WebComponents --configuration Release --output out
RUN dotnet publish src/JoinRpg.Portal --configuration Release --output out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0.1
WORKDIR /app
COPY --from=build-env /app/out .

#We need this to ClosedXML to work
RUN apt-get update && apt-get install -y apt-utils libgdiplus libc6-dev
ENTRYPOINT ["dotnet", "JoinRpg.Portal.dll"]

EXPOSE 80/tcp
EXPOSE 443/tcp
