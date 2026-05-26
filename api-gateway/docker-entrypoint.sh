#!/bin/sh
set -e

echo "=== AUTH_SERVICE_HOST: '${AUTH_SERVICE_HOST}' ==="
echo "=== AUTH_SERVICE_PORT: '${AUTH_SERVICE_PORT}' ==="

envsubst '${AUTH_SERVICE_HOST} ${AUTH_SERVICE_PORT}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

echo "=== nginx.conf sample ==="
sed -n '1,30p' /etc/nginx/nginx.conf

exec "$@"