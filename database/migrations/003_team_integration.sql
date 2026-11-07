CREATE TABLE IF NOT EXISTS `lecturer_campaign_assignments` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT,
    `campaign_id` bigint unsigned NOT NULL,
    `lecturer_user_id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
    `student_user_id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
    `status` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
    `exception_reason` varchar(1000) COLLATE utf8mb4_unicode_ci NULL,
    `reviewed_by_user_id` varchar(255) COLLATE utf8mb4_unicode_ci NULL,
    `reviewed_at` datetime(6) NULL,
    `created_at` datetime(6) NOT NULL,
    `company_mentor_id` bigint unsigned NULL,
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

-- The old assignment table did not have the internship mentor reference.
SET @has_mentor = (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='lecturer_campaign_assignments' AND column_name='company_mentor_id');
SET @statement = IF(@has_mentor=0, 'ALTER TABLE lecturer_campaign_assignments ADD COLUMN company_mentor_id bigint unsigned NULL', 'SELECT 1');
PREPARE team_schema FROM @statement; EXECUTE team_schema; DEALLOCATE PREPARE team_schema;

-- Migrate the team's former scalar profiles when those columns exist. Existing
-- normalized profiles always take precedence; legacy columns remain as an archive.
SET @has_profiles = (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='AspNetUsers' AND column_name='major_id');
SET @statement = IF(@has_profiles>0,
 'INSERT IGNORE INTO student_classes(major_id,code,name) SELECT DISTINCT u.major_id, CONCAT("LEG-",LEFT(SHA2(CONCAT(u.major_id,":",u.class_name),256),30)), u.class_name FROM AspNetUsers u JOIN majors m ON m.id=u.major_id JOIN AspNetUserRoles ur ON ur.UserId=u.Id JOIN AspNetRoles r ON r.Id=ur.RoleId AND r.Name="Student" WHERE u.class_name IS NOT NULL AND TRIM(u.class_name)<>""', 'SELECT 1');
PREPARE team_schema FROM @statement; EXECUTE team_schema; DEALLOCATE PREPARE team_schema;
SET @statement = IF(@has_profiles>0,
 'INSERT IGNORE INTO student_profiles(user_id,student_code,major_id,class_id,skills,interests) SELECT u.Id,CONCAT("LEG-",LEFT(SHA2(u.Id,256),32)),u.major_id,c.id,u.skills,u.interests FROM AspNetUsers u JOIN majors m ON m.id=u.major_id JOIN AspNetUserRoles ur ON ur.UserId=u.Id JOIN AspNetRoles r ON r.Id=ur.RoleId AND r.Name="Student" LEFT JOIN student_classes c ON c.code=CONCAT("LEG-",LEFT(SHA2(CONCAT(u.major_id,":",u.class_name),256),30))', 'SELECT 1');
PREPARE team_schema FROM @statement; EXECUTE team_schema; DEALLOCATE PREPARE team_schema;

SET @has_archive = (SELECT COUNT(*) FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name='lecturer_campaign_quotas_legacy');
-- Only infer a lecturer's faculty when all previous quota campaigns agree.
-- Ambiguous/unassigned lecturers remain in the archive for staff to link explicitly.
SET @statement = IF(@has_archive>0,
 'INSERT IGNORE INTO lecturer_profiles(user_id,lecturer_code,faculty_id,expertise) SELECT u.Id,CONCAT("LEG-",LEFT(SHA2(u.Id,256),32)), MIN(m.faculty_id),MAX(u.lecturer_specialty) FROM lecturer_campaign_quotas_legacy q JOIN AspNetUsers u ON u.Id=q.lecturer_user_id JOIN campaigns ca ON ca.id=q.campaign_id JOIN majors m ON m.id=ca.major_id GROUP BY u.Id HAVING COUNT(DISTINCT m.faculty_id)=1', 'SELECT 1');
PREPARE team_schema FROM @statement; EXECUTE team_schema; DEALLOCATE PREPARE team_schema;
SET @statement = IF(@has_archive>0,
 'INSERT IGNORE INTO lecturer_campaign_quotas(lecturer_profile_id,campaign_id,max_students,updated_at) SELECT p.id,q.campaign_id,q.capacity,q.updated_at FROM lecturer_campaign_quotas_legacy q JOIN lecturer_profiles p ON p.user_id=q.lecturer_user_id WHERE q.capacity BETWEEN 0 AND 1000', 'SELECT 1');
PREPARE team_schema FROM @statement; EXECUTE team_schema; DEALLOCATE PREPARE team_schema;

-- Approved legacy project assignments become canonical assignments. Internship
-- rows without a verified mentor remain archived for staff reconciliation.
INSERT IGNORE INTO student_assignments(CampaignId,StudentProfileId,LecturerProfileId,CompanyMentorId,QuotaOverrideReason,AssignedBy,AssignedAt)
SELECT a.campaign_id,s.id,l.id,a.company_mentor_id,a.exception_reason,COALESCE(a.reviewed_by_user_id,c.created_by_user_id),a.created_at
FROM lecturer_campaign_assignments a JOIN student_profiles s ON s.user_id=a.student_user_id
JOIN lecturer_profiles l ON l.user_id=a.lecturer_user_id JOIN campaigns c ON c.id=a.campaign_id
JOIN majors m ON m.id=c.major_id
WHERE a.status IN ('ASSIGNED','APPROVED_EXCEPTION') AND s.major_id=c.major_id AND l.faculty_id=m.faculty_id
AND (c.kind='PROJECT' OR EXISTS(SELECT 1 FROM company_mentors cm JOIN companies co ON co.Id=cm.CompanyId WHERE cm.Id=a.company_mentor_id AND co.Status='APPROVED' AND co.MajorId=c.major_id));

-- Preserve references to existing PDF uploads without moving/deleting their files.
SET @statement = IF(@has_profiles>0,
 'INSERT INTO file_attachments(student_profile_id,file_category,original_file_name,storage_key,content_type,size_bytes,uploaded_by_user_id,uploaded_at) SELECT p.id,"STUDENT_CV","CV.pdf",CONCAT("legacy_cvs/",u.cv_stored_file_name),"application/pdf",0,u.Id,UTC_TIMESTAMP(6) FROM AspNetUsers u JOIN student_profiles p ON p.user_id=u.Id WHERE u.cv_stored_file_name IS NOT NULL AND u.cv_stored_file_name REGEXP "^[A-Za-z0-9_-]+[.]pdf$" AND NOT EXISTS(SELECT 1 FROM file_attachments f WHERE f.student_profile_id=p.id AND f.file_category="STUDENT_CV")', 'SELECT 1');
PREPARE team_schema FROM @statement; EXECUTE team_schema; DEALLOCATE PREPARE team_schema;
