# ---------- Etapa 1: compilar y publicar ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo el .csproj para aprovechar la caché de Docker al restaurar paquetes
COPY FerreteriaApp.csproj ./
RUN dotnet restore

# Después el resto del código
COPY . ./
RUN dotnet publish FerreteriaApp.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- Etapa 2: imagen final (solo el runtime, más liviana) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

# Render inyecta la variable PORT; Program.cs la lee y escucha en ese puerto.
# Si no existe (por ejemplo corriendo el contenedor a mano), se usa 8080.
ENV PORT=8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "FerreteriaApp.dll"]
