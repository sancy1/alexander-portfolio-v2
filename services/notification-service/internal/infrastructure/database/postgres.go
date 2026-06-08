// File: internal/infrastructure/database/postgres.go
package database

import (
	"context"
	"fmt"
	"time" // Added missing time package

	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/config"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/migrations"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/pkg/logger"
)

// PostgresDB cleanly extends our unified DatabaseConnection pool manager
type PostgresDB struct {
	Conn *DatabaseConnection
}

// NewPostgresDB initializes the enhanced sqlx connection manager and auto-executes structural migrations
func NewPostgresDB(cfg *config.Config) (*PostgresDB, error) {
	// Initialize our robust database connection manager
	dbConn, err := NewDatabaseConnection(cfg)
	if err != nil {
		return nil, fmt.Errorf("failed to spin up core database connectivity engine: %w", err)
	}

	db := &PostgresDB{
		Conn: dbConn,
	}

	// Create execution context boundary for transactional schema assembly passes
	ctx, cancel := context.WithTimeout(context.Background(), 15*time.Second)
	defer cancel()

	// Execute transactional structural setup
	if err := db.Migrate(ctx); err != nil {
		db.Close()
		return nil, fmt.Errorf("schema auto-migration engine execution pass aborted: %w", err)
	}

	return db, nil
}

// Migrate reads your embedded initialization schema scripts and registers objects safely on Neon
func (db *PostgresDB) Migrate(ctx context.Context) error {
	logger.Info("🔄 Inspecting data storage layer structures. Syncing migrations...")

	// Execute your raw schema DDL script directly through our active sqlx pool manager
	_, err := db.Conn.DB.ExecContext(ctx, migrations.InitSchemaSQL)
	if err != nil {
		return fmt.Errorf("failed executing initialization schema sequence: %w", err)
	}

	logger.Info("✅ Database schema is verified and fully up-to-date.")
	return nil
}

// Close gracefully terminates the shared connection pool
func (db *PostgresDB) Close() error {
	if db.Conn != nil {
		return db.Conn.Close()
	}
	return nil
}
