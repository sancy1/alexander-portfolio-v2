// File: internal/domain/interfaces/repositories.go
package interfaces

import (
	"context"
	"github.com/google/uuid"
	"github.com/sancy1/alexander-portfolio-v2/services/notification-service/internal/domain/entities"
)

// NotificationRepository specifies structural mapping interfaces handling notification mutations.
type NotificationRepository interface {
	Create(ctx context.Context, notification *entities.Notification) error
	GetByID(ctx context.Context, id uuid.UUID, userID uuid.UUID) (*entities.Notification, error)
	GetByUserID(ctx context.Context, userID uuid.UUID, limit, offset int) ([]entities.Notification, int64, error)
	GetUnreadCount(ctx context.Context, userID uuid.UUID) (int64, error)
	MarkAsRead(ctx context.Context, id uuid.UUID, userID uuid.UUID) error
	Archive(ctx context.Context, id uuid.UUID, userID uuid.UUID) error
	SoftDelete(ctx context.Context, id uuid.UUID, userID uuid.UUID) error
	Update(ctx context.Context, notification *entities.Notification) error
}

// OutboxRepository handles intermediate tracking of local transactional outbox tables.
type OutboxRepository interface {
	Create(ctx context.Context, message *entities.OutboxMessage) error
	GetPending(ctx context.Context, limit int) ([]entities.OutboxMessage, error)
	MarkAsProcessed(ctx context.Context, id uuid.UUID) error
	MarkAsFailed(ctx context.Context, id uuid.UUID, err string, retryCount int) error
}

// IdempotencyRepository secures downstream ingest operations from duplicated messages.
type IdempotencyRepository interface {
	Exists(ctx context.Context, key string) (bool, error)
	Store(ctx context.Context, key string, ttlSeconds int) error
}
