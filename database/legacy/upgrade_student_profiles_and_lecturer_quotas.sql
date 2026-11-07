-- Run once against the existing ql_thuctap_doan database before starting the updated application.
ALTER TABLE `AspNetUsers`
    ADD COLUMN `class_name` varchar(100) NULL,
    ADD COLUMN `major_id` bigint unsigned NULL,
    ADD COLUMN `skills` varchar(2000) NULL,
    ADD COLUMN `interests` varchar(2000) NULL,
    ADD COLUMN `cv_stored_file_name` varchar(255) NULL,
    ADD COLUMN `lecturer_specialty` varchar(1000) NULL,
    ADD KEY `IX_AspNetUsers_major_id` (`major_id`),
    ADD CONSTRAINT `FK_AspNetUsers_majors_major_id`
        FOREIGN KEY (`major_id`) REFERENCES `majors` (`id`) ON DELETE SET NULL;

CREATE TABLE `lecturer_campaign_quotas` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT,
    `campaign_id` bigint unsigned NOT NULL,
    `lecturer_user_id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
    `capacity` int NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `IX_lecturer_campaign_quotas_campaign_lecturer` (`campaign_id`, `lecturer_user_id`),
    CONSTRAINT `FK_lecturer_campaign_quotas_campaigns_campaign_id`
        FOREIGN KEY (`campaign_id`) REFERENCES `campaigns` (`id`) ON DELETE CASCADE,
    CONSTRAINT `FK_lecturer_campaign_quotas_AspNetUsers_lecturer_user_id`
        FOREIGN KEY (`lecturer_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `lecturer_campaign_assignments` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT,
    `campaign_id` bigint unsigned NOT NULL,
    `lecturer_user_id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
    `student_user_id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
    `status` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
    `exception_reason` varchar(1000) COLLATE utf8mb4_unicode_ci NULL,
    `reviewed_by_user_id` varchar(255) COLLATE utf8mb4_unicode_ci NULL,
    `reviewed_at` datetime(6) NULL,
    `created_at` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    KEY `IX_lecturer_campaign_assignments_campaign_student_status` (`campaign_id`, `student_user_id`, `status`),
    KEY `IX_lecturer_campaign_assignments_campaign_lecturer_status` (`campaign_id`, `lecturer_user_id`, `status`),
    CONSTRAINT `FK_lecturer_campaign_assignments_campaigns_campaign_id`
        FOREIGN KEY (`campaign_id`) REFERENCES `campaigns` (`id`) ON DELETE CASCADE,
    CONSTRAINT `FK_lecturer_campaign_assignments_AspNetUsers_lecturer_user_id`
        FOREIGN KEY (`lecturer_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_lecturer_campaign_assignments_AspNetUsers_student_user_id`
        FOREIGN KEY (`student_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_lecturer_campaign_assignments_AspNetUsers_reviewed_by_user_id`
        FOREIGN KEY (`reviewed_by_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
