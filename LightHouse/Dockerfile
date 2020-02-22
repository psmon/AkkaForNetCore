FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app

# should be a comma-delimited list
ENV CLUSTER_SEEDS "[]"
ENV CLUSTER_IP ""
ENV CLUSTER_PORT "4053"

# 9110 - Petabridge.Cmd
# 4053 - Akka.Cluster
EXPOSE 9110 4053

RUN mkdir -p /app/SearchApi && cp /usr/share/zoneinfo/Asia/Seoul /etc/localtime

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["LightHouse/LightHouse.csproj", "LightHouse/"]
RUN dotnet restore "LightHouse/LightHouse.csproj"
COPY . .
WORKDIR "/src/LightHouse"
RUN dotnet build "LightHouse.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LightHouse.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LightHouse.dll"]