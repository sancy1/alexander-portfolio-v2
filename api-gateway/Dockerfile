FROM nginx:alpine

# Install gettext for envsubst, dos2unix to fix Windows line endings
RUN apk add --no-cache gettext dos2unix

# Copy nginx configuration template
COPY nginx.conf /etc/nginx/nginx.conf.template

# Copy entrypoint and force LF line endings at build time
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN dos2unix /docker-entrypoint.sh && chmod +x /docker-entrypoint.sh

EXPOSE 80

ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]