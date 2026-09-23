FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out .
# 預先建立 DataProtection Key 目錄，掛載 volume 時沿用此目錄權限
RUN mkdir -p /app/keys
# 店家實際營運地為台灣，容器預設 UTC 會導致 ToLocalTime()／DateTime.Today 等日期判斷偏差
ENV TZ=Asia/Taipei

ENTRYPOINT ["dotnet", "ManTingEats.dll"]