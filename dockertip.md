

docker network create --driver=bridge --subnet=172.19.0.0/16 devnet

## DataDog

    DataDog은 Sass형태의 모니터링 툴로
    테스트를 위해서라면 무료로 사용가능 (이벤트가 1일만 보관됨)

    docker run -d --name psmon-agent -v /var/run/docker.sock:/var/run/docker.sock:ro -v^
    /proc/:/host/proc/:ro -v /sys/fs/cgroup/:/host/sys/fs/cgroup:ro^
    -e DD_API_KEY=XXXXXXXXXXXXXXXXXXXXXXXXXXXX^
    -e DD_DOGSTATSD_NON_LOCAL_TRAFFIC=true^
    -p 8125:8125/udp datadog/agent:7

## 프로메테우스

    # docker-compose.yml
    version: '2'
    services:
        prometheus:
            image: prom/prometheus:0.18.0
            volumes:
                - ./prometheus.yml:/etc/prometheus/prometheus.yml
            command:
                - '-config.file=/etc/prometheus/prometheus.yml'
            ports:
                - '9090:9090'
            networks:
                - dockernet
    networks:
        dockernet:
            external: true

    # prometheus.yml
    global:
        scrape_interval: 5s
        external_labels:
            monitor: 'my-monitor'
    scrape_configs:
        - job_name: 'prometheus'
          target_groups:
              - targets: ['192.168.0.51:10250']


