FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY *.props .
COPY AF0E.WebApi/Logbook/Logbook.Api/Logbook.Api.csproj Logbook.Api/
COPY AF0E.DB/AF0E.DB.csproj DB/
RUN sed -i 's|..\\..\\..\\AF0E.DB\\|../DB/|g' Logbook.Api/Logbook.Api.csproj
RUN dotnet restore Logbook.Api/Logbook.Api.csproj
COPY AF0E.WebApi/Logbook/Logbook.Api/. ./Logbook.Api/
COPY AF0E.DB/. ./DB/
RUN sed -i 's|..\\..\\..\\AF0E.DB\\|../DB/|g' Logbook.Api/Logbook.Api.csproj
WORKDIR /src/Logbook.Api
RUN dotnet build Logbook.Api.csproj --no-restore -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish Logbook.Api.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

#FROM base AS final
FROM mcr.microsoft.com/dotnet/aspnet:10.0
EXPOSE 8080
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Logbook.Api.dll"]
