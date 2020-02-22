FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY ["AkkaNetCore/AkkaNetCore.csproj", "AkkaNetCore/"]
RUN dotnet restore "AkkaNetCore/AkkaNetCore.csproj"
COPY . .
WORKDIR "/src/AkkaNetCore"
RUN dotnet build "AkkaNetCore.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "AkkaNetCore.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "AkkaNetCore.dll"]