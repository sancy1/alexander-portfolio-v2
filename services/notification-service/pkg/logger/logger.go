package logger

import (
	"go.uber.org/zap"
	"go.uber.org/zap/zapcore"
	"os"
)

var globalLogger *zap.Logger

// Init initializes the global logger engine context matching the host execution environment.
func Init(env string) error {
	var config zap.Config
	if env == "production" {
		config = zap.NewProductionConfig()
		config.EncoderConfig.TimeKey = "timestamp"
		config.EncoderConfig.EncodeTime = zapcore.ISO8601TimeEncoder
	} else {
		config = zap.NewDevelopmentConfig()
		// FIXED: Changed from CapitalColorEncoder to CapitalColorLevelEncoder
		config.EncoderConfig.EncodeLevel = zapcore.CapitalColorLevelEncoder
	}

	config.OutputPaths = []string{"stdout"}
	config.ErrorOutputPaths = []string{"stderr"}

	logger, err := config.Build(
		zap.AddCaller(),
		zap.AddCallerSkip(1),
	)
	if err != nil {
		return err
	}

	globalLogger = logger
	return nil
}

// Get returns the global logger instance with a safe dynamic initializer fallback.
func Get() *zap.Logger {
	if globalLogger == nil {
		env := os.Getenv("APP_ENV")
		if env == "" {
			env = "development"
		}
		_ = Init(env)
	}
	return globalLogger
}

// Sync flushes any buffered log entries down to standard output streams.
func Sync() {
	if globalLogger != nil {
		_ = globalLogger.Sync()
	}
}

// Helper functions for convenient structural logging
func Debug(msg string, fields ...zap.Field) { Get().Debug(msg, fields...) }
func Info(msg string, fields ...zap.Field)  { Get().Info(msg, fields...) }
func Warn(msg string, fields ...zap.Field)  { Get().Warn(msg, fields...) }
func Error(msg string, fields ...zap.Field) { Get().Error(msg, fields...) }
func Fatal(msg string, fields ...zap.Field) { Get().Fatal(msg, fields...) }
