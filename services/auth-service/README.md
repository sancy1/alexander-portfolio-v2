<div align="center">
  
# 🏗️ Alexander Portfolio - Microservices Architecture

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=csharp&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-7.0-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3.13-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)
![Kafka](https://img.shields.io/badge/Apache%20Kafka-3.6-231F20?style=for-the-badge&logo=apachekafka&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-25.0-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![Nx](https://img.shields.io/badge/Nx-19.0-143055?style=for-the-badge&logo=nx&logoColor=white)
![GitHub Actions](https://img.shields.io/badge/GitHub%20Actions-CI/CD-2088FF?style=for-the-badge&logo=githubactions&logoColor=white)

</div>

---

## 📋 Table of Contents

- [🏗️ Architecture Overview](#️-architecture-overview)
- [📁 Project Structure](#-project-structure)
- [🚀 Quick Start](#-quick-start)
- [🐳 Docker Setup](#-docker-setup)
- [🔧 CI/CD Pipeline](#-cicd-pipeline)
- [🛡️ Security Features](#️-security-features)
- [📡 Message Brokers](#-message-brokers)
- [🚦 API Gateway](#-api-gateway)
- [🔑 Environment Variables](#-environment-variables)
- [📊 Service Status](#-service-status)

---

## 🏗️ Architecture Overview

This project implements a **production-ready microservices architecture** following  
**Clean Architecture** and **Domain-Driven Design (DDD)** principles.

---

### 🌐 System Architecture

```mermaid
graph TB

%% =======================
%% Client Layer
%% =======================
subgraph CLIENT["👥 Client Layer"]
    A[React Frontend]
    B[Mobile App]
end

%% =======================
%% API Gateway
%% =======================
subgraph GATEWAY["🌐 API Gateway"]
    C[NGINX Reverse Proxy]
end

%% =======================
%% Microservices
%% =======================
subgraph SERVICES["⚙️ Microservices"]
    D[Auth Service<br/>.NET 9]
    E[Blog Service<br/>Ruby on Rails]
    F[AI Service<br/>FastAPI]
    G[Core Service<br/>Go Gin]
end

%% =======================
%% Message Brokers
%% =======================
subgraph MESSAGING["📨 Message Brokers"]
    H[RabbitMQ<br/>Command Bus]
    I[Apache Kafka<br/>Event Streaming]
end

%% =======================
%% Data Layer
%% =======================
subgraph DATA["🗄️ Data Layer"]
    J[(PostgreSQL)]
    K[(MongoDB)]
    L[(Redis Cache)]
end

%% =======================
%% Connections
%% =======================

A --> C
B --> C

C --> D
C --> E
C --> F
C --> G

D --> H
D --> I
E --> H
F --> I
G --> H

D --> J
D --> L
E --> K
F --> J
G --> J
```

---

### 🧠 Architecture Highlights

- 🧩 **Microservices-based design**
- 🏛️ **Clean Architecture per service**
- 📦 **Event-driven communication**
- ⚡ **Low coupling via API Gateway**
- 📨 **Async messaging (RabbitMQ + Kafka)**
- 🗄️ **Polyglot persistence (SQL + NoSQL)**
- 🚀 **Scalable cloud-native structure**

---

### 🔄 Data Flow Summary

1. Client sends request → API Gateway  
2. Gateway routes to correct microservice  
3. Service executes business logic  
4. Events emitted via RabbitMQ/Kafka  
5. Data stored in appropriate database  
6. Cache layer (Redis) improves performance  

# 🚀 Alexander Portfolio V2

> A modern **microservices-based portfolio platform** built with scalable architecture, enterprise-grade security, event-driven communication, and cloud-native deployment.

---

# 🎯 Key Features

| Feature | ✅ Implementation |
|----------|------------------|
| 🔐 Authentication | JWT with Redis blacklisting, OAuth2 (Google/GitHub) |
| 🛡️ Authorization | Role-based access control (Admin/User) |
| 🌐 API Gateway | NGINX reverse proxy with routing |
| 📨 Message Queue | RabbitMQ for service communication |
| 📡 Event Streaming | Apache Kafka for audit logs |
| ⚡ Caching | Redis for token blacklist & rate limiting |
| 🗄️ Database | PostgreSQL (Neon cloud) + MongoDB |
| ☁️ File Storage | Cloudinary for avatars |
| 🚀 CI/CD | GitHub Actions + Docker + Render |
| 🧩 Monorepo | Nx for build orchestration |

---

# 📁 Project Structure

```text
alexander-portfolio-v2/
├── .github/workflows/                # 🚀 CI/CD pipelines
│   ├── deploy-auth-service.yml
│   └── deploy-api-gateway.yml
│
├── api-gateway/                      # 🌐 NGINX API Gateway
│   ├── Dockerfile
│   └── nginx.conf
│
├── services/
│   ├── auth-service/                 # ✅ Authentication Service
│   │   ├── AuthService.API/          # Controllers, Middleware
│   │   ├── AuthService.Application/  # Commands, Queries, Handlers
│   │   ├── AuthService.Domain/       # Entities, Value Objects
│   │   ├── AuthService.Infrastructure/ # Repositories, Messaging
│   │   └── AuthService.Tests/        # Unit & Integration Tests
│   │
│   ├── blog-service/                 # ⏳ Coming Soon
│   ├── ai-service/                   # ⏳ Coming Soon
│   └── core-info-service/            # ⏳ Coming Soon
│
├── shared/
│   └── event-schemas/                # 📦 JSON schemas for events
│
├── docker-compose.yml                # 🐳 Local development
├── render.yaml                       # ☁️ Render deployment config
└── nx.json                           # 🧩 Nx monorepo config
```

---

# 🚀 Quick Start

## 📋 Prerequisites

```bash
# Required versions

- Docker Desktop 4.25+
- .NET SDK 9.0
- Node.js 20+ (for Nx)
- Git 2.40+
```

---

## 📥 Clone & Setup

```bash
# Clone repository
git clone https://github.com/sancy1/alexander-portfolio-v2.git

cd alexander-portfolio-v2

# Install Nx globally
npm install -g nx

# Restore .NET packages
cd services/auth-service

dotnet restore
```

---

## ▶️ Run Locally

### 🐳 Terminal 1 — Start Infrastructure

```bash
docker-compose up -d postgres rabbitmq kafka redis
```

### ⚙️ Terminal 2 — Run Auth Service

```bash
cd services/auth-service/AuthService.API

dotnet run
```

### 🌐 Terminal 3 — Run API Gateway (Optional)

```bash
docker-compose up -d api-gateway
```

---

## ✅ Verify Installation

```bash
# Health check
curl http://localhost:5000/api/v1/health

# Swagger UI
open http://localhost:5000/swagger

# RabbitMQ Management
open http://localhost:15672
# guest / guest

# Redis
docker exec portfolio-redis redis-cli ping
```

---

# 🐳 Docker Setup

## 🧱 Infrastructure Services

| Service | Port | Credentials | Purpose |
|----------|------|-------------|----------|
| 🐘 PostgreSQL | 5432 | postgres/postgres | Primary database |
| ⚡ Redis | 6379 | - | Cache & blacklist |
| 🐇 RabbitMQ | 5672, 15672 | guest/guest | Message broker |
| 📡 Kafka | 9092 | - | Event streaming |
| 🌐 API Gateway | 80 | - | Reverse proxy |

---

## ⚙️ Docker Commands

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f auth-service

# Stop all services
docker-compose down

# Remove volumes (clean slate)
docker-compose down -v
```

---

# 🔧 CI/CD Pipeline

## 🚀 GitHub Actions Workflows

| Workflow | Trigger | Actions |
|----------|----------|----------|
| `deploy-auth-service.yml` | Changes in `services/auth-service/` | Build, test, push to GHCR, deploy to Render |
| `deploy-api-gateway.yml` | Changes in `api-gateway/` | Build, push to GHCR, deploy to Render |

---

## 🔄 Deployment Flow

```text
Developer Push
      ↓
GitHub Actions
      ↓
Run Tests
      ↓
Build Docker Images
      ↓
Push to GHCR
      ↓
Deploy to Render
      ↓
Production 🚀
```

---

# 🛡️ Security Features

## 🔐 Authentication Flow

```text
┌─────────────────────────────────────────────────────────────┐
│                 JWT Authentication Flow                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ 1. User logs in → Validates credentials                    │
│                         ↓                                   │
│ 2. Generates JWT (expires in 60-360 min)                   │
│                         ↓                                   │
│ 3. Token stored in Redis blacklist on logout               │
│                         ↓                                   │
│ 4. JwtBlacklistMiddleware checks Redis on every request    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 🌍 OAuth2 Providers

| Provider | Endpoint | Status |
|----------|-----------|---------|
| 🟢 Google | `/api/v1/auth/google/login` | ✅ |
| ⚫ GitHub | `/api/v1/auth/github/login` | ✅ |

---

## 🗑️ Account Deletion

| Type | Reversible | Avatar Deleted |
|------|------------|----------------|
| 🟡 Soft Delete | ✅ (30 days) | ❌ |
| 🔴 Hard Delete | ❌ | ✅ |

---

# 📡 Message Brokers

## 🐇 RabbitMQ (Command Bus)

```csharp
// Publish user.modified event

await OutboxHelper.AddToOutboxAsync(
    outboxRepository,
    unitOfWork,
    "user.modified",
    "user.modified",
    "rabbitmq",
    new UserModifiedEvent { ... }
);
```

---

## 📡 Kafka (Audit Logs)

```csharp
// Publish security audit event

await OutboxHelper.AddToOutboxAsync(
    outboxRepository,
    unitOfWork,
    "admin.loggedin.audit",
    "admin.loggedin.audit",
    "kafka",
    new AuditEvent { ... }
);
```

---

## 📦 Outbox Pattern

```text
┌─────────────────────────────────────────────────────────────┐
│                     OUTBOX PATTERN                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ Business Logic → Save to OutboxMessages                    │
│                   (SAME transaction)                       │
│                         ↓                                   │
│ HTTP Response (API never waits for brokers)                │
│                         ↓                                   │
│ Background Worker (manual or scheduled)                    │
│                         ↓                                   │
│ Send to RabbitMQ / Kafka                                   │
│                         ↓                                   │
│ Mark as Processed                                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

# 🚦 API Gateway

## 🌐 Routes Configuration

| Path | Upstream | Purpose |
|------|-----------|----------|
| `/api/v1/auth/*` | `auth-service:8080` | Authentication |
| `/api/v1/admins/*` | `auth-service:8080` | Admin management |
| `/api/v1/blog/*` | `blog-service:3000` | Blog (future) |
| `/api/v1/ai/*` | `ai-service:8000` | AI (future) |
| `/api/v1/core/*` | `core-info-service:8080` | Core (future) |
| `/swagger` | `auth-service:8080/swagger` | API docs |
| `/health` | - | Gateway health |

---

## 🧪 Testing Gateway

```bash
# Gateway health
curl http://localhost/health

# Auth through gateway
curl http://localhost/api/v1/health

# Swagger through gateway
open http://localhost/swagger
```

---

# 🔑 Environment Variables

## 📌 Required Variables

| Variable | Purpose | Example |
|----------|----------|----------|
| `DATABASE_URL` | Neon PostgreSQL connection | `postgresql://...` |
| `JWT_SECRET` | Token signing (min 32 chars) | `your-secret-key` |
| `ADMIN_MASTER_KEY` | Admin registration key | `SUPER_SECRET_KEY` |
| `CLOUDINARY_CLOUD_NAME` | Image upload | `debbpghel` |
| `GOOGLE_CLIENT_ID` | Google OAuth | `xxx.apps.googleusercontent.com` |
| `GITHUB_CLIENT_ID` | GitHub OAuth | `Iv1.xxx` |

---

## ⚙️ .env Example

```bash
# Copy example
cp services/auth-service/AuthService.API/.env.example .env

# Edit with your values
DATABASE_URL=postgresql://user:pass@host/db

JWT_SECRET=your-super-secret-key-minimum-32-characters

ADMIN_MASTER_KEY=SUPER_SECRET_ADMIN_KEY_2024
```

---

# 📊 Service Status

| Service | Language | Port | Database | Status |
|----------|-----------|------|-----------|---------|
| 🔐 Auth Service | C# .NET 9 | 5000 | PostgreSQL | ✅ Complete |
| 🌐 API Gateway | NGINX | 80 | - | ✅ Complete |
| 📝 Blog Service | Ruby on Rails | 3000 | MongoDB | ⏳ Planned |
| 🤖 AI Service | FastAPI | 8000 | PostgreSQL | ⏳ Planned |
| ⚡ Core Service | Go Gin | 8080 | PostgreSQL | ⏳ Planned |

---

# ✅ Completed Endpoints

| Category | Count | Details |
|----------|------|----------|
| ❤️ Health | 4 | `/health`, `/ping`, `/db`, `/ready` |
| 🔐 Admin Auth | 11 | Register, login, logout, profile, avatar, password |
| 🌍 Social Auth | 9 | Google/GitHub login, profile, avatar, delete |
| 👨‍💼 Admin Management | 5 | Block, unblock, delete, list social users |
| 📦 Outbox | 3 | Pending, process, cleanup |
| 🎯 Total | **32** | All working ✅ |

---

<div align="center">

# ⬆ Back to Top
</div>