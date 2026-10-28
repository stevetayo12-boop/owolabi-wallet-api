FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OwolabiVentures.API/OwolabiVentures.API.csproj", "OwolabiVentures.API/"]
RUN dotnet restore "OwolabiVentures.API/OwolabiVentures.API.csproj"
COPY . .
WORKDIR "/src/OwolabiVentures.API"
RUN dotnet publish "OwolabiVentures.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OwolabiVentures.API.dll"]