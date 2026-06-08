// File: internal/application/dto/payloads.go
package dto

import "time"

type AdminLoggedInPayload struct {
	UserID      string    `json:"userId"`
	Email       string    `json:"email"`
	DisplayName string    `json:"displayName"`
	LoginMethod string    `json:"loginMethod"`
	LoginTime   time.Time `json:"loginTime"`
	ClientIP    string    `json:"clientIp"`
	UserAgent   string    `json:"userAgent"`
}

type AdminLoggedOutPayload struct {
	UserID     string    `json:"userId"`
	Email      string    `json:"email"`
	LogoutTime time.Time `json:"logoutTime"`
	SessionID  string    `json:"sessionId"`
}

type AdminPasswordChangedPayload struct {
	UserID    string    `json:"userId"`
	Email     string    `json:"email"`
	ChangedAt time.Time `json:"changedAt"`
	RequestIP string    `json:"requestIp"`
}

type UserRegisteredPayload struct {
	UserID       string    `json:"userId"`
	Email        string    `json:"email"`
	DisplayName  string    `json:"displayName"`
	Provider     string    `json:"provider"`
	RegisteredAt time.Time `json:"registeredAt"`
}

type UserLoggedInPayload struct {
	UserID      string    `json:"userId"`
	Email       string    `json:"email"`
	DisplayName string    `json:"displayName"`
	Provider    string    `json:"provider"`
	LoginTime   time.Time `json:"loginTime"`
	ClientIP    string    `json:"clientIp"`
	UserAgent   string    `json:"userAgent"`
}
