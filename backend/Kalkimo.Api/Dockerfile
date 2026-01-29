FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Kalkimo.sln .
COPY Kalkimo.Domain/Kalkimo.Domain.csproj Kalkimo.Domain/
COPY Kalkimo.Api/Kalkimo.Api.csproj Kalkimo.Api/
COPY Kalkimo.Domain.Tests/Kalkimo.Domain.Tests.csproj Kalkimo.Domain.Tests/
COPY Kalkimo.Api.Tests/Kalkimo.Api.Tests.csproj Kalkimo.Api.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build
WORKDIR /src/Kalkimo.Api
RUN dotnet build -c Release -o /app/build

# Publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Kalkimo.Api.dll"]
