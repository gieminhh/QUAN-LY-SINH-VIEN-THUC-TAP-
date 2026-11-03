-- Full MySQL 8 schema and demo data. Import into an EMPTY database.
-- Exported 2026-11-03. Account hashes replaced with documented DEMO passwords.
-- This file does not contain CREATE DATABASE or USE; select the destination before import.

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
DROP TABLE IF EXISTS `academic_years`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `academic_years` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `code` varchar(15) COLLATE utf8mb4_unicode_ci NOT NULL,
  `start_date` date NOT NULL,
  `end_date` date NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_academic_years_code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `academic_years` WRITE;
/*!40000 ALTER TABLE `academic_years` DISABLE KEYS */;
INSERT INTO `academic_years` (`id`, `code`, `start_date`, `end_date`) VALUES (1,'2026-2027','2026-09-01','2027-06-30');
INSERT INTO `academic_years` (`id`, `code`, `start_date`, `end_date`) VALUES (2,'2025-2026','2025-09-01','2026-06-30');
/*!40000 ALTER TABLE `academic_years` ENABLE KEYS */;
UNLOCK TABLES;
DROP TABLE IF EXISTS `AspNetRoleClaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AspNetRoleClaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `RoleId` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ClaimType` longtext COLLATE utf8mb4_unicode_ci,
  `ClaimValue` longtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`Id`),
  KEY `IX_RoleClaims_RoleId` (`RoleId`),
  CONSTRAINT `FK_RoleClaims_Roles` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `AspNetRoleClaims` WRITE;
/*!40000 ALTER TABLE `AspNetRoleClaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `AspNetRoleClaims` ENABLE KEYS */;
UNLOCK TABLES;
DROP TABLE IF EXISTS `AspNetRoles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AspNetRoles` (
  `Id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Name` varchar(256) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `NormalizedName` varchar(256) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ConcurrencyStamp` longtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `AspNetRoles` WRITE;
/*!40000 ALTER TABLE `AspNetRoles` DISABLE KEYS */;
INSERT INTO `AspNetRoles` (`Id`, `Name`, `NormalizedName`, `ConcurrencyStamp`) VALUES ('role_admin','Admin','ADMIN',NULL);
INSERT INTO `AspNetRoles` (`Id`, `Name`, `NormalizedName`, `ConcurrencyStamp`) VALUES ('role_company','Company','COMPANY',NULL);
INSERT INTO `AspNetRoles` (`Id`, `Name`, `NormalizedName`, `ConcurrencyStamp`) VALUES ('role_faculty_manager','FacultyManager','FACULTYMANAGER',NULL);
INSERT INTO `AspNetRoles` (`Id`, `Name`, `NormalizedName`, `ConcurrencyStamp`) VALUES ('role_lecturer','Lecturer','LECTURER',NULL);
INSERT INTO `AspNetRoles` (`Id`, `Name`, `NormalizedName`, `ConcurrencyStamp`) VALUES ('role_student','Student','STUDENT',NULL);
INSERT INTO `AspNetRoles` (`Id`, `Name`, `NormalizedName`, `ConcurrencyStamp`) VALUES ('role_training_manager','TrainingManager','TRAININGMANAGER',NULL);
/*!40000 ALTER TABLE `AspNetRoles` ENABLE KEYS */;
UNLOCK TABLES;
DROP TABLE IF EXISTS `AspNetUserClaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AspNetUserClaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ClaimType` longtext COLLATE utf8mb4_unicode_ci,
  `ClaimValue` longtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`Id`),
  KEY `IX_UserClaims_UserId` (`UserId`),
  CONSTRAINT `FK_UserClaims_Users` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `AspNetUserClaims` WRITE;
/*!40000 ALTER TABLE `AspNetUserClaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `AspNetUserClaims` ENABLE KEYS */;
UNLOCK TABLES;
DROP TABLE IF EXISTS `AspNetUserLogins`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AspNetUserLogins` (
  `LoginProvider` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ProviderKey` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ProviderDisplayName` longtext COLLATE utf8mb4_unicode_ci,
  `UserId` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`LoginProvider`,`ProviderKey`),
  KEY `IX_UserLogins_UserId` (`UserId`),
  CONSTRAINT `FK_UserLogins_Users` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `AspNetUserLogins` WRITE;
/*!40000 ALTER TABLE `AspNetUserLogins` DISABLE KEYS */;
/*!40000 ALTER TABLE `AspNetUserLogins` ENABLE KEYS */;
UNLOCK TABLES;
DROP TABLE IF EXISTS `AspNetUserRoles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AspNetUserRoles` (
  `UserId` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `RoleId` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `IX_UserRoles_RoleId` (`RoleId`),
  CONSTRAINT `FK_UserRoles_Roles` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_UserRoles_Users` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `AspNetUserRoles` WRITE;
/*!40000 ALTER TABLE `AspNetUserRoles` DISABLE KEYS */;
INSERT INTO `AspNetUserRoles` (`UserId`, `RoleId`) VALUES ('f454486c-479c-4765-9050-450301406f0b','role_admin');
INSERT INTO `AspNetUserRoles` (`UserId`, `RoleId`) VALUES ('2497a097-65c6-4447-ac8e-e4485dae3291','role_company');
INSERT INTO `AspNetUserRoles` (`UserId`, `RoleId`) VALUES ('c1989175-b03e-4292-98ae-a9ab042c394b','role_faculty_manager');
INSERT INTO `AspNetUserRoles` (`UserId`, `RoleId`) VALUES ('dfe628a0-20eb-4756-91fd-075e6ff74851','role_lecturer');
INSERT INTO `AspNetUserRoles` (`UserId`, `RoleId`) VALUES ('1840c602-2711-461a-a2f1-d9d375dc6be1','role_student');
INSERT INTO `AspNetUserRoles` (`UserId`, `RoleId`) VALUES ('3641841a-8923-4958-8859-749253aac6cf','role_student');
INSERT INTO `AspNetUserRoles` (`UserId`, `RoleId`) VALUES ('b221cae9-17b6-4350-adea-dff8146ac0eb','role_training_manager');
/*!40000 ALTER TABLE `AspNetUserRoles` ENABLE KEYS */;
UNLOCK TABLES;
DROP TABLE IF EXISTS `AspNetUsers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AspNetUsers` (
  `Id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `UserName` varchar(256) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `NormalizedUserName` varchar(256) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Email` varchar(256) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `NormalizedEmail` varchar(256) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `EmailConfirmed` tinyint(1) NOT NULL DEFAULT '0',
  `PasswordHash` longtext COLLATE utf8mb4_unicode_ci,
  `SecurityStamp` longtext COLLATE utf8mb4_unicode_ci,
  `ConcurrencyStamp` longtext COLLATE utf8mb4_unicode_ci,
  `PhoneNumber` longtext COLLATE utf8mb4_unicode_ci,
  `PhoneNumberConfirmed` tinyint(1) NOT NULL DEFAULT '0',
  `TwoFactorEnabled` tinyint(1) NOT NULL DEFAULT '0',
  `LockoutEnd` datetime(6) DEFAULT NULL,
  `LockoutEnabled` tinyint(1) NOT NULL DEFAULT '1',
  `AccessFailedCount` int NOT NULL DEFAULT '0',
  `FullName` varchar(180) COLLATE utf8mb4_unicode_ci NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UserNameIndex` (`NormalizedUserName`),
  KEY `EmailIndex` (`NormalizedEmail`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `AspNetUsers` WRITE;
/*!40000 ALTER TABLE `AspNetUsers` DISABLE KEYS */;
INSERT INTO `AspNetUsers` (`Id`, `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount`, `FullName`, `IsActive`, `CreatedAt`) VALUES ('1840c602-2711-461a-a2f1-d9d375dc6be1','test123','TEST123','235748010110013@vinhuni.edu.vn','235748010110013@VINHUNI.EDU.VN',1,'AQAAAAIAAYagAAAAEPFX7PzBg4TgUb01C3nmRziGOhupKR/OBoEdkZ/5voWIQwfMI3wPZ5f1X97lgT1c9w==','bae363b95dbd42e887ee3cdb510f3f5e','04736414-a374-4a8a-8ac0-36acf3ead9b7',NULL,0,0,NULL,1,0,'test',1,'2026-11-03 06:28:35.980376');
INSERT INTO `AspNetUsers` (`Id`, `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount`, `FullName`, `IsActive`, `CreatedAt`) VALUES ('2497a097-65c6-4447-ac8e-e4485dae3291','doanhnghiep_demo','DOANHNGHIEP_DEMO','doanhnghiep.demo@example.com','DOANHNGHIEP.DEMO@EXAMPLE.COM',1,'AQAAAAIAAYagAAAAEEDA3iGy1gtacY3ZbMglj+ayAHabZta6UZHE2FcgeVfxlm5i64sqgMlICRCHqAfuKA==','a90bbabbb3104ab1ac3b9c56cb280e5c','9e15d403-e6a1-4cf1-af9e-7c300772112d',NULL,0,0,NULL,1,0,'Doanh nghiệp Demo',1,'2026-11-02 02:50:17.092196');
INSERT INTO `AspNetUsers` (`Id`, `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount`, `FullName`, `IsActive`, `CreatedAt`) VALUES ('3641841a-8923-4958-8859-749253aac6cf','student','STUDENT','student@vinhuni.edu.vn','STUDENT@VINHUNI.EDU.VN',1,'AQAAAAIAAYagAAAAEP4lcqdUQ/8HOYhtyQX3LGLrZpf7iubtWP3iJB261hWWQDZc58MH7ShLkLdRWsEbug==','679c7777758b4e039f0eaab0d129caf3','dae56f42-70b2-4c24-8b79-d5716b4a6cc9',NULL,0,0,NULL,1,0,'Sinh viên Demo',1,'2026-11-02 02:20:11.467638');
INSERT INTO `AspNetUsers` (`Id`, `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount`, `FullName`, `IsActive`, `CreatedAt`) VALUES ('b221cae9-17b6-4350-adea-dff8146ac0eb','cb_daotao','CB_DAOTAO','cb_daotao@vinhuni.edu.vn','CB_DAOTAO@VINHUNI.EDU.VN',1,'AQAAAAIAAYagAAAAECXuM/8j5T437eApkDyabjptTcqiMHUri9oL2fap/Xg2/3wQfVDroU0MlHddASaCHg==','3bf8b05f5445470d94608a870ad6aa27','a2d61f60-7dd6-46bf-888a-acefa17e9680',NULL,0,0,NULL,1,0,'Trần Văn Cán Bộ',1,'2026-11-02 02:22:28.340540');
INSERT INTO `AspNetUsers` (`Id`, `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount`, `FullName`, `IsActive`, `CreatedAt`) VALUES ('c1989175-b03e-4292-98ae-a9ab042c394b','cb_kinhte','CB_KINHTE','cb_kinhte@vinhuni.edu.vn','CB_KINHTE@VINHUNI.EDU.VN',1,'AQAAAAIAAYagAAAAEJa+hYuN5hHBy1l1hKpe8GuonSeyPHxHmNt36dowgjtsPNk4o5PaTJEuoBBSGtBf1g==','f062ee0315344af7a4337690f517a59b','64e71f65-21d8-4430-a24a-cb35f207a0fb',NULL,0,0,NULL,1,0,'Cán bộ Khoa Kinh tế',1,'2026-11-02 02:22:29.359345');
INSERT INTO `AspNetUsers` (`Id`, `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount`, `FullName`, `IsActive`, `CreatedAt`) VALUES ('dfe628a0-20eb-4756-91fd-075e6ff74851','giangvien_demo','GIANGVIEN_DEMO','giangvien.demo@example.com','GIANGVIEN.DEMO@EXAMPLE.COM',1,'AQAAAAIAAYagAAAAEOVJOUL9hoXCMk8MXtO6rTVja3J9BicdSfWzlFhf2Uyu3qAGEfus424yTEL/y8BHhQ==','f98d068b46594834ac07b6696c539189','8e345f9d-3896-475d-883e-cc095560c23f',NULL,0,0,NULL,1,0,'Giảng viên Demo',1,'2026-11-02 02:49:56.255517');
INSERT INTO `AspNetUsers` (`Id`, `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount`, `FullName`, `IsActive`, `CreatedAt`) VALUES ('f454486c-479c-4765-9050-450301406f0b','admin','ADMIN','admin@vinhuni.edu.vn','ADMIN@VINHUNI.EDU.VN',1,'AQAAAAIAAYagAAAAEJAcRPGQ4H5YjEVvZMBtMGn+hVOX4JDQaDkPJrG6cE5dlLfgAeIEEmQywkWZstq+sA==','d300fc62196f44c39d3422bf16d10aa3','dad44840-2883-489e-ae36-6079a4b685e1',NULL,0,0,NULL,1,0,'Quản trị viên Hệ thống',1,'2026-11-02 02:20:11.141682');
/*!40000 ALTER TABLE `AspNetUsers` ENABLE KEYS */;
UNLOCK TABLES;
DROP TABLE IF EXISTS `AspNetUserTokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AspNetUserTokens` (
  `UserId` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `LoginProvider` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Name` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Value` longtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`UserId`,`LoginProvider`,`Name`),
  CONSTRAINT `FK_UserTokens_Users` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `AspNetUserTokens` WRITE;
/*!40000 ALTER TABLE `AspNetUserTokens` DISABLE KEYS */;
/*!40000 ALTER TABLE `AspNetUserTokens` ENABLE KEYS */;
UNLOCK TABLES;
DROP TABLE IF EXISTS `campaigns`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `campaigns` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `code` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `name` varchar(250) COLLATE utf8mb4_unicode_ci NOT NULL,
  `term_id` bigint unsigned NOT NULL,
  `major_id` bigint unsigned NOT NULL,
  `kind` enum('PROJECT','INTERNSHIP') COLLATE utf8mb4_unicode_ci NOT NULL,
  `training_system` varchar(80) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Hệ đào tạo, vd: CHINH_QUY',
  `start_date` date NOT NULL,
  `end_date` date NOT NULL,
  `status` enum('DRAFT','OPEN','CLOSED','ARCHIVED') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'DRAFT',
  `created_by_user_id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `updated_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_campaigns_code` (`code`),
  KEY `ix_campaigns_filters` (`term_id`,`major_id`,`kind`,`status`),
  KEY `ix_campaigns_major_id` (`major_id`),
  KEY `ix_campaigns_created_by` (`created_by_user_id`),
  CONSTRAINT `fk_campaign_creator` FOREIGN KEY (`created_by_user_id`) REFERENCES `AspNetUsers` (`Id`),
  CONSTRAINT `fk_campaign_major` FOREIGN KEY (`major_id`) REFERENCES `majors` (`id`),
  CONSTRAINT `fk_campaign_term` FOREIGN KEY (`term_id`) REFERENCES `terms` (`id`),
  CONSTRAINT `chk_campaign_dates` CHECK ((`end_date` >= `start_date`))
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `campaigns` WRITE;
/*!40000 ALTER TABLE `campaigns` DISABLE KEYS */;
INSERT INTO `campaigns` (`id`, `code`, `name`, `term_id`, `major_id`, `kind`, `training_system`, `start_date`, `end_date`, `status`, `created_by_user_id`, `created_at`, `updated_at`) VALUES (2,'DA_CNTT_K63','Đồ án tốt nghiệp CNTT Khóa 63',1,3,'PROJECT','CHINH_QUY','2026-09-01','2027-01-15','OPEN','b221cae9-17b6-4350-adea-dff8146ac0eb','2026-11-02 02:22:28.789413','2026-11-02 02:22:28.949699');
/*!40000 ALTER TABLE `campaigns` ENABLE KEYS */;
UNLOCK TABLES;
DROP TABLE IF EXISTS `faculties`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `faculties` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `code` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `name` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_faculties_code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `faculties` WRITE;
/*!40000 ALTER TABLE `faculties` DISABLE KEYS */;
INSERT INTO `faculties` (`id`, `code`, `name`, `is_active`) VALUES (1,'CNTT','Khoa Công nghệ Thông tin',1);
INSERT INTO `faculties` (`id`, `code`, `name`, `is_active`) VALUES (2,'KTKD','Khoa Kinh tế & Quản trị Kinh doanh',1);
INSERT INTO `faculties` (`id`, `code`, `name`, `is_active`) VALUES (3,'SPKT','Khoa Sư phạm Kỹ thuật',1);
/*!40000 ALTER TABLE `faculties` ENABLE KEYS */;
UNLOCK TABLES;
DROP TABLE IF EXISTS `majors`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `majors` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `faculty_id` bigint unsigned NOT NULL,
  `code` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `name` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_majors_code` (`code`),
  KEY `ix_majors_faculty_id` (`faculty_id`),
  CONSTRAINT `fk_majors_faculties` FOREIGN KEY (`faculty_id`) REFERENCES `faculties` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `majors` WRITE;
/*!40000 ALTER TABLE `majors` DISABLE KEYS */;
INSERT INTO `majors` (`id`, `faculty_id`, `code`, `name`, `is_active`) VALUES (1,1,'7480201','Công nghệ Thông tin',1);
INSERT INTO `majors` (`id`, `faculty_id`, `code`, `name`, `is_active`) VALUES (2,1,'7480103','Kỹ thuật Phần mềm',1);
INSERT INTO `majors` (`id`, `faculty_id`, `code`, `name`, `is_active`) VALUES (3,1,'7480104','Hệ thống Thông tin',1);
INSERT INTO `majors` (`id`, `faculty_id`, `code`, `name`, `is_active`) VALUES (4,2,'7340101','Quản trị Kinh doanh',1);
/*!40000 ALTER TABLE `majors` ENABLE KEYS */;
UNLOCK TABLES;
DROP TABLE IF EXISTS `terms`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `terms` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `academic_year_id` bigint unsigned NOT NULL,
  `term_no` tinyint unsigned NOT NULL,
  `start_date` date NOT NULL,
  `end_date` date NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_terms_year_no` (`academic_year_id`,`term_no`),
  CONSTRAINT `fk_terms_years` FOREIGN KEY (`academic_year_id`) REFERENCES `academic_years` (`id`),
  CONSTRAINT `chk_terms_dates` CHECK ((`end_date` >= `start_date`)),
  CONSTRAINT `chk_terms_no` CHECK ((`term_no` between 1 and 3))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `terms` WRITE;
/*!40000 ALTER TABLE `terms` DISABLE KEYS */;
INSERT INTO `terms` (`id`, `academic_year_id`, `term_no`, `start_date`, `end_date`) VALUES (1,1,1,'2026-09-01','2027-01-15');
INSERT INTO `terms` (`id`, `academic_year_id`, `term_no`, `start_date`, `end_date`) VALUES (2,1,2,'2027-01-16','2027-05-30');
INSERT INTO `terms` (`id`, `academic_year_id`, `term_no`, `start_date`, `end_date`) VALUES (3,1,3,'2027-06-01','2027-08-15');
INSERT INTO `terms` (`id`, `academic_year_id`, `term_no`, `start_date`, `end_date`) VALUES (4,2,1,'2025-09-01','2026-01-15');
INSERT INTO `terms` (`id`, `academic_year_id`, `term_no`, `start_date`, `end_date`) VALUES (5,2,2,'2026-01-16','2026-05-30');
/*!40000 ALTER TABLE `terms` ENABLE KEYS */;
UNLOCK TABLES;
DROP TABLE IF EXISTS `user_role_scopes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_role_scopes` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `UserId` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `RoleId` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `scope_type` enum('GLOBAL','FACULTY','MAJOR','CAMPAIGN') COLLATE utf8mb4_unicode_ci NOT NULL,
  `faculty_id` bigint unsigned DEFAULT NULL,
  `major_id` bigint unsigned DEFAULT NULL,
  `campaign_id` bigint unsigned DEFAULT NULL,
  `ScopeTargetId` bigint unsigned GENERATED ALWAYS AS ((case `scope_type` when _utf8mb4'GLOBAL' then 0 when _utf8mb4'FACULTY' then `faculty_id` when _utf8mb4'MAJOR' then `major_id` when _utf8mb4'CAMPAIGN' then `campaign_id` end)) STORED,
  `granted_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `granted_by_user_id` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_role_scope` (`UserId`,`RoleId`,`scope_type`,`ScopeTargetId`),
  KEY `ix_scopes_role_id` (`RoleId`),
  KEY `ix_scopes_faculty` (`faculty_id`),
  KEY `ix_scopes_major` (`major_id`),
  KEY `ix_scopes_campaign` (`campaign_id`),
  KEY `ix_scopes_granted_by` (`granted_by_user_id`),
  CONSTRAINT `fk_scope_campaign` FOREIGN KEY (`campaign_id`) REFERENCES `campaigns` (`id`),
  CONSTRAINT `fk_scope_faculty` FOREIGN KEY (`faculty_id`) REFERENCES `faculties` (`id`),
  CONSTRAINT `fk_scope_grantor` FOREIGN KEY (`granted_by_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `fk_scope_major` FOREIGN KEY (`major_id`) REFERENCES `majors` (`id`),
  CONSTRAINT `fk_scope_membership` FOREIGN KEY (`UserId`, `RoleId`) REFERENCES `AspNetUserRoles` (`UserId`, `RoleId`) ON DELETE CASCADE,
  CONSTRAINT `chk_scope_target` CHECK ((((`scope_type` = _utf8mb4'GLOBAL') and (`faculty_id` is null) and (`major_id` is null) and (`campaign_id` is null)) or ((`scope_type` = _utf8mb4'FACULTY') and (`faculty_id` is not null) and (`major_id` is null) and (`campaign_id` is null)) or ((`scope_type` = _utf8mb4'MAJOR') and (`faculty_id` is null) and (`major_id` is not null) and (`campaign_id` is null)) or ((`scope_type` = _utf8mb4'CAMPAIGN') and (`faculty_id` is null) and (`major_id` is null) and (`campaign_id` is not null))))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

LOCK TABLES `user_role_scopes` WRITE;
/*!40000 ALTER TABLE `user_role_scopes` DISABLE KEYS */;
INSERT INTO `user_role_scopes` (`id`, `UserId`, `RoleId`, `scope_type`, `faculty_id`, `major_id`, `campaign_id`, `granted_at`, `granted_by_user_id`) VALUES (1,'f454486c-479c-4765-9050-450301406f0b','role_admin','GLOBAL',NULL,NULL,NULL,'2026-11-02 02:20:11.410798','f454486c-479c-4765-9050-450301406f0b');
INSERT INTO `user_role_scopes` (`id`, `UserId`, `RoleId`, `scope_type`, `faculty_id`, `major_id`, `campaign_id`, `granted_at`, `granted_by_user_id`) VALUES (3,'b221cae9-17b6-4350-adea-dff8146ac0eb','role_training_manager','GLOBAL',NULL,NULL,NULL,'2026-11-02 02:22:28.451635','f454486c-479c-4765-9050-450301406f0b');
INSERT INTO `user_role_scopes` (`id`, `UserId`, `RoleId`, `scope_type`, `faculty_id`, `major_id`, `campaign_id`, `granted_at`, `granted_by_user_id`) VALUES (4,'c1989175-b03e-4292-98ae-a9ab042c394b','role_faculty_manager','FACULTY',2,NULL,NULL,'2026-11-02 02:22:29.665552','f454486c-479c-4765-9050-450301406f0b');
INSERT INTO `user_role_scopes` (`id`, `UserId`, `RoleId`, `scope_type`, `faculty_id`, `major_id`, `campaign_id`, `granted_at`, `granted_by_user_id`) VALUES (5,'1840c602-2711-461a-a2f1-d9d375dc6be1','role_student','MAJOR',NULL,1,NULL,'2026-11-03 06:29:16.170111','f454486c-479c-4765-9050-450301406f0b');
/*!40000 ALTER TABLE `user_role_scopes` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

