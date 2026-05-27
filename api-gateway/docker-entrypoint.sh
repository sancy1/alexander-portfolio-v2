#!/bin/sh
set -e

echo "=== AUTH_SERVICE_HOST: '${AUTH_SERVICE_HOST}' ==="

envsubst '${AUTH_SERVICE_HOST}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

exec "$@"