-- ============================================================================
-- TABLE: notifications
-- DESCRIPTION: Stores all notifications from ALL services
-- FEATURES: Soft delete, archive, read/unread, priority, auto-delete, metadata
-- ============================================================================

CREATE TABLE notifications (
    -- Primary identifier
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
        
    -- User identification (works across all services)
    user_id UUID NOT NULL,
    user_type VARCHAR(50) NOT NULL,              -- 'admin', 'social_user', 'blog_author', 'system'
    source_service VARCHAR(100) NOT NULL,        -- 'auth-service', 'blog-service'
        
    -- Event tracking
    event_type VARCHAR(200) NOT NULL,            -- 'admin.loggedin', 'post.published'
    notification_type VARCHAR(100) NOT NULL,     -- 'security_alert', 'content_update'
    event_id VARCHAR(255),                       -- Original event ID from source service
        
    -- Content
    title VARCHAR(500) NOT NULL,
    message TEXT NOT NULL,
    short_message VARCHAR(255),                  -- For mobile push notifications
        
    -- Priority & Urgency
    priority VARCHAR(20) DEFAULT 'normal',       -- 'low', 'normal', 'high', 'urgent'
    is_critical BOOLEAN DEFAULT FALSE NOT NULL,
        
    -- Flexible data storage
    metadata JSONB DEFAULT '{}'::jsonb NOT NULL, -- Original payload + extra context
    actions JSONB DEFAULT '{}'::jsonb NOT NULL,  -- Action buttons (view, dismiss, etc.)
        
    -- Lifecycle Status Flags
    is_read BOOLEAN DEFAULT FALSE NOT NULL,
    is_archived BOOLEAN DEFAULT FALSE NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL,   -- Soft delete
    is_dismissed BOOLEAN DEFAULT FALSE NOT NULL, -- User dismissed without reading
    is_flagged BOOLEAN DEFAULT FALSE NOT NULL,   -- User flagged as important
    is_pinned BOOLEAN DEFAULT FALSE NOT NULL,    -- Pinned to top of feed
        
    -- Read Receipt & Delivery Tracking
    read_at TIMESTAMP WITH TIME ZONE,
    delivered_at TIMESTAMP WITH TIME ZONE,       -- First delivered to client
    seen_at TIMESTAMP WITH TIME ZONE,            -- User actually saw it
    dismissed_at TIMESTAMP WITH TIME ZONE,
        
    -- Archive & Deletion Timestamps
    archived_at TIMESTAMP WITH TIME ZONE,
    deleted_at TIMESTAMP WITH TIME ZONE,
    flagged_at TIMESTAMP WITH TIME ZONE,
    pinned_at TIMESTAMP WITH TIME ZONE,
        
    -- Expiration & Cleanup
    expires_at TIMESTAMP WITH TIME ZONE,         -- When notification expires (auto soft delete)
    auto_delete_at TIMESTAMP WITH TIME ZONE,     -- When to permanently delete (hard delete run)
        
    -- Grouping & Batching
    group_key VARCHAR(255),                      -- Group similar notifications
    batch_id UUID,                               -- Batch identifier for digests
        
    -- Optional External References
    reference_id VARCHAR(255),                   -- External reference (post ID, etc.)
    reference_type VARCHAR(100),                 -- 'post', 'comment', 'order'
        
    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_by VARCHAR(255),                     -- System or user who created
    updated_at TIMESTAMP WITH TIME ZONE,
    version INTEGER DEFAULT 1 NOT NULL,          -- Optimistic locking
    
    -- High Performance Pre-computed Full Text Search Column
    search_vector tsvector GENERATED ALWAYS AS (
        to_tsvector('english', COALESCE(title, '') || ' ' || COALESCE(message, ''))
    ) STORED,

    -- Constraints
    CONSTRAINT valid_priority CHECK (priority IN ('low', 'normal', 'high', 'urgent')),
    CONSTRAINT valid_user_type CHECK (user_type IN ('admin', 'social_user', 'blog_author', 'system')),
    CONSTRAINT valid_dates CHECK (read_at IS NULL OR read_at >= created_at)
);

-- ============================================================================
-- INDEXES FOR notifications
-- ============================================================================

-- Primary query: Get user's active feed (Highly critical for app dashboard landing page)
CREATE INDEX idx_notifications_user_active 
    ON notifications(user_id, created_at DESC) 
    WHERE is_deleted = FALSE AND is_archived = FALSE;

-- Unread count (Keeps your badge counter query at O(1) read operational speed)
CREATE INDEX idx_notifications_user_unread 
    ON notifications(user_id, created_at DESC) 
    WHERE is_read = FALSE AND is_deleted = FALSE AND is_archived = FALSE;

-- Archived feed lookup
CREATE INDEX idx_notifications_user_archived 
    ON notifications(user_id, created_at DESC) 
    WHERE is_deleted = FALSE AND is_archived = TRUE;

-- Deleted items view (For the trash bin / restore operational views)
CREATE INDEX idx_notifications_user_deleted 
    ON notifications(user_id, deleted_at DESC) 
    WHERE is_deleted = TRUE;

-- Pinned feed items priority bump placement
CREATE INDEX idx_notifications_pinned 
    ON notifications(user_id, pinned_at DESC) 
    WHERE is_pinned = TRUE AND is_deleted = FALSE;

-- User flagged/starred critical followups
CREATE INDEX idx_notifications_flagged 
    ON notifications(user_id, flagged_at DESC) 
    WHERE is_flagged = TRUE AND is_deleted = FALSE;

-- Multi-tenant / Cross-service isolation analysis queries
CREATE INDEX idx_notifications_source_service 
    ON notifications(source_service, user_id, created_at DESC);

-- Targeted category event analysis tracking 
CREATE INDEX idx_notifications_type 
    ON notifications(user_id, notification_type, created_at DESC);

-- Chronological sorting fallback range query index
CREATE INDEX idx_notifications_created_at 
    ON notifications(created_at DESC);

-- Cron Expiration engine tracking index (Optimized for processing mutations to true soft deletes)
CREATE INDEX idx_notifications_expires_at 
    ON notifications(expires_at) 
    WHERE expires_at IS NOT NULL AND is_deleted = FALSE;

-- Hard Delete Background Engine Purge Index (Optimized for permanent destruction sweeps)
CREATE INDEX idx_notifications_auto_delete_at 
    ON notifications(auto_delete_at) 
    WHERE auto_delete_at IS NOT NULL;

-- Dynamic system batch cluster indexing parameters
CREATE INDEX idx_notifications_group_key 
    ON notifications(group_key, created_at DESC) 
    WHERE group_key IS NOT NULL;

CREATE INDEX idx_notifications_batch_id 
    ON notifications(batch_id) 
    WHERE batch_id IS NOT NULL;

-- Polymorphic model link mapping connections lookup index
CREATE INDEX idx_notifications_reference 
    ON notifications(reference_type, reference_id) 
    WHERE reference_id IS NOT NULL;

-- Complex Deep JSON Structural Query Indexes 
CREATE INDEX idx_notifications_metadata ON notifications USING GIN (metadata);
CREATE INDEX idx_notifications_actions ON notifications USING GIN (actions);

-- Composite operational coverage index for priority routing feeds
CREATE INDEX idx_notifications_user_priority_date 
    ON notifications(user_id, priority, created_at DESC) 
    WHERE is_deleted = FALSE;

-- Scalable, high-speed generated Full-Text Search Vector Index
CREATE INDEX idx_notifications_search ON notifications USING GIN (search_vector);





-- ============================================================================
-- TABLE: notification_delivery_status
-- DESCRIPTION: Tracks delivery across multiple channels
-- ============================================================================

CREATE TABLE notification_delivery_status (
    id BIGSERIAL PRIMARY KEY,
    notification_id UUID NOT NULL REFERENCES notifications(id) ON DELETE CASCADE,
    user_id UUID NOT NULL,
        
    -- Channel tracking
    channel VARCHAR(50) NOT NULL,                -- 'in_app', 'websocket', 'email', 'push', 'sms', 'slack'
        
    -- Status tracking
    status VARCHAR(50) DEFAULT 'pending' NOT NULL, -- 'pending', 'sent', 'delivered', 'read', 'failed', 'bounced'
    status_message TEXT,
        
    -- Retry logic
    retry_count SMALLINT DEFAULT 0 NOT NULL,
    max_retries SMALLINT DEFAULT 3 NOT NULL,
    next_retry_at TIMESTAMP WITH TIME ZONE,
        
    -- Timing
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE,
    sent_at TIMESTAMP WITH TIME ZONE,
    delivered_at TIMESTAMP WITH TIME ZONE,
    failed_at TIMESTAMP WITH TIME ZONE,
        
    -- Provider tracking
    provider_reference VARCHAR(500),             -- Email service ID, push notification ID
    provider_response JSONB,                     -- Full response from provider
        
    -- Headers & metadata
    ip_address INET,
    user_agent TEXT,
    device_id VARCHAR(255)
);

-- Separate Postgres Index Definitions for delivery status
CREATE INDEX idx_delivery_notification ON notification_delivery_status(notification_id);
CREATE INDEX idx_delivery_user ON notification_delivery_status(user_id, created_at DESC);
CREATE INDEX idx_delivery_pending ON notification_delivery_status(status, next_retry_at) WHERE status = 'pending';
CREATE INDEX idx_delivery_channel_status ON notification_delivery_status(channel, status, created_at DESC);
CREATE INDEX idx_delivery_provider ON notification_delivery_status(provider_reference);




-- ============================================================================
-- TABLE: user_notification_preferences
-- DESCRIPTION: Centralized user preferences for ALL services
-- ============================================================================

CREATE TABLE user_notification_preferences (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    user_id UUID NOT NULL UNIQUE,
    user_type VARCHAR(50) NOT NULL,
        
    -- Channel enablement
    in_app_enabled BOOLEAN DEFAULT TRUE NOT NULL,
    email_enabled BOOLEAN DEFAULT TRUE NOT NULL,
    push_enabled BOOLEAN DEFAULT TRUE NOT NULL,
    sms_enabled BOOLEAN DEFAULT FALSE NOT NULL,
    webhook_enabled BOOLEAN DEFAULT FALSE NOT NULL,
        
    -- Channel addresses
    email_address VARCHAR(255),
    phone_number VARCHAR(50),
    webhook_url VARCHAR(500),
        
    -- Mute settings (service-level and type-level)
    muted_services TEXT[] DEFAULT '{}'::text[] NOT NULL,          -- ['blog-service', 'ai-service']
    muted_notification_types TEXT[] DEFAULT '{}'::text[] NOT NULL, -- ['engagement', 'promotional']
    muted_priorities TEXT[] DEFAULT '{}'::text[] NOT NULL,         -- ['low']
        
    -- Quiet hours
    quiet_hours_enabled BOOLEAN DEFAULT FALSE NOT NULL,
    quiet_hours_start TIME,
    quiet_hours_end TIME,
    quiet_hours_timezone VARCHAR(50) DEFAULT 'UTC' NOT NULL,
        
    -- Digest settings
    digest_enabled BOOLEAN DEFAULT FALSE NOT NULL,
    digest_frequency VARCHAR(20) DEFAULT 'daily' NOT NULL,         -- 'hourly', 'daily', 'weekly'
    digest_time TIME DEFAULT '09:00:00' NOT NULL,
    last_digest_sent_at TIMESTAMP WITH TIME ZONE,
        
    -- Auto-cleanup
    auto_delete_enabled BOOLEAN DEFAULT TRUE NOT NULL,
    auto_delete_days INT DEFAULT 90 NOT NULL,
    auto_delete_read_only BOOLEAN DEFAULT TRUE NOT NULL,          -- Only delete read notifications
    auto_archive_read_days INT DEFAULT 30 NOT NULL,               -- Auto-archive after N days
        
    -- Email settings
    email_batch_enabled BOOLEAN DEFAULT TRUE NOT NULL,
    email_batch_interval_hours SMALLINT DEFAULT 1 NOT NULL,
        
    -- Push notification settings
    push_sound_enabled BOOLEAN DEFAULT TRUE NOT NULL,
    push_badge_enabled BOOLEAN DEFAULT TRUE NOT NULL,
        
    -- UI Preferences
    default_page_size SMALLINT DEFAULT 20 NOT NULL,
    sort_order VARCHAR(20) DEFAULT 'desc' NOT NULL,               -- 'asc' or 'desc'
    group_by_service BOOLEAN DEFAULT FALSE NOT NULL,
        
    -- Unsubscribe tokens
    unsubscribe_token VARCHAR(255),                               -- For one-click unsubscribe
    unsubscribe_all BOOLEAN DEFAULT FALSE NOT NULL,               -- Global unsubscribe
        
    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE,
    last_updated_by VARCHAR(255),
        
    CONSTRAINT valid_frequency CHECK (digest_frequency IN ('hourly', 'daily', 'weekly')),
    CONSTRAINT valid_sort_order CHECK (sort_order IN ('asc', 'desc'))
);

-- Separate Postgres Index Definitions for user preferences
CREATE INDEX idx_preferences_user_type ON user_notification_preferences(user_type);
CREATE INDEX idx_preferences_digest ON user_notification_preferences(digest_enabled, digest_frequency, digest_time);
CREATE INDEX idx_preferences_auto_delete ON user_notification_preferences(auto_delete_enabled, auto_delete_days);
CREATE INDEX idx_preferences_unsubscribe ON user_notification_preferences(unsubscribe_token);

-- GIN Indexes to keep dynamic Array queries blisteringly fast on Neon
CREATE INDEX idx_preferences_muted_services ON user_notification_preferences USING GIN (muted_services);
CREATE INDEX idx_preferences_muted_types ON user_notification_preferences USING GIN (muted_notification_types);




-- ============================================================================
-- TABLE: user_device_registry
-- DESCRIPTION: Tracks user devices for push notifications (APNS/FCM/WebPush)
-- ============================================================================

CREATE TABLE user_device_registry (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    user_id UUID NOT NULL,
    user_type VARCHAR(50) NOT NULL,
        
    -- Device identification
    device_id VARCHAR(255) NOT NULL,
    device_name VARCHAR(255),
    device_type VARCHAR(50),                     -- 'ios', 'android', 'web'
    device_model VARCHAR(100),
    os_version VARCHAR(50),
    app_version VARCHAR(50),
        
    -- Push tokens
    push_token VARCHAR(500) NOT NULL,
    push_provider VARCHAR(50),                   -- 'fcm', 'apns', 'webpush'
        
    -- Metadata
    last_active_at TIMESTAMP WITH TIME ZONE,
    last_failed_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT TRUE NOT NULL,
        
    -- Geolocation
    last_known_latitude DECIMAL(10, 8),
    last_known_longitude DECIMAL(11, 8),
    timezone VARCHAR(50),
    language VARCHAR(10),
        
    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE,
        
    UNIQUE(user_id, device_id)
);

-- Separate Postgres Index Definitions for device registry
CREATE INDEX idx_devices_user ON user_device_registry(user_id, is_active);
CREATE INDEX idx_devices_token ON user_device_registry(push_token);
CREATE INDEX idx_devices_last_active ON user_device_registry(last_active_at DESC);




-- ============================================================================
-- TABLE: event_routing_config
-- DESCRIPTION: Maps incoming events to notification templates
-- MOST IMPORTANT TABLE FOR FUTURE SERVICES
-- ============================================================================

CREATE TABLE event_routing_config (
    id SERIAL PRIMARY KEY,
        
    -- Source identification
    source_service VARCHAR(100) NOT NULL,        -- 'auth-service', 'blog-service'
    event_type VARCHAR(200) NOT NULL,            -- 'admin.loggedin', 'post.published'
    event_version VARCHAR(20) DEFAULT '1.0' NOT NULL,
        
    -- Target configuration
    notification_type VARCHAR(100) NOT NULL,
    priority VARCHAR(20) DEFAULT 'normal' NOT NULL,
        
    -- Templates (Handlebars/Jinja syntax parsing targets)
    title_template TEXT NOT NULL,                -- 'New sign-in from {{location}}'
    message_template TEXT NOT NULL,              -- 'Detected on {{device}} at {{time}}'
    short_message_template TEXT,                 -- For push notifications
    action_template JSONB DEFAULT '{}'::jsonb NOT NULL, 
        
    -- Metadata mapping (which fields go where)
    metadata_mapping JSONB DEFAULT '{}'::jsonb NOT NULL, 
        
    -- Rules & conditions
    condition_expression TEXT,                   -- Condition evaluation logic for filtering
    rate_limit_per_user INT DEFAULT 0 NOT NULL,  -- 0 = no limit, max notifications allowed per hour
    rate_limit_per_service INT DEFAULT 0 NOT NULL,
        
    -- Delivery settings
    delivery_channels JSONB DEFAULT '["in_app"]'::jsonb NOT NULL, -- ['in_app', 'email', 'push']
    require_confirmation BOOLEAN DEFAULT FALSE NOT NULL,
        
    -- Expiration & cleanup
    default_expires_days INT DEFAULT 30 NOT NULL,
    default_auto_delete_days INT DEFAULT 90 NOT NULL,
        
    -- Status
    is_active BOOLEAN DEFAULT TRUE NOT NULL,
    is_deprecated BOOLEAN DEFAULT FALSE NOT NULL,
        
    -- Documentation
    description TEXT,
    example_payload JSONB,
        
    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE,
    created_by VARCHAR(255),
        
    CONSTRAINT unique_service_event_version UNIQUE (source_service, event_type, event_version),
    CONSTRAINT valid_priority CHECK (priority IN ('low', 'normal', 'high', 'urgent')),
    CONSTRAINT valid_limits CHECK (rate_limit_per_user >= 0 AND rate_limit_per_service >= 0)
);

-- Standalone Index definitions for optimized configuration scanning
CREATE INDEX idx_routing_service_active ON event_routing_config(source_service, is_active);
CREATE INDEX idx_routing_event_type ON event_routing_config(event_type, is_active);
CREATE INDEX idx_routing_notification_type ON event_routing_config(notification_type);

-- ============================================================================
-- SEED DATA FOR event_routing_config
-- ============================================================================

INSERT INTO event_routing_config 
    (source_service, event_type, notification_type, priority, title_template, message_template, short_message_template, delivery_channels)
VALUES
    -- Admin Auth Events
    ('auth-service', 'admin.loggedin', 'security_alert', 'normal', 
     'New sign-in detected', 
     'Sign-in from {{payload.location}} on {{payload.device}}', 
     'New login from {{payload.location}}', 
     '["in_app", "email"]'::jsonb),
     
    ('auth-service', 'admin.password.changed', 'security_alert', 'high', 
     'Password changed', 
     'Your password was changed on {{payload.changedAt}}', 
     'Password changed', 
     '["in_app", "email"]'::jsonb),
     
    ('auth-service', 'admin.profile.updated', 'account_update', 'low', 
     'Profile updated', 
     'Your profile information has been updated', 
     'Profile updated', 
     '["in_app"]'::jsonb),
     
    -- Social User Auth Events
    ('auth-service', 'social.user.loggedin', 'security_alert', 'normal', 
     'New login detected', 
     'Login from {{payload.location}} using {{payload.provider}}', 
     'New login from {{payload.location}}', 
     '["in_app", "email"]'::jsonb),
     
    ('auth-service', 'social.user.registered', 'welcome', 'high', 
     'Welcome to Alexander Portfolio!', 
     'Thanks for joining {{payload.displayName}}! Complete your profile to get started.', 
     'Welcome!', 
     '["in_app", "email"]'::jsonb),
     
    -- Staged Blog Service Infrastructure Events (Ready out-of-the-box)
    ('blog-service', 'post.published', 'content_update', 'normal', 
     'New post published', 
     '{{payload.author}} published "{{payload.title}}"', 
     'New post: {{payload.title}}', 
     '["in_app", "email"]'::jsonb),
     
    ('blog-service', 'comment.received', 'engagement', 'low', 
     'New comment', 
     '{{payload.commenter}} commented: "{{payload.preview}}"', 
     'New comment on your post', 
     '["in_app"]'::jsonb)
ON CONFLICT (source_service, event_type, event_version) DO NOTHING;




-- ============================================================================
-- TABLE: notification_templates
-- DESCRIPTION: Reusable notification templates for common scenarios
-- ============================================================================

CREATE TABLE notification_templates (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
        
    template_name VARCHAR(100) NOT NULL UNIQUE,
    template_category VARCHAR(50),               -- 'security', 'marketing', 'system'
        
    -- Versioning
    version INT DEFAULT 1 NOT NULL,
    is_active BOOLEAN DEFAULT TRUE NOT NULL,
        
    -- Content
    title_template TEXT NOT NULL,
    message_template TEXT NOT NULL,
    short_message_template TEXT,
    email_html_template TEXT,                    -- Rich layout version for mailings
    push_template TEXT,                          -- Mobile payload template bindings
    sms_template TEXT,                           -- SMS limited boundary version
        
    -- Default settings
    default_priority VARCHAR(20) DEFAULT 'normal' NOT NULL,
    default_channels JSONB DEFAULT '["in_app"]'::jsonb NOT NULL,
    default_expires_days INT DEFAULT 30 NOT NULL,
        
    -- Placeholder documentation (Helps UI layout engines read expected properties)
    required_placeholders JSONB DEFAULT '[]'::jsonb NOT NULL, 
    optional_placeholders JSONB DEFAULT '[]'::jsonb NOT NULL,
        
    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE,
    created_by VARCHAR(255)
);

-- Separate Postgres Index Definitions for template queries
CREATE INDEX idx_templates_name ON notification_templates(template_name, is_active);
CREATE INDEX idx_templates_category ON notification_templates(template_category);




-- ============================================================================
-- 📊 PART 4: ANALYTICS & AUDIT
-- ============================================================================

-- 4.1 notification_analytics_daily (Daily Aggregates)
CREATE TABLE IF NOT EXISTS notification_analytics_daily (
    id BIGSERIAL PRIMARY KEY,
    analytics_date DATE NOT NULL,
    source_service VARCHAR(100),
    notification_type VARCHAR(100),
    user_type VARCHAR(50),
        
    -- Counts
    total_sent INT DEFAULT 0 NOT NULL,
    total_delivered INT DEFAULT 0 NOT NULL,
    total_read INT DEFAULT 0 NOT NULL,
    total_clicked INT DEFAULT 0 NOT NULL,
    total_dismissed INT DEFAULT 0 NOT NULL,
    total_archived INT DEFAULT 0 NOT NULL,
    total_deleted INT DEFAULT 0 NOT NULL,
        
    -- Unique metrics
    unique_users_reached INT DEFAULT 0 NOT NULL,
    unique_users_read INT DEFAULT 0 NOT NULL,
        
    -- Engagement rates
    read_rate DECIMAL(5, 2),                     
    click_through_rate DECIMAL(5, 2),        
    
    -- Timing metrics
    avg_time_to_read_seconds INT,
    avg_time_to_dismiss_seconds INT,
        
    -- Channel breakdown
    in_app_count INT DEFAULT 0 NOT NULL,
    email_count INT DEFAULT 0 NOT NULL,
    push_count INT DEFAULT 0 NOT NULL,
    sms_count INT DEFAULT 0 NOT NULL,
        
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
        
    CONSTRAINT uniq_analytics_composite UNIQUE(analytics_date, source_service, notification_type, user_type)
);

CREATE INDEX IF NOT EXISTS idx_analytics_date ON notification_analytics_daily(analytics_date);
CREATE INDEX IF NOT EXISTS idx_analytics_service ON notification_analytics_daily(source_service, analytics_date);


-- 4.2 notification_user_actions_log (Detailed Audit Trail)
CREATE TABLE IF NOT EXISTS notification_user_actions_log (
    id BIGSERIAL PRIMARY KEY,
    notification_id UUID NOT NULL,
    user_id UUID NOT NULL,
    user_type VARCHAR(50) NOT NULL,
        
    -- Action details
    action VARCHAR(50) NOT NULL,                 -- 'view', 'read', 'archive', 'restore', 'delete', 'dismiss', 'flag', 'pin', 'click_action'
    action_details JSONB DEFAULT '{}'::jsonb NOT NULL,                        
        
    -- State before/after (for undo operations)
    previous_state JSONB,
    new_state JSONB,
        
    -- Source of action
    source VARCHAR(50) DEFAULT 'user' NOT NULL,   -- 'user', 'system', 'cron', 'admin'
    channel VARCHAR(50),                         -- 'web', 'mobile', 'api', 'email'
        
    -- Request context
    ip_address INET,
    user_agent TEXT,
    session_id VARCHAR(255),
    request_id VARCHAR(255),
        
    -- Timing
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_actions_notification ON notification_user_actions_log(notification_id);
CREATE INDEX IF NOT EXISTS idx_actions_user ON notification_user_actions_log(user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_actions_action ON notification_user_actions_log(action, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_actions_source ON notification_user_actions_log(source, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_actions_request ON notification_user_actions_log(request_id);


-- ============================================================================
-- 🔄 PART 5: OUTBOX & EVENT PUBLISHING
-- ============================================================================

-- 5.1 outbox_messages (Transactional Outbox)
CREATE TABLE IF NOT EXISTS outbox_messages (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
        
    -- Event identification
    event_type VARCHAR(100) NOT NULL,            -- 'notification.sent', 'notification.read'
    event_id VARCHAR(255) NOT NULL UNIQUE,       
    correlation_id VARCHAR(255),
    causation_id VARCHAR(255),
        
    -- Routing
    routing_key VARCHAR(100) NOT NULL,           -- 'notification.events'
    broker VARCHAR(50) NOT NULL,                 -- 'kafka', 'rabbitmq', 'redis'
        
    -- Payload
    payload JSONB NOT NULL,                      
    payload_size_bytes INT,                  
        
    -- Status tracking
    status VARCHAR(50) DEFAULT 'pending' NOT NULL, -- 'pending', 'processing', 'sent', 'failed'
    processed_at TIMESTAMP WITH TIME ZONE,
    published_at TIMESTAMP WITH TIME ZONE,
        
    -- Retry logic
    retry_count SMALLINT DEFAULT 0 NOT NULL,
    max_retries SMALLINT DEFAULT 3 NOT NULL,
    next_retry_at TIMESTAMP WITH TIME ZONE,
    last_error TEXT,
    error_details JSONB,
        
    -- Partition key for Kafka
    partition_key VARCHAR(255),
        
    -- Headers
    headers JSONB DEFAULT '{}'::jsonb NOT NULL,
        
    -- Delivery guarantee
    delivery_guarantee VARCHAR(20) DEFAULT 'at_least_once' NOT NULL,
        
    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE
);

CREATE INDEX IF NOT EXISTS idx_outbox_pending ON outbox_messages(status, next_retry_at, created_at) WHERE status = 'pending';
CREATE INDEX IF NOT EXISTS idx_outbox_broker ON outbox_messages(broker, status, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_outbox_event_type ON outbox_messages(event_type, status);
CREATE INDEX IF NOT EXISTS idx_outbox_published ON outbox_messages(published_at) WHERE status = 'sent';
CREATE INDEX IF NOT EXISTS idx_outbox_retry ON outbox_messages(retry_count, next_retry_at) WHERE status = 'pending';


-- 5.2 outbox_consumer_offset_tracking (Consumer Progress)
CREATE TABLE IF NOT EXISTS outbox_consumer_offset_tracking (
    id SERIAL PRIMARY KEY,
    consumer_group VARCHAR(100) NOT NULL,        -- 'notification-service-group'
    broker VARCHAR(50) NOT NULL,                 -- 'kafka', 'rabbitmq'
    topic VARCHAR(100) NOT NULL,                 -- 'notification.events'
    partition_id INT NOT NULL,
    last_processed_offset BIGINT,                
    last_processed_id UUID,                      
    last_processed_at TIMESTAMP WITH TIME ZONE,
        
    -- Statistics
    messages_processed_total BIGINT DEFAULT 0 NOT NULL,
    messages_failed_total BIGINT DEFAULT 0 NOT NULL,
    last_error TEXT,
        
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE,
        
    CONSTRAINT uniq_consumer_surface UNIQUE(consumer_group, broker, topic, partition_id)
);


-- ============================================================================
-- 🎯 PART 6: BATCHING & DIGESTS
-- ============================================================================

-- 6.1 notification_digests (Batch Notifications)
CREATE TABLE IF NOT EXISTS notification_digests (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    user_id UUID NOT NULL,
    user_type VARCHAR(50) NOT NULL,
        
    -- Digest metadata
    digest_frequency VARCHAR(20) NOT NULL,       -- 'hourly', 'daily', 'weekly'
    digest_period_start TIMESTAMP WITH TIME ZONE NOT NULL,
    digest_period_end TIMESTAMP WITH TIME ZONE NOT NULL,
        
    -- Content
    notification_ids UUID[] NOT NULL,            
    summary JSONB NOT NULL,                      
    total_count INT DEFAULT 0 NOT NULL,
    unread_count INT DEFAULT 0 NOT NULL,
    urgent_count INT DEFAULT 0 NOT NULL,
        
    -- Priority grouping
    high_priority_count INT DEFAULT 0 NOT NULL,
    normal_priority_count INT DEFAULT 0 NOT NULL,
    low_priority_count INT DEFAULT 0 NOT NULL,
        
    -- Service breakdown
    services_breakdown JSONB DEFAULT '{}'::jsonb NOT NULL,                    
        
    -- Delivery tracking
    delivered_at TIMESTAMP WITH TIME ZONE,
    read_at TIMESTAMP WITH TIME ZONE,
    status VARCHAR(50) DEFAULT 'pending' NOT NULL, -- 'pending', 'sent', 'read', 'failed'
        
    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_digests_user ON notification_digests(user_id, digest_period_start DESC);
CREATE INDEX IF NOT EXISTS idx_digests_status ON notification_digests(status, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_digests_period ON notification_digests(digest_period_start, digest_period_end);


-- ============================================================================
-- 🔧 PART 7: SUPPORTING FUNCTIONS & TRIGGERS
-- ============================================================================

-- 7.1 Auto-update Timestamp Trigger
CREATE OR REPLACE FUNCTION fn_update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply core structural updates hooks
DROP TRIGGER IF EXISTS trg_notifications_updated_at ON notifications;
CREATE TRIGGER trg_notifications_updated_at
    BEFORE UPDATE ON notifications
    FOR EACH ROW EXECUTE FUNCTION fn_update_updated_at_column();

DROP TRIGGER IF EXISTS trg_user_notification_preferences_updated_at ON user_notification_preferences;
CREATE TRIGGER trg_user_notification_preferences_updated_at
    BEFORE UPDATE ON user_notification_preferences
    FOR EACH ROW EXECUTE FUNCTION fn_update_updated_at_column();

DROP TRIGGER IF EXISTS trg_event_routing_config_updated_at ON event_routing_config;
CREATE TRIGGER trg_event_routing_config_updated_at
    BEFORE UPDATE ON event_routing_config
    FOR EACH ROW EXECUTE FUNCTION fn_update_updated_at_column();

DROP TRIGGER IF EXISTS trg_outbox_messages_updated_at ON outbox_messages;
CREATE TRIGGER trg_outbox_messages_updated_at
    BEFORE UPDATE ON outbox_messages
    FOR EACH ROW EXECUTE FUNCTION fn_update_updated_at_column();


-- 7.2 Auto-Archive Old Read Notifications
CREATE OR REPLACE FUNCTION fn_auto_archive_read_notifications()
RETURNS INTEGER AS $$
DECLARE
    archived_count INTEGER := 0;
BEGIN
    WITH to_archive AS (
        SELECT n.id
        FROM notifications n
        INNER JOIN user_notification_preferences p ON n.user_id = p.user_id
        WHERE n.is_read = TRUE 
          AND n.is_archived = FALSE 
          AND n.is_deleted = FALSE 
          AND p.auto_archive_read_days IS NOT NULL 
          AND n.read_at < (CURRENT_TIMESTAMP - (p.auto_archive_read_days || ' days')::INTERVAL)
        LIMIT 1000
    )
    UPDATE notifications n
    SET 
        is_archived = TRUE,
        archived_at = CURRENT_TIMESTAMP
    FROM to_archive ta
    WHERE n.id = ta.id;
    
    GET DIAGNOSTICS archived_count = ROW_COUNT;
    RETURN archived_count;
END;
$$ LANGUAGE plpgsql;


-- 7.3 Auto-Delete Expired Notifications
CREATE OR REPLACE FUNCTION fn_auto_delete_expired_notifications()
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER := 0;
BEGIN
    WITH to_soft_delete AS (
        SELECT id
        FROM notifications
        WHERE expires_at IS NOT NULL 
          AND expires_at < CURRENT_TIMESTAMP 
          AND is_deleted = FALSE
        LIMIT 5000
    )
    UPDATE notifications n
    SET 
        is_deleted = TRUE,
        deleted_at = CURRENT_TIMESTAMP
    FROM to_soft_delete td
    WHERE n.id = td.id;
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;


-- 7.4 Permanently Delete Old Soft-Deleted Notifications
CREATE OR REPLACE FUNCTION fn_permanent_delete_old_notifications(
    retention_days INTEGER DEFAULT 30
)
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER := 0;
BEGIN
    DELETE FROM notifications
    WHERE is_deleted = TRUE 
      AND deleted_at < (CURRENT_TIMESTAMP - (retention_days || ' days')::INTERVAL);
      
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;


-- 7.5 Archive Old Outbox Messages
CREATE OR REPLACE FUNCTION fn_archive_old_outbox_messages(
    retention_days INTEGER DEFAULT 7
)
RETURNS INTEGER AS $$
DECLARE
    archived_count INTEGER := 0;
BEGIN
    DELETE FROM outbox_messages
    WHERE status = 'sent' 
      AND published_at < (CURRENT_TIMESTAMP - (retention_days || ' days')::INTERVAL);
      
    GET DIAGNOSTICS archived_count = ROW_COUNT;
    RETURN archived_count;
END;
$$ LANGUAGE plpgsql;


-- 7.6 Update Unread Counts View
CREATE OR REPLACE VIEW v_user_unread_notification_counts AS
SELECT 
    user_id,
    user_type,
    COUNT(*) as total_unread,
    COUNT(*) FILTER (WHERE priority = 'urgent') as urgent_unread,
    COUNT(*) FILTER (WHERE priority = 'high') as high_unread,
    COUNT(*) FILTER (WHERE priority = 'normal') as normal_unread,
    MAX(created_at) as oldest_unread_at,
    MIN(created_at) as newest_unread_at
FROM notifications
WHERE is_read = FALSE 
  AND is_deleted = FALSE 
  AND is_archived = FALSE
GROUP BY user_id, user_type;


-- 7.7 User Recent Notifications View (Optimized for Feed Displays)
CREATE OR REPLACE VIEW v_user_recent_notifications AS
SELECT 
    n.id,
    n.user_id,
    n.user_type,
    n.source_service,
    n.notification_type,
    n.title,
    n.message,
    n.priority,
    n.is_read,
    n.is_archived,
    n.created_at,
    n.read_at,
    ds.status as delivery_status,
    ds.channel as delivery_channel
FROM notifications n
LEFT JOIN LATERAL (
    SELECT status, channel
    FROM notification_delivery_status
    WHERE notification_id = n.id
    ORDER BY created_at DESC
    LIMIT 1
) ds ON true
WHERE n.is_deleted = FALSE;


-- ============================================================================
-- 🧹 PART 8: CLEANUP & MAINTENANCE
-- ============================================================================

-- 8.1 Scheduled Maintenance Functions
CREATE OR REPLACE FUNCTION fn_run_notification_maintenance()
RETURNS JSONB AS $$
DECLARE
    archived_read INTEGER;
    expired_deleted INTEGER;
    permanent_deleted INTEGER;
    outbox_archived INTEGER;
    result JSONB;
BEGIN
    archived_read := fn_auto_archive_read_notifications();
    expired_deleted := fn_auto_delete_expired_notifications();
    permanent_deleted := fn_permanent_delete_old_notifications(30);
    outbox_archived := fn_archive_old_outbox_messages(7);
        
    result := jsonb_build_object(
        'timestamp', CURRENT_TIMESTAMP,
        'archived_read_count', archived_read,
        'expired_deleted_count', expired_deleted,
        'permanent_deleted_count', permanent_deleted,
        'outbox_archived_count', outbox_archived
    );
        
    RETURN result;
END;
$$ LANGUAGE plpgsql;


-- 8.2 Statistics Function (Safely Fixed Subquery Object Aggregation)
CREATE OR REPLACE FUNCTION fn_get_notification_stats(
    p_user_id UUID DEFAULT NULL,
    p_days_back INTEGER DEFAULT 30
)
RETURNS JSONB AS $$
DECLARE
    result JSONB;
    v_by_service JSONB;
BEGIN
    -- Separate aggregation query block to prevent GROUP BY collision boundaries
    SELECT COALESCE(jsonb_object_agg(src, stats), '{}'::jsonb)
    INTO v_by_service
    FROM (
        SELECT 
            COALESCE(source_service, 'unknown') as src,
            jsonb_build_object(
                'count', COUNT(*),
                'unread', COUNT(*) FILTER (WHERE is_read = FALSE AND is_deleted = FALSE)
            ) as stats
        FROM notifications
        WHERE (p_user_id IS NULL OR user_id = p_user_id)
          AND created_at > CURRENT_TIMESTAMP - (p_days_back || ' days')::INTERVAL
        GROUP BY source_service
    ) sub;

    SELECT jsonb_build_object(
        'total_sent', COUNT(*),
        'total_read', COUNT(*) FILTER (WHERE is_read = TRUE),
        'total_archived', COUNT(*) FILTER (WHERE is_archived = TRUE),
        'total_deleted', COUNT(*) FILTER (WHERE is_deleted = TRUE),
        'unread_count', COUNT(*) FILTER (WHERE is_read = FALSE AND is_deleted = FALSE AND is_archived = FALSE),
        'by_service', v_by_service,
        'by_priority', jsonb_build_object(
            'urgent', COUNT(*) FILTER (WHERE priority = 'urgent'),
            'high', COUNT(*) FILTER (WHERE priority = 'high'),
            'normal', COUNT(*) FILTER (WHERE priority = 'normal'),
            'low', COUNT(*) FILTER (WHERE priority = 'low')
        )
    )
    INTO result
    FROM notifications
    WHERE (p_user_id IS NULL OR user_id = p_user_id)
      AND created_at > CURRENT_TIMESTAMP - (p_days_back || ' days')::INTERVAL;
        
    RETURN COALESCE(result, '{}'::jsonb);
END;
$$ LANGUAGE plpgsql;



