FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore
COPY . .
RUN dotnet restore PicoGK.Mcp.csproj
RUN dotnet build -c Release PicoGK.Mcp.csproj
RUN dotnet pack -c Release -o /app/nupkg PicoGK.Mcp.csproj
RUN dotnet tool install -g --add-source /app/nupkg PicoGK.Mcp

ENV PATH="${PATH}:/root/.dotnet/tools"

ENTRYPOINT ["picogk-mcp"]
