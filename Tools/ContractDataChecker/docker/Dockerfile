# Variables
ARG BUILDER=mcr.microsoft.com/dotnet/sdk:10.0
ARG IMAGE=${BUILDER}
ARG APP_HOME=/app

# Build
FROM ${IMAGE} AS builder
ARG APP_HOME

WORKDIR ${APP_HOME}
COPY ./Tools/ContractDataChecker ./Tools/ContractDataChecker
COPY ./Framework ./Framework
COPY ./ProjectPlugins ./ProjectPlugins
RUN dotnet restore Tools/ContractDataChecker
RUN dotnet publish Tools/ContractDataChecker -c Release -o out

# Create
FROM ${IMAGE}
ARG APP_HOME
ENV APP_HOME=${APP_HOME}

WORKDIR ${APP_HOME}
COPY --from=builder ${APP_HOME}/out .
CMD dotnet ${APP_HOME}/ContractDataChecker.dll
