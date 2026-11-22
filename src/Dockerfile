# Step 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy .csproj files and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the files and build the application
COPY . ./
RUN dotnet publish -c Release -o out

# Step 2: Setting up the runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update && apt-get install -y ca-certificates

COPY --from=build-env /app/out .

# Expose port 88 to the container
EXPOSE 88

# Application startup command
ENTRYPOINT ["dotnet", "XerifeTv.CMS.dll"]