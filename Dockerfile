
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build-env
COPY . /app
WORKDIR /app

ARG GITHUB_TOKEN

RUN ["dotnet", "restore"]
RUN dotnet publish -c Release -o out Metaflow.Orleans.DefaultHost/Metaflow.Orleans.DefaultHost.csproj

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app

ARG Hosting=Local
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "Metaflow.Orleans.DefaultHost"]
