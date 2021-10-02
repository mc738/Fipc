FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Fipc.ExampleServer/Fipc.ExampleServer.fsproj", "Fipc.ExampleServer/"]
RUN dotnet restore "Fipc.ExampleServer/Fipc.ExampleServer.fsproj"
COPY . .
WORKDIR "/src/Fipc.ExampleServer"
RUN dotnet build "Fipc.ExampleServer.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fipc.ExampleServer.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fipc.ExampleServer.dll"]
