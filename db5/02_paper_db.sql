-- ============================================================
-- Paper Service Database
-- DB Name: paper_db
-- Handles: papers, authors, journals, keywords + sync jobs
-- (Gộp PaperService + SyncService)
-- ============================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";     -- full-text search

CREATE TYPE paper_source  AS ENUM ('openalex', 'semantic_scholar', 'crossref');
CREATE TYPE keyword_source AS ENUM ('user', 'api');
CREATE TYPE sync_status   AS ENUM ('running', 'success', 'failed', 'cancelled');

-- ── JOURNALS ─────────────────────────────────────────────────
CREATE TABLE journals (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    external_id     VARCHAR(255) UNIQUE,
    name            VARCHAR(500) NOT NULL,
    issn            VARCHAR(20),
    e_issn          VARCHAR(20),
    publisher       VARCHAR(255),
    field           VARCHAR(255),
    homepage_url    TEXT,
    created_at      TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- ── AUTHORS ──────────────────────────────────────────────────
CREATE TABLE authors (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    external_id     VARCHAR(255) UNIQUE,
    name            VARCHAR(255) NOT NULL,
    affiliation     VARCHAR(500),
    orcid           VARCHAR(50),
    created_at      TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- ── KEYWORDS ─────────────────────────────────────────────────
CREATE TABLE keywords (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    term            VARCHAR(255) NOT NULL UNIQUE,
    normalized_term VARCHAR(255) NOT NULL,
    source          keyword_source NOT NULL DEFAULT 'api',
    usage_count     INTEGER NOT NULL DEFAULT 0,
    created_at      TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- ── PAPERS ───────────────────────────────────────────────────
CREATE TABLE papers (
    id               UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    external_id      VARCHAR(255) NOT NULL,
    source           paper_source NOT NULL,
    title            TEXT NOT NULL,
    abstract         TEXT,
    publication_year SMALLINT,
    doi              VARCHAR(255),
    url              TEXT,
    citation_count   INTEGER NOT NULL DEFAULT 0,
    reference_count  INTEGER NOT NULL DEFAULT 0,
    fields_of_study  TEXT[],
    journal_id       UUID REFERENCES journals(id) ON DELETE SET NULL,
    raw_data         JSONB,                 -- backup raw API response
    synced_at        TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_at       TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_paper_source UNIQUE (external_id, source)
);

-- ── MANY-TO-MANY ─────────────────────────────────────────────
CREATE TABLE paper_authors (
    paper_id      UUID NOT NULL REFERENCES papers(id)  ON DELETE CASCADE,
    author_id     UUID NOT NULL REFERENCES authors(id) ON DELETE CASCADE,
    author_order  SMALLINT NOT NULL DEFAULT 0,
    PRIMARY KEY (paper_id, author_id)
);

CREATE TABLE paper_keywords (
    paper_id        UUID NOT NULL REFERENCES papers(id)   ON DELETE CASCADE,
    keyword_id      UUID NOT NULL REFERENCES keywords(id) ON DELETE CASCADE,
    relevance_score FLOAT,
    PRIMARY KEY (paper_id, keyword_id)
);

-- ── SYNC JOBS (gộp từ SyncService) ───────────────────────────
CREATE TABLE api_sync_jobs (
    id               UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    source_name      VARCHAR(100) NOT NULL,  -- 'OpenAlex', 'SemanticScholar'
    source_base_url  TEXT NOT NULL,
    query_params     JSONB,
    scheduled_at     TIMESTAMP WITH TIME ZONE,
    started_at       TIMESTAMP WITH TIME ZONE,
    finished_at      TIMESTAMP WITH TIME ZONE,
    status           sync_status NOT NULL DEFAULT 'running',
    papers_fetched   INTEGER NOT NULL DEFAULT 0,
    papers_inserted  INTEGER NOT NULL DEFAULT 0,
    papers_updated   INTEGER NOT NULL DEFAULT 0,
    error_message    TEXT,
    created_at       TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Lưu cursor để tiếp tục sync nếu bị gián đoạn
CREATE TABLE sync_cursors (
    id             UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    source_name    VARCHAR(100) NOT NULL UNIQUE,
    last_cursor    TEXT,
    last_synced_at TIMESTAMP WITH TIME ZONE,
    updated_at     TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE TABLE sync_errors (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    job_id      UUID NOT NULL REFERENCES api_sync_jobs(id) ON DELETE CASCADE,
    external_id VARCHAR(255),
    error_type  VARCHAR(100),
    error_detail TEXT,
    occurred_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- ── INDEXES ──────────────────────────────────────────────────
CREATE INDEX idx_papers_external    ON papers(external_id);
CREATE INDEX idx_papers_year        ON papers(publication_year);
CREATE INDEX idx_papers_journal     ON papers(journal_id);
CREATE INDEX idx_papers_doi         ON papers(doi) WHERE doi IS NOT NULL;
CREATE INDEX idx_papers_title       ON papers USING gin(title gin_trgm_ops);
CREATE INDEX idx_papers_abstract    ON papers USING gin(abstract gin_trgm_ops);
CREATE INDEX idx_papers_fields      ON papers USING gin(fields_of_study);
CREATE INDEX idx_journals_name      ON journals USING gin(name gin_trgm_ops);
CREATE INDEX idx_authors_name       ON authors  USING gin(name gin_trgm_ops);
CREATE INDEX idx_authors_orcid      ON authors(orcid) WHERE orcid IS NOT NULL;
CREATE INDEX idx_keywords_term      ON keywords USING gin(term gin_trgm_ops);
CREATE INDEX idx_keywords_norm      ON keywords(normalized_term);
CREATE INDEX idx_paper_authors_auth ON paper_authors(author_id);
CREATE INDEX idx_paper_kw_kw        ON paper_keywords(keyword_id);
CREATE INDEX idx_sync_jobs_status   ON api_sync_jobs(status);
CREATE INDEX idx_sync_jobs_source   ON api_sync_jobs(source_name);
CREATE INDEX idx_sync_errors_job    ON sync_errors(job_id);

-- ── TRIGGERS ─────────────────────────────────────────────────
CREATE OR REPLACE FUNCTION update_updated_at()
RETURNS TRIGGER AS $$
BEGIN NEW.updated_at = NOW(); RETURN NEW; END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_journals_updated_at
    BEFORE UPDATE ON journals FOR EACH ROW EXECUTE FUNCTION update_updated_at();
CREATE TRIGGER trg_authors_updated_at
    BEFORE UPDATE ON authors  FOR EACH ROW EXECUTE FUNCTION update_updated_at();
CREATE TRIGGER trg_papers_updated_at
    BEFORE UPDATE ON papers   FOR EACH ROW EXECUTE FUNCTION update_updated_at();
CREATE TRIGGER trg_sync_cursor_updated_at
    BEFORE UPDATE ON sync_cursors FOR EACH ROW EXECUTE FUNCTION update_updated_at();

-- ── SEED: Sync cursors mặc định ──────────────────────────────
INSERT INTO sync_cursors (source_name) VALUES
    ('OpenAlex'),
    ('SemanticScholar'),
    ('Crossref');
