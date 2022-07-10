#build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

COPY . .
RUN dotnet restore "./gRPCServer/Server.csproj"
RUN dotnet publish "./gRPCServer/Server.csproj" -c release -o /server --no-restore

#serve stage

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS runtime
WORKDIR /server
COPY --from=build /server .

ENTRYPOINT ["dotnet", "Server.dll"]