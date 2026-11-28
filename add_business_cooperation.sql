CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;
IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261110023025_AddTopicManagement')
BEGIN
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
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261110023025_AddTopicManagement')
BEGIN
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
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261110023025_AddTopicManagement')
BEGIN
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
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261110023025_AddTopicManagement')
BEGIN
    CREATE INDEX `IX_topic_attachments_topic_id` ON `topic_attachments` (`topic_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261110023025_AddTopicManagement')
BEGIN
    CREATE INDEX `IX_topic_attachments_uploaded_by_user_id` ON `topic_attachments` (`uploaded_by_user_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261110023025_AddTopicManagement')
BEGIN
    CREATE INDEX `IX_topic_lecturers_lecturer_user_id` ON `topic_lecturers` (`lecturer_user_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261110023025_AddTopicManagement')
BEGIN
    CREATE INDEX `IX_topic_lecturers_topic_id_is_primary` ON `topic_lecturers` (`topic_id`, `is_primary`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261110023025_AddTopicManagement')
BEGIN
    CREATE UNIQUE INDEX `IX_topic_lecturers_topic_id_lecturer_user_id` ON `topic_lecturers` (`topic_id`, `lecturer_user_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261110023025_AddTopicManagement')
BEGIN
    CREATE INDEX `IX_topics_campaign_id_status` ON `topics` (`campaign_id`, `status`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261110023025_AddTopicManagement')
BEGIN
    CREATE INDEX `IX_topics_created_by_user_id` ON `topics` (`created_by_user_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261110023025_AddTopicManagement')
BEGIN
    CREATE INDEX `IX_topics_source_topic_id` ON `topics` (`source_topic_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261110023025_AddTopicManagement')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261110023025_AddTopicManagement', '10.0.14');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261112171651_AddTopicApprovalWorkflow')
BEGIN
    ALTER TABLE `topics` ADD `approval_status` varchar(20) NOT NULL DEFAULT 'PENDING';
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261112171651_AddTopicApprovalWorkflow')
BEGIN
    UPDATE topics SET approval_status = 'APPROVED';
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261112171651_AddTopicApprovalWorkflow')
BEGIN
    ALTER TABLE `topics` ADD `rejection_reason` longtext NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261112171651_AddTopicApprovalWorkflow')
BEGIN
    ALTER TABLE `topics` ADD `reviewed_at` datetime(6) NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261112171651_AddTopicApprovalWorkflow')
BEGIN
    ALTER TABLE `topics` ADD `reviewed_by_user_id` varchar(255) NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261112171651_AddTopicApprovalWorkflow')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261112171651_AddTopicApprovalWorkflow', '10.0.14');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203140_AddTopicProposals')
BEGIN
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
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203140_AddTopicProposals')
BEGIN
    CREATE INDEX `IX_topic_proposals_campaign_id_status` ON `topic_proposals` (`campaign_id`, `status`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203140_AddTopicProposals')
BEGIN
    CREATE INDEX `IX_topic_proposals_lecturer_user_id_status` ON `topic_proposals` (`lecturer_user_id`, `status`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203140_AddTopicProposals')
BEGIN
    CREATE INDEX `IX_topic_proposals_student_user_id_created_at` ON `topic_proposals` (`student_user_id`, `created_at`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203140_AddTopicProposals')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261114203140_AddTopicProposals', '10.0.14');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261118025950_ChangeLecturerQuotaToTerm')
BEGIN
    CREATE TABLE `lecturer_term_quotas` (
        `id` bigint unsigned NOT NULL AUTO_INCREMENT,
        `term_id` bigint unsigned NOT NULL,
        `lecturer_user_id` varchar(255) NOT NULL,
        `capacity` int NOT NULL,
        `updated_at` datetime(6) NOT NULL,
        PRIMARY KEY (`id`),
        CONSTRAINT `FK_lecturer_term_quotas_terms_term_id` FOREIGN KEY (`term_id`) REFERENCES `terms` (`id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261118025950_ChangeLecturerQuotaToTerm')
BEGIN

                    ALTER TABLE lecturer_term_quotas
                    MODIFY lecturer_user_id
                    varchar(255)
                    CHARACTER SET utf8mb4
                    COLLATE utf8mb4_unicode_ci
                    NOT NULL;
                
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261118025950_ChangeLecturerQuotaToTerm')
BEGIN
    ALTER TABLE `lecturer_term_quotas` ADD CONSTRAINT `FK_lecturer_term_quotas_AspNetUsers_lecturer_user_id` FOREIGN KEY (`lecturer_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261118025950_ChangeLecturerQuotaToTerm')
BEGIN
    CREATE INDEX `IX_lecturer_term_quotas_lecturer_user_id` ON `lecturer_term_quotas` (`lecturer_user_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261118025950_ChangeLecturerQuotaToTerm')
BEGIN
    CREATE UNIQUE INDEX `IX_lecturer_term_quotas_term_id_lecturer_user_id` ON `lecturer_term_quotas` (`term_id`, `lecturer_user_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261118025950_ChangeLecturerQuotaToTerm')
BEGIN
    ALTER TABLE `topics` ADD `major_id` bigint unsigned NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261118025950_ChangeLecturerQuotaToTerm')
BEGIN

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
                
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261118025950_ChangeLecturerQuotaToTerm')
BEGIN

                    UPDATE topics t
                    INNER JOIN campaigns c ON c.id = t.campaign_id
                    SET t.major_id = c.major_id;
                
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261118025950_ChangeLecturerQuotaToTerm')
BEGIN
    ALTER TABLE `topics` MODIFY `major_id` bigint unsigned NOT NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261118025950_ChangeLecturerQuotaToTerm')
BEGIN
    CREATE INDEX `IX_topics_major_id` ON `topics` (`major_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261118025950_ChangeLecturerQuotaToTerm')
BEGIN
    ALTER TABLE `topics` ADD CONSTRAINT `FK_topics_majors_major_id` FOREIGN KEY (`major_id`) REFERENCES `majors` (`id`) ON DELETE CASCADE;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261118025950_ChangeLecturerQuotaToTerm')
BEGIN
    DROP TABLE `lecturer_campaign_quotas`;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261118025950_ChangeLecturerQuotaToTerm')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261118025950_ChangeLecturerQuotaToTerm', '10.0.14');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261121161226_AddNoPreferencePolicy')
BEGIN
    ALTER TABLE `campaigns` ADD `no_preference_policy` int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261121161226_AddNoPreferencePolicy')
BEGIN
    ALTER TABLE `campaigns` ADD `random_seed` int NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261121161226_AddNoPreferencePolicy')
BEGIN
    CREATE TABLE `student_topic_preferences` (
        `id` bigint unsigned NOT NULL AUTO_INCREMENT,
        `student_id` varchar(255) NOT NULL,
        `topic_id` bigint unsigned NOT NULL,
        `priority` int NOT NULL,
        `created_at` datetime(6) NOT NULL,
        PRIMARY KEY (`id`),
        CONSTRAINT `FK_student_topic_preferences_AspNetUsers_student_id` FOREIGN KEY (`student_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_student_topic_preferences_topics_topic_id` FOREIGN KEY (`topic_id`) REFERENCES `topics` (`id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261121161226_AddNoPreferencePolicy')
BEGIN
    CREATE TABLE `topic_student_assignments` (
        `id` bigint unsigned NOT NULL AUTO_INCREMENT,
        `topic_id` bigint unsigned NULL,
        `student_user_id` varchar(255) NOT NULL,
        `campaign_id` bigint unsigned NOT NULL,
        `status` int NOT NULL DEFAULT 0,
        `assigned_at` datetime(6) NOT NULL,
        PRIMARY KEY (`id`),
        CONSTRAINT `FK_topic_student_assignments_AspNetUsers_student_user_id` FOREIGN KEY (`student_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_topic_student_assignments_campaigns_campaign_id` FOREIGN KEY (`campaign_id`) REFERENCES `campaigns` (`id`) ON DELETE CASCADE,
        CONSTRAINT `FK_topic_student_assignments_topics_topic_id` FOREIGN KEY (`topic_id`) REFERENCES `topics` (`id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261121161226_AddNoPreferencePolicy')
BEGIN
    CREATE UNIQUE INDEX `IX_student_topic_preferences_student_id_priority` ON `student_topic_preferences` (`student_id`, `priority`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261121161226_AddNoPreferencePolicy')
BEGIN
    CREATE UNIQUE INDEX `IX_student_topic_preferences_student_id_topic_id` ON `student_topic_preferences` (`student_id`, `topic_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261121161226_AddNoPreferencePolicy')
BEGIN
    CREATE INDEX `IX_student_topic_preferences_topic_id` ON `student_topic_preferences` (`topic_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261121161226_AddNoPreferencePolicy')
BEGIN
    CREATE INDEX `IX_topic_student_assignments_campaign_id_status` ON `topic_student_assignments` (`campaign_id`, `status`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261121161226_AddNoPreferencePolicy')
BEGIN
    CREATE INDEX `IX_topic_student_assignments_student_user_id` ON `topic_student_assignments` (`student_user_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261121161226_AddNoPreferencePolicy')
BEGIN
    CREATE UNIQUE INDEX `IX_topic_student_assignments_topic_id_student_user_id` ON `topic_student_assignments` (`topic_id`, `student_user_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261121161226_AddNoPreferencePolicy')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261121161226_AddNoPreferencePolicy', '10.0.14');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261122021530_AddParticipationAndLecturerPriorities')
BEGIN
    CREATE TABLE `campaign_participation_policies` (
        `CampaignId` bigint unsigned NOT NULL,
        `GroupsAllowed` tinyint(1) NOT NULL,
        `MaxGroupSize` int NOT NULL,
        `RegistrationOpensAt` datetime(6) NOT NULL,
        `RegistrationClosesAt` datetime(6) NOT NULL,
        `LecturerRankingEnabled` tinyint(1) NOT NULL,
        `RankingOpensAt` datetime(6) NOT NULL,
        `RankingClosesAt` datetime(6) NOT NULL,
        PRIMARY KEY (`CampaignId`),
        CONSTRAINT `chk_participation_group_size` CHECK (`MaxGroupSize` BETWEEN 2 AND 20),
        CONSTRAINT `FK_campaign_participation_policies_campaigns_CampaignId` FOREIGN KEY (`CampaignId`) REFERENCES `campaigns` (`id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261122021530_AddParticipationAndLecturerPriorities')
BEGIN
    CREATE TABLE `lecturer_priority_lists` (
        `TopicId` bigint unsigned NOT NULL,
        `LecturerId` varchar(255) NOT NULL,
        `Version` varchar(32) NOT NULL,
        `Reason` varchar(1000) NOT NULL,
        `SavedAt` datetime(6) NOT NULL,
        PRIMARY KEY (`TopicId`, `LecturerId`),
        CONSTRAINT `FK_lecturer_priority_lists_AspNetUsers_LecturerId` FOREIGN KEY (`LecturerId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_lecturer_priority_lists_topics_TopicId` FOREIGN KEY (`TopicId`) REFERENCES `topics` (`id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261122021530_AddParticipationAndLecturerPriorities')
BEGIN
    CREATE TABLE `project_registration_groups` (
        `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
        `CampaignId` bigint unsigned NOT NULL,
        `LeaderId` varchar(255) NOT NULL,
        `Name` varchar(100) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `AK_project_registration_groups_CampaignId_Id` UNIQUE (`CampaignId`, `Id`),
        CONSTRAINT `FK_project_registration_groups_AspNetUsers_LeaderId` FOREIGN KEY (`LeaderId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_project_registration_groups_campaigns_CampaignId` FOREIGN KEY (`CampaignId`) REFERENCES `campaigns` (`id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261122021530_AddParticipationAndLecturerPriorities')
BEGIN
    CREATE TABLE `lecturer_student_priorities` (
        `TopicId` bigint unsigned NOT NULL,
        `LecturerId` varchar(255) NOT NULL,
        `StudentId` varchar(255) NOT NULL,
        `Rank` int NOT NULL,
        PRIMARY KEY (`TopicId`, `LecturerId`, `StudentId`),
        CONSTRAINT `chk_lecturer_priority_rank` CHECK (`Rank` > 0),
        CONSTRAINT `FK_lecturer_student_priorities_AspNetUsers_StudentId` FOREIGN KEY (`StudentId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_lecturer_student_priorities_lecturer_priority_lists_TopicId_~` FOREIGN KEY (`TopicId`, `LecturerId`) REFERENCES `lecturer_priority_lists` (`TopicId`, `LecturerId`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261122021530_AddParticipationAndLecturerPriorities')
BEGIN
    CREATE TABLE `campaign_participants` (
        `CampaignId` bigint unsigned NOT NULL,
        `StudentId` varchar(255) NOT NULL,
        `GroupId` bigint unsigned NULL,
        `ConfirmedAt` datetime(6) NOT NULL,
        PRIMARY KEY (`CampaignId`, `StudentId`),
        CONSTRAINT `FK_campaign_participants_AspNetUsers_StudentId` FOREIGN KEY (`StudentId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_campaign_participants_campaigns_CampaignId` FOREIGN KEY (`CampaignId`) REFERENCES `campaigns` (`id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_campaign_participants_project_registration_groups_CampaignId~` FOREIGN KEY (`CampaignId`, `GroupId`) REFERENCES `project_registration_groups` (`CampaignId`, `Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261122021530_AddParticipationAndLecturerPriorities')
BEGIN
    CREATE TABLE `project_group_invitations` (
        `GroupId` bigint unsigned NOT NULL,
        `StudentId` varchar(255) NOT NULL,
        `Status` varchar(20) NOT NULL,
        `InvitedAt` datetime(6) NOT NULL,
        `RespondedAt` datetime(6) NULL,
        PRIMARY KEY (`GroupId`, `StudentId`),
        CONSTRAINT `FK_project_group_invitations_AspNetUsers_StudentId` FOREIGN KEY (`StudentId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_project_group_invitations_project_registration_groups_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `project_registration_groups` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261122021530_AddParticipationAndLecturerPriorities')
BEGIN
    CREATE INDEX `IX_campaign_participants_CampaignId_GroupId` ON `campaign_participants` (`CampaignId`, `GroupId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261122021530_AddParticipationAndLecturerPriorities')
BEGIN
    CREATE INDEX `IX_campaign_participants_StudentId` ON `campaign_participants` (`StudentId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261122021530_AddParticipationAndLecturerPriorities')
BEGIN
    CREATE INDEX `IX_lecturer_priority_lists_LecturerId` ON `lecturer_priority_lists` (`LecturerId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261122021530_AddParticipationAndLecturerPriorities')
BEGIN
    CREATE INDEX `IX_lecturer_student_priorities_StudentId` ON `lecturer_student_priorities` (`StudentId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261122021530_AddParticipationAndLecturerPriorities')
BEGIN
    CREATE UNIQUE INDEX `IX_lecturer_student_priorities_TopicId_LecturerId_Rank` ON `lecturer_student_priorities` (`TopicId`, `LecturerId`, `Rank`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261122021530_AddParticipationAndLecturerPriorities')
BEGIN
    CREATE INDEX `IX_project_group_invitations_StudentId` ON `project_group_invitations` (`StudentId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261122021530_AddParticipationAndLecturerPriorities')
BEGIN
    CREATE INDEX `IX_project_registration_groups_LeaderId` ON `project_registration_groups` (`LeaderId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261122021530_AddParticipationAndLecturerPriorities')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261122021530_AddParticipationAndLecturerPriorities', '10.0.14');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261123142546_AddMatchingOrder')
BEGIN
    ALTER TABLE `campaigns` ADD `matching_order` int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261123142546_AddMatchingOrder')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261123142546_AddMatchingOrder', '10.0.14');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261125013044_AddInternshipRegistrationWorkflow')
BEGIN
    CREATE TABLE `internship_places` (
        `id` bigint unsigned NOT NULL AUTO_INCREMENT,
        `name` varchar(250) NOT NULL,
        `address` varchar(500) NULL,
        `contact_person` varchar(180) NULL,
        `contact_phone` varchar(50) NULL,
        `contact_email` varchar(180) NULL,
        `default_position` varchar(200) NULL,
        `is_active` tinyint(1) NOT NULL,
        `created_by_user_id` varchar(255) NOT NULL,
        `created_at` datetime(6) NOT NULL,
        `updated_at` datetime(6) NOT NULL,
        PRIMARY KEY (`id`),
        CONSTRAINT `FK_internship_places_AspNetUsers_created_by_user_id` FOREIGN KEY (`created_by_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261125013044_AddInternshipRegistrationWorkflow')
BEGIN
    CREATE TABLE `internship_registrations` (
        `id` bigint unsigned NOT NULL AUTO_INCREMENT,
        `campaign_id` bigint unsigned NOT NULL,
        `student_user_id` varchar(255) NOT NULL,
        `internship_place_id` bigint unsigned NOT NULL,
        `position_title` varchar(200) NULL,
        `start_date` date NULL,
        `end_date` date NULL,
        `note` longtext NULL,
        `original_file_name` varchar(255) NULL,
        `stored_file_name` varchar(255) NULL,
        `content_type` varchar(120) NULL,
        `file_size` bigint NULL,
        `approval_status` varchar(20) NOT NULL DEFAULT 'PENDING',
        `rejection_reason` longtext NULL,
        `reviewed_by_user_id` varchar(255) NULL,
        `reviewed_at` datetime(6) NULL,
        `submitted_at` datetime(6) NOT NULL,
        `created_at` datetime(6) NOT NULL,
        `updated_at` datetime(6) NOT NULL,
        PRIMARY KEY (`id`),
        CONSTRAINT `FK_internship_registrations_AspNetUsers_reviewed_by_user_id` FOREIGN KEY (`reviewed_by_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_internship_registrations_AspNetUsers_student_user_id` FOREIGN KEY (`student_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_internship_registrations_campaigns_campaign_id` FOREIGN KEY (`campaign_id`) REFERENCES `campaigns` (`id`) ON DELETE CASCADE,
        CONSTRAINT `FK_internship_registrations_internship_places_internship_place_~` FOREIGN KEY (`internship_place_id`) REFERENCES `internship_places` (`id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261125013044_AddInternshipRegistrationWorkflow')
BEGIN
    CREATE TABLE `internship_registration_reviews` (
        `id` bigint unsigned NOT NULL AUTO_INCREMENT,
        `registration_id` bigint unsigned NOT NULL,
        `attempt_no` int NOT NULL,
        `action` varchar(30) NOT NULL,
        `comment` longtext NULL,
        `place_name_snapshot` varchar(250) NULL,
        `position_snapshot` varchar(200) NULL,
        `actor_user_id` varchar(255) NOT NULL,
        `created_at` datetime(6) NOT NULL,
        PRIMARY KEY (`id`),
        CONSTRAINT `FK_internship_registration_reviews_AspNetUsers_actor_user_id` FOREIGN KEY (`actor_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_internship_registration_reviews_internship_registrations_reg~` FOREIGN KEY (`registration_id`) REFERENCES `internship_registrations` (`id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261125013044_AddInternshipRegistrationWorkflow')
BEGIN
    CREATE INDEX `IX_internship_places_created_by_user_id` ON `internship_places` (`created_by_user_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261125013044_AddInternshipRegistrationWorkflow')
BEGIN
    CREATE INDEX `IX_internship_places_is_active` ON `internship_places` (`is_active`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261125013044_AddInternshipRegistrationWorkflow')
BEGIN
    CREATE INDEX `IX_internship_places_name` ON `internship_places` (`name`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261125013044_AddInternshipRegistrationWorkflow')
BEGIN
    CREATE INDEX `IX_internship_registration_reviews_actor_user_id` ON `internship_registration_reviews` (`actor_user_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261125013044_AddInternshipRegistrationWorkflow')
BEGIN
    CREATE INDEX `IX_internship_registration_reviews_registration_id_created_at` ON `internship_registration_reviews` (`registration_id`, `created_at`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261125013044_AddInternshipRegistrationWorkflow')
BEGIN
    CREATE INDEX `IX_internship_registrations_campaign_id_approval_status` ON `internship_registrations` (`campaign_id`, `approval_status`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261125013044_AddInternshipRegistrationWorkflow')
BEGIN
    CREATE INDEX `IX_internship_registrations_campaign_id_student_user_id` ON `internship_registrations` (`campaign_id`, `student_user_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261125013044_AddInternshipRegistrationWorkflow')
BEGIN
    CREATE INDEX `IX_internship_registrations_internship_place_id` ON `internship_registrations` (`internship_place_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261125013044_AddInternshipRegistrationWorkflow')
BEGIN
    CREATE INDEX `IX_internship_registrations_reviewed_by_user_id` ON `internship_registrations` (`reviewed_by_user_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261125013044_AddInternshipRegistrationWorkflow')
BEGIN
    CREATE INDEX `IX_internship_registrations_student_user_id` ON `internship_registrations` (`student_user_id`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261125013044_AddInternshipRegistrationWorkflow')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261125013044_AddInternshipRegistrationWorkflow', '10.0.14');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261127085953_AddBusinessCooperationToInternshipPlace')
BEGIN
    ALTER TABLE `internship_places` ADD `cooperation_note` varchar(1000) NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261127085953_AddBusinessCooperationToInternshipPlace')
BEGIN
    ALTER TABLE `internship_places` ADD `cooperation_status` varchar(40) NOT NULL DEFAULT '';
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261127085953_AddBusinessCooperationToInternshipPlace')
BEGIN
    ALTER TABLE `internship_places` ADD `tax_code` varchar(50) NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261127085953_AddBusinessCooperationToInternshipPlace')
BEGIN
    ALTER TABLE `internship_places` ADD `website` varchar(250) NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261127085953_AddBusinessCooperationToInternshipPlace')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261127085953_AddBusinessCooperationToInternshipPlace', '10.0.14');
END;

COMMIT;

