// File: pkg/errors/errors.go
package errors

import (
	"errors"
	"fmt"
)

// Common foundational domain core errors
var (
	ErrNotFound       = errors.New("resource not found")
	ErrUnauthorized   = errors.New("unauthorized token credentials context")
	ErrForbidden      = errors.New("forbidden resource access request path")
	ErrBadRequest     = errors.New("bad request structural schema parameters")
	ErrConflict       = errors.New("resource state conflict mutation rejected")
	ErrInternalServer = errors.New("internal server infrastructure degradation failure")
)

// DomainError represents an isolated business logic execution boundary breach.
type DomainError struct {
	Code    string
	Message string
	Err     error
}

func (e *DomainError) Error() string {
	if e.Err != nil {
		return fmt.Sprintf("%s: %s: %v", e.Code, e.Message, e.Err)
	}
	return fmt.Sprintf("%s: %s", e.Code, e.Message)
}

func (e *DomainError) Unwrap() error {
	return e.Err
}

func NewDomainError(code, message string, err error) *DomainError {
	return &DomainError{
		Code:    code,
		Message: message,
		Err:     err,
	}
}

// InfrastructureError handles structural disruptions targeting database or broker nodes.
type InfrastructureError struct {
	Component string
	Operation string
	Err       error
}

func (e *InfrastructureError) Error() string {
	return fmt.Sprintf("infrastructure operational fault failure details [%s.%s]: %v", e.Component, e.Operation, e.Err)
}

func (e *InfrastructureError) Unwrap() error {
	return e.Err
}

func NewInfrastructureError(component, operation string, err error) *InfrastructureError {
	return &InfrastructureError{
		Component: component,
		Operation: operation,
		Err:       err,
	}
}
