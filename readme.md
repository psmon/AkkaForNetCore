# akka.net for .net core

## AKKA 소개

Akka는 오픈 소스 툴킷으로,동시성과 분산 애플리케이션을 단순화하는 라이브러리입니다.


소개 : http://wiki.webnori.com/display/webfr/.NET+Core+With+Akka


## 이 저장소의 목적

범용적인 .net core 의 버젼에서, akka.net을 활용하는것을 연습하는 프로젝트입니다.

- net core : https://dotnet.microsoft.com/download/dotnet-core/3.1
- akka.net : https://getakka.net/articles/intro/modules.html


다음 버젼에 최적화 되었습니다.


## Local Sigle Node

다음과 같은 옵션(시드노드를 자신으로 지정)으로 싱글 노드로 작동가능합니다.

    launchSetting.json
      "environmentVariables": {
        "akkaport": "7100",
        "akkaip": "127.0.0.1",
        "akkaseed": "akka.tcp://actor-cluster@127.0.0.1:7100",
        "roles": "akkanet",
        "port": "5000",
        "ASPNETCORE_ENVIRONMENT": "Development"
      }

# 도커 빌드

    솔류션이 위치한 디렉토리에서 명령수행

    ## LightHouse : Akka의 클러스터를 위한 시드노드이며 아파치의 주키퍼와 유사한 기능을 수행합니다.
    # Build
    docker build -f LightHouse/Dockerfile --force-rm -t lighthouse:latest --label "com.webnori.created-by=psmon" --label "com.microsoft.visual-studio.project-name=LightHouse" .
    # LightHouse (SeedNode) : 시드는 도커활용
    docker run -e CLUSTER_IP=127.0.0.1 -e CLUSTER_PORT=4053 -e CLUSTER_SEEDS=akka.tcp://actor-cluster@127.0.0.1:4053 --publish 4053:4053 --name netcore_lighthouse lighthouse:latest

    docker build -f AkkaNetCore/Dockerfile --force-rm -t akkanetcore:latest --label "com.webnori.created-by=psmon" --label "com.microsoft.visual-studio.project-name=AkkaNetCore" .


## Local Cluster

로컬 개발환경에서 여러대 어플리케이션의 분산 처리 기능을 체크할때

port : 웹 API 포트이며 충돌이 안나도록 설정
akkaip/akkaport : 자신의 ip/port이며 충돌이 안나도록 설정
akkaseed : Akka 클러스터 시드를 관리
빌드 특성 : node1을 실행시만 빌드, 이후 노드는 동일 빌드를 사용

    # 멀티 노드 : 첫번째 노드를 Seed로 작동시켜 스탠드얼론 작동가능

    dotnet run  --configuration Release --project AkkaNetCore --environment "Development" --port 5001 --akkaip 127.0.0.1 --akkaport 7100 --roles akkanet --akkaseed akka.tcp://actor-cluster@127.0.0.1:7100 --MonitorTool win
    
    dotnet run --no-build --configuration Release --project AkkaNetCore --environment "Development" --port 5002 --akkaip 127.0.0.1 --akkaport 5102 --roles akkanet,apiwork --akkaseed akka.tcp://actor-cluster@127.0.0.1:7100 --MonitorTool win
    
    dotnet run --no-build --configuration Release --project AkkaNetCore --environment "Development" --port 5003 --akkaip 127.0.0.1 --akkaport 5103 --roles akkanet,apiwork --akkaseed akka.tcp://actor-cluster@127.0.0.1:7100 --MonitorTool win


## Docker-Compose Cluster

Local에 서 포트충돌을 피하면서, 클러스터 구성하기는 번거로움으로

Docker-Compose로 멀티 인스턴스 구성이 되어 있습니다. - VisualStudio 2019에서 Run 가능

Docker-Compose로 클러스터 구성을 참고하여,  클라우드또는 쿠버네틱스로의 전환이 용이합니다.

more info : https://docs.microsoft.com/ko-kr/dotnet/architecture/microservices/multi-container-microservice-net-applications/multi-container-applications-docker-compose


## 주요 의존 모듈

- NLog.Web.AspNetCore : 로깅
- Akka.Cluster : Akka를 포함한 클러스터링 모듈
- Akka.Monitoring : 모니터링 모듈
- Akka.Logger.NLog : Nlog호환 Akka 로깅
- Swashbuckle.AspNetCore : API문서 자동

