-- ============================================================
-- V6 - Branch activation functions
--
-- Validation belongs in the API. This function only persists the
-- activation state after the application has approved the branch.
-- ============================================================

CREATE OR REPLACE FUNCTION public.activate_branch(p_branch_id BIGINT)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
	WITH updated AS (
		UPDATE public.branches
		SET status = 1,
			accepts_bookings = TRUE
		WHERE id = p_branch_id
		RETURNING 1
	)
	SELECT EXISTS (SELECT 1 FROM updated);
$$;
