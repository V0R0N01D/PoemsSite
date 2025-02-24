FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Poems.Loader/Poems.Loader.csproj", "Poems.Loader/"]
COPY ["Poems.Common/Poems.Common.csproj", "Poems.Common/"]
RUN dotnet restore "Poems.Loader/Poems.Loader.csproj"

COPY . .

WORKDIR "/src/Poems.Loader"
RUN dotnet publish "Poems.Loader.csproj" -c $BUILD_CONFIGURATION -o /app/publish  /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Poems.Loader.dll"]
