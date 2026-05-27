#!/bin/sh
set -e

echo "=== AUTH_SERVICE_HOST: '${AUTH_SERVICE_HOST}' ==="
echo "=== GATEWAY_SECRET present: '$([ -n "${GATEWAY_SECRET}" ] && echo yes || echo NO - MISSING)' ==="

envsubst '${AUTH_SERVICE_HOST} ${GATEWAY_SECRET}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

exec "$@"