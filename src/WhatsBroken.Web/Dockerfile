#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
WORKDIR /src
COPY ["src/WhatsBroken.Web/WhatsBroken.Web.csproj", "src/WhatsBroken.Web/"]
RUN dotnet restore "src/WhatsBroken.Web/WhatsBroken.Web.csproj"
COPY . .
WORKDIR "/src/src/WhatsBroken.Web"

# Azure DevOps Metadata
ARG BUILD_BUILDNUMBER
ARG BUILD_BUILDDEFINITIONNAME
ARG BUILD_BUILDID
ARG BUILD_SOURCEVERSION
ARG BUILD_SOURCEBRANCH
ENV BUILD_BUILDNUMBER=$BUILD_BUILDNUMBER \
    BUILD_BUILDDEFINITIONNAME=$BUILD_BUILDDEFINITIONNAME \
    BUILD_BUILDID=$BUILD_BUILDID \
    BUILD_SOURCEVERSION=$BUILD_SOURCEVERSION \
    BUILD_SOURCEBRANCH=$BUILD_SOURCEBRANCH 

RUN dotnet build "WhatsBroken.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WhatsBroken.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WhatsBroken.Web.dll"]