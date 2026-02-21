FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and projects
COPY Piece/Piece.sln ./
COPY Piece/Piece/Piece.csproj ./Piece/
COPY Piece/Piece.Client/Piece.Client.csproj ./Piece.Client/
COPY Piece/Piece.Tests/Piece.Tests.csproj ./Piece.Tests/

# Restore
RUN dotnet restore

# Copy everything else
COPY Piece/ ./

# Build
RUN dotnet publish Piece/Piece.csproj -c Release -o /app/out

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Piece.dll"]
