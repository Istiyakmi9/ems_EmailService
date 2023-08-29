#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

ENV ASPNETCORE_ENVIRONMENT Production

COPY ["ems_EmailService/EmailRequest.csproj", "ems_EmailService/"]
COPY ["BottomhalfCore/BottomhalfCore.csproj", "BottomhalfCore/"]
COPY ["ModalLayer/ModalLayer.csproj", "ModalLayer/"]
RUN dotnet restore "ems_EmailService/EmailRequest.csproj"
COPY . .
WORKDIR "/src/ems_EmailService"
RUN dotnet build "EmailRequest.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EmailRequest.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EmailRequest.dll"]