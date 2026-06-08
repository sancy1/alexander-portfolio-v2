📘 Notification Service API Documentation: Core Operations
1. Health Check & Observability
These endpoints allow your infrastructure (like Kubernetes, Docker, or Load Balancers) to verify that your service is alive and the database connection is stable.

Endpoint: /health
http://localhost:8081/health

Method: GET

Purpose: Deep health check of the service and the Neon database backend.

Success Response: 200 OK

Error Response: 503 Service Unavailable (if DB latency is too high or connectivity is lost).

Example Body:

JSON
{
  "status": "healthy",
  "database": {
    "connected": true,
    "latency_ms": 282,
    "open_conns": 5,
    "idle_conns": 2
  },
  "service": "notification-service",
  "environment": "production"
}
Endpoint: /ready

Method: GET

Purpose: Readiness probe. Tells the load balancer if the service is fully booted and ready to start accepting high-traffic requests.

Success Response: 200 OK


--------------------------------------------------------
SWAGGER DOCUMENTATION
--------------------------------------------------------
Swagger Documentation: http://localhost:8081/swagger/index.html

Development: Should load the UI immediately.

Production: If you want to test the Production Security locally, temporarily set APP_ENV=production in your .env file and restart. You should be prompted for a username/password before the Swagger UI renders.