FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
WORKDIR /application
EXPOSE 80

# Copy all .csproj files and restore as distinct layers.
# Take advantage of docker caching by copying every docker file in the project.
COPY CarSales.sln CarSales.sln
COPY src/AuctionService/AuctionService.csproj src/AuctionService/AuctionService.csproj
COPY src/SearchService/SearchService.csproj src/SearchService/SearchService.csproj
COPY src/GatewayService/GatewayService.csproj src/GatewayService/GatewayService.csproj
COPY src/Contracts/Contracts.csproj src/Contracts/Contracts.csproj
COPY src/IdentityService/IdentityService.csproj src/IdentityService/IdentityService.csproj

# Restore package deps
RUN dotnet restore CarSales.sln

# Copy the application folders over
COPY src/AuctionService src/AuctionService
COPY src/Contracts src/Contracts
WORKDIR /application/src/AuctionService
# publish AuctionService application files to out directory:
RUN dotnet publish -c Release -o /application/src/out

# build runtime image (from runtime rather than SDK, which would be much bigger)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /application
# copy everything from "build" and put it into /application/src/out:
COPY --from=build /application/src/out .
ENTRYPOINT [ "dotnet", "AuctionService.dll" ]