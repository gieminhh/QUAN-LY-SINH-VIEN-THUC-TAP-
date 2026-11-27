
USE ql_thuctap_doan;

-- Issue #16: Bổ sung các cột phục vụ trạng thái duyệt đề tài
ALTER TABLE topics
ADD COLUMN approval_status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
ADD COLUMN reviewed_by_user_id VARCHAR(255) NULL,
ADD COLUMN reviewed_at DATETIME(6) NULL,
ADD COLUMN rejection_reason LONGTEXT NULL;
