# 阶段1：构建
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/TripPacking/TripPacking.csproj", "src/TripPacking/"]
RUN dotnet restore "src/TripPacking/TripPacking.csproj"
COPY . .
WORKDIR "/src/src/TripPacking"
RUN dotnet publish "TripPacking.csproj" -c Release -o /app/publish

# 阶段2：运行
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8095
ENV ASPNETCORE_URLS=http://+:8095
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8095/health || exit 1
ENTRYPOINT ["dotnet", "TripPacking.dll"]
