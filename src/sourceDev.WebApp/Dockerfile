FROM microsoft/aspnetcore:2.0-nanoserver-1709 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0-nanoserver-1709 AS build
WORKDIR /src
COPY *.sln ./
COPY src/sourceDev.WebApp/sourceDev.WebApp.csproj src/sourceDev.WebApp/
COPY src/cloudscribe.SimpleContent.Syndication/cloudscribe.SimpleContent.Syndication.csproj src/cloudscribe.SimpleContent.Syndication/
COPY src/cloudscribe.SimpleContent.Models/cloudscribe.SimpleContent.Models.csproj src/cloudscribe.SimpleContent.Models/
COPY src/cloudscribe.SimpleContent.Web/cloudscribe.SimpleContent.Web.csproj src/cloudscribe.SimpleContent.Web/
COPY src/cloudscribe.SimpleContent.Storage.EFCore.Common/cloudscribe.SimpleContent.Storage.EFCore.Common.csproj src/cloudscribe.SimpleContent.Storage.EFCore.Common/
COPY src/cloudscribe.SimpleContent.Storage.EFCore.MySQL/cloudscribe.SimpleContent.Storage.EFCore.MySQL.csproj src/cloudscribe.SimpleContent.Storage.EFCore.MySQL/
COPY src/cloudscribe.SimpleContent.Storage.EFCore.pgsql/cloudscribe.SimpleContent.Storage.EFCore.pgsql.csproj src/cloudscribe.SimpleContent.Storage.EFCore.pgsql/
COPY src/cloudscribe.SimpleContent.Web.Mvc/cloudscribe.SimpleContent.Web.Mvc.csproj src/cloudscribe.SimpleContent.Web.Mvc/
COPY src/cloudscribe.SimpleContent.Web.Views.Bootstrap3/cloudscribe.SimpleContent.Web.Views.Bootstrap3.csproj src/cloudscribe.SimpleContent.Web.Views.Bootstrap3/
COPY src/cloudscribe.SimpleContent.Storage.EFCore.MSSQL/cloudscribe.SimpleContent.Storage.EFCore.MSSQL.csproj src/cloudscribe.SimpleContent.Storage.EFCore.MSSQL/
COPY src/cloudscribe.SimpleContent.Storage.NoDb/cloudscribe.SimpleContent.Storage.NoDb.csproj src/cloudscribe.SimpleContent.Storage.NoDb/
COPY src/cloudscribe.Core.SimpleContent.Mvc/cloudscribe.Core.SimpleContent.Mvc.csproj src/cloudscribe.Core.SimpleContent.Mvc/
COPY src/cloudscribe.Core.SimpleContent/cloudscribe.Core.SimpleContent.csproj src/cloudscribe.Core.SimpleContent/
COPY src/cloudscribe.SimpleContent.MetaWeblog/cloudscribe.SimpleContent.MetaWeblog.csproj src/cloudscribe.SimpleContent.MetaWeblog/
RUN dotnet restore
COPY . .
WORKDIR /src/src/sourceDev.WebApp
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "sourceDev.WebApp.dll"]
