FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy project files first to optimize build cache
COPY ["src/TodoTaskAPI.API/TodoTaskAPI.API.csproj", "src/TodoTaskAPI.API/"]
COPY ["src/TodoTaskAPI.Core/TodoTaskAPI.Core.csproj", "src/TodoTaskAPI.Core/"]
COPY ["src/TodoTaskAPI.Infrastructure/TodoTaskAPI.Infrastructure.csproj", "src/TodoTaskAPI.Infrastructure/"]
COPY ["src/TodoTaskAPI.Application/TodoTaskAPI.Application.csproj", "src/TodoTaskAPI.Application/"]
RUN dotnet restore "src/TodoTaskAPI.API/TodoTaskAPI.API.csproj"
# Copy the rest of the code
COPY . .
# Build with explicit output path for XML
RUN dotnet build "src/TodoTaskAPI.API/TodoTaskAPI.API.csproj" -c Release -o /app/build

FROM build AS publish
# Publish with explicit output path
RUN dotnet publish "src/TodoTaskAPI.API/TodoTaskAPI.API.csproj" -c Release -o /app/publish /p:GenerateDocumentationFile=true

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# Copy everything from publish folder
COPY --from=publish /app/publish .
# Verify the XML file exists
RUN ls -la /app/TodoTaskAPI.API.xml || echo "XML file not found"
ENTRYPOINT ["dotnet", "TodoTaskAPI.API.dll"]