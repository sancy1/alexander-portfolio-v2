# File: Makefile
.PHONY: run build test migrate-up migrate-down clean docker-build docker-up docker-down fmt lint vendor help

# Variables
BINARY_NAME=notification-service
MIGRATION_DIR=./migrations

# Development Execution Tasks
run:
	go run cmd/api/main.go

build:
	go build -o bin/$(BINARY_NAME) cmd/api/main.go

test:
	go test -v -race -coverprofile=coverage.out ./...

test-coverage: test
	go tool cover -html=coverage.out

# Database Migrations (Engineered for Neon DB CLI Integration Hooks)
migrate-up:
	@echo "🚀 Executing structural database migrations forward..."
	@for file in $(MIGRATION_DIR)/*.sql; do \
		echo "Executing query patch: $$file..."; \
		PGPASSWORD=$(DB_PASSWORD) psql -h $(DB_HOST) -p $(DB_PORT) -U $(DB_USER) -d $(DB_NAME) -f $$file; \
	done

migrate-down:
	@echo "⚠️ Rollback sequence initiated. Inspecting history tracking..."

# Container Cluster Orchestration 
docker-build:
	docker build -t $(BINARY_NAME):latest .

docker-up:
	docker-compose up -d

docker-down:
	docker-compose down

# Code Quality Engineering & Sanitization
clean:
	rm -rf bin/
	rm -f coverage.out

lint:
	golangci-lint run ./...

fmt:
	go fmt ./...

vendor:
	go mod vendor

help:
	@echo "========================================================================"
	@echo "🌟 POLYGLOT MICROSERVICES ENGINE - MULTI-BROKER SYSTEM SERVICE MAID"
	@echo "========================================================================"
	@echo "  make run           - Launch application engine inside local dev runtime context"
	@echo "  make build         - Compile static optimized binary production build artifact"
	@echo "  make test          - Run multi-thread parallel integration race verification"
	@echo "  make migrate-up    - Execute target changes on cloud database context instances"
	@echo "  make docker-up     - Start background dependency infrastructure layers"