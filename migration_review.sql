START TRANSACTION;
CREATE TABLE `topic_proposals` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT,
    `campaign_id` bigint unsigned NOT NULL,
    `student_user_id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `lecturer_user_id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `title` varchar(250) NOT NULL,
    `description` longtext NOT NULL,
    `objectives` longtext NULL,
    `student_note` longtext NULL,
    `status` varchar(30) NOT NULL,
    `review_feedback` longtext NULL,
    `reviewed_at` datetime(6) NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `FK_topic_proposals_AspNetUsers_lecturer_user_id` FOREIGN KEY (`lecturer_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_topic_proposals_AspNetUsers_student_user_id` FOREIGN KEY (`student_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_topic_proposals_campaigns_campaign_id` FOREIGN KEY (`campaign_id`) REFERENCES `campaigns` (`id`) ON DELETE RESTRICT
);

CREATE INDEX `IX_topic_proposals_campaign_id_status` ON `topic_proposals` (`campaign_id`, `status`);

CREATE INDEX `IX_topic_proposals_lecturer_user_id_status` ON `topic_proposals` (`lecturer_user_id`, `status`);

CREATE INDEX `IX_topic_proposals_student_user_id_created_at` ON `topic_proposals` (`student_user_id`, `created_at`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261114203140_AddTopicProposals', '10.0.14');

CREATE TABLE `lecturer_term_quotas` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT,
    `term_id` bigint unsigned NOT NULL,
    `lecturer_user_id` varchar(255) NOT NULL,
    `capacity` int NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `FK_lecturer_term_quotas_terms_term_id` FOREIGN KEY (`term_id`) REFERENCES `terms` (`id`) ON DELETE CASCADE
);


                ALTER TABLE lecturer_term_quotas
                MODIFY lecturer_user_id
                varchar(255)
                CHARACTER SET utf8mb4
                COLLATE utf8mb4_unicode_ci
                NOT NULL;
            

ALTER TABLE `lecturer_term_quotas` ADD CONSTRAINT `FK_lecturer_term_quotas_AspNetUsers_lecturer_user_id` FOREIGN KEY (`lecturer_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE;

CREATE INDEX `IX_lecturer_term_quotas_lecturer_user_id` ON `lecturer_term_quotas` (`lecturer_user_id`);

CREATE UNIQUE INDEX `IX_lecturer_term_quotas_term_id_lecturer_user_id` ON `lecturer_term_quotas` (`term_id`, `lecturer_user_id`);

ALTER TABLE `topics` ADD `major_id` bigint unsigned NULL;


                INSERT INTO lecturer_term_quotas
                    (term_id, lecturer_user_id, capacity, updated_at)
                SELECT
                    c.term_id,
                    q.lecturer_user_id,
                    SUM(q.capacity),
                    MAX(q.updated_at)
                FROM lecturer_campaign_quotas q
                INNER JOIN campaigns c ON c.id = q.campaign_id
                GROUP BY c.term_id, q.lecturer_user_id;
            


                UPDATE topics t
                INNER JOIN campaigns c ON c.id = t.campaign_id
                SET t.major_id = c.major_id;
            

ALTER TABLE `topics` MODIFY `major_id` bigint unsigned NOT NULL;

CREATE INDEX `IX_topics_major_id` ON `topics` (`major_id`);

ALTER TABLE `topics` ADD CONSTRAINT `FK_topics_majors_major_id` FOREIGN KEY (`major_id`) REFERENCES `majors` (`id`) ON DELETE CASCADE;

DROP TABLE `lecturer_campaign_quotas`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261118025950_ChangeLecturerQuotaToTerm', '10.0.14');

COMMIT;

