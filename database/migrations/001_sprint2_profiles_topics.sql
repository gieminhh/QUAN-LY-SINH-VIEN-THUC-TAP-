-- Additive schema only, no account data. Select database before import.

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
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `student_classes` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `major_id` bigint unsigned NOT NULL,
  `code` varchar(40) COLLATE utf8mb4_unicode_ci NOT NULL,
  `name` varchar(180) COLLATE utf8mb4_unicode_ci NOT NULL,
  `cohort` varchar(30) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'VD: K64',
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_s2_student_classes_code` (`code`),
  KEY `ix_s2_student_classes_major` (`major_id`),
  CONSTRAINT `fk_s2_student_classes_major` FOREIGN KEY (`major_id`) REFERENCES `majors` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `student_profiles` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `user_id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `student_code` varchar(40) COLLATE utf8mb4_unicode_ci NOT NULL,
  `major_id` bigint unsigned NOT NULL,
  `class_id` bigint unsigned DEFAULT NULL,
  `skills` text COLLATE utf8mb4_unicode_ci COMMENT 'MVP: mo ta van ban, co the chuan hoa Sprint sau',
  `interests` text COLLATE utf8mb4_unicode_ci COMMENT 'Linh vuc quan tam',
  `created_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `updated_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_s2_student_profiles_user` (`user_id`),
  UNIQUE KEY `uq_s2_student_profiles_code` (`student_code`),
  KEY `ix_s2_student_profiles_major` (`major_id`),
  KEY `ix_s2_student_profiles_class` (`class_id`),
  CONSTRAINT `fk_s2_student_profiles_class` FOREIGN KEY (`class_id`) REFERENCES `student_classes` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `fk_s2_student_profiles_major` FOREIGN KEY (`major_id`) REFERENCES `majors` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `fk_s2_student_profiles_user` FOREIGN KEY (`user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `student_gpa_records` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `student_profile_id` bigint unsigned NOT NULL,
  `term_id` bigint unsigned NOT NULL,
  `gpa_4` decimal(4,2) DEFAULT NULL,
  `gpa_10` decimal(4,2) DEFAULT NULL,
  `source_type` enum('API','EXCEL','MANUAL') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'MANUAL',
  `recorded_by_user_id` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `recorded_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `updated_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_s2_student_gpa_term` (`student_profile_id`,`term_id`),
  KEY `ix_s2_student_gpa_term` (`term_id`),
  KEY `ix_s2_student_gpa_recorded_by` (`recorded_by_user_id`),
  CONSTRAINT `fk_s2_student_gpa_recorded_by` FOREIGN KEY (`recorded_by_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL ON UPDATE RESTRICT,
  CONSTRAINT `fk_s2_student_gpa_student` FOREIGN KEY (`student_profile_id`) REFERENCES `student_profiles` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `fk_s2_student_gpa_term` FOREIGN KEY (`term_id`) REFERENCES `terms` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `lecturer_profiles` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `user_id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `lecturer_code` varchar(40) COLLATE utf8mb4_unicode_ci NOT NULL,
  `faculty_id` bigint unsigned NOT NULL,
  `department_name` varchar(180) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `academic_degree` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Hoc vi',
  `academic_title` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Hoc ham',
  `expertise` text COLLATE utf8mb4_unicode_ci COMMENT 'Chuyen mon',
  `research_interests` text COLLATE utf8mb4_unicode_ci COMMENT 'Huong nghien cuu',
  `created_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `updated_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_s2_lecturer_profiles_user` (`user_id`),
  UNIQUE KEY `uq_s2_lecturer_profiles_code` (`lecturer_code`),
  KEY `ix_s2_lecturer_profiles_faculty` (`faculty_id`),
  CONSTRAINT `fk_s2_lecturer_profiles_faculty` FOREIGN KEY (`faculty_id`) REFERENCES `faculties` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `fk_s2_lecturer_profiles_user` FOREIGN KEY (`user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `lecturer_campaign_quotas` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `lecturer_profile_id` bigint unsigned NOT NULL,
  `campaign_id` bigint unsigned NOT NULL,
  `max_students` int unsigned NOT NULL DEFAULT '0',
  `assigned_by_user_id` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `updated_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_s2_lecturer_campaign_quota` (`lecturer_profile_id`,`campaign_id`),
  KEY `ix_s2_lecturer_quota_campaign` (`campaign_id`),
  KEY `ix_s2_lecturer_quota_assigned_by` (`assigned_by_user_id`),
  CONSTRAINT `fk_s2_lecturer_quota_assigned_by` FOREIGN KEY (`assigned_by_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL ON UPDATE RESTRICT,
  CONSTRAINT `fk_s2_lecturer_quota_campaign` FOREIGN KEY (`campaign_id`) REFERENCES `campaigns` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `fk_s2_lecturer_quota_lecturer` FOREIGN KEY (`lecturer_profile_id`) REFERENCES `lecturer_profiles` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `topics` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `campaign_id` bigint unsigned NOT NULL,
  `lecturer_profile_id` bigint unsigned NOT NULL,
  `code` varchar(60) COLLATE utf8mb4_unicode_ci NOT NULL,
  `title` varchar(300) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `objectives` text COLLATE utf8mb4_unicode_ci,
  `knowledge_requirements` text COLLATE utf8mb4_unicode_ci,
  `technologies` text COLLATE utf8mb4_unicode_ci,
  `research_field` varchar(180) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `max_students` int unsigned NOT NULL DEFAULT '1',
  `status` enum('DRAFT','PENDING','REVISION_REQUIRED','APPROVED','REJECTED') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'DRAFT',
  `approved_at` datetime(6) DEFAULT NULL,
  `created_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `updated_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_s2_topics_campaign_code` (`campaign_id`,`code`),
  KEY `ix_s2_topics_lecturer` (`lecturer_profile_id`),
  KEY `ix_s2_topics_status_campaign` (`campaign_id`,`status`),
  CONSTRAINT `fk_s2_topics_campaign` FOREIGN KEY (`campaign_id`) REFERENCES `campaigns` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `fk_s2_topics_lecturer` FOREIGN KEY (`lecturer_profile_id`) REFERENCES `lecturer_profiles` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `topic_approval_history` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `topic_id` bigint unsigned NOT NULL,
  `actor_user_id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `action_type` enum('SUBMIT','RESUBMIT','REQUEST_REVISION','APPROVE','REJECT') COLLATE utf8mb4_unicode_ci NOT NULL,
  `from_status` enum('DRAFT','PENDING','REVISION_REQUIRED','APPROVED','REJECTED') COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `to_status` enum('DRAFT','PENDING','REVISION_REQUIRED','APPROVED','REJECTED') COLLATE utf8mb4_unicode_ci NOT NULL,
  `comment_text` text COLLATE utf8mb4_unicode_ci,
  `action_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`),
  KEY `ix_s2_topic_history_topic_time` (`topic_id`,`action_at`),
  KEY `ix_s2_topic_history_actor` (`actor_user_id`),
  CONSTRAINT `fk_s2_topic_history_actor` FOREIGN KEY (`actor_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `fk_s2_topic_history_topic` FOREIGN KEY (`topic_id`) REFERENCES `topics` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `file_attachments` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `student_profile_id` bigint unsigned DEFAULT NULL,
  `topic_id` bigint unsigned DEFAULT NULL,
  `file_category` enum('STUDENT_CV','TOPIC_DOCUMENT') COLLATE utf8mb4_unicode_ci NOT NULL,
  `original_file_name` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `storage_key` varchar(500) COLLATE utf8mb4_unicode_ci NOT NULL,
  `content_type` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `size_bytes` bigint unsigned NOT NULL,
  `uploaded_by_user_id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `uploaded_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`),
  KEY `ix_s2_attachments_student` (`student_profile_id`),
  KEY `ix_s2_attachments_topic` (`topic_id`),
  KEY `ix_s2_attachments_uploaded_by` (`uploaded_by_user_id`),
  CONSTRAINT `fk_s2_attachments_student` FOREIGN KEY (`student_profile_id`) REFERENCES `student_profiles` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `fk_s2_attachments_topic` FOREIGN KEY (`topic_id`) REFERENCES `topics` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `fk_s2_attachments_uploader` FOREIGN KEY (`uploaded_by_user_id`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

