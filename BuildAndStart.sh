docker build -f fishslice/Dockerfile --force-rm -t fishslice .
docker-compose up $1 
