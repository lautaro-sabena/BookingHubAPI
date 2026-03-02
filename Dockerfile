FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/BookingHubAPI.API/BookingHubAPI.API.csproj", "src/BookingHubAPI.API/"]
COPY ["src/BookingHubAPI.Application/BookingHubAPI.Application.csproj", "src/BookingHubAPI.Application/"]
COPY ["src/BookingHubAPI.Domain/BookingHubAPI.Domain.csproj", "src/BookingHubAPI.Domain/"]
COPY ["src/BookingHubAPI.Infrastructure/BookingHubAPI.Infrastructure.csproj", "src/BookingHubAPI.Infrastructure/"]

RUN dotnet restore "src/BookingHubAPI.API/BookingHubAPI.API.csproj"

COPY . .
WORKDIR "/src/src/BookingHubAPI.API"
RUN dotnet build "BookingHubAPI.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BookingHubAPI.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingHubAPI.API.dll"]
