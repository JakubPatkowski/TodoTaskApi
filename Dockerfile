FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/TodoTaskAPI.API/TodoTaskAPI.API.csproj", "src/TodoTaskAPI.API/"]
COPY ["src/TodoTaskAPI.Core/TodoTaskAPI.Core.csproj", "src/TodoTaskAPI.Core/"]
COPY ["src/TodoTaskAPI.Infrastructure/TodoTaskAPI.Infrastructure.csproj", "src/TodoTaskAPI.Infrastructure/"]
COPY ["src/TodoTaskAPI.Application/TodoTaskAPI.Application.csproj", "src/TodoTaskAPI.Application/"]
RUN dotnet restore "src/TodoTaskAPI.API/TodoTaskAPI.API.csproj"
COPY . .
RUN dotnet build "src/TodoTaskAPI.API/TodoTaskAPI.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/TodoTaskAPI.API/TodoTaskAPI.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TodoTaskAPI.API.dll"]