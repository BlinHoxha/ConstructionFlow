CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE TABLE tenants (
        id uuid NOT NULL,
        name character varying(200) NOT NULL,
        is_active boolean NOT NULL,
        locale text NOT NULL,
        default_currency text NOT NULL,
        CONSTRAINT "PK_tenants" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        subject character varying(300) NOT NULL,
        display_name character varying(160) NOT NULL,
        is_active boolean NOT NULL,
        CONSTRAINT "PK_users" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE TABLE projects (
        id uuid NOT NULL,
        code character varying(40) NOT NULL,
        name character varying(200) NOT NULL,
        location_text character varying(300) NOT NULL,
        planned_start date NOT NULL,
        planned_end date,
        currency_code text NOT NULL,
        status text NOT NULL,
        tenant_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        row_version bigint NOT NULL,
        CONSTRAINT "PK_projects" PRIMARY KEY (id),
        CONSTRAINT "AK_projects_tenant_id_id" UNIQUE (tenant_id, id),
        CONSTRAINT "FK_projects_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE TABLE suppliers (
        id uuid NOT NULL,
        name character varying(200) NOT NULL,
        registration_number character varying(60),
        is_active boolean NOT NULL,
        tenant_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        row_version bigint NOT NULL,
        CONSTRAINT "PK_suppliers" PRIMARY KEY (id),
        CONSTRAINT "AK_suppliers_tenant_id_id" UNIQUE (tenant_id, id),
        CONSTRAINT "FK_suppliers_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE TABLE audit_events (
        id uuid NOT NULL,
        actor_user_id uuid NOT NULL,
        entity_type text NOT NULL,
        entity_id uuid NOT NULL,
        action text NOT NULL,
        tenant_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        row_version bigint NOT NULL,
        CONSTRAINT "PK_audit_events" PRIMARY KEY (id),
        CONSTRAINT "AK_audit_events_tenant_id_id" UNIQUE (tenant_id, id),
        CONSTRAINT "FK_audit_events_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_audit_events_users_actor_user_id" FOREIGN KEY (actor_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE TABLE tenant_memberships (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        role text NOT NULL,
        is_active boolean NOT NULL,
        tenant_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        row_version bigint NOT NULL,
        CONSTRAINT "PK_tenant_memberships" PRIMARY KEY (id),
        CONSTRAINT "AK_tenant_memberships_tenant_id_id" UNIQUE (tenant_id, id),
        CONSTRAINT "AK_tenant_memberships_tenant_id_user_id" UNIQUE (tenant_id, user_id),
        CONSTRAINT "FK_tenant_memberships_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_tenant_memberships_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE TABLE daily_reports (
        id uuid NOT NULL,
        project_id uuid NOT NULL,
        author_user_id uuid NOT NULL,
        report_date date NOT NULL,
        notes text NOT NULL,
        status text NOT NULL,
        reviewed_by uuid,
        reviewed_at timestamp with time zone,
        tenant_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        row_version bigint NOT NULL,
        CONSTRAINT "PK_daily_reports" PRIMARY KEY (id),
        CONSTRAINT "AK_daily_reports_tenant_id_id" UNIQUE (tenant_id, id),
        CONSTRAINT "FK_daily_reports_projects_tenant_id_project_id" FOREIGN KEY (tenant_id, project_id) REFERENCES projects (tenant_id, id) ON DELETE RESTRICT,
        CONSTRAINT "FK_daily_reports_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_daily_reports_users_author_user_id" FOREIGN KEY (author_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_daily_reports_users_reviewed_by" FOREIGN KEY (reviewed_by) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE TABLE documents (
        id uuid NOT NULL,
        project_id uuid NOT NULL,
        title character varying(255) NOT NULL,
        language text NOT NULL,
        sha256 character varying(64) NOT NULL,
        reviewed_by uuid NOT NULL,
        version_no integer NOT NULL,
        tenant_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        row_version bigint NOT NULL,
        CONSTRAINT "PK_documents" PRIMARY KEY (id),
        CONSTRAINT "AK_documents_tenant_id_id" UNIQUE (tenant_id, id),
        CONSTRAINT "AK_documents_tenant_id_project_id_id" UNIQUE (tenant_id, project_id, id),
        CONSTRAINT "FK_documents_projects_tenant_id_project_id" FOREIGN KEY (tenant_id, project_id) REFERENCES projects (tenant_id, id) ON DELETE RESTRICT,
        CONSTRAINT "FK_documents_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_documents_users_reviewed_by" FOREIGN KEY (reviewed_by) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE TABLE rfqs (
        id uuid NOT NULL,
        project_id uuid NOT NULL,
        description text NOT NULL,
        unit_code text NOT NULL,
        quantity numeric(18,4) NOT NULL,
        currency_code text NOT NULL,
        tenant_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        row_version bigint NOT NULL,
        CONSTRAINT "PK_rfqs" PRIMARY KEY (id),
        CONSTRAINT "AK_rfqs_tenant_id_id" UNIQUE (tenant_id, id),
        CONSTRAINT "AK_rfqs_tenant_id_project_id_id" UNIQUE (tenant_id, project_id, id),
        CONSTRAINT "FK_rfqs_projects_tenant_id_project_id" FOREIGN KEY (tenant_id, project_id) REFERENCES projects (tenant_id, id) ON DELETE RESTRICT,
        CONSTRAINT "FK_rfqs_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE TABLE project_members (
        id uuid NOT NULL,
        project_id uuid NOT NULL,
        user_id uuid NOT NULL,
        role text NOT NULL,
        tenant_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        row_version bigint NOT NULL,
        CONSTRAINT "PK_project_members" PRIMARY KEY (id),
        CONSTRAINT "AK_project_members_tenant_id_id" UNIQUE (tenant_id, id),
        CONSTRAINT "FK_project_members_projects_tenant_id_project_id" FOREIGN KEY (tenant_id, project_id) REFERENCES projects (tenant_id, id) ON DELETE RESTRICT,
        CONSTRAINT "FK_project_members_tenant_memberships_tenant_id_user_id" FOREIGN KEY (tenant_id, user_id) REFERENCES tenant_memberships (tenant_id, user_id) ON DELETE RESTRICT,
        CONSTRAINT "FK_project_members_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE TABLE document_chunks (
        id uuid NOT NULL,
        project_id uuid NOT NULL,
        document_id uuid NOT NULL,
        chunk_index integer NOT NULL,
        content text NOT NULL,
        tenant_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        row_version bigint NOT NULL,
        CONSTRAINT "PK_document_chunks" PRIMARY KEY (id),
        CONSTRAINT "AK_document_chunks_tenant_id_id" UNIQUE (tenant_id, id),
        CONSTRAINT "FK_document_chunks_documents_tenant_id_project_id_document_id" FOREIGN KEY (tenant_id, project_id, document_id) REFERENCES documents (tenant_id, project_id, id) ON DELETE RESTRICT,
        CONSTRAINT "FK_document_chunks_projects_tenant_id_project_id" FOREIGN KEY (tenant_id, project_id) REFERENCES projects (tenant_id, id) ON DELETE RESTRICT,
        CONSTRAINT "FK_document_chunks_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE TABLE supplier_quotes (
        id uuid NOT NULL,
        project_id uuid NOT NULL,
        rfq_id uuid NOT NULL,
        supplier_id uuid NOT NULL,
        reference character varying(100) NOT NULL,
        unit_code text NOT NULL,
        currency_code text NOT NULL,
        quantity numeric(18,4) NOT NULL,
        unit_price numeric(18,4) NOT NULL,
        vat_percent numeric(5,2) NOT NULL,
        specification_confirmed boolean NOT NULL,
        valid_until date NOT NULL,
        tenant_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        row_version bigint NOT NULL,
        CONSTRAINT "PK_supplier_quotes" PRIMARY KEY (id),
        CONSTRAINT "AK_supplier_quotes_tenant_id_id" UNIQUE (tenant_id, id),
        CONSTRAINT "FK_supplier_quotes_projects_tenant_id_project_id" FOREIGN KEY (tenant_id, project_id) REFERENCES projects (tenant_id, id) ON DELETE RESTRICT,
        CONSTRAINT "FK_supplier_quotes_rfqs_tenant_id_project_id_rfq_id" FOREIGN KEY (tenant_id, project_id, rfq_id) REFERENCES rfqs (tenant_id, project_id, id) ON DELETE RESTRICT,
        CONSTRAINT "FK_supplier_quotes_suppliers_tenant_id_supplier_id" FOREIGN KEY (tenant_id, supplier_id) REFERENCES suppliers (tenant_id, id) ON DELETE RESTRICT,
        CONSTRAINT "FK_supplier_quotes_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE INDEX "IX_audit_events_actor_user_id" ON audit_events (actor_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE INDEX "IX_audit_events_tenant_id_created_at" ON audit_events (tenant_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE INDEX "IX_daily_reports_author_user_id" ON daily_reports (author_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE INDEX "IX_daily_reports_reviewed_by" ON daily_reports (reviewed_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE INDEX "IX_daily_reports_tenant_id_project_id_report_date" ON daily_reports (tenant_id, project_id, report_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE UNIQUE INDEX "IX_document_chunks_tenant_id_document_id_chunk_index" ON document_chunks (tenant_id, document_id, chunk_index);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE INDEX "IX_document_chunks_tenant_id_project_id_document_id" ON document_chunks (tenant_id, project_id, document_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE INDEX "IX_documents_reviewed_by" ON documents (reviewed_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE UNIQUE INDEX "IX_documents_tenant_id_project_id_sha256" ON documents (tenant_id, project_id, sha256);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE UNIQUE INDEX "IX_project_members_tenant_id_project_id_user_id" ON project_members (tenant_id, project_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE INDEX "IX_project_members_tenant_id_user_id" ON project_members (tenant_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE UNIQUE INDEX "IX_projects_tenant_id_code" ON projects (tenant_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE INDEX "IX_projects_tenant_id_status" ON projects (tenant_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE INDEX "IX_supplier_quotes_tenant_id_project_id_rfq_id" ON supplier_quotes (tenant_id, project_id, rfq_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE UNIQUE INDEX "IX_supplier_quotes_tenant_id_rfq_id_supplier_id_reference" ON supplier_quotes (tenant_id, rfq_id, supplier_id, reference);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE INDEX "IX_supplier_quotes_tenant_id_supplier_id" ON supplier_quotes (tenant_id, supplier_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE INDEX "IX_tenant_memberships_user_id" ON tenant_memberships (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    CREATE UNIQUE INDEX "IX_users_subject" ON users (subject);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261015180542_InitialWorkspace') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261015180542_InitialWorkspace', '9.0.8');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    ALTER TABLE audit_events DROP CONSTRAINT "FK_audit_events_users_actor_user_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    ALTER TABLE daily_reports DROP CONSTRAINT "FK_daily_reports_users_author_user_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    ALTER TABLE daily_reports DROP CONSTRAINT "FK_daily_reports_users_reviewed_by";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    ALTER TABLE documents DROP CONSTRAINT "FK_documents_users_reviewed_by";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    DROP INDEX "IX_documents_reviewed_by";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    DROP INDEX "IX_daily_reports_author_user_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    DROP INDEX "IX_daily_reports_reviewed_by";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    DROP INDEX "IX_audit_events_actor_user_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    CREATE INDEX "IX_documents_tenant_id_reviewed_by" ON documents (tenant_id, reviewed_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    CREATE INDEX "IX_daily_reports_tenant_id_author_user_id" ON daily_reports (tenant_id, author_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    CREATE INDEX "IX_daily_reports_tenant_id_reviewed_by" ON daily_reports (tenant_id, reviewed_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    CREATE INDEX "IX_audit_events_tenant_id_actor_user_id" ON audit_events (tenant_id, actor_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    ALTER TABLE audit_events ADD CONSTRAINT "FK_audit_events_tenant_memberships_tenant_id_actor_user_id" FOREIGN KEY (tenant_id, actor_user_id) REFERENCES tenant_memberships (tenant_id, user_id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    ALTER TABLE daily_reports ADD CONSTRAINT "FK_daily_reports_tenant_memberships_tenant_id_author_user_id" FOREIGN KEY (tenant_id, author_user_id) REFERENCES tenant_memberships (tenant_id, user_id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    ALTER TABLE daily_reports ADD CONSTRAINT "FK_daily_reports_tenant_memberships_tenant_id_reviewed_by" FOREIGN KEY (tenant_id, reviewed_by) REFERENCES tenant_memberships (tenant_id, user_id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    ALTER TABLE documents ADD CONSTRAINT "FK_documents_tenant_memberships_tenant_id_reviewed_by" FOREIGN KEY (tenant_id, reviewed_by) REFERENCES tenant_memberships (tenant_id, user_id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261016093634_EnforceTenantActorMembership') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261016093634_EnforceTenantActorMembership', '9.0.8');
    END IF;
END $EF$;
COMMIT;

