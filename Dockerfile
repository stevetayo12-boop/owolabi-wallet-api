FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .

# Find the API project no matter where it is and publish it
RUN API_PATH=$(find . -name "OwolabiVentures.API.csproj" | head -n 1) && \
    echo "Found API at: $API_PATH" && \
    dotnet restore $API_PATH && \
    dotnet publish $API_PATH -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "OwolabiVentures.API.dll"]
