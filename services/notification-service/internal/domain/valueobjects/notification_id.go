// File: internal/domain/valueobjects/notification_id.go
package valueobjects

import (
	"database/sql/driver"
	"fmt"
	"github.com/google/uuid"
)

// NotificationID acts as an immutable value object type layer wrapped around standard tracking primitives.
type NotificationID struct {
	value uuid.UUID
}

func NewNotificationID() NotificationID {
	return NotificationID{value: uuid.New()}
}

func ParseNotificationID(s string) (NotificationID, error) {
	id, err := uuid.Parse(s)
	if err != nil {
		return NotificationID{}, fmt.Errorf("invalid semantic notification identity signature format: %w", err)
	}
	return NotificationID{value: id}, nil
}

func MustParseNotificationID(s string) NotificationID {
	id, err := ParseNotificationID(s)
	if err != nil {
		panic(err)
	}
	return id
}

func (n NotificationID) String() string {
	return n.value.String()
}

func (n NotificationID) UUID() uuid.UUID {
	return n.value
}

func (n NotificationID) IsZero() bool {
	return n.value == uuid.Nil
}

// Value transforms value object domains into driver ready insertion components.
func (n NotificationID) Value() (driver.Value, error) {
	if n.IsZero() {
		return nil, nil
	}
	return n.value.String(), nil
}

// Scan intercepts inbound raw values safely for native typed model mapping conversions.
func (n *NotificationID) Scan(value interface{}) error {
	if value == nil {
		n.value = uuid.Nil
		return nil
	}

	switch v := value.(type) {
	case []byte:
		id, err := uuid.ParseBytes(v)
		if err != nil {
			return err
		}
		n.value = id
	case string:
		id, err := uuid.Parse(v)
		if err != nil {
			return err
		}
		n.value = id
	case uuid.UUID:
		n.value = v
	default:
		return fmt.Errorf("type assertion error: cannot scan raw native storage driver entity type %T directly into NotificationID package context", value)
	}
	return nil
}
