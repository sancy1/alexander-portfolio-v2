// infrastructure/messaging/kafka.go
package messaging

import (
	"crypto/tls"
	"crypto/x509"
	"fmt"
	"os"
	"time"

	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/config"
	"github.com/segmentio/kafka-go"
	"github.com/segmentio/kafka-go/sasl/scram"
)

func NewKafkaReader(cfg *config.KafkaConfig) (*kafka.Reader, error) {
	// 1. Load the CA certificate
	caCert, err := os.ReadFile("ca.pem")
	if err != nil {
		return nil, fmt.Errorf("failed to read ca.pem: %w", err)
	}

	caCertPool := x509.NewCertPool()
	if ok := caCertPool.AppendCertsFromPEM(caCert); !ok {
		return nil, fmt.Errorf("failed to append ca.pem to cert pool")
	}

	// 2. Configure SASL mechanism
	mechanism, err := scram.Mechanism(scram.SHA256, cfg.Username, cfg.Password)
	if err != nil {
		return nil, err
	}

	// 3. Configure TLS
	tlsConfig := &tls.Config{
		RootCAs:    caCertPool,
		MinVersion: tls.VersionTLS12,
	}

	dialer := &kafka.Dialer{
		Timeout:       10 * time.Second,
		DualStack:     true,
		SASLMechanism: mechanism,
		TLS:           tlsConfig,
	}

	return kafka.NewReader(kafka.ReaderConfig{
		Brokers: cfg.Brokers,
		GroupID: cfg.ConsumerGroup,
		Topic:   cfg.Topic,
		Dialer:  dialer,
	}), nil
}
