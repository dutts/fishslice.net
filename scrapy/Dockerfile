#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["scrapy/scrapy.csproj", "scrapy/"]
RUN dotnet restore "scrapy/scrapy.csproj"
COPY . .
WORKDIR "/src/scrapy"
RUN dotnet build "scrapy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "scrapy.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "scrapy.dll"]
