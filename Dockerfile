# Backend Bolletta Analyzer — immagine per hosting container (Render, Fly.io, Railway, Azure).
# Build:  docker build -t bolletta-api .
# Run:    docker run -p 8080:8080 -e Jwt__Key=... -e Encryption__Key=... bolletta-api

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/Backend/ ./src/Backend/
RUN dotnet publish src/Backend/BollettaAnalyzer.Api/BollettaAnalyzer.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .

# La piattaforma di hosting termina TLS davanti al container: qui basta HTTP interno.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BollettaAnalyzer.Api.dll"]
