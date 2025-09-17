# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["bookstoree/bookstoree.csproj", "bookstoree/"]
RUN dotnet restore "bookstoree/bookstoree.csproj"
COPY . .
WORKDIR "/src/bookstoree"
RUN dotnet publish "bookstoree.csproj" -c Release -o /app/publish

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080 # Render uses port 8080 for Docker services
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "bookstoree.dll"]