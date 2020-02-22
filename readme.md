# akka.net for .net core

## AKKA 소개

Akka는 오픈 소스 툴킷으로,동시성과 분산 애플리케이션을 단순화하는 라이브러리입니다.


소개 : http://wiki.webnori.com/display/AKKA


## 이 저장소의 목적

범용적인 .net core 의 버젼에서, akka.net을 활용하는것을 연습하는 프로젝트입니다.

- .net core : https://dotnet.microsoft.com/download/dotnet-core/2.2
- akka.net : https://getakka.net/articles/intro/modules.html


다음 버젼에 최적화 되었습니다.

# 빌드

이 프로젝트의 최종목적은 클러스터를 활용하는 분산처리 마이크로 서비스를 구동하는것입니다.

로컬개발환경 -> 도커개발환경 -> 클라우드운영 이질감이 없는 개발환경 컨셉을 지향합니다.

    솔류션이 위치한 디렉토리에서 명령수행

    ## LightHouse : Akka의 클러스터를 위한 시드노드이며 아파치의 주키퍼와 유사한 기능을 수행합니다.

    docker login hub.webnori.com  - Private 레지스트리를 활용하였으며, 자신의 레지스트리로 교체 가능합니다.

    docker build -f LightHouse/Dockerfile --force-rm -t hub.webnori.com/lighthouse:dev --label "com.webnori.created-by=psmon" --label "com.microsoft.visual-studio.project-name=LightHouse" .

    docker run -e CLUSTER_IP=127.0.0.1 -e CLUSTER_PORT=4053 -e CLUSTER_SEEDS=akka.tcp://actor-cluster@127.0.0.1:4053 --publish 4053:4053 --name netcore_lighthouse hub.webnori.com/lighthouse:dev

    docker push hub.webnori.com/lighthouse:dev

## Local Sigle Node

다음과 같은 옵션으로 싱글 노드로 작동가능합니다.

    launchSetting.json
      "environmentVariables": {
        "akkaport": "7100",
        "akkaip": "127.0.0.1",
        "akkaseed": "akka.tcp://actor-cluster@127.0.0.1:7100",
        "roles": "akkanet",
        "port": "5000",
        "ASPNETCORE_ENVIRONMENT": "Development"
      }

## Local Cluster

로컬 개발환경에서 여러대 어플리케이션의 분산 처리 기능을 체크할때

port : 웹 API 포트이며 충돌이 안나도록 설정
akkaip/akkaport : 자신의 ip/port이며 충돌이 안나도록 설정
akkaseed : Akka 클러스터 시드를 관리
빌드 특성 : node1을 실행시만 빌드, 이후 노드는 동일 빌드를 사용

- seed: docker run -e CLUSTER_IP=127.0.0.1 -e CLUSTER_PORT=4053 -e CLUSTER_SEEDS=akka.tcp://actor-cluster@127.0.0.1:4053 --publish 4053:4053 --name netcore_lighthouse hub.webnori.com/lighthouse:dev
- node1: dotnet run  --configuration Release --project AkkaNetCore --environment "Development" --port 5001 --akkaip 127.0.0.1 --akkaport 5101 --role akkanet --akkaseed akka.tcp://actor-cluster@127.0.0.1:4053
- node2: dotnet run --no-build --configuration Release --project AkkaNetCore --environment "Development" --port 5002 --akkaip 127.0.0.1 --akkaport 5102 --role akkanet --akkaseed akka.tcp://actor-cluster@127.0.0.1:4053


## Docker-Compose Cluster

Local에 서 포트충돌을 피하면서, 클러스터 구성하기는 번거로움으로

Docker-Compose로 멀티 인스턴스 구성이 되어 있습니다. - VisualStudio 2019에서 Run 가능

Docker-Compose로 클러스터 구성을 참고하여,  클라우드또는 쿠버네틱스로의 전환이 용이합니다.

## 모니터링

    docker network create -d bridge --subnet 192.170.0.0/24 --gateway 192.170.0.1 dockernet

    docker network inspect dockernet

    http://localhost:9090/status

    http://localhost:10250/metrics

## 주요 의존 모듈

- NLog.Web.AspNetCore : 로깅
- Akka.Cluster : Akka를 포함한 클러스터링 모듈
- Akka.Monitoring : 모니터링 모듈
- Akka.Logger.NLog : Nlog호환 Akka 로깅
- Swashbuckle.AspNetCore : API문서 자동

