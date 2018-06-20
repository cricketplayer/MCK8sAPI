FROM microsoft/aspnetcore:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.1-sdk AS build
WORKDIR /src
COPY MCK8sAPI.sln ./
COPY MCK8sAPI/MCK8sAPI.csproj MCK8sAPI/
RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/MCK8sAPI
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MCK8sAPI.dll"]
