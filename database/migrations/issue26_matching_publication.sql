-- Issue #26 only: publication rounds, snapshots and supplementary preferences.
-- Select the deployment schema first. Requires the existing EF migration history table.
-- No changes to pre-existing application tables; run once per deployment schema.

START TRANSACTION;
CREATE TABLE `matching_rounds` (
    `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
    `CampaignId` bigint unsigned NOT NULL,
    `Number` int NOT NULL,
    `State` int NOT NULL,
    `OpenedAt` datetime(6) NOT NULL,
    `ClosesAt` datetime(6) NULL,
    `OpenedBy` varchar(255) NOT NULL,
    `PublishedAt` datetime(6) NULL,
    `PublishedBy` varchar(255) NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_matching_rounds_AspNetUsers_OpenedBy` FOREIGN KEY (`OpenedBy`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_matching_rounds_AspNetUsers_PublishedBy` FOREIGN KEY (`PublishedBy`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_matching_rounds_campaigns_CampaignId` FOREIGN KEY (`CampaignId`) REFERENCES `campaigns` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `matching_published_results` (
    `RoundId` bigint unsigned NOT NULL,
    `StudentId` varchar(255) NOT NULL,
    `TopicId` bigint unsigned NULL,
    `TopicTitle` varchar(250) NULL,
    `Status` int NOT NULL,
    `ConfirmedAt` datetime(6) NULL,
    PRIMARY KEY (`RoundId`, `StudentId`),
    CONSTRAINT `FK_matching_published_results_AspNetUsers_StudentId` FOREIGN KEY (`StudentId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_matching_published_results_matching_rounds_RoundId` FOREIGN KEY (`RoundId`) REFERENCES `matching_rounds` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_matching_published_results_topics_TopicId` FOREIGN KEY (`TopicId`) REFERENCES `topics` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `matching_round_preferences` (
    `RoundId` bigint unsigned NOT NULL,
    `StudentId` varchar(255) NOT NULL,
    `TopicId` bigint unsigned NOT NULL,
    `Priority` int NOT NULL,
    `SubmittedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`RoundId`, `StudentId`, `TopicId`),
    CONSTRAINT `FK_matching_round_preferences_AspNetUsers_StudentId` FOREIGN KEY (`StudentId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_matching_round_preferences_matching_rounds_RoundId` FOREIGN KEY (`RoundId`) REFERENCES `matching_rounds` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_matching_round_preferences_topics_TopicId` FOREIGN KEY (`TopicId`) REFERENCES `topics` (`id`) ON DELETE RESTRICT
);

CREATE INDEX `IX_matching_published_results_StudentId` ON `matching_published_results` (`StudentId`);

CREATE INDEX `IX_matching_published_results_TopicId` ON `matching_published_results` (`TopicId`);

CREATE UNIQUE INDEX `IX_matching_round_preferences_RoundId_StudentId_Priority` ON `matching_round_preferences` (`RoundId`, `StudentId`, `Priority`);

CREATE INDEX `IX_matching_round_preferences_StudentId` ON `matching_round_preferences` (`StudentId`);

CREATE INDEX `IX_matching_round_preferences_TopicId` ON `matching_round_preferences` (`TopicId`);

CREATE UNIQUE INDEX `IX_matching_rounds_CampaignId_Number` ON `matching_rounds` (`CampaignId`, `Number`);

CREATE INDEX `IX_matching_rounds_OpenedBy` ON `matching_rounds` (`OpenedBy`);

CREATE INDEX `IX_matching_rounds_PublishedBy` ON `matching_rounds` (`PublishedBy`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261128125053_AddMatchingPublication', '10.0.14');

COMMIT;
