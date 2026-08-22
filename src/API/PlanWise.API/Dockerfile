# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/API/PlanWise.API/PlanWise.Api.csproj", "src/API/PlanWise.API/"]
COPY ["src/Modules/IdentityAccess/PlanWise.Modules.IdentityAccess.Infrastructure/PlanWise.Modules.IdentityAccess.Infrastructure.csproj", "src/Modules/IdentityAccess/PlanWise.Modules.IdentityAccess.Infrastructure/"]
COPY ["src/Modules/IdentityAccess/PlanWise.Modules.IdentityAccess.Application/PlanWise.Modules.IdentityAccess.Application.csproj", "src/Modules/IdentityAccess/PlanWise.Modules.IdentityAccess.Application/"]
COPY ["src/Modules/IdentityAccess/PlanWise.Modules.IdentityAccess.Domain/PlanWise.Modules.IdentityAccess.Domain.csproj", "src/Modules/IdentityAccess/PlanWise.Modules.IdentityAccess.Domain/"]
COPY ["src/Modules/IdentityAccess/PlanWise.Modules.IdentityAccess.Presentation/PlanWise.Modules.IdentityAccess.Presentation.csproj", "src/Modules/IdentityAccess/PlanWise.Modules.IdentityAccess.Presentation/"]
COPY ["src/Modules/WorkspaceManagement/PlanWise.Modules.WorkspaceManagement.Infrastructure/PlanWise.Modules.WorkspaceManagement.Infrastructure.csproj", "src/Modules/WorkspaceManagement/PlanWise.Modules.WorkspaceManagement.Infrastructure/"]
COPY ["src/Modules/WorkspaceManagement/PlanWise.Modules.WorkspaceManagement.Application/PlanWise.Modules.WorkspaceManagement.Application.csproj", "src/Modules/WorkspaceManagement/PlanWise.Modules.WorkspaceManagement.Application/"]
COPY ["src/Modules/WorkspaceManagement/PlanWise.Modules.WorkspaceManagement.Domain/PlanWise.Modules.WorkspaceManagement.Domain.csproj", "src/Modules/WorkspaceManagement/PlanWise.Modules.WorkspaceManagement.Domain/"]
COPY ["src/Modules/WorkspaceManagement/PlanWise.Modules.WorkspaceManagement.Presentation/PlanWise.Modules.WorkspaceManagement.Presentation.csproj", "src/Modules/WorkspaceManagement/PlanWise.Modules.WorkspaceManagement.Presentation/"]
COPY ["src/Common/PlanWise.Common.Infrastructure/PlanWise.Common.Infrastructure.csproj", "src/Common/PlanWise.Common.Infrastructure/"]
COPY ["src/Common/PlanWise.Common.Application/PlanWise.Common.Application.csproj", "src/Common/PlanWise.Common.Application/"]
COPY ["src/Common/PlanWise.Common.Domain/PlanWise.Common.Domain.csproj", "src/Common/PlanWise.Common.Domain/"]
COPY ["src/Common/PlanWise.Common.Presentation/PlanWise.Common.Presentation.csproj", "src/Common/PlanWise.Common.Presentation/"]
RUN dotnet restore "./src/API/PlanWise.API/PlanWise.Api.csproj"
COPY . .
WORKDIR "/src/src/API/PlanWise.API"
RUN dotnet build "./PlanWise.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PlanWise.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PlanWise.Api.dll"]