-- Run before 001. Preserve the previous user-keyed quota table as an archive.
-- The archive is never dropped automatically. Back up the database and App_Data first.
SET @legacy_quota = (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='lecturer_campaign_quotas' AND column_name='lecturer_user_id');
SET @statement = IF(@legacy_quota>0, 'RENAME TABLE lecturer_campaign_quotas TO lecturer_campaign_quotas_legacy', 'SELECT 1');
PREPARE team_schema FROM @statement;
EXECUTE team_schema;
DEALLOCATE PREPARE team_schema;
