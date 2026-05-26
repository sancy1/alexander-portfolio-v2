#!/bin/sh
set -e

# Replace environment variables in nginx config
envsubst '${AUTH_SERVICE_URL} ${AUTH_SERVICE_HOST}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

# Execute the main command
exec "$@"