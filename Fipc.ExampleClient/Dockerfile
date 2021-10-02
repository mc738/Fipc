FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Fipc.ExampleClient/Fipc.ExampleClient.fsproj", "Fipc.ExampleClient/"]
RUN dotnet restore "Fipc.ExampleClient/Fipc.ExampleClient.fsproj"
COPY . .
WORKDIR "/src/Fipc.ExampleClient"
RUN dotnet build "Fipc.ExampleClient.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fipc.ExampleClient.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fipc.ExampleClient.dll"]
