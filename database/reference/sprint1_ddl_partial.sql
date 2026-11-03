-- ============================================================
-- HỆ THỐNG QUẢN LÝ THỰC TẬP & ĐỒ ÁN — SPRINT 1
-- MySQL 8.0.16+ / InnoDB / utf8mb4
-- Yêu cầu: US-01, US-02, US-03, US-05
-- 13 bảng: 7 bảng ASP.NET Core Identity + 6 bảng nghiệp vụ
--
-- IMPORTANT:
-- Đây là DDL tham chiếu và có thể import trên database trống.
-- Nếu dùng EF Core Code First: chỉ tạo DATABASE; để Migrations
-- tạo toàn bộ bảng sau khi cấu hình entity/mapping tương ứng.
-- KHÔNG import script này rồi chạy InitialCreate Migration chứa
-- cùng các bảng lên chính database đó.
-- ============================================================
CREATE DATABASE IF NOT EXISTS `ql_thuctap_doan`
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `ql_thuctap_doan`;

-- =============== NHÓM A: ASP.NET CORE IDENTITY ===============
CREATE TABLE `AspNetUsers` (
  `Id`                  VARCHAR(255) NOT NULL,
  `UserName`            VARCHAR(256) NULL,
  `NormalizedUserName`  VARCHAR(256) NULL,
  `Email`               VARCHAR(256) NULL,
  `NormalizedEmail`     VARCHAR(256) NULL,
  `EmailConfirmed`      TINYINT(1) NOT NULL DEFAULT 0,
  `PasswordHash`        LONGTEXT NULL,
  `SecurityStamp`       LONGTEXT NULL,
  `ConcurrencyStamp`    LONGTEXT NULL,
  `PhoneNumber`         LONGTEXT NULL,
  `PhoneNumberConfirmed` TINYINT(1) NOT NULL DEFAULT 0,
  `TwoFactorEnabled`    TINYINT(1) NOT NULL DEFAULT 0,
  `LockoutEnd`          DATETIME(6) NULL,
  `LockoutEnabled`      TINYINT(1) NOT NULL DEFAULT 1,
  `AccessFailedCount`   INT NOT NULL DEFAULT 0,
  -- Các trường bổ sung vào ApplicationUser : IdentityUser
  `FullName`            VARCHAR(180) NOT NULL,
  `IsActive`            TINYINT(1) NOT NULL DEFAULT 1,
  `CreatedAt`           DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UserNameIndex` (`NormalizedUserName`),
  KEY `EmailIndex` (`NormalizedEmail`)
) ENGINE=InnoDB;

CREATE TABLE `AspNetRoles` (
  `Id`               VARCHAR(255) NOT NULL,
  `Name`             VARCHAR(256) NULL,
  `NormalizedName`   VARCHAR(256) NULL,
  `ConcurrencyStamp` LONGTEXT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB;

CREATE TABLE `AspNetUserRoles` (
  `UserId` VARCHAR(255) NOT NULL,
  `RoleId` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`UserId`, `RoleId`),
  KEY `IX_UserRoles_RoleId` (`RoleId`),
  CONSTRAINT `FK_UserRoles_Users` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_UserRoles_Roles` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE `AspNetUserClaims` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `UserId` VARCHAR(255) NOT NULL,
  `ClaimType` LONGTEXT NULL,
  `ClaimValue` LONGTEXT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_UserClaims_UserId` (`UserId`),
  CONSTRAINT `FK_UserClaims_Users` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE `AspNetRoleClaims` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `RoleId` VARCHAR(255) NOT NULL,
  `ClaimType` LONGTEXT NULL,
  `ClaimValue` LONGTEXT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_RoleClaims_RoleId` (`RoleId`),
  CONSTRAINT `FK_RoleClaims_Roles` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE `AspNetUserLogins` (
  `LoginProvider`       VARCHAR(128) NOT NULL,
  `ProviderKey`         VARCHAR(128) NOT NULL,
  `ProviderDisplayName` LONGTEXT NULL,
  `UserId`              VARCHAR(255) NOT NULL,
  PRIMARY KEY (`LoginProvider`, `ProviderKey`),
  KEY `IX_UserLogins_UserId` (`UserId`),
  CONSTRAINT `FK_UserLogins_Users` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE `AspNetUserTokens` (
  `UserId`        VARCHAR(255) NOT NULL,
  `LoginProvider` VARCHAR(128) NOT NULL,
  `Name`          VARCHAR(128) NOT NULL,
  `Value`         LONGTEXT NULL,
  PRIMARY KEY (`UserId`, `LoginProvider`, `Name`),
  CONSTRAINT `FK_UserTokens_Users` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB;

-- ================ NHÓM B: DANH MỤC ĐÀO TẠO ================
CREATE TABLE `faculties` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `code` VARCHAR(30) NOT NULL,
  `name` VARCHAR(200) NOT NULL,
  `is_active` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_faculties_code` (`code`)
) ENGINE=InnoDB;

CREATE TABLE `majors` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `faculty_id` BIGINT UNSIGNED NOT NULL,
  `code` VARCHAR(30) NOT NULL,
  `name` VARCHAR(200) NOT NULL,
  `is_active` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_majors_code` (`code`),
  KEY `ix_majors_faculty_id` (`faculty_id`),
  CONSTRAINT `fk_majors_faculties` FOREIGN KEY (`faculty_id`) REFERENCES `faculties` (`id`)
) ENGINE=InnoDB;

CREATE TABLE `academic_years` (
    `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `code` VARCHAR(15) NOT NULL,
    `start_date` DATE NOT NULL,
    `end_date` DATE NOT NULL,

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_academic_years_code` (`code`)
) ENGINE=InnoDB;
SELECT VERSION();