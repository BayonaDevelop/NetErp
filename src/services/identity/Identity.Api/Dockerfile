# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/services/identity/Identity.Api/Identity.Api.csproj", "src/services/identity/Identity.Api/"]
COPY ["src/services/identity/Identity.Infrastructure/Identity.Infrastructure.csproj", "src/services/identity/Identity.Infrastructure/"]
COPY ["src/services/identity/Identity.Application/Identity.Application.csproj", "src/services/identity/Identity.Application/"]
COPY ["src/Commons/Commons.csproj", "src/Commons/"]
COPY ["src/services/identity/Identity.Core/Identity.Core.csproj", "src/services/identity/Identity.Core/"]
RUN dotnet restore "./src/services/identity/Identity.Api/Identity.Api.csproj"
COPY . .
WORKDIR "/src/src/services/identity/Identity.Api"
RUN dotnet build "./Identity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Identity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Identity.Api.dll"]