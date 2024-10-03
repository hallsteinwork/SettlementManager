# Use the ASP.NET base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the project file and restore dependencies

COPY ["SettlementManager.csproj", "./"]
RUN dotnet restore "SettlementManager.csproj"

WORKDIR "/src/SettlementManager"
# Copy the entire source code
COPY . .

# Build the application
WORKDIR "/src/SettlementManager"
RUN dotnet build "SettlementManager.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SettlementManager.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage to create the runtime image
FROM base AS final
WORKDIR /app

# Copy the published application from the publish stage
COPY --from=publish /app/publish .

# Set the entry point for the application
ENTRYPOINT ["dotnet", "SettlementManager.dll"]
