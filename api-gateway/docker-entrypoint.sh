#!/bin/sh
set -e

# Replace environment variables in nginx config
envsubst '${AUTH_SERVICE_URL}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

# Execute the main command
exec "$@"