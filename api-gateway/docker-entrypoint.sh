#!/bin/sh
set -e

envsubst '${AUTH_SERVICE_HOST}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

exec "$@"