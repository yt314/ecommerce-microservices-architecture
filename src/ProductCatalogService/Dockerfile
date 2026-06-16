# TEMPORARY WORKAROUND (same as Phase 1): NuGet restore fails inside Docker on
# this machine, so we publish on the host first and the image only runs output.
#   dotnet publish ... -o ./publish   then   docker compose up --build
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish/ ./
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ProductCatalogService.dll"]
