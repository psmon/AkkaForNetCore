

docker network create --driver=bridge --subnet=172.19.0.0/16 devnet

##DataDog
docker run -d --name psmon-agent -v /var/run/docker.sock:/var/run/docker.sock:ro -v^
/proc/:/host/proc/:ro -v /sys/fs/cgroup/:/host/sys/fs/cgroup:ro^
-e DD_API_KEY=927831f7bac6ac9832a3ab5033b9ecd1^
-e DD_DOGSTATSD_NON_LOCAL_TRAFFIC=true^
-p 8125:8125/udp datadog/agent:7


