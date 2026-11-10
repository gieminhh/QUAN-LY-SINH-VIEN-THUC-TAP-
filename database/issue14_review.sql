CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;
CREATE TABLE `topics` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT,
    `campaign_id` bigint unsigned NOT NULL,
    `title` varchar(250) NOT NULL,
    `description` longtext NOT NULL,
    `objectives` longtext NULL,
    `specialty` varchar(1000) NULL,
    `technologies` varchar(1000) NULL,
    `requirements` longtext NULL,
    `topic_type` varchar(20) NOT NULL,
    `max_students` int NULL,
    `max_groups` int NULL,
    `status` varchar(20) NOT NULL,
    `created_by_user_id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `source_topic_id` bigint unsigned NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `FK_topics_AspNetUsers_created_by_user_id` FOREIGN KEY (`created_by_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_topics_campaigns_campaign_id` FOREIGN KEY (`campaign_id`) REFERENCES `campaigns` (`id`) ON DELETE CASCADE,
    CONSTRAINT `FK_topics_topics_source_topic_id` FOREIGN KEY (`source_topic_id`) REFERENCES `topics` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `topic_attachments` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT,
    `topic_id` bigint unsigned NOT NULL,
    `original_file_name` varchar(255) NOT NULL,
    `stored_file_name` varchar(255) NOT NULL,
    `content_type` varchar(100) NULL,
    `file_size` bigint NOT NULL,
    `uploaded_by_user_id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `uploaded_at` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `FK_topic_attachments_AspNetUsers_uploaded_by_user_id` FOREIGN KEY (`uploaded_by_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_topic_attachments_topics_topic_id` FOREIGN KEY (`topic_id`) REFERENCES `topics` (`id`) ON DELETE CASCADE
);

CREATE TABLE `topic_lecturers` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT,
    `topic_id` bigint unsigned NOT NULL,
    `lecturer_user_id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `is_primary` tinyint(1) NOT NULL,
    `joined_at` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `FK_topic_lecturers_AspNetUsers_lecturer_user_id` FOREIGN KEY (`lecturer_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_topic_lecturers_topics_topic_id` FOREIGN KEY (`topic_id`) REFERENCES `topics` (`id`) ON DELETE CASCADE
);

CREATE INDEX `IX_topic_attachments_topic_id` ON `topic_attachments` (`topic_id`);

CREATE INDEX `IX_topic_attachments_uploaded_by_user_id` ON `topic_attachments` (`uploaded_by_user_id`);

CREATE INDEX `IX_topic_lecturers_lecturer_user_id` ON `topic_lecturers` (`lecturer_user_id`);

CREATE INDEX `IX_topic_lecturers_topic_id_is_primary` ON `topic_lecturers` (`topic_id`, `is_primary`);

CREATE UNIQUE INDEX `IX_topic_lecturers_topic_id_lecturer_user_id` ON `topic_lecturers` (`topic_id`, `lecturer_user_id`);

CREATE INDEX `IX_topics_campaign_id_status` ON `topics` (`campaign_id`, `status`);

CREATE INDEX `IX_topics_created_by_user_id` ON `topics` (`created_by_user_id`);

CREATE INDEX `IX_topics_source_topic_id` ON `topics` (`source_topic_id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261110023025_AddTopicManagement', '10.0.14');

COMMIT;

