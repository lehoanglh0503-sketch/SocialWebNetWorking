FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Social Website/Social Website.csproj", "Social Website/"]
RUN dotnet restore "Social Website/Social Website.csproj"
COPY . .
WORKDIR "/src/Social Website"
RUN dotnet build "Social Website.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Social Website.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "Social Website.dll"]
