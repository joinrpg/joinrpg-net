FROM mcr.microsoft.com/dotnet/aspnet:8.0.4

RUN groupadd user && \
    useradd -g user user --home-dir "/app" --create-home

USER user

WORKDIR /app

COPY --chown=user:user ./bin/Release/net8.0/publish/ .

ENTRYPOINT ["/app/Joinrpg.Dal.Migrate"]
