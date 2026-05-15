-- ============================================================
-- Identity Service Database
-- DB Name: identity_db
-- Handles: authentication, Google OAuth, JWT, refresh tokens
-- ============================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TYPE auth_provider AS ENUM ('google', 'github', 'email');
CREATE TYPE user_role     AS ENUM ('researcher', 'lecturer', 'student', 'admin');
CREATE TYPE user_status   AS ENUM ('active', 'locked', 'pending');

CREATE TABLE users (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    full_name       VARCHAR(255) NOT NULL,
    email           VARCHAR(255) NOT NULL UNIQUE,
    password_hash   VARCHAR(512),                   -- NULL nếu đăng nhập OAuth
    avatar_url      TEXT,
    provider        auth_provider NOT NULL DEFAULT 'email',
    provider_id     VARCHAR(255),                   -- Google sub ID
    role            user_role     NOT NULL DEFAULT 'student',
    status          user_status   NOT NULL DEFAULT 'active',
    last_login_at   TIMESTAMP WITH TIME ZONE,
    created_at      TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_provider UNIQUE (provider, provider_id)
);

CREATE TABLE refresh_tokens (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token           VARCHAR(512) NOT NULL UNIQUE,
    expires_at      TIMESTAMP WITH TIME ZONE NOT NULL,
    is_revoked      BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_users_email        ON users(email);
CREATE INDEX idx_users_provider     ON users(provider, provider_id);
CREATE INDEX idx_users_role         ON users(role);
CREATE INDEX idx_refresh_token      ON refresh_tokens(token);
CREATE INDEX idx_refresh_token_user ON refresh_tokens(user_id);

CREATE OR REPLACE FUNCTION update_updated_at()
RETURNS TRIGGER AS $$
BEGIN NEW.updated_at = NOW(); RETURN NEW; END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

-- Seed: default admin (đổi password_hash trước khi deploy)
INSERT INTO users (full_name, email, password_hash, provider, role, status)
VALUES ('System Admin', 'admin@jts.com', '$2b$12$CHANGE_THIS_HASH', 'email', 'admin', 'active');
