-- ============================================================
-- User Service Database
-- DB Name: user_db
-- Handles: bookmarks, follows, user profiles + notifications
-- (Gộp UserService + NotificationService)
-- ============================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

CREATE TYPE bookmark_entity    AS ENUM ('paper', 'keyword', 'journal');
CREATE TYPE follow_type        AS ENUM ('keyword', 'journal', 'topic');
CREATE TYPE notification_type  AS ENUM ('new_paper', 'trend_alert', 'system');
CREATE TYPE delivery_status    AS ENUM ('pending', 'sent', 'failed');

-- ── USER PROFILES ─────────────────────────────────────────────
-- user_id tham chiếu identity_db.users.id (không FK xuyên DB)
CREATE TABLE user_profiles (
    user_id          UUID PRIMARY KEY,          -- ref identity_db.users.id
    bio              TEXT,
    institution      VARCHAR(255),
    research_fields  TEXT[],
    website_url      TEXT,
    created_at       TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- ── BOOKMARKS ────────────────────────────────────────────────
CREATE TABLE bookmarks (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id      UUID NOT NULL,
    entity_type  bookmark_entity NOT NULL,
    entity_id    UUID NOT NULL,                 -- paper_id / keyword_id / journal_id
    entity_title TEXT,                          -- denormalized để hiện nhanh
    note         TEXT,
    created_at   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_bookmark UNIQUE (user_id, entity_type, entity_id)
);

-- ── FOLLOWS ──────────────────────────────────────────────────
CREATE TABLE follows (
    id            UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id       UUID NOT NULL,
    follow_type   follow_type NOT NULL,
    target_id     UUID NOT NULL,                -- keyword_id / journal_id
    target_name   VARCHAR(500),                 -- denormalized
    notify_email  BOOLEAN NOT NULL DEFAULT TRUE,
    notify_inapp  BOOLEAN NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_follow UNIQUE (user_id, follow_type, target_id)
);

-- ── NOTIFICATIONS (gộp từ NotificationService) ───────────────
CREATE TABLE notifications (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id      UUID NOT NULL,
    type         notification_type NOT NULL,
    title        VARCHAR(500) NOT NULL,
    body         TEXT,
    related_id   UUID,                          -- paper_id / keyword_id liên quan
    related_type VARCHAR(50),                   -- 'paper', 'keyword', 'journal'
    is_read      BOOLEAN NOT NULL DEFAULT FALSE,
    read_at      TIMESTAMP WITH TIME ZONE,
    created_at   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- ── EMAIL QUEUE ───────────────────────────────────────────────
CREATE TABLE email_queue (
    id             UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id        UUID NOT NULL,
    to_email       VARCHAR(255) NOT NULL,
    subject        VARCHAR(500) NOT NULL,
    body_html      TEXT NOT NULL,
    status         delivery_status NOT NULL DEFAULT 'pending',
    attempts       SMALLINT NOT NULL DEFAULT 0,
    last_attempted TIMESTAMP WITH TIME ZONE,
    sent_at        TIMESTAMP WITH TIME ZONE,
    error_message  TEXT,
    created_at     TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- ── INDEXES ──────────────────────────────────────────────────
CREATE INDEX idx_bookmarks_user    ON bookmarks(user_id);
CREATE INDEX idx_bookmarks_entity  ON bookmarks(entity_type, entity_id);
CREATE INDEX idx_follows_user      ON follows(user_id);
CREATE INDEX idx_follows_target    ON follows(follow_type, target_id);
CREATE INDEX idx_notif_user        ON notifications(user_id, is_read);
CREATE INDEX idx_notif_created     ON notifications(created_at DESC);
CREATE INDEX idx_notif_related     ON notifications(related_type, related_id);
CREATE INDEX idx_email_status      ON email_queue(status, attempts);
CREATE INDEX idx_email_user        ON email_queue(user_id);

-- ── TRIGGERS ─────────────────────────────────────────────────
CREATE OR REPLACE FUNCTION update_updated_at()
RETURNS TRIGGER AS $$
BEGIN NEW.updated_at = NOW(); RETURN NEW; END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_profile_updated_at
    BEFORE UPDATE ON user_profiles FOR EACH ROW EXECUTE FUNCTION update_updated_at();
