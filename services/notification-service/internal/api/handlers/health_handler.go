// File: internal/api/handlers/health_handler.go
package handlers

import (
	"context"
	"net/http"
	"time"

	"github.com/gin-gonic/gin"
	amqp091 "github.com/rabbitmq/amqp091-go"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/config"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/infrastructure/cache"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/infrastructure/database"
	"github.com/segmentio/kafka-go"
)

type HealthHandler struct {
	cfg           *config.Config
	dbConn        *database.DatabaseConnection
	kafkaReader   *kafka.Reader
	rabbitChannel *amqp091.Channel
	redisClient   *cache.RedisClient
}

// Constructor updated to accept RabbitMQ and Redis
func NewHealthHandler(cfg *config.Config, dbConn *database.DatabaseConnection, kr *kafka.Reader, rb *amqp091.Channel, rc *cache.RedisClient) *HealthHandler {
	return &HealthHandler{
		cfg:           cfg,
		dbConn:        dbConn,
		kafkaReader:   kr,
		rabbitChannel: rb,
		redisClient:   rc,
	}
}

// Health godoc
// @Summary Check service health
// @Description Returns the status of the database, kafka, rabbitmq, redis, and service
// @Tags Health
// @Produce json
// @Success 200 {object} map[string]interface{}
// @Router /health [get]
func (h *HealthHandler) Health(c *gin.Context) {
	ctx, cancel := context.WithTimeout(c.Request.Context(), 5*time.Second)
	defer cancel()

	dbStatus := h.dbConn.GetDetailedHealth(ctx)

	// Check components
	kafkaUp := h.kafkaReader != nil
	rabbitUp := h.rabbitChannel != nil && !h.rabbitChannel.IsClosed()
	
	// Use the custom Ping method we added to the RedisClient struct
	redisUp := false
	if h.redisClient != nil {
		err := h.redisClient.Ping(ctx)
		redisUp = err == nil
	}

	// Determine overall status
	httpStatus := http.StatusOK
	if !dbStatus.IsHealthy {
		httpStatus = http.StatusServiceUnavailable
	}

	c.JSON(httpStatus, gin.H{
		"status":      "healthy",
		"timestamp":   time.Now().UTC().Format(time.RFC3339),
		"service":     "notification-service",
		"database": gin.H{
			"connected":    dbStatus.IsHealthy,
			"latency_ms":   dbStatus.Latency.Milliseconds(),
			"open_conns":   dbStatus.OpenConns,
			"idle_conns":   dbStatus.IdleConns,
			"in_use_conns": dbStatus.InUseConns,
		},
		"kafka":    gin.H{"up": kafkaUp},
		"rabbitmq": gin.H{"up": rabbitUp},
		"redis":    gin.H{"up": redisUp},
		"environment": h.cfg.App.Env,
		"provider":    h.cfg.Database.Provider,
	})
}

func (h *HealthHandler) Ready(c *gin.Context) {
	ctx, cancel := context.WithTimeout(c.Request.Context(), 3*time.Second)
	defer cancel()

	if !h.dbConn.IsHealthy(ctx) {
		c.JSON(http.StatusServiceUnavailable, gin.H{"status": "not ready", "reason": "database down"})
		return
	}

	c.JSON(http.StatusOK, gin.H{"status": "ready"})
}