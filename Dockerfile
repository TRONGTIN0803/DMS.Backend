FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DMS.sln ./
COPY Directory.Build.props ./
COPY global.json ./
COPY src/DMS.Api/DMS.Api.csproj src/DMS.Api/
COPY src/DMS.Application/DMS.Application.csproj src/DMS.Application/
COPY src/DMS.Domain/DMS.Domain.csproj src/DMS.Domain/
COPY src/DMS.Infrastructure/DMS.Infrastructure.csproj src/DMS.Infrastructure/
COPY src/DMS.Shared/DMS.Shared.csproj src/DMS.Shared/
RUN dotnet restore src/DMS.Api/DMS.Api.csproj

COPY . .
RUN dotnet publish src/DMS.Api/DMS.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet DMS.Api.dll"]
