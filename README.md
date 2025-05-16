exec info -> demo

REQUIREMENTS:
- For starters project is using: docker / docker-compose, RabbitMQ, Postgress database, .Net 8.0.
- TODO datailed requirements.
- RabbitMQ and Postgress seems to be automatically get as an image to docker.

COMMANDS:
- CLEAR running/paused dockers -> run if config containers fails
docker-compose down

- server start in server/ folder
docker-compose down
docker-compose up --build
// --build is optional

- database entry -> password is password
psql -h localhost -U user -d order_db
// initially is needed paste there init.sql file from server/

- client start in client/ folder
docker build -t my-client .
docker run --rm -it --network host -e RABBITMQ_HOST=localhost my-client

- RabbitMQ could be checked on http://localhost:15672/
login / password is guest / guest
