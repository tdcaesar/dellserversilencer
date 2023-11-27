FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["DellServerSilencer/DellServerSilencer.csproj", "DellServerSilencer/"]
RUN dotnet restore "DellServerSilencer/DellServerSilencer.csproj"
COPY . .
WORKDIR "/src/DellServerSilencer"
RUN dotnet build "DellServerSilencer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DellServerSilencer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DellServerSilencer.dll"]
