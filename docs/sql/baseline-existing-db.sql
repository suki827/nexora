-- REVIEW ONLY. Do not run until the target database has been backed up and its schema checked.
-- This records the historical InitialCreate migration as a baseline for an existing database.
-- InitialCreate itself creates obsolete singular tables and must not be executed on this database.
BEGIN;

DO $$
BEGIN
    IF to_regclass('public.users') IS NULL OR to_regclass('public.assets') IS NULL OR
       to_regclass('public.asset_files') IS NULL OR to_regclass('public.media_metadata') IS NULL THEN
        RAISE EXCEPTION 'Existing identity/media schema is incomplete; baseline refused';
    END IF;
    IF to_regclass('public.analysis_task') IS NOT NULL OR to_regclass('public.analysis_result') IS NOT NULL OR
       to_regclass('public.analysis_tasks') IS NOT NULL OR to_regclass('public.analysis_results') IS NOT NULL OR
       to_regclass('public.task_inputs') IS NOT NULL OR to_regclass('public.analysis_task_attempts') IS NOT NULL OR
       to_regclass('public.generated_artifacts') IS NOT NULL THEN
        RAISE EXCEPTION 'Task tables already exist; baseline refused';
    END IF;
    IF to_regclass('public."__EFMigrationsHistory"') IS NOT NULL THEN
        RAISE EXCEPTION 'Migration history already exists; baseline refused';
    END IF;
END $$;

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" varchar(150) NOT NULL PRIMARY KEY,
    "ProductVersion" varchar(32) NOT NULL
);
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260726053239_InitialCreate', '10.0.10');

COMMIT;
