exec info -> demo

// when is need to clear running / paused dockers
docker-compose down

>>> server start in server/
docker-compose up --build
// --build je optional

>>> database entry
psql -h localhost -U user -d order_db

>>> client start in client/
docker build -t my-client .
docker run --rm -it --network host -e RABBITMQ_HOST=localhost my-client