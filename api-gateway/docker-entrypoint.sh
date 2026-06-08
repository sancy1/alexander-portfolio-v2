# #!/bin/sh
# set -e

# echo "=== AUTH_SERVICE_HOST: '${AUTH_SERVICE_HOST}' ==="
# echo "=== GATEWAY_SECRET present: '$([ -n "${GATEWAY_SECRET}" ] && echo yes || echo NO - MISSING)' ==="

# envsubst '${AUTH_SERVICE_HOST} ${GATEWAY_SECRET}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

# exec "$@"














# #!/bin/sh
# set -e

# # Log the status of variables for debugging
# echo "=== AUTH_SERVICE_HOST: '${AUTH_SERVICE_HOST}' ==="
# echo "=== NOTIFICATION_SERVICE_HOST: '${NOTIFICATION_SERVICE_HOST}' ==="
# echo "=== GATEWAY_SECRET present: '$([ -n "${GATEWAY_SECRET}" ] && echo yes || echo NO - MISSING)' ==="

# # Perform the environment substitution
# # We add NOTIFICATION_SERVICE_HOST to the list of variables for envsubst
# envsubst '${AUTH_SERVICE_HOST} ${NOTIFICATION_SERVICE_HOST} ${GATEWAY_SECRET}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

# # Execute the main Nginx process
# exec "$@"

















#!/bin/sh
set -e

# Log the status of variables for debugging
echo "=== AUTH_SERVICE_HOST: '${AUTH_SERVICE_HOST}' ==="
echo "=== NOTIFICATION_SERVICE_HOST: '${NOTIFICATION_SERVICE_HOST}' ==="
echo "=== GATEWAY_SECRET present: '$([ -n "${GATEWAY_SECRET}" ] && echo yes || echo NO - MISSING)' ==="

# Perform the environment substitution
envsubst '${AUTH_SERVICE_HOST} ${NOTIFICATION_SERVICE_HOST} ${GATEWAY_SECRET}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

# 🔥 FIX: Explicitly hand execution off directly to Nginx to prevent runtime exec format errors
exec nginx -g "daemon off;"