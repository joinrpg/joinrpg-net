FROM mcr.microsoft.com/dotnet/aspnet:6.0.6

#We need this to make ClosedXML work
RUN apt-get update && apt-get install -y apt-utils libgdiplus libc6-dev

RUN groupadd user && \
    useradd -g user user --home-dir "/app" --create-home

USER user

WORKDIR /app

COPY --chown=user:user ./bin/Release/net6/publish/ .

ENV ASPNETCORE_URLS="http://+:8080"

EXPOSE 8080/tcp

ENTRYPOINT ["/app/JoinRpg.Portal"]
