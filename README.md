<div align="center">

# 🔐 Auth Service — Authentication Microservice

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=csharp&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-7.0-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-Bearer-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white)
![Coverage](https://img.shields.io/badge/coverage-85%25-brightgreen?style=for-the-badge)

### 🚀 Handles authentication, authorization, and user management for the Alexander Portfolio platform

</div>

---

# 📋 Table of Contents

- [🎯 Features](#-features)
- [🏗️ Architecture](#️-architecture)
- [🚀 Quick Start](#-quick-start)
- [📡 API Endpoints](#-api-endpoints)
- [🔐 Authentication](#-authentication)
- [🗄️ Database Schema](#️-database-schema)
- [📨 Message Brokers](#-message-brokers)
- [🧪 Testing](#-testing)
- [🐳 Docker](#-docker)
- [📊 API Documentation](#-api-documentation)
- [🔧 Troubleshooting](#-troubleshooting)

---

# 🎯 Features

## ✅ Implemented Features

| Category | Features |
|----------|----------|
| 🔐 Admin Authentication | Register, Login, Logout, JWT Tokens |
| 🌍 Social Authentication | Google OAuth, GitHub OAuth |
| 👤 Profile Management | View, Update, Avatar Upload (Cloudinary) |
| 🔑 Password Operations | Change password, Reset with admin key |
| 🗑️ Account Management | Soft delete (30 days), Hard delete, Restore |
| 🛡️ Admin Controls | Block/Unblock social users, Delete social users |
| ⚡ Security | Redis token blacklist, Rate limiting |
| 📡 Audit Logging | Kafka event streaming |
| 📨 Service Communication | RabbitMQ event publishing |

---

## 📈 Performance Metrics

| Metric | Value |
|--------|-------|
| ⚡ JWT Generation | `< 10ms` |
| 🗄️ Database Query | `< 50ms` |
| 🚀 Redis Lookup | `< 1ms` |
| 🌐 API Response (avg) | `< 100ms` |

---

# 🏗️ Architecture

## 🧩 Clean Architecture Layers

```text
┌─────────────────────────────────────────────────────────────┐
│                     API Layer (Controllers)                │
│                                                             │
│  • Request/Response handling                               │
│  • Swagger documentation                                   │
│  • JWT middleware                                          │
├─────────────────────────────────────────────────────────────┤
│                 Application Layer (CQRS)                   │
│                                                             │
│  • Commands & Queries                                      │
│  • Handlers                                                │
│  • Validators (FluentValidation)                           │
│  • DTOs                                                    │
├─────────────────────────────────────────────────────────────┤
│                    Domain Layer (Core)                     │
│                                                             │
│  • Entities (Admin, SocialUser)                            │
│  • Value Objects                                           │
│  • Enums                                                   │
│  • Domain Exceptions                                       │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                      │
│                                                             │
│  • EF Core + PostgreSQL                                    │
│  • JWT / BCrypt / AdminKey                                 │
│  • Redis caching                                           │
│  • RabbitMQ / Kafka / Outbox                               │
│  • Cloudinary / OAuth integrations                         │
└─────────────────────────────────────────────────────────────┘
```

---

## ⚙️ CQRS Pattern

```csharp
// Command (Write operation)

public class RegisterAdminCommand : IRequest<AuthResponse>
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string AdminKey { get; set; }
}

// Query (Read operation)

public class GetAdminProfileQuery : IRequest<AdminProfileResponse>
{
    public Guid AdminId { get; set; }
}
```

---

# 🚀 Quick Start

## 📋 Prerequisites

```bash
# Required

- .NET SDK 9.0
- Docker Desktop
- PostgreSQL 16 (or Neon cloud)
- Redis 7.0
- RabbitMQ 3.13
- Kafka 3.6
```

---

## 📥 Setup

```bash
# Clone repository
git clone https://github.com/sancy1/alexander-portfolio-v2.git

cd alexander-portfolio-v2/services/auth-service

# Restore packages
dotnet restore

# Build solution
dotnet build

# Run migrations
dotnet ef database update \
  --project AuthService.Infrastructure \
  --startup-project AuthService.API

# Run API
cd AuthService.API

dotnet run
```

---

## ⚙️ Configuration

Create a `.env` file inside `AuthService.API/`

```env
DATABASE_URL=Host=localhost;Port=5432;Database=portfolio_db;Username=postgres;Password=postgres;

JWT_SECRET=your-super-secret-key-minimum-32-characters

JWT_ISSUER=auth-service

JWT_AUDIENCE=portfolio-api

JWT_EXPIRY_MINUTES=360

ADMIN_MASTER_KEY=SUPER_SECRET_ADMIN_KEY_2024
```

---

# 📡 API Endpoints

## ❤️ Health Checks

| Method | Endpoint | Description |
|--------|-----------|-------------|
| GET | `/api/v1/health` | Service health |
| GET | `/api/v1/health/ping` | Ping test |
| GET | `/api/v1/health/db` | Database status |
| GET | `/health/ready` | Kubernetes readiness |

---

## 🔐 Admin Authentication

| Method | Endpoint | Auth | Description |
|--------|-----------|------|-------------|
| POST | `/api/v1/admins/register` | ❌ | Register admin |
| POST | `/api/v1/admins/login` | ❌ | Login admin |
| POST | `/api/v1/admins/logout` | ✅ | Logout + blacklist |
| GET | `/api/v1/admins/profile` | ✅ | Get profile |
| PUT | `/api/v1/admins/profile` | ✅ | Update profile |
| POST | `/api/v1/admins/avatar` | ✅ | Upload avatar |
| PUT | `/api/v1/admins/change-password` | ✅ | Change password |
| POST | `/api/v1/admins/reset-password` | ❌ | Reset with admin key |
| DELETE | `/api/v1/admins/account` | ✅ | Delete account |
| POST | `/api/v1/admins/account/restore` | ❌ | Restore account |
| GET | `/api/v1/admins/account/status` | ✅ | Deletion status |

---

## 🌍 Social Authentication

| Method | Endpoint | Auth | Description |
|--------|-----------|------|-------------|
| GET | `/api/v1/auth/google/login` | ❌ | Google OAuth |
| GET | `/api/v1/auth/github/login` | ❌ | GitHub OAuth |
| POST | `/api/v1/auth/users/complete-registration` | ❌ | Complete profile |
| GET | `/api/v1/auth/users/profile` | ✅ | Get profile |
| PUT | `/api/v1/auth/users/profile` | ✅ | Update display name |
| POST | `/api/v1/auth/users/avatar` | ✅ | Upload avatar |
| DELETE | `/api/v1/auth/users/account` | ✅ | Delete account |
| POST | `/api/v1/auth/users/account/restore` | ❌ | Restore account |

---

## 👨‍💼 Admin Social User Management

| Method | Endpoint | Auth | Description |
|--------|-----------|------|-------------|
| GET | `/api/v1/admins/admin/social-users` | ✅ | List all users |
| GET | `/api/v1/admins/admin/social-users/{id}` | ✅ | Get user by ID |
| POST | `/api/v1/admins/admin/social-users/{id}/block` | ✅ | Block user |
| POST | `/api/v1/admins/admin/social-users/{id}/unblock` | ✅ | Unblock user |
| DELETE | `/api/v1/admins/admin/social-users/{id}` | ✅ | Delete user |

---

## 📦 Outbox Management (Admin)

| Method | Endpoint | Auth | Description |
|--------|-----------|------|-------------|
| GET | `/api/v1/admin/outbox/pending` | ✅ | Pending count |
| POST | `/api/v1/admin/outbox/process` | ✅ | Process messages |
| DELETE | `/api/v1/admin/outbox/cleanup` | ✅ | Clean old messages |

---

# 🔐 Authentication

## 🪪 JWT Token Structure

```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "nameidentifier": "admin-id",
    "name": "admin-username",
    "email": "admin@example.com",
    "role": "Admin",
    "type": "admin",
    "exp": 1700000000
  }
}
```

---

## ⚡ Token Blacklisting (Redis)

```csharp
// On logout

await _tokenBlacklistService.BlacklistTokenAsync(token, expiry);

// On every request (Middleware)

var isBlacklisted =
    await _tokenBlacklistService.IsTokenBlacklistedAsync(token);

if (isBlacklisted)
    return Unauthorized();
```

---

## 🔑 Password Requirements

| Requirement | Validation |
|-------------|------------|
| 📏 Minimum Length | 8 characters |
| 🔠 Uppercase | At least 1 |
| 🔡 Lowercase | At least 1 |
| 🔢 Number | At least 1 |
| ❗ Special Character | At least 1 (`!@#$%^&*`) |

---

# 🗄️ Database Schema

## 📋 Tables

| Table | Purpose | Row Count |
|-------|----------|------------|
| 👤 Admins | Admin accounts | ~10 |
| 🌍 SocialUsers | Social user accounts | ~100 |
| 📦 OutboxMessages | Event outbox | ~1000 |

---

## 👤 Admin Schema

```sql
CREATE TABLE "Admins" (
    "Id" uuid PRIMARY KEY,
    "Username" varchar(100) UNIQUE NOT NULL,
    "Email" varchar(255) UNIQUE NOT NULL,
    "PasswordHash" varchar(255) NOT NULL,
    "Role" integer NOT NULL,
    "Status" integer NOT NULL,
    "AvatarUrl" text,
    "CreatedAt" timestamptz NOT NULL,
    "IsDeleted" boolean DEFAULT false,
    "DeletedAt" timestamptz,
    "PermanentDeleteAt" timestamptz
);
```

---

## 🌍 SocialUser Schema

```sql
CREATE TABLE "SocialUsers" (
    "Id" uuid PRIMARY KEY,
    "ProviderId" varchar(255) NOT NULL,
    "Provider" integer NOT NULL,
    "Email" varchar(255) NOT NULL,
    "DisplayName" varchar(200) NOT NULL,
    "AvatarUrl" varchar(500),
    "IsProfileComplete" boolean DEFAULT false,
    "IsAdminBlocked" boolean DEFAULT false,
    "IsDeleted" boolean DEFAULT false
);
```

---

# 📨 Message Brokers

## 🐇 RabbitMQ Events

| Event | Routing Key | Consumer |
|-------|-------------|-----------|
| 👨‍💼 Admin logged in | `admin.loggedin` | Future services |
| ✏️ User modified | `user.modified` | Blog/AI/Core services |

---

## 📡 Kafka Events

| Event | Topic | Severity |
|-------|-------|-----------|
| 🔐 Admin logged in | `security-audit-log` | Low |
| 🚪 Admin logged out | `security-audit-log` | Low |
| ❌ Failed login attempt | `security-audit-log` | Medium |
| 🔑 Password reset | `security-audit-log` | High |
| 🗑️ Account deleted | `security-audit-log` | High |
| 🚫 User blocked | `security-audit-log` | Medium |

---

## 📦 Outbox Pattern Implementation

```csharp
// Add to Outbox (same transaction as business logic)

await OutboxHelper.AddToOutboxAsync(
    _outboxRepository,
    _unitOfWork,
    "admin.loggedin",
    "admin.loggedin",
    "kafka",
    new { adminId, username, email, timestamp }
);

// Manual processing (admin endpoint)

await _outboxProcessor.ProcessPendingMessagesAsync(10);
```

---

# 🧪 Testing

## ✅ Unit Tests

```bash
# Run all unit tests
dotnet test AuthService.Tests/AuthService.Tests.csproj

# Run specific test category
dotnet test --filter "FullyQualifiedName~AdminTests"
```

---

## 📊 Test Coverage

| Layer | Coverage | Tests |
|-------|-----------|-------|
| 🧠 Domain | 90% | 45 tests |
| ⚙️ Application | 85% | 67 tests |
| 🏗️ Infrastructure | 75% | 32 tests |
| 🌐 API | 80% | 28 tests |
| 🎯 Total | 82% | 172 tests |

---

## 🔄 Integration Tests

```bash
# Run integration tests (requires Docker)
dotnet test --filter "Category=Integration"
```

---

# 🐳 Docker

## 🏗️ Build Image

```bash
docker build -t auth-service:latest -f Dockerfile .
```

---

## ▶️ Run Container

```bash
docker run -d \
  -p 5000:8080 \
  -e DATABASE_URL="Host=postgres;..." \
  -e JWT_SECRET="..." \
  --name auth-service \
  auth-service:latest
```

---

## ⚙️ Multi-stage Build

```dockerfile
# Stage 1: Build

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY . .

RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AuthService.API.dll"]
```

---

# 📊 API Documentation

## 📚 Swagger UI

Once running, access:

```text
http://localhost:5000/swagger
```

### ✨ Features

- 🔒 JWT authentication (`Authorize` button)
- 📝 Request/response schemas
- 🧪 "Try it out" testing
- 📚 Grouped by controller

---

## 📥 OpenAPI Specification

```bash
# Download OpenAPI spec

curl http://localhost:5000/swagger/v1/swagger.json > openapi.json

# Generate client SDK

npx @openapitools/openapi-generator-cli generate \
  -i openapi.json \
  -g typescript-axios
```

---

# 🔧 Troubleshooting

## ⚠️ Common Issues

| Issue | Solution |
|-------|-----------|
| ❌ Database connection fails | Check Neon/PostgreSQL is running |
| 🔐 JWT token invalid | Verify `JWT_SECRET` in `.env` |
| ⚡ Redis connection refused | Run `docker-compose up -d redis` |
| 📨 RabbitMQ not working | Check port `5672` is open |
| 🗄️ Migration errors | Run `dotnet ef database update` |

---

## 📜 Logs

```bash
# Application logs
dotnet run -- --log-level Debug

# Docker logs
docker-compose logs -f auth-service

# PostgreSQL logs
docker-compose logs postgres
```

---

# 📈 Performance Optimization

## ⚡ Caching Strategy

| Cache | TTL | Purpose |
|-------|-----|----------|
| 🔐 JWT blacklist | Token expiry | Revoked tokens |
| 👤 User profile | 5 min | Reduce DB calls |
| 🛡️ Rate limiting | 1 min | API protection |

---

## 🗄️ Database Indexes

```sql
-- Critical indexes for performance

CREATE INDEX CONCURRENTLY ix_admins_email
ON "Admins" ("Email");

CREATE INDEX CONCURRENTLY ix_socialusers_email
ON "SocialUsers" ("Email");

CREATE INDEX CONCURRENTLY ix_outbox_unprocessed
ON "OutboxMessages" ("CreatedAt")
WHERE "ProcessedAt" IS NULL;
```

---

<div align="center">

# ⬆ Back to Top

</div>