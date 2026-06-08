
// // @title Notification Service API
// // @version 1.0
// // @description Core notification handling engine for Alexander Portfolio.
// // @BasePath /

// package main

// import (
// 	"context"
// 	"os"
// 	"os/signal"
// 	"syscall"
// 	"time"

// 	"github.com/joho/godotenv"
// 	amqp091 "github.com/rabbitmq/amqp091-go"
// 	"github.com/segmentio/kafka-go"
// 	"go.uber.org/zap"

// 	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/config"
// 	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/infrastructure/cache"
// 	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/infrastructure/messaging"
// 	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/server"
// 	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/pkg/logger"
// )

// func main() {
// 	if err := logger.Init("development"); err != nil {
// 		panic("Failed to initialize logger: " + err.Error())
// 	}
// 	defer logger.Sync()
// 	_ = godotenv.Load()

// 	logger.Info("Starting Resilient Notification Service Runtime Engine...")

// 	cfg, err := config.Load("")
// 	if err != nil {
// 		logger.Fatal("Failed to load config", zap.Error(err))
// 	}

// 	// 1. Initialize Kafka
// 	var kafkaReader *kafka.Reader
// 	if len(cfg.Kafka.Brokers) > 0 && cfg.Kafka.Brokers[0] != "" {
// 		logger.Info("Initializing Kafka consumer engine",
// 			zap.Strings("brokers", cfg.Kafka.Brokers),
// 			zap.String("topic", cfg.Kafka.Topic),
// 		)

// 		kafkaReader, err = messaging.NewKafkaReader(&cfg.Kafka)
// 		if err != nil {
// 			logger.Fatal("Failed to initialize Kafka consumer", zap.Error(err))
// 		}

// 		logger.Info("Kafka consumer engine successfully initialized",
// 			zap.Strings("brokers", cfg.Kafka.Brokers),
// 			zap.String("topic", cfg.Kafka.Topic),
// 			zap.String("consumer_group", cfg.Kafka.ConsumerGroup),
// 		)

// 		go func() {
// 			logger.Info("Kafka event listener loop started - waiting for messages...")
// 			for {
// 				m, err := kafkaReader.ReadMessage(context.Background())
// 				if err != nil {
// 					logger.Error("Kafka read error", zap.Error(err))
// 					time.Sleep(1 * time.Second)
// 					continue
// 				}
// 				logger.Info("Message received from Kafka",
// 					zap.Int64("offset", m.Offset),
// 					zap.String("key", string(m.Key)),
// 					zap.String("value", string(m.Value)),
// 				)
// 			}
// 		}()
// 	}

// 	// 2. Initialize RabbitMQ
// 	var rabbitMQChannel *amqp091.Channel
// 	var rabbitMQConn *amqp091.Connection
// 	if cfg.RabbitMQ.Host != "" {
// 		rabbitMQChannel, rabbitMQConn, err = messaging.NewRabbitMQChannel(&cfg.RabbitMQ)
// 		if err != nil {
// 			logger.Warn("Failed to initialize RabbitMQ, continuing without it", zap.Error(err))
// 		} else {
// 			logger.Info("RabbitMQ connection established successfully")
// 		}
// 	}

// 	// 3. Initialize Redis
// 	redisClient, err := cache.NewRedisClient(&cfg.Redis, nil)
// 	if err != nil {
// 		logger.Warn("Failed to initialize Redis client, continuing without cache", zap.Error(err))
// 		redisClient = nil
// 	}

// 	// 4. Server Init
// 	var srv *server.Server
// 	if redisClient != nil && rabbitMQChannel != nil {
// 		srv, err = server.NewServerWithAll(cfg, kafkaReader, rabbitMQChannel, redisClient)
// 	} else if rabbitMQChannel != nil {
// 		srv, err = server.NewServerWithRabbitMQ(cfg, kafkaReader, rabbitMQChannel)
// 	} else {
// 		srv, err = server.NewServer(cfg, kafkaReader)
// 	}

// 	if err != nil {
// 		logger.Fatal("Failed to initialize server", zap.Error(err))
// 	}

// 	// 5. Start Server
// 	go func() {
// 		logger.Info("HTTP server starting", zap.String("port", cfg.Server.Port))
// 		if err := srv.Start(); err != nil {
// 			logger.Fatal("Server error", zap.Error(err))
// 		}
// 	}()

// 	// 6. Graceful Shutdown
// 	quit := make(chan os.Signal, 1)
// 	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
// 	<-quit

// 	logger.Info("OS signal received. Initiating graceful shutdown...")

// 	if rabbitMQConn != nil {
// 		_ = rabbitMQConn.Close()
// 	}
// 	if kafkaReader != nil {
// 		_ = kafkaReader.Close()
// 	}

// 	ctx, cancel := context.WithTimeout(context.Background(), 20*time.Second)
// 	defer cancel()

// 	_ = srv.Shutdown(ctx)
// 	logger.Info("Notification service terminated safely.")
// }



































// @title Notification Service API
// @version 1.0
// @description Core notification handling engine for Alexander Portfolio.
// @BasePath /

package main

import (
	"context"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/joho/godotenv"
	amqp091 "github.com/rabbitmq/amqp091-go"
	"github.com/segmentio/kafka-go"
	"go.uber.org/zap"

	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/config"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/infrastructure/cache"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/infrastructure/messaging"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/server"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/pkg/logger"
)

func main() {
	if err := logger.Init("development"); err != nil {
		panic("Failed to initialize logger: " + err.Error())
	}
	defer logger.Sync()
	_ = godotenv.Load()

	logger.Info("Starting Resilient Notification Service Runtime Engine...")

	cfg, err := config.Load("")
	if err != nil {
		logger.Fatal("Failed to load config", zap.Error(err))
	}

	// 1. Initialize Kafka
	var kafkaReader *kafka.Reader
	if len(cfg.Kafka.Brokers) > 0 && cfg.Kafka.Brokers[0] != "" {
		logger.Info("Initializing Kafka consumer engine",
			zap.Strings("brokers", cfg.Kafka.Brokers),
			zap.String("topic", cfg.Kafka.Topic),
		)

		kafkaReader, err = messaging.NewKafkaReader(&cfg.Kafka)
		if err != nil {
			logger.Fatal("Failed to initialize Kafka consumer", zap.Error(err))
		}

		logger.Info("Kafka consumer engine successfully initialized",
			zap.Strings("brokers", cfg.Kafka.Brokers),
			zap.String("topic", cfg.Kafka.Topic),
			zap.String("consumer_group", cfg.Kafka.ConsumerGroup),
		)

		go func() {
			logger.Info("Kafka event listener loop started - waiting for messages...")
			for {
				m, err := kafkaReader.ReadMessage(context.Background())
				if err != nil {
					logger.Error("Kafka read error", zap.Error(err))
					time.Sleep(1 * time.Second)
					continue
				}
				logger.Info("Message received from Kafka",
					zap.Int64("offset", m.Offset),
					zap.String("key", string(m.Key)),
					zap.String("value", string(m.Value)),
				)
			}
		}()
	}

	// 2. Initialize RabbitMQ
	var rabbitMQChannel *amqp091.Channel
	var rabbitMQConn *amqp091.Connection
	if cfg.RabbitMQ.Host != "" {
		rabbitMQChannel, rabbitMQConn, err = messaging.NewRabbitMQChannel(&cfg.RabbitMQ)
		if err != nil {
			logger.Warn("Failed to initialize RabbitMQ, continuing without it", zap.Error(err))
		} else {
			logger.Info("RabbitMQ connection established successfully")
		}
	}

	// 3. Initialize Redis (Silent Failure)
	redisClient, _ := cache.NewRedisClient(&cfg.Redis, nil)
	if redisClient != nil {
		logger.Info("Redis client initialized successfully", zap.String("addr", cfg.Redis.Addr()))
	}

	// 4. Server Init
	var srv *server.Server
	if redisClient != nil && rabbitMQChannel != nil {
		srv, err = server.NewServerWithAll(cfg, kafkaReader, rabbitMQChannel, redisClient)
	} else if rabbitMQChannel != nil {
		srv, err = server.NewServerWithRabbitMQ(cfg, kafkaReader, rabbitMQChannel)
	} else {
		srv, err = server.NewServer(cfg, kafkaReader)
	}

	if err != nil {
		logger.Fatal("Failed to initialize server", zap.Error(err))
	}

	// 5. Start Server
	go func() {
		logger.Info("HTTP server starting", zap.String("port", cfg.Server.Port))
		if err := srv.Start(); err != nil {
			logger.Fatal("Server error", zap.Error(err))
		}
	}()

	// 6. Graceful Shutdown
	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	logger.Info("OS signal received. Initiating graceful shutdown...")

	if rabbitMQConn != nil {
		_ = rabbitMQConn.Close()
	}
	if kafkaReader != nil {
		_ = kafkaReader.Close()
	}

	ctx, cancel := context.WithTimeout(context.Background(), 20*time.Second)
	defer cancel()

	_ = srv.Shutdown(ctx)
	logger.Info("Notification service terminated safely.")
}