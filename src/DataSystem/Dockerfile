FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /data-system

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore 
# Build and publish a release
RUN dotnet publish -c Release -o out
# generate certificate
RUN dotnet dev-certs https -ep /https/aspnetapp.pfx -p "testCertificatePassword"

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /data-system
COPY --from=build-env /data-system/out .
COPY --chmod=0755 --from=build-env /https/ ./https

ENTRYPOINT ["dotnet", "DataSystem.dll"]
