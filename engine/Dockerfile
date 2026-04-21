FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy engine source and the Casbin policy files referenced by Nebula.Infrastructure.csproj
COPY engine/ .
COPY planning-mds/security/policies/ /planning-mds/security/policies/
RUN dotnet restore Nebula.slnx
RUN dotnet publish src/Nebula.Api/Nebula.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Nebula.Api.dll"]
