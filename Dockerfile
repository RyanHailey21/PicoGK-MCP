FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Setup global ShapeKernel
RUN mkdir -p /root/.picogk/lib
RUN git clone https://github.com/leap71/LEAP71_ShapeKernel.git /root/.picogk/LEAP71_ShapeKernel
RUN echo '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><PackageReference Include="PicoGK" Version="2.3.0" /></ItemGroup></Project>' > /root/.picogk/LEAP71_ShapeKernel/ShapeKernel/ShapeKernel.csproj
RUN dotnet build /root/.picogk/LEAP71_ShapeKernel/ShapeKernel/ShapeKernel.csproj -c Release -o /root/.picogk/lib

# Copy project files and package
COPY . .
RUN dotnet restore PicoGK.Mcp.csproj
RUN dotnet build -c Release PicoGK.Mcp.csproj
RUN dotnet pack -c Release -o /app/nupkg PicoGK.Mcp.csproj
RUN dotnet tool install -g --add-source /app/nupkg PicoGK.Mcp

ENV PATH="${PATH}:/root/.dotnet/tools"

ENTRYPOINT ["picogk-mcp"]
