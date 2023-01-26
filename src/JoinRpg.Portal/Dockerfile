FROM mcr.microsoft.com/dotnet/aspnet:7.0.2

#We need this to make ClosedXML work
RUN apt-get update && apt-get install -y fonts-liberation && rm -rf /var/lib/apt/lists/*

RUN groupadd user && useradd -g user user --home-dir "/app" --create-home

USER user

WORKDIR /app

COPY --chown=user:user ./bin/Release/net7.0/publish/ .

ENV ASPNETCORE_URLS="http://+:8080"

EXPOSE 8080/tcp

ENTRYPOINT ["/app/JoinRpg.Portal"]
