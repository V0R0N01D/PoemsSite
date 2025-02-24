FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Poems.Site/Poems.Site.csproj", "Poems.Site/"]
COPY ["Poems.Common/Poems.Common.csproj", "Poems.Common/"]
RUN dotnet restore "Poems.Site/Poems.Site.csproj"

COPY . .

WORKDIR "/src/Poems.Site"
RUN dotnet publish "Poems.Site.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Poems.Site.dll"]