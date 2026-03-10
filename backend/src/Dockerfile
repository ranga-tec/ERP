FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet publish ISS.Api/ISS.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

COPY --from=build /app/publish ./

CMD ["sh", "-c", "dotnet ISS.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
