# 로컬 인프라

이프로젝트의 로컬 테스트를 개별 인프라입니다.

필요한 인프라를 도커 컴포져를 통해 구동 가능합니다.

	docker-compose up -d
	docker-compose down

## akakdb

RDBMS연동을 위해 mysql 5.6을 구동합니다.


## datadog

모니터링 수집 시스템인 DataDog Agent를 구동합니다.


## prometheus

모니터링 수집 시스템인 Prometheus를 구동합니다.
