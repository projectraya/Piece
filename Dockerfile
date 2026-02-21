FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Piece/Piece.sln ./
COPY Piece/Piece/Piece.csproj ./Piece/
COPY Piece/Piece.Client/Piece.Client.csproj ./Piece.Client/
COPY Piece/Piece.Tests/Piece.Tests.csproj ./Piece.Tests/

RUN dotnet restore

COPY Piece/ ./

RUN dotnet publish Piece/Piece.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Piece.dll"]
