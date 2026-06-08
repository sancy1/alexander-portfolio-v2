
// File: internal/api/routes/routes.go
package routes

import (
	"github.com/gin-gonic/gin"
	amqp091 "github.com/rabbitmq/amqp091-go" // Import RabbitMQ
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/api/handlers"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/api/middleware"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/config"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/infrastructure/cache"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/infrastructure/database"
	"github.com/segmentio/kafka-go"
	swaggerFiles "github.com/swaggo/files"
	ginSwagger "github.com/swaggo/gin-swagger"

	_ "github.com/sancy1/alexander-portfolio-v2/services/notification-service/docs" // Swagger docs
)

func SetupRoutes(
	engine *gin.Engine,
	cfg *config.Config,
	dbConn *database.DatabaseConnection,
	redisClient *cache.RedisClient,
	kafkaReader *kafka.Reader, // Kafka reader
	rabbitChannel *amqp091.Channel, // RabbitMQ channel
) {
	// Swagger Auth middleware
	swaggerAuth := func(c *gin.Context) {
		if cfg.App.Env == "production" {
			user, pass, ok := c.Request.BasicAuth()
			if !ok || user != cfg.Swagger.AdminUser || pass != cfg.Swagger.AdminPassword {
				c.Header("WWW-Authenticate", "Basic realm=Restricted")
				c.AbortWithStatus(401)
				return
			}
		}
		c.Next()
	}

	engine.GET("/swagger/*any", swaggerAuth, ginSwagger.WrapHandler(swaggerFiles.Handler))

	// Register Health Handlers with all dependencies
	healthHandler := handlers.NewHealthHandler(cfg, dbConn, kafkaReader, rabbitChannel, redisClient)
	engine.GET("/health", healthHandler.Health)
	engine.GET("/ready", healthHandler.Ready)

	v1 := engine.Group("/api/v1")

	v1.GET("/ping", func(c *gin.Context) {
		c.JSON(200, gin.H{"message": "pong"})
	})

	authMiddleware := middleware.NewAuthMiddleware(&cfg.JWT)
	v1.Use(authMiddleware.Authenticate())
	{
		notifications := v1.Group("/notifications")
		{
			notifications.GET("", func(c *gin.Context) { c.JSON(200, gin.H{"status": "staged"}) })
			notifications.GET("/unread-count", func(c *gin.Context) { c.JSON(200, gin.H{"count": 0}) })
			notifications.PATCH("/:id/read", func(c *gin.Context) { c.JSON(200, gin.H{"status": "updated"}) })
			notifications.PATCH("/:id/archive", func(c *gin.Context) { c.JSON(200, gin.H{"status": "archived"}) })
			notifications.DELETE("/:id", func(c *gin.Context) { c.JSON(200, gin.H{"status": "deleted"}) })
		}

		preferences := v1.Group("/preferences")
		{
			preferences.GET("", func(c *gin.Context) {
				c.JSON(200, gin.H{"message": "Preferences sub-module gateway route offline"})
			})
		}
	}
}