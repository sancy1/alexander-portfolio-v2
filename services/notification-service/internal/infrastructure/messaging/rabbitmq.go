// // infrastructure/messaging/rabbitmq.go
package messaging

import (
	"fmt"

	"github.com/rabbitmq/amqp091-go"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/config"
)

// NewRabbitMQChannel creates a RabbitMQ connection and channel using the same logic as auth-service
// This ensures consistency across microservices
func NewRabbitMQChannel(cfg *config.RabbitMQConfig) (*amqp091.Channel, *amqp091.Connection, error) {
	if cfg == nil {
		return nil, nil, fmt.Errorf("rabbitmq config cannot be nil")
	}

	// Construct the connection URL exactly like auth-service does
	// Using AMQPS for secure connection (typical for CloudAMQP)
	url := fmt.Sprintf("amqps://%s:%s@%s:%s/%s",
		cfg.Username,
		cfg.Password,
		cfg.Host,
		cfg.Port,
		cfg.VHost,
	)

	conn, err := amqp091.Dial(url)
	if err != nil {
		return nil, nil, fmt.Errorf("failed to connect to RabbitMQ: %w", err)
	}

	ch, err := conn.Channel()
	if err != nil {
		conn.Close()
		return nil, nil, fmt.Errorf("failed to open RabbitMQ channel: %w", err)
	}

	return ch, conn, nil
}
