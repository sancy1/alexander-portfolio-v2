bash
cat > ~/Desktop/alexander-portfolio-v2/shared/event-schemas/README.md << 'EOF'
# Event Schemas for Alexander Portfolio Microservices

## Overview
This directory contains JSON Schema definitions for all events emitted across the microservices architecture.

## Event Flow
┌─────────────────┐ RabbitMQ ┌─────────────────┐
│ Auth Service │ ───────────────▶ │ Blog Service │
│ (Publisher) │ │ (Subscriber) │
└─────────────────┘ └─────────────────┘
│ │
│ Kafka │
▼ ▼
┌─────────────────┐ ┌─────────────────┐
│ Audit Service │ │ AI Service │
│ (Consumer) │ │ (Consumer) │
└─────────────────┘ └─────────────────┘

text

## Schema Directory Structure
event-schemas/
├── common/
│ └── event-base.json # Base schema for all events
├── user/
│ ├── user-registered.json # New user registration
│ ├── user-loggedin.json # User login event
│ └── user-profile-updated.json # Profile completion/update
├── admin/
│ ├── admin-created.json # Admin account creation
│ ├── admin-loggedin.json # Admin login event
│ └── admin-password-changed.json # Password change
└── security/
└── security-log-emitted.json # Security audit events

text

## Event Examples

### User Registered Event (RabbitMQ)
```json
{
  "eventId": "123e4567-e89b-12d3-a456-426614174000",
  "eventType": "user.registered",
  "eventVersion": "1.0.0",
  "timestamp": "2026-05-21T10:30:00Z",
  "source": "auth-service",
  "correlationId": "123e4567-e89b-12d3-a456-426614174001",
  "userId": "123e4567-e89b-12d3-a456-426614174002",
  "data": {
    "userId": "123e4567-e89b-12d3-a456-426614174002",
    "email": "user@example.com",
    "provider": "Google",
    "providerId": "123456789",
    "displayName": "John Doe",
    "isProfileComplete": false
  }
}
Security Log Event (Kafka)
json
{
  "eventId": "123e4567-e89b-12d3-a456-426614174003",
  "eventType": "security.log.emitted",
  "timestamp": "2026-05-21T10:30:00Z",
  "source": "auth-service",
  "data": {
    "securityEventType": "failed_login",
    "severity": "Medium",
    "ipAddress": "192.168.1.1",
    "attemptCount": 3
  }
}
Message Brokers Usage
Broker	Use Case	Events
RabbitMQ	Service-to-service communication	user., admin.
Kafka	Audit, analytics, long-term storage	security.*
Versioning
All events use semantic versioning (major.minor.patch)

Breaking changes increment major version

New optional fields increment minor version

Last Updated
2026-05-21
EOF

text

### Step 7: Verify All Files Created

```bash
cd ~/Desktop/alexander-portfolio-v2

# List all schema files
find shared/event-schemas -type f -name "*.json" | sort

# Should show:
# shared/event-schemas/common/event-base.json
# shared/event-schemas/user/user-registered.json
# shared/event-schemas/user/user-loggedin.json
# shared/event-schemas/user/user-profile-updated.json
# shared/event-schemas/admin/admin-created.json
# shared/event-schemas/admin/admin-loggedin.json
# shared/event-schemas/admin/admin-password-changed.json
# shared/event-schemas/security/security-log-emitted.json

# Count files
find shared/event-schemas -type f -name "*.json" | wc -l
# Should output: 8