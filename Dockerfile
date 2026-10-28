FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for better caching
COPY OwolabiVentures.sln ./
COPY OwolabiVentures.API/*.csproj ./OwolabiVentures.API/
COPY OwolabiVentures.POS/*.csproj ./OwolabiVentures.POS/
RUN dotnet restore

# Copy everything else and publish ONLY the API
COPY . .
RUN dotnet publish OwolabiVentures.API/OwolabiVentures.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "OwolabiVentures.API.dll"]
