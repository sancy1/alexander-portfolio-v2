// File: internal/api/middleware/recovery.go
package middleware

import (
	"net/http"
	"runtime/debug"

	"github.com/gin-gonic/gin"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/pkg/logger"
	"go.uber.org/zap"
)

// Recovery handles uncaught exceptions securely to isolate failures.
func Recovery() gin.HandlerFunc {
	return func(c *gin.Context) {
		defer func() {
			if err := recover(); err != nil {
				logger.Error("Critical runtime application routine panic intercepted and recovered",
					zap.Any("error", err),
					zap.String("stack", string(debug.Stack())),
					zap.String("path", c.Request.URL.Path),
					zap.String("method", c.Request.Method),
				)

				c.AbortWithStatusJSON(http.StatusInternalServerError, gin.H{
					"error":   "internal_server_error",
					"message": "An unhandled execution routine disruption occurred",
				})
			}
		}()
		c.Next()
	}
}
