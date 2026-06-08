// File: internal/domain/entities/notification.go
package entities

import (
	"github.com/google/uuid"
	"time"
)

// Priority string-mapped constants
const (
	PriorityLow    = "low"
	PriorityNormal = "normal"
	PriorityHigh   = "high"
	PriorityUrgent = "urgent"
)

// User context categorization values
const (
	UserTypeAdmin      = "admin"
	UserTypeSocialUser = "social-user"
)

// Outbox tracking lifecycle flags
const (
	OutboxStatePending   = "PENDING"
	OutboxStateProcessed = "PROCESSED"
	OutboxStateFailed    = "FAILED"
)

// Notification represents the primary entity model for storage engines.
type Notification struct {
	ID               uuid.UUID              `json:"id" gorm:"type:uuid;primaryKey"`
	UserID           uuid.UUID              `json:"userId" gorm:"type:uuid;not null;index"`
	UserType         string                 `json:"userType" gorm:"type:varchar(50);not null"`
	SourceService    string                 `json:"sourceService" gorm:"type:varchar(100);not null"`
	EventType        string                 `json:"eventType" gorm:"type:varchar(200);not null"`
	NotificationType string                 `json:"notificationType" gorm:"type:varchar(100);not null"`
	Title            string                 `json:"title" gorm:"type:varchar(255);not null"`
	Message          string                 `json:"message" gorm:"type:text;not null"`
	Metadata         map[string]interface{} `json:"metadata" gorm:"serializer:json;type:jsonb"`
	Priority         string                 `json:"priority" gorm:"type:varchar(20);default:'normal'"`
	IsRead           bool                   `json:"isRead" gorm:"default:false;not null"`
	IsArchived       bool                   `json:"isArchived" gorm:"default:false;not null"`
	IsDeleted        bool                   `json:"isDeleted" gorm:"default:false;not null"`
	CreatedAt        time.Time              `json:"createdAt" gorm:"not null;index"`
	ReadAt           *time.Time             `json:"readAt,omitempty"`
	ArchivedAt       *time.Time             `json:"archivedAt,omitempty"`
	DeletedAt        *time.Time             `json:"deletedAt,omitempty"`
	AutoDeleteAt     *time.Time             `json:"autoDeleteAt,omitempty"`
}

func (Notification) TableName() string {
	return "notifications"
}

// OutboxMessage holds outbound tracking entries inside transactional blocks.
type OutboxMessage struct {
	ID          uuid.UUID  `gorm:"type:uuid;primaryKey"`
	EventType   string     `gorm:"type:varchar(255);not null"`
	Payload     string     `gorm:"type:text;not null"`
	Broker      string     `gorm:"type:varchar(50);not null;default:'kafka'"`
	Status      string     `gorm:"type:varchar(50);not null;default:'PENDING'"`
	RetryCount  int        `gorm:"type:integer;not null;default:0"`
	Error       *string    `gorm:"type:text"`
	CreatedAt   time.Time  `gorm:"not null;index"`
	ProcessedAt *time.Time `gorm:"index"`
}

func (OutboxMessage) TableName() string {
	return "outbox_messages"
}

func (n *Notification) IsValidPriority() bool {
	switch n.Priority {
	case PriorityLow, PriorityNormal, PriorityHigh, PriorityUrgent:
		return true
	}
	return false
}

func (n *Notification) MarkAsRead() {
	now := time.Now().UTC()
	n.IsRead = true
	n.ReadAt = &now
}

func (n *Notification) Archive() {
	now := time.Now().UTC()
	n.IsArchived = true
	n.ArchivedAt = &now
}

func (n *Notification) SoftDelete() {
	now := time.Now().UTC()
	n.IsDeleted = true
	n.DeletedAt = &now
}
