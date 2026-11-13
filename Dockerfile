FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PmfApi.Domain/PmfApi.Domain.csproj                 PmfApi.Domain/
COPY PmfApi.Application/PmfApi.Application.csproj       PmfApi.Application/
COPY PmfApi.Infrastructure/PmfApi.Infrastructure.csproj PmfApi.Infrastructure/
COPY PmfApi.Api/PmfApi.Api.csproj                       PmfApi.Api/
RUN dotnet restore PmfApi.Api/PmfApi.Api.csproj

COPY . .
RUN dotnet publish PmfApi.Api/PmfApi.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=10000
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 10000

ENTRYPOINT ["dotnet", "PmfApi.Api.dll"]
