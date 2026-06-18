FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MHStore.sln ./
COPY MHStore.API/MHStore.API.csproj MHStore.API/
COPY MHStore.Services/MHStore.Services.csproj MHStore.Services/
COPY MHStore.Repositories/MHStore.Repositories.csproj MHStore.Repositories/
RUN dotnet restore MHStore.sln

COPY . .
RUN dotnet publish MHStore.API/MHStore.API.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MHStore.API.dll"]
