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
COPY ["src/services/address/Address.Api/Address.Api.csproj", "src/services/address/Address.Api/"]
COPY ["src/services/address/Address.Infrastructure/Address.Infrastructure.csproj", "src/services/address/Address.Infrastructure/"]
COPY ["src/services/address/Address.Application/Address.Application.csproj", "src/services/address/Address.Application/"]
COPY ["src/Commons/Commons.csproj", "src/Commons/"]
COPY ["src/services/address/Address.Core/Address.Core.csproj", "src/services/address/Address.Core/"]
RUN dotnet restore "./src/services/address/Address.Api/Address.Api.csproj"
COPY . .
WORKDIR "/src/src/services/address/Address.Api"
RUN dotnet build "./Address.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Address.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Address.Api.dll"]