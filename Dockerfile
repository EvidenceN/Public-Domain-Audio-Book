FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# NeoUI Extra's packages are in ./packages (nuget.config points at them first).
COPY nuget.config ./
COPY packages/ packages/
COPY src/ src/
RUN dotnet publish src/PublicDomainAudioBooks.Web -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# The book files and, on a machine without object storage, the media. Both are mounted; neither is in the image.
ENV CONTENT_DIR=/content ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PublicDomainAudioBooks.Web.dll"]
