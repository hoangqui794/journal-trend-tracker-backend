-- ============================================================
-- Trend Service Database
-- DB Name: trend_db
-- Handles: publication trends, analytics snapshots, report cache
-- ============================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ── KEYWORD TREND SNAPSHOTS ───────────────────────────────────
-- keyword_id tham chiếu paper_db.keywords.id (không FK xuyên DB)
CREATE TABLE trend_snapshots (
    id            UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    keyword_id    UUID NOT NULL,
    keyword_term  VARCHAR(255) NOT NULL,        -- denormalized để tránh gọi qua paper_db
    year          SMALLINT NOT NULL,
    paper_count   INTEGER NOT NULL DEFAULT 0,
    citation_sum  INTEGER NOT NULL DEFAULT 0,
    growth_rate   FLOAT,                        -- % tăng trưởng so với năm trước
    recorded_at   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_keyword_trend UNIQUE (keyword_id, year)
);

-- ── JOURNAL TREND SNAPSHOTS ───────────────────────────────────
CREATE TABLE journal_trend_snapshots (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    journal_id   UUID NOT NULL,
    journal_name VARCHAR(500) NOT NULL,         -- denormalized
    year         SMALLINT NOT NULL,
    paper_count  INTEGER NOT NULL DEFAULT 0,
    citation_sum INTEGER NOT NULL DEFAULT 0,
    growth_rate  FLOAT,
    recorded_at  TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_journal_trend UNIQUE (journal_id, year)
);

-- ── SEARCH HISTORY ────────────────────────────────────────────
-- Lưu lại lịch sử tìm kiếm để phân tích trending topics
CREATE TABLE search_history (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id     UUID,                           -- NULL nếu guest
    query       TEXT NOT NULL,
    search_type VARCHAR(50),                    -- 'keyword', 'author', 'journal'
    result_count INTEGER,
    created_at  TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- ── REPORT CACHE ─────────────────────────────────────────────
CREATE TABLE report_cache (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    report_type  VARCHAR(100) NOT NULL,         -- 'keyword_trend', 'top_journals', 'hot_topics'
    params_hash  VARCHAR(64) NOT NULL,          -- MD5 của params để lookup nhanh
    result_json  JSONB NOT NULL,
    generated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    expires_at   TIMESTAMP WITH TIME ZONE NOT NULL,

    CONSTRAINT uq_report_cache UNIQUE (report_type, params_hash)
);

-- ── INDEXES ──────────────────────────────────────────────────
CREATE INDEX idx_trend_kw_id      ON trend_snapshots(keyword_id);
CREATE INDEX idx_trend_kw_term    ON trend_snapshots(keyword_term);
CREATE INDEX idx_trend_year       ON trend_snapshots(year);
CREATE INDEX idx_journal_trend_id ON journal_trend_snapshots(journal_id);
CREATE INDEX idx_journal_trend_yr ON journal_trend_snapshots(year);
CREATE INDEX idx_search_user      ON search_history(user_id);
CREATE INDEX idx_search_created   ON search_history(created_at DESC);
CREATE INDEX idx_report_lookup    ON report_cache(report_type, params_hash);
CREATE INDEX idx_report_expires   ON report_cache(expires_at);
