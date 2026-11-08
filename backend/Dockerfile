FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG SERVICE
WORKDIR /src
COPY Directory.Packages.props ./
COPY backend/shared/shared.csproj backend/shared/
COPY backend/${SERVICE}/${SERVICE}.csproj backend/${SERVICE}/
RUN dotnet restore backend/${SERVICE}/${SERVICE}.csproj
COPY backend/shared/ backend/shared/
COPY backend/${SERVICE}/ backend/${SERVICE}/
RUN dotnet publish backend/${SERVICE}/${SERVICE}.csproj \
    --no-restore -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG SERVICE
ENV SERVICE_DLL=${SERVICE}.dll
ENV ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet \"$SERVICE_DLL\""]
