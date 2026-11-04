FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["NotificationService.slnx", "./"]
COPY ["NotificationService.Core/NotificationService.Core.csproj", "NotificationService.Core/"]
COPY ["NotificationService.Application/NotificationService.Application.csproj", "NotificationService.Application/"]
COPY ["NotificationService.Infrastructure/NotificationService.Infrastructure.csproj", "NotificationService.Infrastructure/"]
COPY ["NotificationService.Worker/NotificationService.Worker.csproj", "NotificationService.Worker/"]
RUN dotnet restore "NotificationService.Worker/NotificationService.Worker.csproj"
COPY . .
WORKDIR "/src/NotificationService.Worker"
RUN dotnet publish "NotificationService.Worker.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
ENV DOTNET_EnableDiagnostics=0
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.Worker.dll"]
