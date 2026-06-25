FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Lexify.API/Lexify.API.csproj", "src/Lexify.API/"]
COPY ["src/Lexify.Application/Lexify.Application.csproj", "src/Lexify.Application/"]
COPY ["src/Lexify.Infrastructure/Lexify.Infrastructure.csproj", "src/Lexify.Infrastructure/"]
COPY ["src/Lexify.Domain/Lexify.Domain.csproj", "src/Lexify.Domain/"]
RUN dotnet restore "src/Lexify.API/Lexify.API.csproj"

COPY . .
RUN dotnet publish "src/Lexify.API/Lexify.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Lexify.API.dll"]
