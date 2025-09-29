# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# .csproj faylni aniq joyidan copy qilamiz
COPY UserManagmentSystem.Web/UserManagmentSystem.Web.csproj UserManagmentSystem.Web/

# Restore
RUN dotnet restore UserManagmentSystem.Web/UserManagmentSystem.Web.csproj

# Boshqa hamma fayllarni copy qilamiz
COPY . .

# Publish
WORKDIR /src/UserManagmentSystem.Web
RUN dotnet publish -c Release -o /app

# 2. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "UserManagmentSystem.Web.dll"]
