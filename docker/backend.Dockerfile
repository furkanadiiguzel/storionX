# Backend multi-stage Dockerfile — builds Migration.Api and Migration.MockStorionX

ARG DOTNET_VERSION=10.0

# ── Stage: restore ─────────────────────────────────────────────────────────────
# Separate restore stage so NuGet packages are cached as a layer.
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS restore
WORKDIR /src

COPY EvStorionx.sln .
COPY Directory.Build.props .

# Copy only project files first to maximise cache reuse on restore.
COPY src/Migration.Api/Migration.Api.csproj                   src/Migration.Api/
COPY src/Migration.MockStorionX/Migration.MockStorionX.csproj src/Migration.MockStorionX/

RUN dotnet restore EvStorionx.sln

# ── Stage: build ───────────────────────────────────────────────────────────────
FROM restore AS build
ARG PROJECT_PATH

COPY src/ src/

RUN dotnet publish "${PROJECT_PATH}" \
    -c Release \
    --no-restore \
    -o /out

# ── Stage: tools ───────────────────────────────────────────────────────────────
# Keep SDK available for dotnet-ef migrations during development.
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS tools
WORKDIR /src
COPY --from=restore /src .
COPY src/ src/
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"
CMD ["bash"]

# ── Stage: api ─────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS api
WORKDIR /app
COPY --from=build /out .
USER app
ENTRYPOINT ["dotnet", "Migration.Api.dll"]

# ── Stage: mockstorionx ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS mockstorionx
WORKDIR /app
COPY --from=build /out .
USER app
ENTRYPOINT ["dotnet", "Migration.MockStorionX.dll"]
