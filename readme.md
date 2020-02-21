# akka.net for .net core

## AKKA 소개

Akka는 오픈 소스 툴킷으로,동시성과 분산 애플리케이션을 단순화하는 라이브러리입니다.


소개 : http://wiki.webnori.com/display/AKKA


## 이 저장소의 목적

범용적인 .net core 의 버젼에서, akka.net을 활용하는것을 연습하는 프로젝트입니다.

- .net core : https://dotnet.microsoft.com/download/dotnet-core/2.2
- akka.net : https://getakka.net/articles/intro/modules.html


다음 버젼에 최적화 되었습니다.

#
## Local Cluster

로컬에 여러대를 뛰워 분산처리 체크가 필요할시 사용

- master: dotnet run --configuration Release --project AkkaNetCore --environment "Development" --akkaip 127.0.0.1 --akkaport 5100  --akkaseed 127.0.0.1:5100 --port 5000
- node1: dotnet run --no-build --configuration Release --project AkkaNetCore --environment "Development" --akkaip 127.0.0.1 --akkaport 5101  --akkaseed 127.0.0.1:5100 --port 5001



## 주요 의존 모듈

    <TargetFramework>netcoreapp2.2</TargetFramework>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
    <PackageReference Include="NLog.Web.AspNetCore" Version="4.8.1" />
    <PackageReference Include="Newtonsoft.Json" Version="12.0.2" />
    <PackageReference Include="Akka" Version="1.3.12" />
    <PackageReference Include="Akka.Streams" Version="1.3.12" />
    <PackageReference Include="Akka.Logger.NLog" Version="1.3.3" />