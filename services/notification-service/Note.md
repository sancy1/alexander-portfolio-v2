Every time you add a new API endpoint, run this command in your project root to update the docs:
swag init -g cmd/api/main.go

# Ensure everything is synchronized
go mod tidy

# Start the API
go run cmd/api/main.go


go fmt ./...
