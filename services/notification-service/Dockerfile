# ====================================================================
# 🏗️ MULTI-STAGE COMPILATION: INTERMEDIATE BUILD CONTAINER ENV
# ====================================================================
FROM golang:1.21-alpine AS builder

WORKDIR /app

RUN apk add --no-cache git ca-certificates

COPY go.mod ./
# To account for missing dynamic lock files during fresh stage initializations
COPY go.sum* ./ 

RUN go mod download

COPY . .

RUN CGO_ENABLED=0 GOOS=linux GOARCH=amd64 go build \
    -ldflags="-w -s" \
    -o /app/bin/notification-service \
    ./cmd/api

# ====================================================================
# 🚀 ECOSYSTEM RUNTIME INSTANCE EXECUTION CONTAINER
# ====================================================================
FROM alpine:latest
RUN apk --no-cache add ca-certificates
# Add a non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
WORKDIR /app
COPY --from=builder /app/bin/notification-service .
COPY --from=builder /app/configs ./configs
# Change ownership
RUN chown -R appuser:appgroup /app
USER appuser

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

CMD ["./notification-service"]