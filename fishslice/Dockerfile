FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ARG TARGETARCH
WORKDIR /app

COPY ./fishslice/install-powershell.sh /scripts/install-powershell.sh
RUN chmod +x /scripts/install-powershell.sh
RUN /scripts/install-powershell.sh

RUN    apt-get update \
    && apt-get install -y wget \
    && rm -rf /var/lib/apt/lists/* 

FROM --platform=$BUILDPLATFORM  mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY ["fishslice/fishslice.csproj", "fishslice/"]
RUN dotnet restore "fishslice/fishslice.csproj" --arch $TARGETARCH
COPY . .
WORKDIR "/src/fishslice"
RUN dotnet build "fishslice.csproj" -c Release -o /app/build --arch $TARGETARCH

WORKDIR /src_playwright
RUN dotnet new console --no-restore
RUN dotnet add package Microsoft.Playwright
RUN dotnet build --property:PlaywrightPlatform=all

FROM --platform=$BUILDPLATFORM base AS playwright-install
ARG TARGETARCH
COPY --from=build /src_playwright/bin/Debug/net8.0/ /src/
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
RUN mkdir /ms-playwright
RUN ["pwsh", "/src/playwright.ps1", "install", "--with-deps", "chromium"]
RUN rm -rf /src/*

FROM --platform=$BUILDPLATFORM build AS publish
ARG TARGETARCH
WORKDIR "/src/fishslice"
RUN dotnet publish "fishslice.csproj" -c Release -o /app/publish --arch $TARGETARCH

FROM playwright-install AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "fishslice.dll"]
