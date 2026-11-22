START TRANSACTION;
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

CREATE INDEX `IX_campaign_participants_CampaignId_GroupId` ON `campaign_participants` (`CampaignId`, `GroupId`);

CREATE INDEX `IX_campaign_participants_StudentId` ON `campaign_participants` (`StudentId`);

CREATE INDEX `IX_lecturer_priority_lists_LecturerId` ON `lecturer_priority_lists` (`LecturerId`);

CREATE INDEX `IX_lecturer_student_priorities_StudentId` ON `lecturer_student_priorities` (`StudentId`);

CREATE UNIQUE INDEX `IX_lecturer_student_priorities_TopicId_LecturerId_Rank` ON `lecturer_student_priorities` (`TopicId`, `LecturerId`, `Rank`);

CREATE INDEX `IX_project_group_invitations_StudentId` ON `project_group_invitations` (`StudentId`);

CREATE INDEX `IX_project_registration_groups_LeaderId` ON `project_registration_groups` (`LeaderId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261122021530_AddParticipationAndLecturerPriorities', '10.0.14');

COMMIT;
