FROM mcr.microsoft.com/dotnet/aspnet:8.0.6

RUN groupadd user && useradd -g user user --home-dir "/app" --create-home

USER user

WORKDIR /app

COPY --chown=user:user ./bin/Release/net8.0/publish/ .

ENV ASPNETCORE_URLS="http://+:8080"

EXPOSE 8080/tcp

ENTRYPOINT ["/app/JoinRpg.Portal"]
