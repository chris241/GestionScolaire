FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/GestionScolaire.Domain/GestionScolaire.Domain.csproj src/GestionScolaire.Domain/
COPY src/GestionScolaire.Application/GestionScolaire.Application.csproj src/GestionScolaire.Application/
COPY src/GestionScolaire.Infrastructure/GestionScolaire.Infrastructure.csproj src/GestionScolaire.Infrastructure/
COPY src/GestionScolaire.Api/GestionScolaire.Api.csproj src/GestionScolaire.Api/
RUN dotnet restore src/GestionScolaire.Api/GestionScolaire.Api.csproj

COPY src/ src/
RUN dotnet publish src/GestionScolaire.Api/GestionScolaire.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# libfontconfig1: required by QuestPDF/SkiaSharp to resolve glyphs when rendering the bulletin PDF.
# curl: used by the docker-compose healthcheck.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GestionScolaire.Api.dll"]
