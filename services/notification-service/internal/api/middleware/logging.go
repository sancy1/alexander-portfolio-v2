// File: internal/api/middleware/logging.go
package middleware

import (
	"time"

	"github.com/gin-gonic/gin"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/pkg/logger"
	"go.uber.org/zap"
)

// Logging returns a middleware that interceptively structures tracing metadata for HTTP routing paths.
func Logging() gin.HandlerFunc {
	return func(c *gin.Context) {
		start := time.Now()
		path := c.Request.URL.Path
		method := c.Request.Method

		c.Next()

		latency := time.Since(start)
		statusCode := c.Writer.Status()

		// Eliminate telemetry heartbeat poll pollution within infrastructure clusters
		if path != "/health" && path != "/ready" {
			logger.Info("HTTP Request Log Frame",
				zap.String("method", method),
				zap.String("path", path),
				zap.Int("status", statusCode),
				zap.Duration("latency", latency),
				zap.String("client_ip", c.ClientIP()),
			)
		}
	}
}
