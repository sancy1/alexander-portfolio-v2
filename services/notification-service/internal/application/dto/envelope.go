// File: internal/application/dto/envelope.go
package dto

import (
	"encoding/json"
	"time"

	"github.com/google/uuid"
)

// EventEnvelope represents the standard CloudEvents-compliant structure matching the .NET outbox relay.
type EventEnvelope struct {
	EventID       string          `json:"eventId"`
	EventType     string          `json:"eventType"`
	SourceService string          `json:"sourceService"`
	Timestamp     time.Time       `json:"timestamp"`
	Version       string          `json:"version"`
	UserID        *string         `json:"userId"`
	UserType      *string         `json:"userType"`
	CorrelationID *string         `json:"correlationId"`
	CausationID   *string         `json:"causationId"`
	Payload       json.RawMessage `json:"payload"`
}

func (e *EventEnvelope) GetUserID() *uuid.UUID {
	if e.UserID == nil || *e.UserID == "" {
		return nil
	}
	id, err := uuid.Parse(*e.UserID)
	if err != nil {
		return nil
	}
	return &id
}

func (e *EventEnvelope) GetUserType() string {
	if e.UserType == nil {
		return ""
	}
	return *e.UserType
}
