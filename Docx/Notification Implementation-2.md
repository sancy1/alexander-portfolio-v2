FINAL IMPLEMENTATION PLAN (TEXT ONLY)
Notification-Service: Phases 10 to 23
Enterprise-Grade Event-Driven Microservice
COMPLETED PHASES (0-9) - SUMMARY
Phase	What Was Achieved
0	Project directory structure following Clean Architecture was created
1	Go module with all dependencies (Gin, Kafka, Redis, RabbitMQ, PostgreSQL drivers) was initialized
2	Configuration management using YAML + environment variables with 12-factor principles was implemented
3	Database schema with 13 tables (notifications, preferences, routing, delivery logs, analytics, device registry, outbox, action logs, digests) was created in Neon PostgreSQL
4	PostgreSQL connection pool with health checks and graceful shutdown was established
5	Kafka consumer with Aiven SASL/SCRAM-SHA-512 authentication was connected and verified
6	Redis connection with Aiven TLS configuration was implemented (basic Get/Set/Delete/Exists operations)
7	HTTP server using Gin with graceful shutdown on SIGTERM was set up
8	Middleware stack (JWT Auth, Request Logging, Panic Recovery, CORS) was implemented
9	Health endpoints (/health and /ready) returning full service status were created
PHASE 10: DOMAIN ENTITIES COMPLETION
Objective
Extend the existing domain entities to include ALL fields defined in your 13-table database schema. Create missing entity files for tables that currently have no Go struct representation.

What to Achieve
The existing Notification entity currently has only 15 fields. You need to add 25+ missing fields including: is_critical, is_dismissed, is_flagged, is_pinned, event_id, short_message, actions, delivered_at, seen_at, dismissed_at, flagged_at, pinned_at, expires_at, group_key, batch_id, reference_id, reference_type, search_vector, version

Create a new OutboxMessage entity that maps to the outbox_messages table with fields: id, event_type, event_id, routing_key, exchange, payload, status, retry_count, last_error, next_retry_at, created_at, processed_at

Create a new WebSocketMessage entity for real-time communication structure with fields: type, data, user_id, timestamp

Create a new NotificationDeliveryStatus entity mapping to notification_delivery_status table with fields: id, notification_id, user_id, channel, status, status_message, retry_count, max_retries, next_retry_at, created_at, delivered_at, failed_at, provider_reference

Create a new UserDeviceRegistry entity mapping to user_device_registry table with fields: id, user_id, user_type, device_id, device_name, device_type, push_token, push_provider, last_active_at, is_active, created_at, updated_at

Create a new NotificationAnalyticsDaily entity mapping to notification_analytics_daily table with fields: id, analytics_date, source_service, notification_type, user_type, total_sent, total_delivered, total_read, total_clicked, total_dismissed, total_archived, total_deleted, unique_users_reached, unique_users_read, read_rate, click_through_rate, in_app_count, email_count, push_count, sms_count, created_at

Create a new NotificationDigest entity mapping to notification_digests table with fields: id, user_id, user_type, digest_frequency, digest_period_start, digest_period_end, notification_ids, summary, total_count, unread_count, urgent_count, high_priority_count, normal_priority_count, low_priority_count, services_breakdown, delivered_at, read_at, status, created_at

Create a new NotificationActionLog entity mapping to notification_user_actions_log table with fields: id, notification_id, user_id, user_type, action, action_details, previous_state, new_state, source, channel, ip_address, user_agent, session_id, request_id, created_at

How to Achieve
Navigate to internal/domain/entities/ directory

Open notification.go and add the 25+ missing fields with appropriate JSON and DB tags. Ensure pointer types (*time.Time) for nullable timestamp fields

Create new file outbox.go and define the OutboxMessage struct with all columns from the outbox_messages table

Create new file websocket.go and define WebSocketMessage and WebSocketSession structs

Create new file delivery.go and define NotificationDeliveryStatus struct

Create new file device.go and define UserDeviceRegistry struct

Create new file analytics.go and define NotificationAnalyticsDaily struct

Create new file digest.go and define NotificationDigest struct

Create new file action_log.go and define NotificationActionLog struct

For each struct, implement a TableName() method that returns the exact PostgreSQL table name

Verification
Run go build ./... - no compilation errors

All struct fields match database column names exactly

All nullable database columns use pointer types in Go

PHASE 11: REPOSITORY LAYER IMPLEMENTATION
Objective
Implement complete data access layer with all CRUD operations for all 13 tables. Each repository must provide methods for every database operation your service needs.

What to Achieve
11.1 Notification Repository (8+ methods):

Method to insert a new notification with all 40+ fields into the notifications table

Method to retrieve paginated notifications for a specific user with sorting by priority (urgent first) then creation date

Method to fetch a single notification by ID, scoped to user ID for security

Method to mark a notification as read (updates is_read=true and read_at=now)

Method to mark a notification as unread (sets is_read=false, clears read_at)

Method to archive a notification (sets is_archived=true, archived_at=now)

Method to restore an archived notification (sets is_archived=false, clears archived_at)

Method to soft delete a notification (sets is_deleted=true, deleted_at=now)

Method to dismiss a notification (sets is_dismissed=true, dismissed_at=now)

Method to flag a notification for follow-up (sets is_flagged=true, flagged_at=now)

Method to pin a notification to top (sets is_pinned=true, pinned_at=now)

Method to get unread count using the pre-created view v_user_unread_notification_counts

Method to get recent notifications using the view v_user_recent_notifications

Method to bulk mark multiple notifications as read in a single query

Method to bulk soft delete multiple notifications in a single query

11.2 Routing Repository (3+ methods):

Method to query event_routing_config by source_service and event_type (used when processing incoming Kafka events)

Method to retrieve all active routing configurations (for admin panel display)

Method to parse JSONB delivery_channels column into a Go string slice

Method to parse JSONB action_template column into a Go map

11.3 Preference Repository (5+ methods):

Method to get user preferences by user_id; if no record exists, return sensible defaults (in_app_enabled=true, email_enabled=true, push_enabled=true, auto_delete_enabled=true, auto_delete_days=90)

Method to upsert preferences (insert if not exists, update if exists) using PostgreSQL ON CONFLICT clause

Method to check if a specific service is in user's muted_services array

Method to check if a specific notification type is in user's muted_notification_types array

Method to check if quiet hours are currently active (considering user's timezone and current time)

11.4 Outbox Repository (4+ methods):

Method to insert a new pending outbox message

Method to retrieve pending messages with retry_count less than max and next_retry_at in the past, limited by batch size

Method to mark a message as sent with processed_at timestamp

Method to mark a message as failed, increment retry_count, set next_retry_at using exponential backoff (2^retry_count seconds)

11.5 Delivery Repository (4+ methods):

Method to create a delivery record for each channel (in_app, email, push, sms) with status='pending'

Method to update delivery status to 'sent', 'delivered', or 'failed' with appropriate timestamps

Method to get pending deliveries for retry where next_retry_at <= now()

Method to increment retry count and set next_retry_at for failed deliveries

11.6 Device Repository (4+ methods):

Method to register a new device (store push token, device info, user association)

Method to get all active devices for a user (for sending push notifications)

Method to update last_active_at timestamp

Method to deactivate a device (set is_active=false) when user logs out or token expires

11.7 Analytics Repository (3+ methods):

Method to increment daily counters (total_sent, total_delivered, total_read) for a specific date, service, notification_type

Method to retrieve analytics for a date range (for dashboard charts)

Method to calculate read rate = total_read / total_delivered and click-through rate = total_clicked / total_read

11.8 Action Log Repository (2 methods):

Method to log user action (read, archive, delete, dismiss, flag, pin) with previous and new state as JSON

Method to retrieve action history for a specific notification (audit trail)

How to Achieve
Navigate to internal/infrastructure/repositories/

Create notification_repo.go - implement all 15+ methods using sqlx queries

Create routing_repo.go - implement routing queries with JSONB parsing helpers

Create preference_repo.go - implement preference CRUD with default factory method

Create outbox_repo.go - implement outbox with retry logic

Create delivery_repo.go - implement delivery tracking with status management

Create device_repo.go - implement device registry CRUD

Create analytics_repo.go - implement daily aggregation and retrieval

Create action_log_repo.go - implement audit logging

Verification
Each repository method accepts context.Context as first parameter

All database errors are wrapped with appropriate error types (DomainError for not found, InfrastructureError for connection issues)

Transactions are used where multiple operations must succeed or fail together

PHASE 12: TEMPLATE RENDERING ENGINE
Objective
Build a template engine that transforms raw event payloads from Kafka into user-friendly notification titles and messages using the templates stored in the event_routing_config table.

What to Achieve
Create a template parser that uses Go's text/template package with custom delimiters {{ and }} to match the auth-service's existing template format

Support nested field access syntax like {{payload.userId}}, {{payload.loginMethod}}, {{payload.clientIp}} - the engine must be able to traverse nested maps

Support conditional logic using {{if payload.location}}from {{payload.location}}{{end}} - only render content if the field exists and is non-empty

Support loops using {{range payload.items}}...{{end}} for events that contain arrays (e.g., multiple order items)

Implement custom template functions:

upper: Convert string to uppercase (e.g., {{upper payload.status}})

lower: Convert string to lowercase

title: Convert to title case (first letter of each word capitalized)

truncate: Limit string length with ellipsis (e.g., {{truncate 50 payload.message}})

formatDate: Format timestamp as "2006-01-02" (e.g., {{formatDate payload.loginTime}})

formatTime: Format timestamp as "15:04:05"

formatDateTime: Format timestamp as "2006-01-02 15:04:05"

default: Provide default value if field missing (e.g., {{default "Unknown" payload.location}})

Create example templates for all auth-service events:

For admin.loggedin: title_template = "New sign-in detected", message_template = "Sign-in from {{payload.clientIp}} using {{payload.loginMethod}} on {{formatDate payload.loginTime}}"

For social.user.registered: title_template = "Welcome {{payload.displayName}}!", message_template = "Thanks for joining! Please complete your profile to get started"

For admin.password.changed: title_template = "Password changed", message_template = "Your password was changed on {{formatDateTime payload.changedAt}} from IP {{payload.requestIp}}"

How to Achieve
Create internal/application/services/template_renderer.go

Define a struct with a funcMap field containing all custom functions

Implement NewTemplateRenderer() constructor that initializes the function map

Implement Render(templateStr string, payload map[string]interface{}) (string, error) method

Inside Render: parse template with custom delimiters, execute with payload wrapped in a data map under key "payload"

Add error handling: if template parsing fails, return raw template string (don't crash the service)

Add RenderWithExtra() method that accepts additional context beyond payload (for digest notifications)

Verification
Template "Hello {{payload.displayName}}" with payload {"displayName": "John"} returns "Hello John"

Template "{{if payload.location}}from {{payload.location}}{{end}}" with no location returns empty string

Template "{{formatDate payload.time}}" with {"time": "2026-06-08T10:00:00Z"} returns "2026-06-08"

PHASE 13: EVENT PROCESSOR (THE CORE ENGINE)
Objective
Build the central orchestrator that takes a Kafka event and transforms it into a complete notification through a 14-step pipeline, handling idempotency, user preferences, rate limiting, database storage, Redis caching, and outbox queuing.

What to Achieve
Step 1 - Parse and Validate: Accept an EventEnvelope containing eventId, eventType, sourceService, userId, userType, and raw JSON payload. Extract user_id and validate it's a valid UUID.

Step 2 - Idempotency Check: Before any processing, check Redis for key event:processed:{eventId}. If it exists, skip processing entirely (prevents duplicate notifications from Kafka replay). If not, set the key with 7-day TTL.

Step 3 - Fetch Routing Config: Query the event_routing_config table using source_service and event_type. If no config exists, skip silently (not all events need notifications). If config is inactive, skip.

Step 4 - Parse Payload: Unmarshal the raw JSON payload into a map[string]interface{} for template rendering.

Step 5 - Check User Preferences: Query user_notification_preferences for the user. If user has muted the source_service, skip notification. If user has muted the notification_type, skip.

Step 6 - Check Quiet Hours: If user has quiet_hours_enabled=true, check if current time in user's timezone falls between quiet_hours_start and quiet_hours_end. If yes, skip notification (do not queue - just drop).

Step 7 - Apply Rate Limiting: If routing config has rate_limit_per_user > 0, check Redis counter for key ratelimit:user:{userId}:{minute}. If count exceeds limit, skip notification. If rate_limit_per_service > 0, check similarly.

Step 8 - Render Templates: Pass payload through template renderer to generate title, message, and short_message using the templates from routing config.

Step 9 - Create Notification Entity: Build a complete Notification struct with all fields populated: generate new UUID, set created_at to now, set priority from routing config, set is_critical=true if priority is urgent or high.

Step 10 - Set Expiration Dates: If routing.default_expires_days > 0, calculate expires_at = now + default_expires_days days. If routing.default_auto_delete_days > 0, calculate auto_delete_at similarly.

Step 11 - Save to PostgreSQL: Insert the notification into the notifications table. If this fails, return error to Kafka (will retry).

Step 12 - Add to Redis Inbox: Add the notification to Redis sorted set user:inbox:{userId} with score = timestamp nanoseconds. Trim the set to keep only 100 most recent. Set TTL of 7 days.

Step 13 - Increment Unread Counter: Increment Redis key user:unread:{userId} by 1. Set TTL to 7 days.

Step 14 - Create Outbox Record: Build a WebSocket message containing the notification data. Insert into outbox_messages table with status='pending'. The background worker will publish to RabbitMQ.

How to Achieve
Create internal/application/services/event_processor.go

Define EventProcessor struct with dependencies (repositories, cache, template renderer, rate limiter)

Implement ProcessEvent(ctx context.Context, envelope *dto.EventEnvelope) error method

Each step should be a separate private method for testability

Use structured logging at each step (INFO for creation, DEBUG for skips, ERROR for failures)

Implement graceful degradation: if Redis fails, continue (non-critical); if PostgreSQL fails, return error (critical)

Verification
When a valid admin.loggedin event arrives, a notification is created in PostgreSQL

The same event arriving twice (duplicate) is ignored by idempotency check

A user who has muted "auth-service" receives no notification

During quiet hours (e.g., 10 PM to 8 AM), notifications are suppressed

PHASE 14: KAFKA CONSUMER IMPLEMENTATION
Objective
Connect to Aiven Kafka with SASL/SCRAM-SHA-512 authentication, consume messages from the auth-events topic, and feed them to the Event Processor.

What to Achieve
Kafka Connection Setup:

Configure SASL/SCRAM-SHA-512 authentication using Aiven credentials (username, password)

Enable TLS for secure connection

Set consumer group ID to notification-service-group (enables offset tracking across restarts)

Set topic to auth-events

Set start offset to earliest to process historical events on first run

Configure session timeout of 30 seconds and heartbeat interval of 10 seconds

Message Consumption Loop:

Continuously poll for new messages in an infinite loop

For each message, unmarshal the JSON into EventEnvelope struct

Extract Kafka headers (if any) for correlation_id and causation_id

Call Event Processor's ProcessEvent method

If processing succeeds, commit the offset to Kafka

If processing fails, log error but DO NOT commit offset (message will be retried)

Consumer Health Monitoring:

Track messages processed per minute (for metrics endpoint)

Track errors per minute (for alerting)

Expose consumer lag metric (difference between latest offset and committed offset)

How to Achieve
Create internal/infrastructure/messaging/kafka_consumer.go

Use github.com/segmentio/kafka-go package

Create a Dialer with SASL mechanism and TLS config

Create a Reader with ReaderConfig containing brokers, topic, group ID, dialer

Implement Start(ctx context.Context) error method with select statement for graceful shutdown

Implement Close() method to clean up resources

Verification
Service successfully connects to Aiven Kafka (check logs for "Connected to Kafka")

Messages published by auth-service appear in logs ("Received event from Kafka")

After service restart, consumption resumes from last committed offset (no duplicate processing of old messages)

PHASE 15: REDIS REAL-TIME INBOX IMPLEMENTATION
Objective
Extend the existing Redis client (which only has Get/Set/Delete/Exists) to support notification-specific operations: sorted sets for user inboxes, counters for unread badges, and idempotency keys.

What to Achieve
User Inbox as Sorted Set:

Key pattern: user:inbox:{user_id}

Score: timestamp in nanoseconds (for chronological sorting)

Member: JSON serialized notification

Operations: ZADD to add new notification, ZREVRANGE to get most recent, ZREMRANGEBYRANK to trim to 100 items

TTL: 7 days (automatically expire old inboxes)

Unread Counter:

Key pattern: user:unread:{user_id}

Value: integer count

Operations: INCR to increment, SET to reset to 0, GET to retrieve

TTL: 7 days

Idempotency Store:

Key pattern: event:processed:{event_id}

Value: "1"

Operation: SETNX (set if not exists) to atomically check and store

TTL: 7 days

Rate Limiting Counters:

Key pattern: ratelimit:{scope}:{key}:{unix_minute}

Value: request count

Operation: INCR to increment, EXPIRE to set 1-minute TTL

How to Achieve
Create internal/infrastructure/cache/notification_cache.go

Define NotificationCache struct with Redis client reference

Implement AddToInbox() - serializes notification, executes ZADD, ZREMRANGEBYRANK, EXPIRE

Implement GetInbox() - executes ZREVRANGE and deserializes JSON

Implement IncrementUnreadCount() - executes INCR and EXPIRE

Implement GetUnreadCount() - executes GET

Implement ResetUnreadCount() - executes SET

Implement SetNX() for idempotency (already exists, ensure it's exposed)

Verification
After notification creation, Redis contains user:inbox:{userId} with 1 member

Unread counter increments from 0 to 1

Duplicate event detection works via SETNX returning false on second attempt

Inbox automatically trims to 100 items (send 101 notifications, only 100 remain)

PHASE 16: RABBITMQ IMPLEMENTATION
Objective
Implement a fanout exchange pattern where the notification-service publishes WebSocket messages to RabbitMQ, and all WebSocket server instances consume them to broadcast to connected clients.

What to Achieve
RabbitMQ Publisher:

Connect to Aiven RabbitMQ using AMQPS (TLS-enabled)

Declare a fanout exchange named notifications.fanout (broadcasts to all bound queues)

Publish WebSocketMessage as JSON payload with transient delivery mode (no persistence needed for real-time)

Include headers: user_id, notification_id, priority for filtering if needed

RabbitMQ Consumer:

Connect to same RabbitMQ instance

Declare the same fanout exchange

Create an anonymous queue (random name, exclusive to this connection, auto-delete when connection closes)

Bind the queue to the fanout exchange

Consume messages and extract user_id from headers

Call WebSocket manager to broadcast to that user's connections

Retry Mechanism (Outbox Pattern):

When publish fails (network issue, RabbitMQ down), save message to outbox_messages table instead of failing

Background worker runs every 5 seconds to process pending outbox messages

Retry with exponential backoff: 1s, 2s, 4s, 8s, 16s, 32s (max 5 retries)

After max retries, mark as failed and log alert (dead letter queue)

How to Achieve
Create internal/infrastructure/messaging/rabbitmq_publisher.go

Use github.com/rabbitmq/amqp091-go package

Dial with TLS config, declare exchange, implement Publish method

Create internal/infrastructure/messaging/rabbitmq_consumer.go

Implement consumer with queue declaration and binding

Create internal/application/workers/outbox_worker.go

Worker queries pending messages, attempts publish, updates status

Run worker as goroutine in main.go

Verification
When notification is created, RabbitMQ exchange receives a message

WebSocket consumer receives the message and broadcasts

If RabbitMQ is down, message goes to outbox table

When RabbitMQ recovers, outbox worker delivers pending messages

PHASE 17: WEBSOCKET SERVER IMPLEMENTATION
Objective
Build a WebSocket server that maintains persistent connections with frontend clients, authenticates via JWT, and broadcasts notifications in real-time.

What to Achieve
Connection Manager:

Store active connections in a thread-safe map: map[user_id]map[connection_id]*websocket.Conn

Support multiple connections per user (user logged in on multiple devices)

Handle connection lifecycle (register on connect, unregister on disconnect)

Send periodic ping/pong to detect dead connections (30-second interval)

JWT Authentication:

Extract JWT token from query parameter: ?token=xxx

Validate token using same secret as auth-service

Extract user_id and user_type from claims

Reject connection if token invalid or expired

Message Types:

connected: Sent to client immediately after successful handshake

notification: Sent when new notification arrives (contains title, message, type, priority)

unread_count_update: Sent when unread count changes (badge update)

ping/pong: Keep-alive messages

read_receipt: Received from client when user reads notification (triggers Redis update)

Broadcast to User:

Function that takes user_id and message payload

Finds all connections for that user

Sends message to each connection concurrently

Removes connections that fail to send

How to Achieve
Create internal/api/handlers/websocket_handler.go

Use github.com/gorilla/websocket package

Define WebSocketManager struct with sync.RWMutex for thread safety

Implement HandleWebSocket - upgrades HTTP, validates JWT, registers connection

Implement handleConnection - reads messages, handles ping, unregister on close

Implement BroadcastToUser - iterates connections, sends message

Add WebSocket endpoint to routes: router.GET("/ws", wsManager.HandleWebSocket)

Verification
Frontend can connect to ws://localhost:8081/ws?token={valid_jwt}

Connection confirmation message received

When notification is created, WebSocket sends it to connected client within 100ms

Multiple browser tabs all receive the same notification simultaneously

Dead connections are automatically cleaned up

PHASE 18: HTTP API HANDLERS IMPLEMENTATION
Objective
Build complete REST API for frontend to manage notifications, preferences, devices, and admin analytics.

What to Achieve
18.1 Notification Endpoints (13 endpoints):

GET /api/v1/notifications - Returns paginated list of user's notifications with filters (by read status, by priority, by date range). Supports page and pageSize query parameters.

GET /api/v1/notifications/unread-count - Returns unread count (fast, uses Redis if available, falls back to PostgreSQL)

GET /api/v1/notifications/:id - Returns single notification by ID, validates user owns it

PATCH /api/v1/notifications/:id/read - Marks notification as read, updates Redis unread counter

PATCH /api/v1/notifications/:id/unread - Marks as unread, decrements Redis counter

PATCH /api/v1/notifications/:id/archive - Moves to archive (still accessible but filtered from main list)

PATCH /api/v1/notifications/:id/restore - Restores from archive

DELETE /api/v1/notifications/:id - Soft deletes notification

POST /api/v1/notifications/:id/dismiss - Dismisses notification (user clicked "X")

POST /api/v1/notifications/:id/flag - Flags for follow-up (user wants to remember)

POST /api/v1/notifications/:id/pin - Pins to top of list

POST /api/v1/notifications/bulk-read - Accepts array of IDs, marks all as read

DELETE /api/v1/notifications/bulk-delete - Accepts array of IDs, soft deletes all

18.2 Preference Endpoints (7 endpoints):

GET /api/v1/preferences - Returns user's notification preferences (channels, muted services, quiet hours, digest settings)

PUT /api/v1/preferences - Updates all preferences at once

PATCH /api/v1/preferences/mute/service - Adds service to muted_services array

PATCH /api/v1/preferences/unmute/service - Removes service from muted_services

PATCH /api/v1/preferences/mute/type - Adds notification type to muted_notification_types

PATCH /api/v1/preferences/quiet-hours - Updates quiet hours (enabled, start, end, timezone)

PATCH /api/v1/preferences/digest - Updates digest settings (enabled, frequency, time)

18.3 Device Endpoints (3 endpoints):

POST /api/v1/devices/register - Registers device with push token for push notifications

DELETE /api/v1/devices/:device_id - Unregisters device

PATCH /api/v1/devices/:device_id/last-active - Updates last_active_at timestamp

18.4 Admin Analytics Endpoints (3 endpoints, admin-only):

GET /api/v1/admin/analytics/daily - Returns daily aggregated metrics (total sent, delivered, read, read rate)

GET /api/v1/admin/analytics/user/:user_id - Returns analytics for specific user

GET /api/v1/admin/analytics/notification-types - Returns breakdown by notification type

How to Achieve
Create internal/api/handlers/notification_handler.go - implement all 13 methods

Create internal/api/handlers/preference_handler.go - implement all 7 methods

Create internal/api/handlers/device_handler.go - implement all 3 methods

Create internal/api/handlers/admin_handler.go - implement all 3 methods

Each handler should be thin: parse request, call service, return response. No business logic in handlers.

All endpoints require JWT authentication (except health checks)

Admin endpoints require additional role check (userType must be "admin")

Use DTOs for request/response (never expose database entities directly)

Verification
Frontend can fetch notifications and see paginated results

Marking notification as read updates both database and Redis

Unread count badge updates correctly

Muting a service prevents future notifications from that service

Admin analytics show correct metrics

PHASE 19: BACKGROUND WORKERS
Objective
Implement five background workers that handle asynchronous tasks without blocking the main request flow.

What to Achieve
19.1 Outbox Processor (runs every 5 seconds):

Queries outbox_messages table for status='pending' AND (next_retry_at <= NOW() OR next_retry_at IS NULL)

Limits to 10 messages per batch

For each message: attempts to publish to RabbitMQ

On success: updates status='sent', sets processed_at=NOW()

On failure: increments retry_count, calculates next_retry_at = NOW() + (2^retry_count seconds), updates last_error

If retry_count >= 5: marks status='failed' (dead letter queue)

19.2 Delivery Retry Worker (runs every 30 seconds):

Queries notification_delivery_status for status='pending' AND next_retry_at <= NOW()

For each: attempts delivery based on channel (in_app, email, push, sms)

Updates status accordingly (sent, delivered, failed)

19.3 Auto-Delete Worker (runs daily at midnight UTC):

Deletes expired notifications: UPDATE notifications SET is_deleted=true, deleted_at=NOW() WHERE auto_delete_at <= NOW() AND is_deleted=false

Cleans up Redis: DELETE FROM user_inbox_cache WHERE user_id NOT IN (SELECT user_id FROM active_users)

Logs count of deleted records

19.4 Digest Worker (runs hourly for hourly digests, daily at 9 AM for daily digests):

For each user with digest_enabled=true

Queries notifications from last digest period (1 hour or 24 hours)

Aggregates into single digest notification with summary JSON

Creates digest record in notification_digests table

Sends digest via WebSocket

Updates last_digest_sent_at

19.5 Analytics Aggregator (runs daily at 1 AM UTC):

Aggregates previous day's data from notifications table

Groups by source_service, notification_type, user_type

Calculates: total_sent, total_delivered, total_read, total_clicked, total_dismissed, total_archived, total_deleted

Calculates read_rate = total_read / total_delivered

Calculates click_through_rate = total_clicked / total_read

Stores results in notification_analytics_daily table

How to Achieve
Create internal/application/workers/outbox_worker.go

Create internal/application/workers/delivery_worker.go

Create internal/application/workers/cleanup_worker.go

Create internal/application/workers/digest_worker.go

Create internal/application/workers/analytics_worker.go

Each worker has a Start(ctx context.Context) method with a ticker loop

Workers are started as goroutines in main.go

All workers respect context cancellation for graceful shutdown

Verification
Outbox worker processes pending messages within 5 seconds of insertion

Failed deliveries retry with exponential backoff (1s, 2s, 4s, 8s, 16s)

Notifications with auto_delete_at in the past are automatically deleted daily

Users with digest enabled receive aggregated notifications at configured times

Analytics table contains daily aggregates for previous day

PHASE 20: INPUT VALIDATION & DTOS
Objective
Validate all incoming API requests before they reach service layer, and define clean DTOs for API responses.

What to Achieve
Pagination Validator: Ensure page >= 1, pageSize between 1 and 100 (inclusive)

UUID Validator: Validate notification_id, user_id, device_id are valid UUID format

Preference Validator: Validate digest_frequency is one of: "hourly", "daily", "weekly". Validate sort_order is "asc" or "desc". Validate priority is "low", "normal", "high", or "urgent"

UserType Validator: Ensure user_type is one of: "admin", "social-user", "blog-author", "system"

NotificationResponseDTO: Contains id, title, message, short_message, priority, notification_type, is_read, is_archived, is_pinned, created_at, metadata (but never internal fields like search_vector)

PreferenceResponseDTO: Contains in_app_enabled, email_enabled, push_enabled, muted_services, muted_notification_types, quiet_hours_enabled, quiet_hours_start, quiet_hours_end, quiet_hours_timezone, digest_enabled, digest_frequency, auto_delete_enabled, auto_delete_days

How to Achieve
Create internal/api/validators/notification_validator.go

Create internal/api/validators/preference_validator.go

Create internal/application/dto/responses.go

Use github.com/go-playground/validator/v10 for struct validation

Each validator returns a standard error response with field-specific messages

DTOs use json tags for serialization, never expose database column names

Verification
Request with page=0 returns validation error "page must be >= 1"

Request with pageSize=200 returns validation error "pageSize must be between 1 and 100"

Response JSON uses camelCase (userId, isRead, createdAt) not snake_case

PHASE 21: ERROR HANDLING & OBSERVABILITY
Objective
Implement comprehensive error handling with typed errors and expose metrics for monitoring.

What to Achieve
Error Types:

DomainError: Business logic violations (notification not found, invalid preference, user not authorized)

InfrastructureError: Database connection failure, Kafka error, Redis error, RabbitMQ error

ValidationError: Invalid input data (wrong format, missing required field)

UnauthorizedError: JWT missing, invalid, or expired

Global Error Handler:

Catch all panics in HTTP handlers using recovery middleware

Log panic with stack trace using zap

Return appropriate HTTP status code (500 for panics, 400 for validation, 401 for auth, 404 for not found)

Never expose internal error details to client (safe error messages only)

Structured Logging Standards:

All logs in JSON format (zap production config)

Log levels: DEBUG (development only), INFO (normal operations), WARN (recoverable issues), ERROR (failed operations), FATAL (service cannot start)

Include correlation_id for request tracing across services

Include user_id for audit purposes

Metrics Endpoint (/metrics for Prometheus):

notifications_consumed_total - counter of Kafka messages processed

notifications_created_total - counter of notifications created

notification_processing_duration_seconds - histogram of processing time

errors_total - counter of errors by type

rabbitmq_publish_success_total - counter of successful RabbitMQ publishes

redis_hit_total - counter of Redis cache hits

How to Achieve
Create pkg/errors/error_types.go - define error interfaces and constructors

Create pkg/errors/error_handler.go - HTTP response formatter

Create internal/api/middleware/error_handler.go - Gin middleware

Create pkg/metrics/prometheus.go - Prometheus counter/ histogram definitions

Add /metrics endpoint to routes (protected by admin auth)

Verification
When notification not found, API returns 404 with message "Notification not found" (no stack trace)

When database connection fails, log shows ERROR level with full error details

Prometheus metrics show increasing counters for each notification

All logs are valid JSON and can be parsed by log aggregation tools

PHASE 22: TESTING STRATEGY
Objective
Implement comprehensive test coverage to ensure reliability before production deployment.

What to Achieve
Unit Tests:

Template renderer: test all functions (upper, lower, truncate, formatDate) with various inputs

Event processor: mock all repositories, test each decision path (muted, quiet hours, rate limited)

Rate limiter: test limit enforcement and window expiration

Validators: test all validation rules with valid and invalid inputs

Integration Tests:

Kafka consumer + processor: run test Kafka container, publish test message, verify notification created

PostgreSQL + Redis + RabbitMQ: run containers, test full flow from event to WebSocket

WebSocket: connect test client, verify message receipt

API endpoints: test all 13 notification endpoints with real database

Load Tests:

Simulate 1000 events/second using Go benchmark tools

Measure database write latency (target: <50ms p95)

Measure Redis read latency (target: <5ms p95)

Measure RabbitMQ publish latency (target: <10ms p95)

Verify no memory leaks under sustained load

How to Achieve
Create test/unit/template_renderer_test.go

Create test/unit/event_processor_test.go with mocks

Create test/unit/rate_limiter_test.go

Create test/integration/kafka_test.go using testcontainers

Create test/integration/api_test.go with httptest

Create test/load/simulation.go with concurrent goroutines

Run go test -race -coverprofile=coverage.out ./... in CI pipeline

Verification
Unit tests pass with >80% coverage

Integration tests pass in CI environment

Load test shows service handles 1000 events/second without errors

No race conditions detected (go test -race passes)

PHASE 23: DOCKER & DEPLOYMENT
Objective
Containerize the notification-service and deploy to production with GitHub Actions CI/CD.

What to Achieve
Dockerfile Optimization:

Multi-stage build: builder stage (Go 1.21 Alpine) + final stage (Alpine)

Copy only binary and configs to final stage (keep image small <20MB)

Set HEALTHCHECK instruction (curl /health every 30 seconds)

Use non-root user for security

Docker Compose for Development:

notification-service service with environment variables

PostgreSQL, Kafka, Redis, RabbitMQ as external services (not in compose, use Aiven/Neon/Upstash)

Port mapping: 8081:8080

GitHub Actions Workflow (.github/workflows/deploy-notification-service.yml):

Trigger: push to main branch with changes in services/notification-service/**

Steps:

Checkout code

Setup Go 1.21

Run go mod download

Run go test -race ./...

Run golangci-lint run

Build binary: go build -o notification-service ./cmd/api

Build Docker image: docker build -t notification-service:latest

Push to container registry (Docker Hub / GitHub Container Registry)

Deploy to Render/Fly.io/AWS ECS

Environment Configuration:

Production .env file with actual secrets (NEON_DB_URL, KAFKA_BROKERS, REDIS_URL, RABBITMQ_URL, JWT_SECRET)

Database connection pool settings (max_open_conns=25, max_idle_conns=10)

Kafka consumer group configuration (session_timeout=30s)

Redis memory limits (maxmemory=256mb, maxmemory-policy=allkeys-lru)

How to Achieve
Create Dockerfile with multi-stage build

Create docker-compose.yml for local development

Create .github/workflows/deploy-notification-service.yml

Create deploy/production.env.example (never commit actual secrets)

Configure Render.com or Fly.io with environment variables

Verification
docker build -t notification-service . builds successfully

docker run notification-service starts and passes health check

GitHub Actions workflow runs tests on every push

Production deployment receives traffic and processes events

IMPLEMENTATION ROADMAP (WEEK BY WEEK)
Week	Phases	Deliverable	Estimated Hours
Week 1	10, 11	Complete domain entities + all repositories	8 hours
Week 2	12, 13	Template renderer + event processor	6 hours
Week 3	14, 15	Kafka consumer + Redis inbox	5 hours
Week 4	16, 17	RabbitMQ + WebSocket server	6 hours
Week 5	18	HTTP API handlers (all endpoints)	5 hours
Week 6	19	Background workers (5 workers)	4 hours
Week 7	20, 21	Validation + error handling + metrics	4 hours
Week 8	22, 23	Testing + Docker + deployment	6 hours
Total Remaining: ~44 hours

CRITICAL SUCCESS FACTORS
Idempotency is NON-NEGOTIABLE: Kafka can deliver the same event twice. You MUST check Redis for event:processed:{eventId} before processing any event. Without this, users will see duplicate notifications.

Outbox Pattern for RabbitMQ: RabbitMQ publish can fail. You MUST save to outbox_messages table FIRST in the same transaction as the notification, then have a background worker retry. NEVER publish directly in the event processor.

Redis Inbox TTL: Redis memory is limited. Always set TTL on user inbox (7 days) and enforce max size (100 notifications). Older notifications live in PostgreSQL, Redis is only for "recent".

Graceful Degradation: If Redis is down → notification still saves to PostgreSQL, frontend can poll REST API. If RabbitMQ is down → notification saves to outbox table, retry worker publishes later. If Kafka is down → service cannot function (critical), health check should fail.

No Business Logic in Handlers: HTTP handlers MUST be thin (only parsing requests, calling services, returning responses). All business logic belongs in Event Processor and Service layer.

User Preferences Take Priority: If user has muted auth-service, DO NOT create notification even if routing config exists. If user has quiet hours active, DO NOT send notification during those hours.

VERIFICATION CHECKLIST (BEFORE PRODUCTION DEPLOYMENT)
Service starts without errors

Health endpoint returns {"status":"healthy"}

Kafka consumer connects and reads messages from auth-service

Admin login event creates notification in PostgreSQL

Redis inbox contains the notification

Unread count increments correctly

RabbitMQ receives notification message

WebSocket receives message and broadcasts

Frontend can fetch notifications via REST API

Frontend can mark notification as read

Unread count badge updates

User can mute services

Quiet hours suppress notifications

Rate limiting prevents spam

Idempotency prevents duplicates (same event processed twice = one notification)

Outbox retries failed RabbitMQ publishes

Auto-delete removes expired notifications daily

Digest worker sends aggregated notifications

Analytics aggregator populates daily metrics

Graceful shutdown works (SIGTERM kills within 30 seconds)

Docker image builds and runs

GitHub Actions workflow passes