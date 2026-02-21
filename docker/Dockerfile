# ── Stage 1: Publish Blazor WASM frontend ────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS frontend-build
WORKDIR /src
COPY frontend/ frontend/
RUN dotnet publish frontend/RatingApp.Client/RatingApp.Client.csproj \
    -c Release -o /app/frontend-out

# ── Stage 2: Publish ASP.NET Core backend (with Blazor files embedded) ────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src
COPY backend/ backend/
# Inject the Blazor publish output into the API's wwwroot before publishing
COPY --from=frontend-build /app/frontend-out/wwwroot ./backend/src/RatingApp.Api/wwwroot/
RUN dotnet publish backend/src/RatingApp.Api/RatingApp.Api.csproj \
    -c Release -o /app/publish

# ── Stage 3: Runtime image ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=backend-build /app/publish .
ENTRYPOINT ["dotnet", "RatingApp.Api.dll"]