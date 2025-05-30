FROM mcr.microsoft.com/dotnet/runtime:9.0-bookworm-slim AS base
ENV DEBIAN_FRONTEND=noninteractive
RUN apt update -qq
RUN apt install -qq -y chromium
RUN rm -rf /var/lib/apt/lists/*
RUN rm -rf /src/*.deb
RUN apt clean

FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
COPY src/*/*.csproj ./

RUN for file in $(ls *.csproj); do \
      mkdir -p src/${file%.*}/ && \
      mv $file src/${file%.*}/; \
    done
RUN dotnet restore ./src/Crawling/Crawling.csproj --use-current-runtime

FROM build AS publish
COPY ./src/ ./src/
RUN dotnet publish ./src/Crawling/Crawling.csproj \
    -c Release \
    --no-restore \
    -p:PublishSingleFile=true \
    --self-contained false \
    -o /app

FROM base AS final
WORKDIR /app
COPY ./appsettings.local.json .
COPY --from=publish /app .

ENTRYPOINT ["./crawling"]