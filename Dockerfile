FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Okafor-.NET.csproj ./
RUN dotnet restore Okafor-.NET.csproj

COPY . .
RUN dotnet publish Okafor-.NET.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DataProtection__KeysPath=/data/data-protection-keys \
    PatientDocuments__StorageRoot=/data/patient-documents

COPY --from=build /app/publish .

RUN mkdir -p /data/data-protection-keys /data/patient-documents /app/wwwroot/uploads/posts \
    && chown -R "$APP_UID":"$APP_UID" /data /app/wwwroot/uploads

USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "Okafor-.NET.dll"]
