#build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

COPY . .
RUN dotnet restore "./gRPCServerASP/gRPCServer.csproj"
RUN dotnet publish "./gRPCServerASP/gRPCServer.csproj" -c release -o /server --no-restore

#serve stage

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /server
COPY --from=build /server .

ENTRYPOINT ["dotnet", "gRPCServer.dll"]