
FROM mcr.microsoft.com/dotnet/sdk:5.0-focal as build-env
COPY . /app
WORKDIR /app

ARG GITHUB_TOKEN

RUN ["dotnet", "restore"]
RUN dotnet publish -c Release -o out Metaflow.Orleans.DefaultHost/Metaflow.Orleans.DefaultHost.csproj

FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal
WORKDIR /app

ARG Hosting=Local
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "Metaflow.Orleans.DefaultHost.dll"]
