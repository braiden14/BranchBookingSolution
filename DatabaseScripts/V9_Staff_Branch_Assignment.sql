ALTER TABLE public.staff_profiles
	ADD COLUMN IF NOT EXISTS branch_id BIGINT REFERENCES public.branches(id) ON DELETE RESTRICT;

CREATE INDEX IF NOT EXISTS ix_staff_profiles_branch_id
	ON public.staff_profiles(branch_id);
