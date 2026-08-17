FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

EXPOSE 8080


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy project files first so Docker can cache package restore
COPY ["LedgerGuard.Api/LedgerGuard.Api.csproj", "LedgerGuard.Api/"]
COPY ["LedgerGuard.Application/LedgerGuard.Application.csproj", "LedgerGuard.Application/"]
COPY ["LedgerGuard.Domain/LedgerGuard.Domain.csproj", "LedgerGuard.Domain/"]
COPY ["LedgerGuard.Infrastructure/LedgerGuard.Infrastructure.csproj", "LedgerGuard.Infrastructure/"]

RUN dotnet restore "LedgerGuard.Api/LedgerGuard.Api.csproj"

# Copy source code
COPY . .

WORKDIR "/src/LedgerGuard.Api"

RUN dotnet build \
    "LedgerGuard.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    --no-restore


FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish \
    "LedgerGuard.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore


FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "LedgerGuard.Api.dll"]