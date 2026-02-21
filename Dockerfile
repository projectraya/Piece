FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Piece/Piece.sln ./
COPY Piece/Piece/Piece.csproj ./Piece/
COPY Piece/Piece.Client/Piece.Client.csproj ./Piece.Client/
COPY Piece/Piece.Tests/Piece.Tests.csproj ./Piece.Tests/

# Restore packages
RUN dotnet restore Piece.sln

# Copy everything else
COPY Piece/ ./

# Build and publish
RUN dotnet publish Piece/Piece.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Piece.dll"]
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Piece.dll"]
