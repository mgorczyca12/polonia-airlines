FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Packages.props ./
COPY backend/shared/shared.csproj backend/shared/
COPY backend/flight-service/flight-service.csproj backend/flight-service/
COPY backend/booking-service/booking-service.csproj backend/booking-service/
COPY backend/seating-service/seating-service.csproj backend/seating-service/
COPY tests/system/fixture/Fixture.csproj tests/system/fixture/
RUN dotnet restore tests/system/fixture/Fixture.csproj
COPY backend/shared/ backend/shared/
COPY backend/flight-service/ backend/flight-service/
COPY backend/booking-service/ backend/booking-service/
COPY backend/seating-service/ backend/seating-service/
COPY tests/system/fixture/ tests/system/fixture/
RUN dotnet publish tests/system/fixture/Fixture.csproj --no-restore -c Release -o /fixture /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /fixture
COPY --from=build /fixture/ ./
ENTRYPOINT ["dotnet", "Fixture.dll"]
