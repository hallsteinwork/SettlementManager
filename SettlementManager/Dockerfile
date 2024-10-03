# Use the official .NET SDK image for the build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["SettlementManager.csproj", "./"]
RUN dotnet restore "SettlementManager.csproj"

# Copy the rest of the code
COPY . .

# Build the application
WORKDIR "/src/SettlementManager"
RUN dotnet build "SettlementManager.csproj" -c Release -o /app/build

# Use the official ASP.NET image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "SettlementManager.dll"]
