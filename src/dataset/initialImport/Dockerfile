FROM --platform=linux/amd64 python:3.12-alpine3.20 as build

WORKDIR /import

RUN apk update && apk add postgresql-dev gcc python3-dev musl-dev
RUN apk add --no-cache bash

COPY . .
RUN pip install --no-cache-dir -r requirements.txt

ENTRYPOINT ["/bin/bash", "/import/startup.sh"]
