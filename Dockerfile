# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["MachlyAPI/MachlyAPI.csproj", "MachlyAPI/"]
RUN dotnet restore "MachlyAPI/MachlyAPI.csproj"
COPY . .
WORKDIR "/src/MachlyAPI"
RUN dotnet build "MachlyAPI.csproj" -c Release -o /app/build
RUN dotnet publish "MachlyAPI.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "MachlyAPI.dll"]