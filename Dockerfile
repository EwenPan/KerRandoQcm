# Étape 1 : Build de l'application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copie du csproj et restauration des dépendances NuGet
COPY ["KerRandoQcm.csproj", "./"]
RUN dotnet restore "KerRandoQcm.csproj"

# Copie du reste des sources et compilation Release
COPY . .
RUN dotnet publish "KerRandoQcm.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Étape 2 : Image d'exécution légère ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "KerRandoQcm.dll"]
