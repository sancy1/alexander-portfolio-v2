# FROM nginx:alpine

# # Install gettext for envsubst, dos2unix to fix Windows line endings
# RUN apk add --no-cache gettext dos2unix

# # Copy nginx configuration template
# COPY nginx.conf /etc/nginx/nginx.conf.template

# # Copy entrypoint and force LF line endings at build time
# COPY docker-entrypoint.sh /docker-entrypoint.sh
# RUN dos2unix /docker-entrypoint.sh && chmod +x /docker-entrypoint.sh

# EXPOSE 80

# ENTRYPOINT ["/docker-entrypoint.sh"]
# CMD ["nginx", "-g", "daemon off;"]



















# ====================================================================
# 📡 PRODUCTION ENTERPRISE REVERSE PROXY LAYER (API GATEWAY)
# ====================================================================
FROM nginx:alpine

# Install gettext natively to enable envsubst utility features
RUN apk add --no-cache gettext

# Copy nginx configuration template into the system image layout
COPY nginx.conf /etc/nginx/nginx.conf.template

EXPOSE 80

# Execute envsubst variables cleanly and boot up Nginx inline
CMD ["/bin/sh", "-c", "echo \"=== AUTH_SERVICE_HOST: '${AUTH_SERVICE_HOST}' ===\" && echo \"=== NOTIFICATION_SERVICE_HOST: '${NOTIFICATION_SERVICE_HOST}' ===\" && envsubst '${AUTH_SERVICE_HOST} ${NOTIFICATION_SERVICE_HOST} ${GATEWAY_SECRET}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf && exec nginx -g 'daemon off;'"]