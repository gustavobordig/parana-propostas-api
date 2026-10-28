FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Propostas.Api/Propostas.Api.csproj src/Propostas.Api/
COPY src/Propostas.Application/Propostas.Application.csproj src/Propostas.Application/
COPY src/Propostas.Domain/Propostas.Domain.csproj src/Propostas.Domain/
COPY src/Propostas.Infrastructure/Propostas.Infrastructure.csproj src/Propostas.Infrastructure/
COPY src/Propostas.Contracts/Propostas.Contracts.csproj src/Propostas.Contracts/
RUN dotnet restore src/Propostas.Api/Propostas.Api.csproj

COPY src/ src/
RUN dotnet publish src/Propostas.Api/Propostas.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "Propostas.Api.dll"]
