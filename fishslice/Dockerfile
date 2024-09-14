FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ARG TARGETARCH
WORKDIR /app

COPY ./fishslice/install-powershell.sh /scripts/install-powershell.sh
RUN chmod +x /scripts/install-powershell.sh
RUN /scripts/install-powershell.sh

RUN    apt-get update \
    && apt-get install -y wget \
    && rm -rf /var/lib/apt/lists/* 

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["fishslice/fishslice.csproj", "fishslice/"]
RUN dotnet restore "fishslice/fishslice.csproj" -a ${TARGETARCH}
COPY . .
WORKDIR "/src/fishslice"
RUN dotnet build "fishslice.csproj" -c Release -a ${TARGETARCH} -o /app/build

WORKDIR /src_playwright
RUN dotnet new console --no-restore
RUN dotnet add package Microsoft.Playwright
RUN dotnet build --property:PlaywrightPlatform=all

FROM base AS playwright-install
COPY --from=build /src_playwright/bin/Debug/net8.0/ /src/
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
RUN mkdir /ms-playwright
RUN ["pwsh", "/src/playwright.ps1", "install", "--with-deps", "chromium"]
RUN rm -rf /src/*

FROM build AS publish
WORKDIR "/src/fishslice"
RUN dotnet publish "fishslice.csproj" -c Release -a $TARGETARCH -o /app/publish

FROM playwright-install AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "fishslice.dll"]
