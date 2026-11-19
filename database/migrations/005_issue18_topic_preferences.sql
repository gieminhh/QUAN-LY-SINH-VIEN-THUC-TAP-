-- Issue #18. Apply manually to the explicitly selected database for this copy.
-- Prerequisites: campaigns, student_profiles and topics.
CREATE TABLE IF NOT EXISTS student_topic_preference_lists (
    CampaignId BIGINT UNSIGNED NOT NULL,
    StudentProfileId BIGINT UNSIGNED NOT NULL,
    ResearchDirection VARCHAR(180) NULL,
    Version CHAR(32) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    SavedAt DATETIME(6) NOT NULL,
    PRIMARY KEY (CampaignId, StudentProfileId),
    CONSTRAINT fk_preference_list_campaign FOREIGN KEY (CampaignId) REFERENCES campaigns(id),
    CONSTRAINT fk_preference_list_student FOREIGN KEY (StudentProfileId) REFERENCES student_profiles(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS student_topic_preferences (
    CampaignId BIGINT UNSIGNED NOT NULL,
    StudentProfileId BIGINT UNSIGNED NOT NULL,
    PreferenceRank INT UNSIGNED NOT NULL,
    TopicId BIGINT UNSIGNED NOT NULL,
    PRIMARY KEY (CampaignId, StudentProfileId, PreferenceRank),
    UNIQUE KEY uq_preference_topic (CampaignId, StudentProfileId, TopicId),
    KEY ix_preference_topic (TopicId),
    CONSTRAINT fk_preference_list FOREIGN KEY (CampaignId, StudentProfileId)
        REFERENCES student_topic_preference_lists(CampaignId, StudentProfileId) ON DELETE CASCADE,
    CONSTRAINT fk_preference_topic FOREIGN KEY (TopicId) REFERENCES topics(id),
    CONSTRAINT chk_preference_rank CHECK (PreferenceRank > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
