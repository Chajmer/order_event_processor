exec info -> demo

REQUIREMENTS:
- For starters project is using: docker / docker-compose, RabbitMQ, Postgress database, .Net 8.0.
- TODO datailed requirements.
- RabbitMQ and Postgress seems to be automatically get as an image to docker.

COMMANDS:
- CLEAR running/paused dockers -> run if config containers fails<br/>
docker-compose down

- server start in server/ folder<br/>
docker-compose down<br/>
docker-compose up --build<br/>
// --build is optional

- database entry -> password is password<br/>
psql -h localhost -U user -d order_db<br/>
// initially is needed paste there init.sql file from server/

- client start in client/ folder<br/>
docker build -t my-client .<br/>
docker run --rm -it --network host -e RABBITMQ_HOST=localhost my-client

- RabbitMQ could be checked on http://localhost:15672/<br/>
login / password is guest / guest
