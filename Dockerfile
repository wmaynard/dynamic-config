FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY bin/Release/net7.0/ .
ENTRYPOINT ["dotnet", "dynamic-config.dll"]