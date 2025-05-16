FROM mcr.microsoft.com/dotnet/aspnet:9.0.5

RUN groupadd user && \
    useradd -g user user --home-dir "/app" --create-home

USER user

WORKDIR /app

COPY --chown=user:user ./bin/Release/net9.0/publish/ .

ENTRYPOINT ["/app/Joinrpg.Dal.Migrate"]
