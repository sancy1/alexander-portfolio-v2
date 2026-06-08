package server

import (
	"context"
	"net/http"

	"github.com/gin-gonic/gin"
	amqp091 "github.com/rabbitmq/amqp091-go"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/api/middleware"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/api/routes"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/config"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/infrastructure/cache"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/infrastructure/database"
	"github.com/segmentio/kafka-go"
)

type Server struct {
	http        *http.Server
	db          *database.DatabaseConnection
	redis       *cache.RedisClient
	kafkaReader *kafka.Reader
	rbChan      *amqp091.Channel
}

// newBaseServer is now resilient: it will start even if Redis fails.
func newBaseServer(cfg *config.Config, kr *kafka.Reader, rbChan *amqp091.Channel) (*Server, error) {
	// 1. Initialize database (Critical)
	db, err := database.NewDatabaseConnection(cfg)
	if err != nil {
		return nil, err
	}

	// 2. Initialize Redis (Non-critical / Resilient)
	redisClient, err := cache.NewRedisClient(&cfg.Redis, nil)
	if err != nil {
		println("WARNING: Redis unavailable, starting in degraded mode: " + err.Error())
		redisClient = nil
	}

	// 3. Setup Gin
	engine := gin.New()
	engine.Use(middleware.Recovery(), middleware.Logging())

	// 4. Setup routes - Passing all 6 required arguments
	routes.SetupRoutes(engine, cfg, db, redisClient, kr, rbChan)

	return &Server{
		http:        &http.Server{Addr: ":" + cfg.Server.Port, Handler: engine},
		db:          db,
		redis:       redisClient,
		kafkaReader: kr,
		rbChan:      rbChan,
	}, nil
}

func NewServer(cfg *config.Config, kr *kafka.Reader) (*Server, error) {
	return newBaseServer(cfg, kr, nil)
}

func NewServerWithRabbitMQ(cfg *config.Config, kr *kafka.Reader, rbChan *amqp091.Channel) (*Server, error) {
	return newBaseServer(cfg, kr, rbChan)
}

func NewServerWithAll(cfg *config.Config, kr *kafka.Reader, rmqChannel *amqp091.Channel, rc *cache.RedisClient) (*Server, error) {
	// Note: We use the rmqChannel and rc passed in
	return newBaseServer(cfg, kr, rmqChannel)
}

func (s *Server) Start() error {
	println("Server listening on " + s.http.Addr)
	return s.http.ListenAndServe()
}

func (s *Server) Shutdown(ctx context.Context) error {
	if s.db != nil { s.db.Close() }
	if s.redis != nil { s.redis.Close() }
	if s.kafkaReader != nil { s.kafkaReader.Close() }
	if s.rbChan != nil { _ = s.rbChan.Close() }
	return s.http.Shutdown(ctx)
}