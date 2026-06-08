package database

import (
	"context"
	"fmt"
	"time"

	"github.com/jmoiron/sqlx"
	_ "github.com/lib/pq"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/config"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/pkg/logger"
	"go.uber.org/zap"
)

type DetailedHealth struct {
	IsHealthy  bool
	Latency    time.Duration
	OpenConns  int
	IdleConns  int
	InUseConns int
}

type DatabaseConnection struct {
	DB     *sqlx.DB
	Config *config.Config
}

func NewDatabaseConnection(cfg *config.Config) (*DatabaseConnection, error) {
	logger.Info("Initializing database manager integration routing...", zap.String("provider", cfg.Database.Provider))

	conn := &DatabaseConnection{
		Config: cfg,
	}

	if err := conn.connectWithRetry(); err != nil {
		return nil, fmt.Errorf("failed to complete connection retry loop: %w", err)
	}

	conn.configurePool()

	// Direct inline heartbeat checks
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	if err := conn.DB.PingContext(ctx); err != nil {
		return nil, fmt.Errorf("database heartbeat handshake failed: %w", err)
	}

	return conn, nil
}

func (c *DatabaseConnection) connectWithRetry() error {
	maxRetries := c.Config.Database.MaxRetries
	if maxRetries <= 0 {
		maxRetries = 6
	}
	delay := c.Config.Database.RetryDelay
	if delay <= 0 {
		delay = 2 * time.Second
	}
	multiplier := c.Config.Database.BackoffMultiplier
	if multiplier <= 0 {
		multiplier = 1.5
	}

	var err error
	for i := 1; i <= maxRetries; i++ {
		logger.Info(fmt.Sprintf("🔄 Attempting database connection pool initialization (%d/%d)...", i, maxRetries))

		db, connErr := sqlx.Connect("postgres", c.Config.Database.DSN())
		if connErr == nil {
			c.DB = db
			logger.Info("🚀 Relational database backend layers linked successfully.")
			return nil
		}

		err = connErr
		logger.Warn(fmt.Sprintf("⚠️ Connection attempt failed: %v. Retrying in %v...", err, delay))
		time.Sleep(delay)
		delay = time.Duration(float64(delay) * multiplier)
	}

	return fmt.Errorf("exhausted all reconnection attempts: %w", err)
}

func (c *DatabaseConnection) configurePool() {
	c.DB.SetMaxOpenConns(c.Config.Database.MaxOpenConns)
	c.DB.SetMaxIdleConns(c.Config.Database.MaxIdleConns)
	c.DB.SetConnMaxLifetime(c.Config.Database.ConnMaxLifetime)
	c.DB.SetConnMaxIdleTime(c.Config.Database.ConnMaxIdleTime)
}

func (c *DatabaseConnection) Close() error {
	if c.DB != nil {
		return c.DB.Close()
	}
	return nil
}

func (c *DatabaseConnection) GetStats() map[string]interface{} {
	stats := c.DB.Stats()
	return map[string]interface{}{
		"max_open_connections": c.Config.Database.MaxOpenConns,
		"open_connections":     stats.OpenConnections,
		"idle_connections":     stats.Idle,
		"in_use_connections":   stats.InUse,
	}
}

func (c *DatabaseConnection) IsHealthy(ctx context.Context) bool {
	if c.DB == nil {
		return false
	}
	return c.DB.PingContext(ctx) == nil
}

func (c *DatabaseConnection) GetDetailedHealth(ctx context.Context) DetailedHealth {
	stats := c.DB.Stats()
	health := DetailedHealth{
		IsHealthy:  false,
		OpenConns:  stats.OpenConnections,
		IdleConns:  stats.Idle,
		InUseConns: stats.InUse,
	}

	start := time.Now()
	var dummy int
	// Neon optimized swift check query execution
	err := c.DB.QueryRowContext(ctx, "SELECT 1").Scan(&dummy)
	if err == nil {
		health.IsHealthy = true
		health.Latency = time.Since(start)
	}

	return health
}
