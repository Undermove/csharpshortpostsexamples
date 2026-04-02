CREATE TABLE IF NOT EXISTS rate_limit_counters (
    partition_key VARCHAR(256) NOT NULL,
    window_id     VARCHAR(64)  NOT NULL,
    request_count INT          NOT NULL DEFAULT 0,
    expires_at    DATETIME(3)  NOT NULL,
    PRIMARY KEY (partition_key, window_id)
) ENGINE=InnoDB;
