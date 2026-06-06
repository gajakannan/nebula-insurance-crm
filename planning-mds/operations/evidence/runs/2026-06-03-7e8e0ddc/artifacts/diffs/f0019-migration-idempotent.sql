CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "Accounts" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Industry" character varying(100) NOT NULL,
        "PrimaryState" character varying(2) NOT NULL,
        "Region" character varying(50) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255) NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedBy" character varying(255) NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_Accounts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "ActivityTimelineEvents" (
        "Id" uuid NOT NULL,
        "EntityType" character varying(50) NOT NULL,
        "EntityId" uuid NOT NULL,
        "EventType" character varying(50) NOT NULL,
        "EventPayloadJson" jsonb,
        "EventDescription" character varying(500) NOT NULL,
        "ActorSubject" character varying(255) NOT NULL,
        "ActorDisplayName" character varying(200),
        "OccurredAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ActivityTimelineEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "MGAs" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "ExternalCode" character varying(50) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255) NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedBy" character varying(255) NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_MGAs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "ReferenceRenewalStatuses" (
        "Code" character varying(30) NOT NULL,
        "DisplayName" character varying(50) NOT NULL,
        "Description" character varying(255) NOT NULL,
        "IsTerminal" boolean NOT NULL,
        "DisplayOrder" smallint NOT NULL,
        "ColorGroup" character varying(20),
        CONSTRAINT "PK_ReferenceRenewalStatuses" PRIMARY KEY ("Code")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "ReferenceSubmissionStatuses" (
        "Code" character varying(30) NOT NULL,
        "DisplayName" character varying(50) NOT NULL,
        "Description" character varying(255) NOT NULL,
        "IsTerminal" boolean NOT NULL,
        "DisplayOrder" smallint NOT NULL,
        "ColorGroup" character varying(20),
        CONSTRAINT "PK_ReferenceSubmissionStatuses" PRIMARY KEY ("Code")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "ReferenceTaskStatuses" (
        "Code" character varying(30) NOT NULL,
        "DisplayName" character varying(50) NOT NULL,
        "DisplayOrder" smallint NOT NULL,
        CONSTRAINT "PK_ReferenceTaskStatuses" PRIMARY KEY ("Code")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "Tasks" (
        "Id" uuid NOT NULL,
        "Title" character varying(255) NOT NULL,
        "Description" character varying(2000),
        "Status" character varying(20) NOT NULL DEFAULT 'Open',
        "Priority" character varying(20) NOT NULL DEFAULT 'Normal',
        "DueDate" timestamp with time zone,
        "AssignedTo" character varying(255) NOT NULL,
        "LinkedEntityType" character varying(50),
        "LinkedEntityId" uuid,
        "CompletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255) NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedBy" character varying(255) NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_Tasks" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "UserProfiles" (
        "Id" uuid NOT NULL,
        "Subject" character varying(255) NOT NULL,
        "Email" character varying(255) NOT NULL,
        "DisplayName" character varying(200) NOT NULL,
        "Department" character varying(100) NOT NULL,
        "RegionsJson" jsonb NOT NULL,
        "RolesJson" jsonb NOT NULL,
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserProfiles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "WorkflowTransitions" (
        "Id" uuid NOT NULL,
        "WorkflowType" character varying(50) NOT NULL,
        "EntityId" uuid NOT NULL,
        "FromState" character varying(30) NOT NULL,
        "ToState" character varying(30) NOT NULL,
        "Reason" character varying(500),
        "ActorSubject" character varying(255) NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_WorkflowTransitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "Programs" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "ProgramCode" character varying(50) NOT NULL,
        "MgaId" uuid NOT NULL,
        "ManagedBySubject" character varying(255),
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255) NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedBy" character varying(255) NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_Programs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Programs_MGAs_MgaId" FOREIGN KEY ("MgaId") REFERENCES "MGAs" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "Brokers" (
        "Id" uuid NOT NULL,
        "LegalName" character varying(200) NOT NULL,
        "LicenseNumber" character varying(50) NOT NULL,
        "State" character varying(2) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "ManagedBySubject" character varying(255),
        "MgaId" uuid,
        "PrimaryProgramId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255) NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedBy" character varying(255) NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_Brokers" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Brokers_MGAs_MgaId" FOREIGN KEY ("MgaId") REFERENCES "MGAs" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Brokers_Programs_PrimaryProgramId" FOREIGN KEY ("PrimaryProgramId") REFERENCES "Programs" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "BrokerRegions" (
        "BrokerId" uuid NOT NULL,
        "Region" character varying(50) NOT NULL,
        CONSTRAINT "PK_BrokerRegions" PRIMARY KEY ("BrokerId", "Region"),
        CONSTRAINT "FK_BrokerRegions_Brokers_BrokerId" FOREIGN KEY ("BrokerId") REFERENCES "Brokers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "Contacts" (
        "Id" uuid NOT NULL,
        "BrokerId" uuid,
        "AccountId" uuid,
        "FullName" character varying(200) NOT NULL,
        "Email" character varying(255) NOT NULL,
        "Phone" character varying(30) NOT NULL,
        "Role" character varying(50) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255) NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedBy" character varying(255) NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_Contacts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Contacts_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Contacts_Brokers_BrokerId" FOREIGN KEY ("BrokerId") REFERENCES "Brokers" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "Submissions" (
        "Id" uuid NOT NULL,
        "AccountId" uuid NOT NULL,
        "BrokerId" uuid NOT NULL,
        "ProgramId" uuid,
        "CurrentStatus" character varying(30) NOT NULL DEFAULT 'Received',
        "EffectiveDate" timestamp with time zone NOT NULL,
        "PremiumEstimate" numeric(18,2) NOT NULL,
        "AssignedTo" character varying(255) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255) NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedBy" character varying(255) NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_Submissions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Submissions_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Submissions_Brokers_BrokerId" FOREIGN KEY ("BrokerId") REFERENCES "Brokers" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Submissions_Programs_ProgramId" FOREIGN KEY ("ProgramId") REFERENCES "Programs" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE TABLE "Renewals" (
        "Id" uuid NOT NULL,
        "AccountId" uuid NOT NULL,
        "BrokerId" uuid NOT NULL,
        "SubmissionId" uuid,
        "CurrentStatus" character varying(30) NOT NULL DEFAULT 'Created',
        "RenewalDate" timestamp with time zone NOT NULL,
        "AssignedTo" character varying(255) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255) NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedBy" character varying(255) NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_Renewals" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Renewals_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Renewals_Brokers_BrokerId" FOREIGN KEY ("BrokerId") REFERENCES "Brokers" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Renewals_Submissions_SubmissionId" FOREIGN KEY ("SubmissionId") REFERENCES "Submissions" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Bound', NULL, 'Policy renewed and bound', 'Bound', 6, TRUE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Created', 'intake', 'Renewal record created from expiring policy', 'Created', 1, FALSE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Early', 'intake', 'In early renewal window (90-120 days out)', 'Early', 2, FALSE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('InReview', 'review', 'Under underwriter review for renewal terms', 'In Review', 4, FALSE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Lapsed', NULL, 'Policy expired without renewal', 'Lapsed', 8, TRUE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Lost', NULL, 'Lost to competitor', 'Lost', 7, TRUE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('OutreachStarted', 'waiting', 'Active broker/account outreach begun', 'Outreach Started', 3, FALSE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Quoted', 'decision', 'Renewal quote issued', 'Quoted', 5, FALSE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('BindRequested', 'decision', 'Broker accepted quote, bind in progress', 'Bind Requested', 7, FALSE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Bound', NULL, 'Policy bound and issued', 'Bound', 8, TRUE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Declined', NULL, 'Submission declined by underwriter', 'Declined', 9, TRUE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('InReview', 'review', 'Under active underwriter review', 'In Review', 5, FALSE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Quoted', 'decision', 'Quote issued, awaiting broker response', 'Quoted', 6, FALSE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('ReadyForUWReview', 'review', 'All data received, queued for underwriter', 'Ready for UW Review', 4, FALSE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Received', 'intake', 'Initial state when submission is created', 'Received', 1, FALSE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Triaging', 'triage', 'Initial triage and data validation', 'Triaging', 2, FALSE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('WaitingOnBroker', 'waiting', 'Awaiting additional information from broker', 'Waiting on Broker', 3, FALSE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Withdrawn', NULL, 'Broker withdrew submission', 'Withdrawn', 10, TRUE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    INSERT INTO "ReferenceTaskStatuses" ("Code", "DisplayName", "DisplayOrder")
    VALUES ('Done', 'Done', 3);
    INSERT INTO "ReferenceTaskStatuses" ("Code", "DisplayName", "DisplayOrder")
    VALUES ('InProgress', 'In Progress', 2);
    INSERT INTO "ReferenceTaskStatuses" ("Code", "DisplayName", "DisplayOrder")
    VALUES ('Open', 'Open', 1);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Accounts_Region" ON "Accounts" ("Region");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_ATE_EntityType_OccurredAt" ON "ActivityTimelineEvents" ("EntityType", "OccurredAt" DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_BrokerRegions_Region_BrokerId" ON "BrokerRegions" ("Region", "BrokerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Brokers_LicenseNumber" ON "Brokers" ("LicenseNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Brokers_ManagedBySubject" ON "Brokers" ("ManagedBySubject");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Brokers_MgaId" ON "Brokers" ("MgaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Brokers_PrimaryProgramId" ON "Brokers" ("PrimaryProgramId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Contacts_AccountId" ON "Contacts" ("AccountId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Contacts_BrokerId" ON "Contacts" ("BrokerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Programs_ManagedBySubject" ON "Programs" ("ManagedBySubject");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Programs_MgaId" ON "Programs" ("MgaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_ReferenceRenewalStatuses_DisplayOrder" ON "ReferenceRenewalStatuses" ("DisplayOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_ReferenceSubmissionStatuses_DisplayOrder" ON "ReferenceSubmissionStatuses" ("DisplayOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Renewals_AccountId" ON "Renewals" ("AccountId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Renewals_AssignedTo_CurrentStatus" ON "Renewals" ("AssignedTo", "CurrentStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Renewals_BrokerId" ON "Renewals" ("BrokerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Renewals_CurrentStatus" ON "Renewals" ("CurrentStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Renewals_RenewalDate_Status" ON "Renewals" ("RenewalDate", "CurrentStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Renewals_SubmissionId" ON "Renewals" ("SubmissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Submissions_AccountId" ON "Submissions" ("AccountId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Submissions_AssignedTo_CurrentStatus" ON "Submissions" ("AssignedTo", "CurrentStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Submissions_BrokerId" ON "Submissions" ("BrokerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Submissions_CurrentStatus" ON "Submissions" ("CurrentStatus") WHERE "CurrentStatus" NOT IN ('Bound', 'Declined', 'Withdrawn');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Submissions_ProgramId" ON "Submissions" ("ProgramId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Tasks_AssignedTo_Status_DueDate" ON "Tasks" ("AssignedTo", "Status", "DueDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Tasks_DueDate_Status" ON "Tasks" ("DueDate", "Status") WHERE "IsDeleted" = false AND "Status" != 'Done';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_Tasks_LinkedEntity" ON "Tasks" ("LinkedEntityType", "LinkedEntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_UserProfiles_Subject" ON "UserProfiles" ("Subject");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    CREATE INDEX "IX_WT_EntityId_OccurredAt" ON "WorkflowTransitions" ("EntityId", "OccurredAt" DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223071234_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260223071234_InitialCreate', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223130720_AddBrokerEmailPhone') THEN
    ALTER TABLE "Brokers" ADD "Email" character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223130720_AddBrokerEmailPhone') THEN
    ALTER TABLE "Brokers" ADD "Phone" character varying(30);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260223130720_AddBrokerEmailPhone') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260223130720_AddBrokerEmailPhone', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    DROP INDEX "IX_Submissions_CurrentStatus";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    DROP INDEX "IX_ReferenceRenewalStatuses_DisplayOrder";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    DROP INDEX "IX_ReferenceSubmissionStatuses_DisplayOrder";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    UPDATE "ReferenceRenewalStatuses" SET "DisplayOrder" = 10
    WHERE "Code" = 'Bound';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    UPDATE "ReferenceRenewalStatuses" SET "DisplayOrder" = 6
    WHERE "Code" = 'InReview';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    UPDATE "ReferenceRenewalStatuses" SET "DisplayOrder" = 13
    WHERE "Code" = 'Lapsed';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    UPDATE "ReferenceRenewalStatuses" SET "DisplayOrder" = 12
    WHERE "Code" = 'Lost';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    UPDATE "ReferenceRenewalStatuses" SET "DisplayOrder" = 4
    WHERE "Code" = 'OutreachStarted';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    UPDATE "ReferenceRenewalStatuses" SET "DisplayOrder" = 7
    WHERE "Code" = 'Quoted';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('BindRequested', 'decision', 'Renewal bind requested', 'Bind Requested', 9, FALSE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('DataReview', 'triage', 'Coverage and account data review before outreach', 'Data Review', 3, FALSE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Expired', NULL, 'Renewal workflow expired before completion', 'Expired', 15, TRUE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Negotiation', 'decision', 'Actively negotiating renewal terms', 'Negotiation', 8, FALSE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('NotRenewed', NULL, 'Renewal closed without binding', 'Not Renewed', 11, TRUE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('WaitingOnBroker', 'waiting', 'Awaiting broker response or required renewal information', 'Waiting on Broker', 5, FALSE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Withdrawn', NULL, 'Renewal withdrawn by broker or insured', 'Withdrawn', 14, TRUE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    UPDATE "ReferenceSubmissionStatuses" SET "DisplayOrder" = 10
    WHERE "Code" = 'BindRequested';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    UPDATE "ReferenceSubmissionStatuses" SET "DisplayOrder" = 12
    WHERE "Code" = 'Bound';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    UPDATE "ReferenceSubmissionStatuses" SET "DisplayOrder" = 13
    WHERE "Code" = 'Declined';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    UPDATE "ReferenceSubmissionStatuses" SET "DisplayOrder" = 6
    WHERE "Code" = 'InReview';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    UPDATE "ReferenceSubmissionStatuses" SET "DisplayOrder" = 8
    WHERE "Code" = 'Quoted';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    UPDATE "ReferenceSubmissionStatuses" SET "DisplayOrder" = 5
    WHERE "Code" = 'ReadyForUWReview';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    UPDATE "ReferenceSubmissionStatuses" SET "Description" = 'Broker or insured withdrew submission', "DisplayOrder" = 14
    WHERE "Code" = 'Withdrawn';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Binding', 'decision', 'Binding and issuance processing in progress', 'Binding', 11, FALSE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Expired', NULL, 'Submission expired before disposition completed', 'Expired', 17, TRUE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Lost', NULL, 'Opportunity lost to another market or strategy change', 'Lost', 16, TRUE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('NotQuoted', NULL, 'Submission closed without quote issued', 'Not Quoted', 15, TRUE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('QuotePreparation', 'decision', 'Preparing quote terms for broker', 'Quote Preparation', 7, FALSE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('RequoteRequested', 'decision', 'Broker requested revised quote terms', 'Requote Requested', 9, FALSE);
    INSERT INTO "ReferenceSubmissionStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('WaitingOnDocuments', 'waiting', 'Awaiting required underwriting documents', 'Waiting on Documents', 4, FALSE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    CREATE UNIQUE INDEX "IX_ReferenceRenewalStatuses_DisplayOrder" ON "ReferenceRenewalStatuses" ("DisplayOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    CREATE UNIQUE INDEX "IX_ReferenceSubmissionStatuses_DisplayOrder" ON "ReferenceSubmissionStatuses" ("DisplayOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    CREATE INDEX "IX_Submissions_CurrentStatus" ON "Submissions" ("CurrentStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260226034611_ExpandOpportunityStatusesAndFlow') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260226034611_ExpandOpportunityStatusesAndFlow', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    DROP INDEX "IX_UserProfiles_Subject";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    DROP INDEX "IX_Submissions_AssignedTo_CurrentStatus";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    DROP INDEX "IX_Renewals_AssignedTo_CurrentStatus";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    DROP INDEX "IX_Tasks_AssignedTo_Status_DueDate";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    DROP INDEX "IX_Brokers_ManagedBySubject";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    DROP INDEX "IX_Programs_ManagedBySubject";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "UserProfiles" ADD "IdpIssuer" character varying(500) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "UserProfiles" ADD "IdpSubject" character varying(255) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "UserProfiles" DROP COLUMN "Subject";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "ActivityTimelineEvents" ADD "ActorUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "ActivityTimelineEvents" DROP COLUMN "ActorSubject";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "WorkflowTransitions" ADD "ActorUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "WorkflowTransitions" DROP COLUMN "ActorSubject";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Submissions" ADD "AssignedToUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Submissions" ADD "CreatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Submissions" ADD "UpdatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Submissions" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Submissions" DROP COLUMN "AssignedTo";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Submissions" DROP COLUMN "CreatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Submissions" DROP COLUMN "UpdatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Submissions" DROP COLUMN "DeletedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Renewals" ADD "AssignedToUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Renewals" ADD "CreatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Renewals" ADD "UpdatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Renewals" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Renewals" DROP COLUMN "AssignedTo";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Renewals" DROP COLUMN "CreatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Renewals" DROP COLUMN "UpdatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Renewals" DROP COLUMN "DeletedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Tasks" ADD "AssignedToUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Tasks" ADD "CreatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Tasks" ADD "UpdatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Tasks" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Tasks" DROP COLUMN "AssignedTo";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Tasks" DROP COLUMN "CreatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Tasks" DROP COLUMN "UpdatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Tasks" DROP COLUMN "DeletedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Brokers" ADD "ManagedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Brokers" ADD "CreatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Brokers" ADD "UpdatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Brokers" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Brokers" DROP COLUMN "ManagedBySubject";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Brokers" DROP COLUMN "CreatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Brokers" DROP COLUMN "UpdatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Brokers" DROP COLUMN "DeletedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Programs" ADD "ManagedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Programs" ADD "CreatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Programs" ADD "UpdatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Programs" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Programs" DROP COLUMN "ManagedBySubject";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Programs" DROP COLUMN "CreatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Programs" DROP COLUMN "UpdatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Programs" DROP COLUMN "DeletedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Accounts" ADD "CreatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Accounts" ADD "UpdatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Accounts" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Accounts" DROP COLUMN "CreatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Accounts" DROP COLUMN "UpdatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Accounts" DROP COLUMN "DeletedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "MGAs" ADD "CreatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "MGAs" ADD "UpdatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "MGAs" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "MGAs" DROP COLUMN "CreatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "MGAs" DROP COLUMN "UpdatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "MGAs" DROP COLUMN "DeletedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Contacts" ADD "CreatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Contacts" ADD "UpdatedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Contacts" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Contacts" DROP COLUMN "CreatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Contacts" DROP COLUMN "UpdatedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    ALTER TABLE "Contacts" DROP COLUMN "DeletedBy";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    CREATE UNIQUE INDEX "IX_UserProfiles_IdpIssuer_IdpSubject" ON "UserProfiles" ("IdpIssuer", "IdpSubject");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    CREATE INDEX "IX_Submissions_AssignedToUserId_CurrentStatus" ON "Submissions" ("AssignedToUserId", "CurrentStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    CREATE INDEX "IX_Renewals_AssignedToUserId_CurrentStatus" ON "Renewals" ("AssignedToUserId", "CurrentStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    CREATE INDEX "IX_Tasks_AssignedToUserId_Status_DueDate" ON "Tasks" ("AssignedToUserId", "Status", "DueDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    CREATE INDEX "IX_Brokers_ManagedByUserId" ON "Brokers" ("ManagedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    CREATE INDEX "IX_Programs_ManagedByUserId" ON "Programs" ("ManagedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301000000_F0005_IdpPrincipalRefactor') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260301000000_F0005_IdpPrincipalRefactor', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303000000_AddBrokerStatusIsDeletedIndex') THEN
    CREATE INDEX "IX_Brokers_Status_IsDeleted" ON "Brokers" ("Status", "IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303000000_AddBrokerStatusIsDeletedIndex') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260303000000_AddBrokerStatusIsDeletedIndex', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260305000000_F0009_BrokerTenantId_BrokerDescription') THEN
    ALTER TABLE "Brokers" ADD "BrokerTenantId" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260305000000_F0009_BrokerTenantId_BrokerDescription') THEN
    CREATE UNIQUE INDEX "IX_Brokers_BrokerTenantId" ON "Brokers" ("BrokerTenantId") WHERE "BrokerTenantId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260305000000_F0009_BrokerTenantId_BrokerDescription') THEN
    ALTER TABLE "ActivityTimelineEvents" ADD "BrokerDescription" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260305000000_F0009_BrokerTenantId_BrokerDescription') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260305000000_F0009_BrokerTenantId_BrokerDescription', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316014518_F0013_AddLineOfBusinessAndWorkflowSlaThresholds') THEN
    ALTER TABLE "Submissions" ADD "LineOfBusiness" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316014518_F0013_AddLineOfBusinessAndWorkflowSlaThresholds') THEN
    ALTER TABLE "Renewals" ADD "LineOfBusiness" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316014518_F0013_AddLineOfBusinessAndWorkflowSlaThresholds') THEN
    CREATE TABLE "WorkflowSlaThresholds" (
        "Id" uuid NOT NULL,
        "EntityType" character varying(30) NOT NULL,
        "Status" character varying(30) NOT NULL,
        "WarningDays" integer NOT NULL,
        "TargetDays" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_WorkflowSlaThresholds" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_WorkflowSlaThresholds_WarningLessThanTarget" CHECK ("WarningDays" < "TargetDays")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316014518_F0013_AddLineOfBusinessAndWorkflowSlaThresholds') THEN
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('0e5f31e6-af58-4e30-8ea0-f2d6f862994e', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'renewal', 'Early', 30, TIMESTAMPTZ '2026-03-14T00:00:00Z', 7);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('0ef57fce-6bd8-42e7-b1ef-767e44a02817', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'submission', 'BindRequested', 5, TIMESTAMPTZ '2026-03-14T00:00:00Z', 2);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('1fef8cb4-2f9b-41e8-9329-3c1a5d22790a', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'submission', 'Received', 2, TIMESTAMPTZ '2026-03-14T00:00:00Z', 1);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('2a620479-fc25-4a25-b0c5-1dce00a3693a', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'renewal', 'WaitingOnBroker', 10, TIMESTAMPTZ '2026-03-14T00:00:00Z', 5);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('3047cb13-59f8-4d87-a79d-e80e9dcf28ea', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'submission', 'Quoted', 21, TIMESTAMPTZ '2026-03-14T00:00:00Z', 7);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('30efe68f-9e5c-4e7f-9191-e68ee0f8eb26', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'renewal', 'Created', 3, TIMESTAMPTZ '2026-03-14T00:00:00Z', 1);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('33cc5f8d-33ea-4f8a-a737-2f64946f044f', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'submission', 'WaitingOnDocuments', 10, TIMESTAMPTZ '2026-03-14T00:00:00Z', 5);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('379f3ad6-68f0-4d2f-b52f-5ab9bb40f157', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'submission', 'WaitingOnBroker', 10, TIMESTAMPTZ '2026-03-14T00:00:00Z', 5);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('77ca3fa9-fddd-47ec-b4d2-84bcbf001687', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'renewal', 'InReview', 14, TIMESTAMPTZ '2026-03-14T00:00:00Z', 5);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('8b43ed42-17f2-426a-a14a-442f6a7d43d4', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'submission', 'InReview', 14, TIMESTAMPTZ '2026-03-14T00:00:00Z', 5);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('95db58fe-ef54-4c7b-b707-0cdf6458cd5b', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'submission', 'RequoteRequested', 21, TIMESTAMPTZ '2026-03-14T00:00:00Z', 7);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('bb695667-05cf-43dd-a89c-c05e4747967c', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'renewal', 'OutreachStarted', 7, TIMESTAMPTZ '2026-03-14T00:00:00Z', 3);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('d7fe40cd-c9a5-4fd5-b09c-47f10ff0f20f', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'renewal', 'Negotiation', 21, TIMESTAMPTZ '2026-03-14T00:00:00Z', 7);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('ec690f3d-84c8-4709-8b32-ff1efde52e52', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'submission', 'ReadyForUWReview', 7, TIMESTAMPTZ '2026-03-14T00:00:00Z', 3);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('ecf8f24e-8ead-4a44-b123-b85b6527db31', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'submission', 'QuotePreparation', 7, TIMESTAMPTZ '2026-03-14T00:00:00Z', 3);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('f0f6f093-7e6e-45f5-ac84-76510ddfe371', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'renewal', 'DataReview', 5, TIMESTAMPTZ '2026-03-14T00:00:00Z', 2);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('f419f936-3f6f-4135-9d9b-7744bb5e43b8', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'submission', 'Binding', 5, TIMESTAMPTZ '2026-03-14T00:00:00Z', 2);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('f501f5dd-23d4-4250-9eab-65a70d0c08f5', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'renewal', 'Quoted', 21, TIMESTAMPTZ '2026-03-14T00:00:00Z', 7);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('f6969f8b-7ab9-4ab0-a6e9-26f95f57b21c', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'submission', 'Triaging', 5, TIMESTAMPTZ '2026-03-14T00:00:00Z', 2);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('fdf17afe-4182-46e4-bf8b-3079e74b3579', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'renewal', 'BindRequested', 5, TIMESTAMPTZ '2026-03-14T00:00:00Z', 2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316014518_F0013_AddLineOfBusinessAndWorkflowSlaThresholds') THEN
    CREATE UNIQUE INDEX "UX_WorkflowSlaThresholds_EntityType_Status" ON "WorkflowSlaThresholds" ("EntityType", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316014518_F0013_AddLineOfBusinessAndWorkflowSlaThresholds') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260316014518_F0013_AddLineOfBusinessAndWorkflowSlaThresholds', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322184705_F0004_AddTaskAndUserProfileIndexes') THEN
    CREATE INDEX "IX_UserProfiles_DisplayName" ON "UserProfiles" ("DisplayName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322184705_F0004_AddTaskAndUserProfileIndexes') THEN
    CREATE INDEX "IX_Tasks_CreatedByUserId_AssignedToUserId" ON "Tasks" ("CreatedByUserId", "AssignedToUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322184705_F0004_AddTaskAndUserProfileIndexes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260322184705_F0004_AddTaskAndUserProfileIndexes', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DROP INDEX "IX_ReferenceSubmissionStatuses_DisplayOrder";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    UPDATE "Submissions"
    SET "CurrentStatus" = CASE "CurrentStatus"
        WHEN 'WaitingOnDocuments' THEN 'WaitingOnBroker'
        WHEN 'QuotePreparation' THEN 'InReview'
        WHEN 'RequoteRequested' THEN 'Quoted'
        WHEN 'Binding' THEN 'BindRequested'
        WHEN 'NotQuoted' THEN 'Declined'
        WHEN 'Lost' THEN 'Withdrawn'
        WHEN 'Expired' THEN 'Withdrawn'
        ELSE "CurrentStatus"
    END;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    UPDATE "WorkflowTransitions"
    SET "FromState" = CASE "FromState"
        WHEN 'WaitingOnDocuments' THEN 'WaitingOnBroker'
        WHEN 'QuotePreparation' THEN 'InReview'
        WHEN 'RequoteRequested' THEN 'Quoted'
        WHEN 'Binding' THEN 'BindRequested'
        WHEN 'NotQuoted' THEN 'Declined'
        WHEN 'Lost' THEN 'Withdrawn'
        WHEN 'Expired' THEN 'Withdrawn'
        ELSE "FromState"
    END,
    "ToState" = CASE "ToState"
        WHEN 'WaitingOnDocuments' THEN 'WaitingOnBroker'
        WHEN 'QuotePreparation' THEN 'InReview'
        WHEN 'RequoteRequested' THEN 'Quoted'
        WHEN 'Binding' THEN 'BindRequested'
        WHEN 'NotQuoted' THEN 'Declined'
        WHEN 'Lost' THEN 'Withdrawn'
        WHEN 'Expired' THEN 'Withdrawn'
        ELSE "ToState"
    END
    WHERE "WorkflowType" = 'Submission';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "ReferenceSubmissionStatuses"
    WHERE "Code" = 'Binding';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "ReferenceSubmissionStatuses"
    WHERE "Code" = 'Expired';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "ReferenceSubmissionStatuses"
    WHERE "Code" = 'Lost';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "ReferenceSubmissionStatuses"
    WHERE "Code" = 'NotQuoted';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "ReferenceSubmissionStatuses"
    WHERE "Code" = 'QuotePreparation';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "ReferenceSubmissionStatuses"
    WHERE "Code" = 'RequoteRequested';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "ReferenceSubmissionStatuses"
    WHERE "Code" = 'WaitingOnDocuments';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "WorkflowSlaThresholds"
    WHERE "Id" = '0ef57fce-6bd8-42e7-b1ef-767e44a02817';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "WorkflowSlaThresholds"
    WHERE "Id" = '3047cb13-59f8-4d87-a79d-e80e9dcf28ea';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "WorkflowSlaThresholds"
    WHERE "Id" = '33cc5f8d-33ea-4f8a-a737-2f64946f044f';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "WorkflowSlaThresholds"
    WHERE "Id" = '8b43ed42-17f2-426a-a14a-442f6a7d43d4';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "WorkflowSlaThresholds"
    WHERE "Id" = '95db58fe-ef54-4c7b-b707-0cdf6458cd5b';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "WorkflowSlaThresholds"
    WHERE "Id" = 'ec690f3d-84c8-4709-8b32-ff1efde52e52';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "WorkflowSlaThresholds"
    WHERE "Id" = 'ecf8f24e-8ead-4a44-b123-b85b6527db31';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    DELETE FROM "WorkflowSlaThresholds"
    WHERE "Id" = 'f419f936-3f6f-4135-9d9b-7744bb5e43b8';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    ALTER TABLE "WorkflowTransitions" ALTER COLUMN "FromState" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    ALTER TABLE "Submissions" ALTER COLUMN "PremiumEstimate" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    ALTER TABLE "Submissions" ADD "Description" character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    ALTER TABLE "Submissions" ADD "ExpirationDate" date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    UPDATE "ReferenceSubmissionStatuses" SET "DisplayOrder" = 7
    WHERE "Code" = 'BindRequested';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    UPDATE "ReferenceSubmissionStatuses" SET "DisplayOrder" = 8
    WHERE "Code" = 'Bound';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    UPDATE "ReferenceSubmissionStatuses" SET "DisplayOrder" = 9
    WHERE "Code" = 'Declined';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    UPDATE "ReferenceSubmissionStatuses" SET "DisplayOrder" = 5
    WHERE "Code" = 'InReview';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    UPDATE "ReferenceSubmissionStatuses" SET "DisplayOrder" = 6
    WHERE "Code" = 'Quoted';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    UPDATE "ReferenceSubmissionStatuses" SET "DisplayOrder" = 4
    WHERE "Code" = 'ReadyForUWReview';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    UPDATE "ReferenceSubmissionStatuses" SET "DisplayOrder" = 10
    WHERE "Code" = 'Withdrawn';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    UPDATE "WorkflowSlaThresholds" SET "TargetDays" = 3, "WarningDays" = 2
    WHERE "Id" = '379f3ad6-68f0-4d2f-b52f-5ab9bb40f157';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    UPDATE "WorkflowSlaThresholds" SET "TargetDays" = 2, "WarningDays" = 1
    WHERE "Id" = 'f6969f8b-7ab9-4ab0-a6e9-26f95f57b21c';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    CREATE UNIQUE INDEX "IX_ReferenceSubmissionStatuses_DisplayOrder" ON "ReferenceSubmissionStatuses" ("DisplayOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    CREATE INDEX "IX_Submissions_AssignedToUserId" ON "Submissions" ("AssignedToUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    CREATE INDEX "IX_Submissions_EffectiveDate" ON "Submissions" ("EffectiveDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    ALTER TABLE "Submissions" ADD CONSTRAINT "FK_Submissions_UserProfiles_AssignedToUserId" FOREIGN KEY ("AssignedToUserId") REFERENCES "UserProfiles" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331164454_F0006_SubmissionIntakeColumns') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260331164454_F0006_SubmissionIntakeColumns', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    UPDATE "Renewals"
    SET "CurrentStatus" = CASE "CurrentStatus"
        WHEN 'Created' THEN 'Identified'
        WHEN 'Early' THEN 'Identified'
        WHEN 'OutreachStarted' THEN 'Outreach'
        WHEN 'DataReview' THEN 'InReview'
        WHEN 'WaitingOnBroker' THEN 'InReview'
        WHEN 'Negotiation' THEN 'Quoted'
        WHEN 'BindRequested' THEN 'Quoted'
        WHEN 'Bound' THEN 'Completed'
        WHEN 'NotRenewed' THEN 'Lost'
        WHEN 'Lapsed' THEN 'Lost'
        WHEN 'Withdrawn' THEN 'Lost'
        WHEN 'Expired' THEN 'Lost'
        ELSE "CurrentStatus"
    END;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    UPDATE "WorkflowTransitions"
    SET "FromState" = CASE "FromState"
        WHEN 'Created' THEN 'Identified'
        WHEN 'Early' THEN 'Identified'
        WHEN 'OutreachStarted' THEN 'Outreach'
        WHEN 'DataReview' THEN 'InReview'
        WHEN 'WaitingOnBroker' THEN 'InReview'
        WHEN 'Negotiation' THEN 'Quoted'
        WHEN 'BindRequested' THEN 'Quoted'
        WHEN 'Bound' THEN 'Completed'
        WHEN 'NotRenewed' THEN 'Lost'
        WHEN 'Lapsed' THEN 'Lost'
        WHEN 'Withdrawn' THEN 'Lost'
        WHEN 'Expired' THEN 'Lost'
        ELSE "FromState"
    END,
    "ToState" = CASE "ToState"
        WHEN 'Created' THEN 'Identified'
        WHEN 'Early' THEN 'Identified'
        WHEN 'OutreachStarted' THEN 'Outreach'
        WHEN 'DataReview' THEN 'InReview'
        WHEN 'WaitingOnBroker' THEN 'InReview'
        WHEN 'Negotiation' THEN 'Quoted'
        WHEN 'BindRequested' THEN 'Quoted'
        WHEN 'Bound' THEN 'Completed'
        WHEN 'NotRenewed' THEN 'Lost'
        WHEN 'Lapsed' THEN 'Lost'
        WHEN 'Withdrawn' THEN 'Lost'
        WHEN 'Expired' THEN 'Lost'
        ELSE "ToState"
    END
    WHERE "WorkflowType" = 'Renewal';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "ReferenceRenewalStatuses"
    WHERE "Code" = 'BindRequested';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "ReferenceRenewalStatuses"
    WHERE "Code" = 'Bound';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "ReferenceRenewalStatuses"
    WHERE "Code" = 'Created';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "ReferenceRenewalStatuses"
    WHERE "Code" = 'DataReview';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "ReferenceRenewalStatuses"
    WHERE "Code" = 'Early';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "ReferenceRenewalStatuses"
    WHERE "Code" = 'Expired';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "ReferenceRenewalStatuses"
    WHERE "Code" = 'Lapsed';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "ReferenceRenewalStatuses"
    WHERE "Code" = 'Negotiation';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "ReferenceRenewalStatuses"
    WHERE "Code" = 'NotRenewed';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "ReferenceRenewalStatuses"
    WHERE "Code" = 'OutreachStarted';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "ReferenceRenewalStatuses"
    WHERE "Code" = 'WaitingOnBroker';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "ReferenceRenewalStatuses"
    WHERE "Code" = 'Withdrawn';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "WorkflowSlaThresholds"
    WHERE "Id" = '0e5f31e6-af58-4e30-8ea0-f2d6f862994e';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "WorkflowSlaThresholds"
    WHERE "Id" = '2a620479-fc25-4a25-b0c5-1dce00a3693a';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "WorkflowSlaThresholds"
    WHERE "Id" = 'd7fe40cd-c9a5-4fd5-b09c-47f10ff0f20f';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "WorkflowSlaThresholds"
    WHERE "Id" = 'f0f6f093-7e6e-45f5-ac84-76510ddfe371';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    DELETE FROM "WorkflowSlaThresholds"
    WHERE "Id" = 'fdf17afe-4182-46e4-bf8b-3079e74b3579';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    ALTER TABLE "Renewals" ALTER COLUMN "CurrentStatus" SET DEFAULT 'Identified';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    UPDATE "ReferenceRenewalStatuses" SET "Description" = 'Underwriting is reviewing the renewal', "DisplayOrder" = 3
    WHERE "Code" = 'InReview';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    UPDATE "ReferenceRenewalStatuses" SET "Description" = 'Renewal not retained', "DisplayOrder" = 6
    WHERE "Code" = 'Lost';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    UPDATE "ReferenceRenewalStatuses" SET "Description" = 'Quote has been prepared and shared', "DisplayOrder" = 4
    WHERE "Code" = 'Quoted';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Completed', 'won', 'Renewal successfully bound; linked to a policy or submission', 'Completed', 5, TRUE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Identified', 'intake', 'Renewal created from expiring policy; not yet worked', 'Identified', 1, FALSE);
    INSERT INTO "ReferenceRenewalStatuses" ("Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal")
    VALUES ('Outreach', 'waiting', 'Distribution has initiated broker/account contact', 'Outreach', 2, FALSE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    UPDATE "WorkflowSlaThresholds" SET "Status" = 'Identified', "TargetDays" = 30, "WarningDays" = 7
    WHERE "Id" = '30efe68f-9e5c-4e7f-9191-e68ee0f8eb26';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    UPDATE "WorkflowSlaThresholds" SET "Status" = 'Outreach'
    WHERE "Id" = 'bb695667-05cf-43dd-a89c-c05e4747967c';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407151358_F0007_ReconcileRenewalWorkflowStates') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260407151358_F0007_ReconcileRenewalWorkflowStates', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER TABLE "Renewals" DROP CONSTRAINT "FK_Renewals_Submissions_SubmissionId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    DROP INDEX "IX_Renewals_RenewalDate_Status";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER TABLE "Renewals" ADD "BoundPolicyId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER TABLE "Renewals" ADD "LostReasonCode" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER TABLE "Renewals" ADD "LostReasonDetail" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER TABLE "Renewals" ADD "PolicyExpirationDate" date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER TABLE "Renewals" ADD "PolicyId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER TABLE "Renewals" ADD "TargetOutreachDate" date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER TABLE "Renewals" RENAME COLUMN "SubmissionId" TO "RenewalSubmissionId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER INDEX "IX_Renewals_SubmissionId" RENAME TO "IX_Renewals_RenewalSubmissionId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    UPDATE "Renewals"
    SET
        "PolicyId" = COALESCE("PolicyId", "Id"),
        "PolicyExpirationDate" = COALESCE("PolicyExpirationDate", "RenewalDate"::date),
        "TargetOutreachDate" = COALESCE(
            "TargetOutreachDate",
            ("RenewalDate"::date - INTERVAL '90 days')::date)
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER TABLE "Renewals" ALTER COLUMN "PolicyExpirationDate" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER TABLE "Renewals" ALTER COLUMN "PolicyId" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER TABLE "Renewals" ALTER COLUMN "TargetOutreachDate" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER TABLE "Renewals" DROP COLUMN "RenewalDate";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    CREATE INDEX "IX_Renewals_PolicyExpirationDate_CurrentStatus" ON "Renewals" ("PolicyExpirationDate", "CurrentStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    CREATE UNIQUE INDEX "IX_Renewals_PolicyId_Active" ON "Renewals" ("PolicyId") WHERE "IsDeleted" = false AND "CurrentStatus" NOT IN ('Completed', 'Lost');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    CREATE INDEX "IX_Renewals_TargetOutreachDate" ON "Renewals" ("TargetOutreachDate") WHERE "IsDeleted" = false AND "CurrentStatus" = 'Identified';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER TABLE "Renewals" ADD CONSTRAINT "FK_Renewals_Submissions_RenewalSubmissionId" FOREIGN KEY ("RenewalSubmissionId") REFERENCES "Submissions" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    ALTER TABLE "Renewals" ADD CONSTRAINT "FK_Renewals_UserProfiles_AssignedToUserId" FOREIGN KEY ("AssignedToUserId") REFERENCES "UserProfiles" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408013334_F0007_ReconcileRenewalEntityShape') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260408013334_F0007_ReconcileRenewalEntityShape', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    DROP INDEX "UX_WorkflowSlaThresholds_EntityType_Status";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    ALTER TABLE "WorkflowSlaThresholds" ADD "LineOfBusiness" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    CREATE TABLE "Policies" (
        "Id" uuid NOT NULL,
        "PolicyNumber" character varying(50) NOT NULL,
        "AccountId" uuid NOT NULL,
        "BrokerId" uuid NOT NULL,
        "Carrier" character varying(100),
        "LineOfBusiness" character varying(50),
        "EffectiveDate" date NOT NULL,
        "ExpirationDate" date NOT NULL,
        "Premium" numeric(18,2),
        "CurrentStatus" character varying(30) NOT NULL DEFAULT 'Active',
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        CONSTRAINT "PK_Policies" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Policies_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Policies_Brokers_BrokerId" FOREIGN KEY ("BrokerId") REFERENCES "Brokers" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    UPDATE "WorkflowSlaThresholds" SET "LineOfBusiness" = NULL
    WHERE "Id" = '1fef8cb4-2f9b-41e8-9329-3c1a5d22790a';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    UPDATE "WorkflowSlaThresholds" SET "LineOfBusiness" = NULL, "TargetDays" = 90, "WarningDays" = 60
    WHERE "Id" = '30efe68f-9e5c-4e7f-9191-e68ee0f8eb26';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    UPDATE "WorkflowSlaThresholds" SET "LineOfBusiness" = NULL
    WHERE "Id" = '379f3ad6-68f0-4d2f-b52f-5ab9bb40f157';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    UPDATE "WorkflowSlaThresholds" SET "LineOfBusiness" = NULL
    WHERE "Id" = '77ca3fa9-fddd-47ec-b4d2-84bcbf001687';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    UPDATE "WorkflowSlaThresholds" SET "LineOfBusiness" = NULL
    WHERE "Id" = 'bb695667-05cf-43dd-a89c-c05e4747967c';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    UPDATE "WorkflowSlaThresholds" SET "LineOfBusiness" = NULL
    WHERE "Id" = 'f501f5dd-23d4-4250-9eab-65a70d0c08f5';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    UPDATE "WorkflowSlaThresholds" SET "LineOfBusiness" = NULL
    WHERE "Id" = 'f6969f8b-7ab9-4ab0-a6e9-26f95f57b21c';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "LineOfBusiness", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('0ebb7f8c-9709-4b54-a6a4-dcff0b2d3de5', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'renewal', 'ProfessionalLiability', 'Identified', 90, TIMESTAMPTZ '2026-03-14T00:00:00Z', 60);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "LineOfBusiness", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('1e92d4d0-b89a-4b5e-9e01-7d4cf14ed564', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'renewal', 'Property', 'Identified', 90, TIMESTAMPTZ '2026-03-14T00:00:00Z', 60);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "LineOfBusiness", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('c47d2142-e4b2-4dc3-90c8-3f0da6a07f8b', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'renewal', 'GeneralLiability', 'Identified', 90, TIMESTAMPTZ '2026-03-14T00:00:00Z', 60);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "LineOfBusiness", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('d5bc3dd5-17ec-4f56-a8c6-f5b503f17f0d', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'renewal', 'Cyber', 'Identified', 60, TIMESTAMPTZ '2026-03-14T00:00:00Z', 45);
    INSERT INTO "WorkflowSlaThresholds" ("Id", "CreatedAt", "EntityType", "LineOfBusiness", "Status", "TargetDays", "UpdatedAt", "WarningDays")
    VALUES ('d7286c4c-38d5-4e57-9837-2b44cf2a86cf', TIMESTAMPTZ '2026-03-14T00:00:00Z', 'renewal', 'WorkersCompensation', 'Identified', 120, TIMESTAMPTZ '2026-03-14T00:00:00Z', 90);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    INSERT INTO "Policies"
        ("Id", "PolicyNumber", "AccountId", "BrokerId", "Carrier", "LineOfBusiness", "EffectiveDate", "ExpirationDate", "Premium", "CurrentStatus", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
    SELECT DISTINCT ON (r."PolicyId")
        r."PolicyId",
        'LEGACY-' || SUBSTRING(REPLACE(r."PolicyId"::text, '-', '') FROM 1 FOR 12),
        r."AccountId",
        r."BrokerId",
        NULL,
        r."LineOfBusiness",
        (r."PolicyExpirationDate" - INTERVAL '1 year')::date,
        r."PolicyExpirationDate",
        NULL,
        'Active',
        r."CreatedAt",
        r."CreatedByUserId",
        r."UpdatedAt",
        r."UpdatedByUserId",
        FALSE
    FROM "Renewals" r
    LEFT JOIN "Policies" p ON p."Id" = r."PolicyId"
    WHERE p."Id" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    INSERT INTO "Policies"
        ("Id", "PolicyNumber", "AccountId", "BrokerId", "Carrier", "LineOfBusiness", "EffectiveDate", "ExpirationDate", "Premium", "CurrentStatus", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
    SELECT DISTINCT ON (r."BoundPolicyId")
        r."BoundPolicyId",
        'BOUND-' || SUBSTRING(REPLACE(r."BoundPolicyId"::text, '-', '') FROM 1 FOR 12),
        r."AccountId",
        r."BrokerId",
        NULL,
        r."LineOfBusiness",
        r."PolicyExpirationDate",
        (r."PolicyExpirationDate" + INTERVAL '1 year')::date,
        NULL,
        'Bound',
        r."CreatedAt",
        r."CreatedByUserId",
        r."UpdatedAt",
        r."UpdatedByUserId",
        FALSE
    FROM "Renewals" r
    LEFT JOIN "Policies" p ON p."Id" = r."BoundPolicyId"
    WHERE r."BoundPolicyId" IS NOT NULL
      AND p."Id" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    CREATE UNIQUE INDEX "UX_WorkflowSlaThresholds_EntityType_Status_LineOfBusiness"
    ON "WorkflowSlaThresholds" ("EntityType", "Status", COALESCE("LineOfBusiness", '__default__'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    CREATE INDEX "IX_Renewals_BoundPolicyId" ON "Renewals" ("BoundPolicyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    CREATE INDEX "IX_Policies_AccountId" ON "Policies" ("AccountId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    CREATE INDEX "IX_Policies_BrokerId" ON "Policies" ("BrokerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    CREATE INDEX "IX_Policies_ExpirationDate" ON "Policies" ("ExpirationDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    CREATE UNIQUE INDEX "IX_Policies_PolicyNumber" ON "Policies" ("PolicyNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    ALTER TABLE "Renewals" ADD CONSTRAINT "FK_Renewals_Policies_BoundPolicyId" FOREIGN KEY ("BoundPolicyId") REFERENCES "Policies" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    ALTER TABLE "Renewals" ADD CONSTRAINT "FK_Renewals_Policies_PolicyId" FOREIGN KEY ("PolicyId") REFERENCES "Policies" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    CREATE EXTENSION IF NOT EXISTS pg_trgm;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    DROP INDEX "IX_Accounts_Region";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" RENAME COLUMN "Name" TO "DisplayName";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" RENAME COLUMN "PrimaryState" TO "State";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ALTER COLUMN "Industry" TYPE character varying(100);
    ALTER TABLE "Accounts" ALTER COLUMN "Industry" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ALTER COLUMN "State" TYPE character varying(50);
    ALTER TABLE "Accounts" ALTER COLUMN "State" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ALTER COLUMN "Region" TYPE character varying(50);
    ALTER TABLE "Accounts" ALTER COLUMN "Region" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "LegalName" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "TaxId" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "PrimaryLineOfBusiness" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "BrokerOfRecordId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "PrimaryProducerUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "TerritoryCode" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "Address1" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "Address2" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "City" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "PostalCode" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "Country" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "StableDisplayName" character varying(200) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "MergedIntoAccountId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "DeleteReasonCode" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "DeleteReasonDetail" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD "RemovedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    UPDATE "Accounts"
    SET "StableDisplayName" = COALESCE(NULLIF(TRIM("DisplayName"), ''), 'Account-' || SUBSTRING(REPLACE("Id"::text, '-', '') FROM 1 FOR 8))
    WHERE "StableDisplayName" = '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    CREATE INDEX "IX_Accounts_BrokerOfRecordId" ON "Accounts" ("BrokerOfRecordId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    CREATE INDEX "IX_Accounts_MergedIntoAccountId" ON "Accounts" ("MergedIntoAccountId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    CREATE INDEX "IX_Accounts_Status_Region" ON "Accounts" ("Status", "Region");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    CREATE INDEX "IX_Accounts_TerritoryCode" ON "Accounts" ("TerritoryCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD CONSTRAINT "FK_Accounts_Accounts_MergedIntoAccountId" FOREIGN KEY ("MergedIntoAccountId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD CONSTRAINT "FK_Accounts_Brokers_BrokerOfRecordId" FOREIGN KEY ("BrokerOfRecordId") REFERENCES "Brokers" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    ALTER TABLE "Accounts" ADD CONSTRAINT "FK_Accounts_UserProfiles_PrimaryProducerUserId" FOREIGN KEY ("PrimaryProducerUserId") REFERENCES "UserProfiles" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    CREATE UNIQUE INDEX "IX_Accounts_TaxId_Active"
    ON "Accounts" (LOWER(TRIM("TaxId")))
    WHERE "Status" = 'Active' AND "TaxId" IS NOT NULL AND "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    CREATE INDEX "IX_Accounts_DisplayName_Trgm"
    ON "Accounts" USING gin ("DisplayName" gin_trgm_ops);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414120000_F0016_AccountLifecycle') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260414120000_F0016_AccountLifecycle', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414121000_F0016_AccountContactsAndRelationshipHistory') THEN
    CREATE TABLE "AccountContacts" (
        "Id" uuid NOT NULL,
        "AccountId" uuid NOT NULL,
        "FullName" character varying(200) NOT NULL,
        "Role" character varying(100),
        "Email" character varying(200),
        "Phone" character varying(50),
        "IsPrimary" boolean NOT NULL DEFAULT FALSE,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        CONSTRAINT "PK_AccountContacts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AccountContacts_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414121000_F0016_AccountContactsAndRelationshipHistory') THEN
    CREATE TABLE "AccountRelationshipHistory" (
        "Id" uuid NOT NULL,
        "AccountId" uuid NOT NULL,
        "RelationshipType" character varying(30) NOT NULL,
        "PreviousValue" character varying(200),
        "NewValue" character varying(200),
        "EffectiveAt" timestamp with time zone NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "Notes" character varying(500),
        CONSTRAINT "PK_AccountRelationshipHistory" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AccountRelationshipHistory_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414121000_F0016_AccountContactsAndRelationshipHistory') THEN
    CREATE INDEX "IX_AccountContacts_AccountId" ON "AccountContacts" ("AccountId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414121000_F0016_AccountContactsAndRelationshipHistory') THEN
    CREATE UNIQUE INDEX "IX_AccountContacts_AccountId_Primary" ON "AccountContacts" ("AccountId") WHERE "IsPrimary" = true AND "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414121000_F0016_AccountContactsAndRelationshipHistory') THEN
    CREATE INDEX "IX_AccountRelationshipHistory_AccountId_EffectiveAt" ON "AccountRelationshipHistory" ("AccountId", "EffectiveAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414121000_F0016_AccountContactsAndRelationshipHistory') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260414121000_F0016_AccountContactsAndRelationshipHistory', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414122000_F0016_DependentFallbackDenormalization') THEN
    ALTER TABLE "Submissions" ADD "AccountDisplayNameAtLink" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414122000_F0016_DependentFallbackDenormalization') THEN
    ALTER TABLE "Submissions" ADD "AccountStatusAtRead" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414122000_F0016_DependentFallbackDenormalization') THEN
    ALTER TABLE "Submissions" ADD "AccountSurvivorId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414122000_F0016_DependentFallbackDenormalization') THEN
    ALTER TABLE "Renewals" ADD "AccountDisplayNameAtLink" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414122000_F0016_DependentFallbackDenormalization') THEN
    ALTER TABLE "Renewals" ADD "AccountStatusAtRead" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414122000_F0016_DependentFallbackDenormalization') THEN
    ALTER TABLE "Renewals" ADD "AccountSurvivorId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414122000_F0016_DependentFallbackDenormalization') THEN
    ALTER TABLE "Policies" ADD "AccountDisplayNameAtLink" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414122000_F0016_DependentFallbackDenormalization') THEN
    ALTER TABLE "Policies" ADD "AccountStatusAtRead" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414122000_F0016_DependentFallbackDenormalization') THEN
    ALTER TABLE "Policies" ADD "AccountSurvivorId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414122000_F0016_DependentFallbackDenormalization') THEN
    UPDATE "Submissions" s
    SET
        "AccountDisplayNameAtLink" = COALESCE(a."StableDisplayName", a."DisplayName"),
        "AccountStatusAtRead" = a."Status",
        "AccountSurvivorId" = a."MergedIntoAccountId"
    FROM "Accounts" a
    WHERE a."Id" = s."AccountId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414122000_F0016_DependentFallbackDenormalization') THEN
    UPDATE "Renewals" r
    SET
        "AccountDisplayNameAtLink" = COALESCE(a."StableDisplayName", a."DisplayName"),
        "AccountStatusAtRead" = a."Status",
        "AccountSurvivorId" = a."MergedIntoAccountId"
    FROM "Accounts" a
    WHERE a."Id" = r."AccountId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414122000_F0016_DependentFallbackDenormalization') THEN
    UPDATE "Policies" p
    SET
        "AccountDisplayNameAtLink" = COALESCE(a."StableDisplayName", a."DisplayName"),
        "AccountStatusAtRead" = a."Status",
        "AccountSurvivorId" = a."MergedIntoAccountId"
    FROM "Accounts" a
    WHERE a."Id" = p."AccountId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414122000_F0016_DependentFallbackDenormalization') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260414122000_F0016_DependentFallbackDenormalization', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    DROP INDEX "IX_AccountRelationshipHistory_AccountId_EffectiveAt";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    CREATE INDEX "IX_AccountRelationshipHistory_AccountId_EffectiveAt"
    ON "AccountRelationshipHistory" ("AccountId", "EffectiveAt" DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    UPDATE "Submissions" s
    SET
        "AccountDisplayNameAtLink" = COALESCE(s."AccountDisplayNameAtLink", a."StableDisplayName", a."DisplayName"),
        "AccountStatusAtRead" = COALESCE(s."AccountStatusAtRead", a."Status")
    FROM "Accounts" a
    WHERE a."Id" = s."AccountId"
      AND (s."AccountDisplayNameAtLink" IS NULL OR s."AccountStatusAtRead" IS NULL);

    UPDATE "Renewals" r
    SET
        "AccountDisplayNameAtLink" = COALESCE(r."AccountDisplayNameAtLink", a."StableDisplayName", a."DisplayName"),
        "AccountStatusAtRead" = COALESCE(r."AccountStatusAtRead", a."Status")
    FROM "Accounts" a
    WHERE a."Id" = r."AccountId"
      AND (r."AccountDisplayNameAtLink" IS NULL OR r."AccountStatusAtRead" IS NULL);

    UPDATE "Policies" p
    SET
        "AccountDisplayNameAtLink" = COALESCE(p."AccountDisplayNameAtLink", a."StableDisplayName", a."DisplayName"),
        "AccountStatusAtRead" = COALESCE(p."AccountStatusAtRead", a."Status")
    FROM "Accounts" a
    WHERE a."Id" = p."AccountId"
      AND (p."AccountDisplayNameAtLink" IS NULL OR p."AccountStatusAtRead" IS NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    ALTER TABLE "Submissions" ALTER COLUMN "AccountDisplayNameAtLink" TYPE character varying(200);
    UPDATE "Submissions" SET "AccountDisplayNameAtLink" = '' WHERE "AccountDisplayNameAtLink" IS NULL;
    ALTER TABLE "Submissions" ALTER COLUMN "AccountDisplayNameAtLink" SET NOT NULL;
    ALTER TABLE "Submissions" ALTER COLUMN "AccountDisplayNameAtLink" SET DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    ALTER TABLE "Submissions" ALTER COLUMN "AccountStatusAtRead" TYPE character varying(20);
    UPDATE "Submissions" SET "AccountStatusAtRead" = '' WHERE "AccountStatusAtRead" IS NULL;
    ALTER TABLE "Submissions" ALTER COLUMN "AccountStatusAtRead" SET NOT NULL;
    ALTER TABLE "Submissions" ALTER COLUMN "AccountStatusAtRead" SET DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    ALTER TABLE "Renewals" ALTER COLUMN "AccountDisplayNameAtLink" TYPE character varying(200);
    UPDATE "Renewals" SET "AccountDisplayNameAtLink" = '' WHERE "AccountDisplayNameAtLink" IS NULL;
    ALTER TABLE "Renewals" ALTER COLUMN "AccountDisplayNameAtLink" SET NOT NULL;
    ALTER TABLE "Renewals" ALTER COLUMN "AccountDisplayNameAtLink" SET DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    ALTER TABLE "Renewals" ALTER COLUMN "AccountStatusAtRead" TYPE character varying(20);
    UPDATE "Renewals" SET "AccountStatusAtRead" = '' WHERE "AccountStatusAtRead" IS NULL;
    ALTER TABLE "Renewals" ALTER COLUMN "AccountStatusAtRead" SET NOT NULL;
    ALTER TABLE "Renewals" ALTER COLUMN "AccountStatusAtRead" SET DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    ALTER TABLE "Policies" ALTER COLUMN "AccountDisplayNameAtLink" TYPE character varying(200);
    UPDATE "Policies" SET "AccountDisplayNameAtLink" = '' WHERE "AccountDisplayNameAtLink" IS NULL;
    ALTER TABLE "Policies" ALTER COLUMN "AccountDisplayNameAtLink" SET NOT NULL;
    ALTER TABLE "Policies" ALTER COLUMN "AccountDisplayNameAtLink" SET DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    ALTER TABLE "Policies" ALTER COLUMN "AccountStatusAtRead" TYPE character varying(20);
    UPDATE "Policies" SET "AccountStatusAtRead" = '' WHERE "AccountStatusAtRead" IS NULL;
    ALTER TABLE "Policies" ALTER COLUMN "AccountStatusAtRead" SET NOT NULL;
    ALTER TABLE "Policies" ALTER COLUMN "AccountStatusAtRead" SET DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    UPDATE "Accounts"
    SET "TaxId" = UPPER(TRIM("TaxId"))
    WHERE "TaxId" IS NOT NULL AND "TaxId" <> UPPER(TRIM("TaxId"));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    DROP INDEX IF EXISTS "IX_Accounts_TaxId_Active";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    CREATE UNIQUE INDEX "IX_Accounts_TaxId_Active" ON "Accounts" ("TaxId") WHERE "Status" = 'Active' AND "TaxId" IS NOT NULL AND "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    CREATE TABLE "IdempotencyRecords" (
        "Id" uuid NOT NULL,
        "IdempotencyKey" character varying(120) NOT NULL,
        "Operation" character varying(60) NOT NULL,
        "ResourceId" uuid,
        "ActorUserId" uuid NOT NULL,
        "ResponseStatusCode" integer NOT NULL,
        "ResponsePayloadJson" jsonb,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_IdempotencyRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    CREATE UNIQUE INDEX "IX_IdempotencyRecords_Key_Operation" ON "IdempotencyRecords" ("IdempotencyKey", "Operation");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415120000_F0016_MergeHardening') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260415120000_F0016_MergeHardening', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE TABLE "CarrierRefs" (
        "Id" uuid NOT NULL,
        "Name" character varying(160) NOT NULL,
        "NaicCode" character varying(20),
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        CONSTRAINT "PK_CarrierRefs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    INSERT INTO "CarrierRefs" ("Id", "Name", "NaicCode", "IsActive", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
    VALUES
        ('17000000-0000-0000-0000-000000000001', 'Archway Specialty', '10001', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
        ('17000000-0000-0000-0000-000000000002', 'Blue Atlas Insurance', '10002', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
        ('17000000-0000-0000-0000-000000000003', 'Summit National', '10003', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
        ('17000000-0000-0000-0000-000000000004', 'Frontier Casualty', '10004', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
        ('17000000-0000-0000-0000-000000000005', 'Compass Mutual', '10005', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
        ('17000000-0000-0000-0000-000000000006', 'Harbor Re', '10006', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
        ('17000000-0000-0000-0000-000000000007', 'Northstar Indemnity', '10007', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
        ('17000000-0000-0000-0000-000000000008', 'Sterling Insurance Co.', '10008', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
        ('17000000-0000-0000-0000-000000000999', 'Legacy Carrier', NULL, TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "CarrierId" uuid NOT NULL DEFAULT '17000000-0000-0000-0000-000000000999';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "PremiumCurrency" character varying(3) NOT NULL DEFAULT 'USD';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "CurrentVersionId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "BoundAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "IssuedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "CancelledAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "CancellationEffectiveDate" date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "CancellationReasonCode" character varying(60);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "CancellationReasonDetail" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "ReinstatementDeadline" date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "ExpiredAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "PredecessorPolicyId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "ProducerUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "ImportSource" character varying(40) NOT NULL DEFAULT 'manual';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD "ExternalPolicyReference" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    UPDATE "Policies"
    SET
        "LineOfBusiness" = COALESCE("LineOfBusiness", 'GeneralLiability'),
        "Premium" = COALESCE("Premium", 0),
        "PremiumCurrency" = 'USD',
        "CurrentStatus" = CASE
            WHEN "CurrentStatus" IN ('Active', 'Bound', 'Expiring') THEN 'Issued'
            WHEN "CurrentStatus" IN ('Pending', 'Issued', 'Cancelled', 'Expired') THEN "CurrentStatus"
            ELSE 'Issued'
        END,
        "IssuedAt" = CASE WHEN "CurrentStatus" IN ('Active', 'Bound', 'Expiring', 'Issued', 'Expired') THEN "CreatedAt" ELSE NULL END,
        "BoundAt" = CASE WHEN "CurrentStatus" IN ('Active', 'Bound', 'Expiring', 'Issued', 'Expired') THEN "CreatedAt" ELSE NULL END,
        "ExpiredAt" = CASE WHEN "ExpirationDate" < CURRENT_DATE THEN "ExpirationDate"::timestamp AT TIME ZONE 'UTC' ELSE NULL END;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ALTER COLUMN "LineOfBusiness" TYPE character varying(50);
    ALTER TABLE "Policies" ALTER COLUMN "LineOfBusiness" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ALTER COLUMN "Premium" TYPE decimal(18,2);
    UPDATE "Policies" SET "Premium" = 0.0 WHERE "Premium" IS NULL;
    ALTER TABLE "Policies" ALTER COLUMN "Premium" SET NOT NULL;
    ALTER TABLE "Policies" ALTER COLUMN "Premium" SET DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ALTER COLUMN "CurrentStatus" TYPE character varying(30);
    ALTER TABLE "Policies" ALTER COLUMN "CurrentStatus" SET DEFAULT 'Pending';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE TABLE "PolicyVersions" (
        "Id" uuid NOT NULL,
        "PolicyId" uuid NOT NULL,
        "VersionNumber" integer NOT NULL,
        "VersionReason" character varying(40) NOT NULL,
        "EndorsementId" uuid,
        "EffectiveDate" date NOT NULL,
        "ExpirationDate" date NOT NULL,
        "TotalPremium" decimal(18,2) NOT NULL,
        "PremiumCurrency" character varying(3) NOT NULL DEFAULT 'USD',
        "ProfileSnapshotJson" jsonb NOT NULL,
        "CoverageSnapshotJson" jsonb NOT NULL,
        "PremiumSnapshotJson" jsonb NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        CONSTRAINT "PK_PolicyVersions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PolicyVersions_Policies_PolicyId" FOREIGN KEY ("PolicyId") REFERENCES "Policies" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE TABLE "PolicyEndorsements" (
        "Id" uuid NOT NULL,
        "PolicyId" uuid NOT NULL,
        "EndorsementNumber" integer NOT NULL,
        "PolicyVersionId" uuid NOT NULL,
        "EndorsementReasonCode" character varying(80) NOT NULL,
        "EndorsementReasonDetail" character varying(1000),
        "EffectiveDate" date NOT NULL,
        "PremiumDelta" decimal(18,2) NOT NULL,
        "PremiumCurrency" character varying(3) NOT NULL DEFAULT 'USD',
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        CONSTRAINT "PK_PolicyEndorsements" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PolicyEndorsements_Policies_PolicyId" FOREIGN KEY ("PolicyId") REFERENCES "Policies" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_PolicyEndorsements_PolicyVersions_PolicyVersionId" FOREIGN KEY ("PolicyVersionId") REFERENCES "PolicyVersions" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE TABLE "PolicyCoverageLines" (
        "Id" uuid NOT NULL,
        "PolicyId" uuid NOT NULL,
        "PolicyVersionId" uuid NOT NULL,
        "VersionNumber" integer NOT NULL,
        "CoverageCode" character varying(40) NOT NULL,
        "CoverageName" character varying(200),
        "Limit" decimal(18,2) NOT NULL,
        "Deductible" decimal(18,2),
        "Premium" decimal(18,2) NOT NULL,
        "PremiumCurrency" character varying(3) NOT NULL DEFAULT 'USD',
        "ExposureBasis" character varying(40),
        "ExposureQuantity" decimal(18,2),
        "IsCurrent" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        CONSTRAINT "PK_PolicyCoverageLines" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PolicyCoverageLines_Policies_PolicyId" FOREIGN KEY ("PolicyId") REFERENCES "Policies" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_PolicyCoverageLines_PolicyVersions_PolicyVersionId" FOREIGN KEY ("PolicyVersionId") REFERENCES "PolicyVersions" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE TEMP TABLE f0018_policy_version_ids ON COMMIT DROP AS
    SELECT
        "Id" AS policy_id,
        (substr(md5("Id"::text || ':version:1'), 1, 8) || '-' ||
         substr(md5("Id"::text || ':version:1'), 9, 4) || '-' ||
         substr(md5("Id"::text || ':version:1'), 13, 4) || '-' ||
         substr(md5("Id"::text || ':version:1'), 17, 4) || '-' ||
         substr(md5("Id"::text || ':version:1'), 21, 12))::uuid AS version_id
    FROM "Policies";

    INSERT INTO "PolicyVersions"
        ("Id", "PolicyId", "VersionNumber", "VersionReason", "EffectiveDate", "ExpirationDate", "TotalPremium",
         "PremiumCurrency", "ProfileSnapshotJson", "CoverageSnapshotJson", "PremiumSnapshotJson",
         "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
    SELECT
        v.version_id,
        p."Id",
        1,
        'IssuedInitial',
        p."EffectiveDate",
        p."ExpirationDate",
        p."Premium",
        p."PremiumCurrency",
        jsonb_build_object('accountId', p."AccountId", 'brokerOfRecordId', p."BrokerId", 'carrierId', p."CarrierId", 'producerUserId', p."ProducerUserId"),
        '[]'::jsonb,
        jsonb_build_object('totalPremium', p."Premium", 'premiumCurrency', p."PremiumCurrency"),
        p."CreatedAt",
        p."CreatedByUserId",
        p."UpdatedAt",
        p."UpdatedByUserId",
        FALSE
    FROM "Policies" p
    JOIN f0018_policy_version_ids v ON v.policy_id = p."Id";

    UPDATE "Policies" p
    SET "CurrentVersionId" = v.version_id
    FROM f0018_policy_version_ids v
    WHERE v.policy_id = p."Id";

    INSERT INTO "PolicyCoverageLines"
        ("Id", "PolicyId", "PolicyVersionId", "VersionNumber", "CoverageCode", "CoverageName", "Limit", "Premium",
         "PremiumCurrency", "IsCurrent", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
    SELECT
        (substr(md5(p."Id"::text || ':coverage:1'), 1, 8) || '-' ||
         substr(md5(p."Id"::text || ':coverage:1'), 9, 4) || '-' ||
         substr(md5(p."Id"::text || ':coverage:1'), 13, 4) || '-' ||
         substr(md5(p."Id"::text || ':coverage:1'), 17, 4) || '-' ||
         substr(md5(p."Id"::text || ':coverage:1'), 21, 12))::uuid,
        p."Id",
        p."CurrentVersionId",
        1,
        p."LineOfBusiness",
        p."LineOfBusiness",
        p."Premium" * 10,
        p."Premium",
        p."PremiumCurrency",
        TRUE,
        p."CreatedAt",
        p."CreatedByUserId",
        p."UpdatedAt",
        p."UpdatedByUserId",
        FALSE
    FROM "Policies" p
    WHERE p."CurrentVersionId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    INSERT INTO "WorkflowSlaThresholds" ("Id", "EntityType", "Status", "LineOfBusiness", "WarningDays", "TargetDays", "CreatedAt", "UpdatedAt")
    SELECT new_id, 'policy', 'ReinstatementWindow', lob, 7, target_days, TIMESTAMPTZ '2026-04-22T00:00:00Z', TIMESTAMPTZ '2026-04-22T00:00:00Z'
    FROM (VALUES
        ('18000000-0000-0000-0000-000000000000'::uuid, NULL::text, 30),
        ('18000000-0000-0000-0000-000000000001'::uuid, 'Property', 30),
        ('18000000-0000-0000-0000-000000000002'::uuid, 'GeneralLiability', 30),
        ('18000000-0000-0000-0000-000000000003'::uuid, 'CommercialAuto', 30),
        ('18000000-0000-0000-0000-000000000004'::uuid, 'WorkersCompensation', 45),
        ('18000000-0000-0000-0000-000000000005'::uuid, 'ProfessionalLiability', 30),
        ('18000000-0000-0000-0000-000000000006'::uuid, 'Marine', 30),
        ('18000000-0000-0000-0000-000000000007'::uuid, 'Umbrella', 30),
        ('18000000-0000-0000-0000-000000000008'::uuid, 'Surety', 30),
        ('18000000-0000-0000-0000-000000000009'::uuid, 'Cyber', 15),
        ('18000000-0000-0000-0000-000000000010'::uuid, 'DirectorsOfficers', 30)
    ) AS rows(new_id, lob, target_days)
    WHERE NOT EXISTS (
        SELECT 1 FROM "WorkflowSlaThresholds" existing
        WHERE existing."EntityType" = 'policy'
          AND existing."Status" = 'ReinstatementWindow'
          AND COALESCE(existing."LineOfBusiness", '__default__') = COALESCE(rows.lob, '__default__')
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE UNIQUE INDEX "UX_CarrierRefs_Name" ON "CarrierRefs" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE INDEX "IX_Policies_CarrierId" ON "Policies" ("CarrierId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE INDEX "IX_Policies_CurrentStatus" ON "Policies" ("CurrentStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE INDEX "IX_Policies_CurrentVersionId" ON "Policies" ("CurrentVersionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE INDEX "IX_Policies_PredecessorPolicyId" ON "Policies" ("PredecessorPolicyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE INDEX "IX_Policies_ProducerUserId" ON "Policies" ("ProducerUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE UNIQUE INDEX "UX_PolicyVersions_PolicyId_VersionNumber" ON "PolicyVersions" ("PolicyId", "VersionNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE INDEX "IX_PolicyVersions_EndorsementId" ON "PolicyVersions" ("EndorsementId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE UNIQUE INDEX "UX_PolicyEndorsements_PolicyId_EndorsementNumber" ON "PolicyEndorsements" ("PolicyId", "EndorsementNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE INDEX "IX_PolicyEndorsements_PolicyVersionId" ON "PolicyEndorsements" ("PolicyVersionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE INDEX "IX_PolicyCoverageLines_PolicyId_IsCurrent" ON "PolicyCoverageLines" ("PolicyId", "IsCurrent");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    CREATE INDEX "IX_PolicyCoverageLines_PolicyVersionId" ON "PolicyCoverageLines" ("PolicyVersionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD CONSTRAINT "FK_Policies_CarrierRefs_CarrierId" FOREIGN KEY ("CarrierId") REFERENCES "CarrierRefs" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD CONSTRAINT "FK_Policies_Policies_PredecessorPolicyId" FOREIGN KEY ("PredecessorPolicyId") REFERENCES "Policies" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    ALTER TABLE "Policies" ADD CONSTRAINT "FK_Policies_UserProfiles_ProducerUserId" FOREIGN KEY ("ProducerUserId") REFERENCES "UserProfiles" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260422021000_F0018_PolicyLifecycleAggregate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260422021000_F0018_PolicyLifecycleAggregate', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE TABLE "LobProducts" (
        "Id" uuid NOT NULL,
        "ProductKey" character varying(80) NOT NULL,
        "LineOfBusiness" character varying(50),
        "Name" character varying(160) NOT NULL,
        "Status" character varying(30) NOT NULL DEFAULT 'Active',
        "Description" character varying(1000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        CONSTRAINT "PK_LobProducts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE TABLE "LobProductVersions" (
        "Id" uuid NOT NULL,
        "LobProductId" uuid NOT NULL,
        "Version" character varying(40) NOT NULL,
        "Status" character varying(30) NOT NULL DEFAULT 'Active',
        "EffectiveFrom" date NOT NULL,
        "DeprecatedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        CONSTRAINT "PK_LobProductVersions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LobProductVersions_LobProducts_LobProductId" FOREIGN KEY ("LobProductId") REFERENCES "LobProducts" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE TABLE "LobSchemaBundles" (
        "Id" uuid NOT NULL,
        "LobProductVersionId" uuid NOT NULL,
        "SchemaVersion" character varying(40) NOT NULL,
        "Status" character varying(30) NOT NULL DEFAULT 'Draft',
        "DataSchemaJson" jsonb NOT NULL,
        "UiSchemaJson" jsonb NOT NULL,
        "RulesJson" jsonb NOT NULL,
        "ProjectionMapJson" jsonb NOT NULL,
        "ContentHash" character varying(128) NOT NULL,
        "ActivatedAt" timestamp with time zone,
        "ActivatedByUserId" uuid,
        "RetiredAt" timestamp with time zone,
        "RetiredByUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        CONSTRAINT "PK_LobSchemaBundles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LobSchemaBundles_LobProductVersions_LobProductVersionId" FOREIGN KEY ("LobProductVersionId") REFERENCES "LobProductVersions" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE TABLE "LobBundleActivationEvents" (
        "Id" uuid NOT NULL,
        "LobSchemaBundleId" uuid NOT NULL,
        "FromStatus" character varying(30) NOT NULL,
        "ToStatus" character varying(30) NOT NULL,
        "ChangeNote" character varying(1000),
        "ActorUserId" uuid NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        CONSTRAINT "PK_LobBundleActivationEvents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LobBundleActivationEvents_LobSchemaBundles_LobSchemaBundleId" FOREIGN KEY ("LobSchemaBundleId") REFERENCES "LobSchemaBundles" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "Submissions" ADD "LobProductVersionId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "Submissions" ADD "LobAttributesJson" jsonb NOT NULL DEFAULT ('{}'::jsonb);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "Renewals" ADD "LobProductVersionId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "Renewals" ADD "LobAttributesJson" jsonb NOT NULL DEFAULT ('{}'::jsonb);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "PolicyVersions" ADD "LineOfBusiness" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "PolicyVersions" ADD "LobProductVersionId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "PolicyVersions" ADD "LobAttributesJson" jsonb NOT NULL DEFAULT ('{}'::jsonb);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "PolicyEndorsements" ADD "LineOfBusiness" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "PolicyEndorsements" ADD "LobProductVersionId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "PolicyEndorsements" ADD "LobAttributesJson" jsonb NOT NULL DEFAULT ('{}'::jsonb);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    INSERT INTO "LobProducts" ("Id", "ProductKey", "LineOfBusiness", "Name", "Status", "Description", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
    VALUES
      ('7b8f0034-0000-5000-9000-000000000000', '_unspecified', NULL, 'Unspecified product attributes', 'Internal', 'F0034 sentinel for null LOB carriers with empty attributes.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0000-5000-9000-000000000001', '_legacy_property', 'Property', 'Legacy Property attributes', 'Internal', 'F0034 pass-through sentinel for legacy Property carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0000-5000-9000-000000000002', '_legacy_general_liability', 'GeneralLiability', 'Legacy General Liability attributes', 'Internal', 'F0034 pass-through sentinel for legacy General Liability carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0000-5000-9000-000000000003', '_legacy_commercial_auto', 'CommercialAuto', 'Legacy Commercial Auto attributes', 'Internal', 'F0034 pass-through sentinel for legacy Commercial Auto carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0000-5000-9000-000000000004', '_legacy_workers_compensation', 'WorkersCompensation', 'Legacy Workers Compensation attributes', 'Internal', 'F0034 pass-through sentinel for legacy Workers Compensation carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0000-5000-9000-000000000005', '_legacy_professional_liability', 'ProfessionalLiability', 'Legacy Professional Liability attributes', 'Internal', 'F0034 pass-through sentinel for legacy Professional Liability carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0000-5000-9000-000000000006', '_legacy_marine', 'Marine', 'Legacy Marine attributes', 'Internal', 'F0034 pass-through sentinel for legacy Marine carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0000-5000-9000-000000000007', '_legacy_umbrella', 'Umbrella', 'Legacy Umbrella attributes', 'Internal', 'F0034 pass-through sentinel for legacy Umbrella carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0000-5000-9000-000000000008', '_legacy_surety', 'Surety', 'Legacy Surety attributes', 'Internal', 'F0034 pass-through sentinel for legacy Surety carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0000-5000-9000-000000000009', '_legacy_cyber', 'Cyber', 'Legacy Cyber attributes', 'Internal', 'F0034 pass-through sentinel for legacy Cyber carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0000-5000-9000-000000000010', '_legacy_directors_officers', 'DirectorsOfficers', 'Legacy Directors Officers attributes', 'Internal', 'F0034 pass-through sentinel for legacy Directors Officers carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('34000000-0000-0000-0000-000000000001', 'cyber', 'Cyber', 'Cyber Liability', 'Active', 'Cyber product schema registry entry for F0034.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE);

    INSERT INTO "LobProductVersions" ("Id", "LobProductId", "Version", "Status", "EffectiveFrom", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
    VALUES
      ('aa901058-2402-5370-9978-66eb184066be', '7b8f0034-0000-5000-9000-000000000000', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0001-5000-9000-000000000001', '7b8f0034-0000-5000-9000-000000000001', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0001-5000-9000-000000000002', '7b8f0034-0000-5000-9000-000000000002', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0001-5000-9000-000000000003', '7b8f0034-0000-5000-9000-000000000003', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0001-5000-9000-000000000004', '7b8f0034-0000-5000-9000-000000000004', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0001-5000-9000-000000000005', '7b8f0034-0000-5000-9000-000000000005', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0001-5000-9000-000000000006', '7b8f0034-0000-5000-9000-000000000006', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0001-5000-9000-000000000007', '7b8f0034-0000-5000-9000-000000000007', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0001-5000-9000-000000000008', '7b8f0034-0000-5000-9000-000000000008', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('4ffc79e6-4e32-5d39-a82c-891b6034ab9e', '7b8f0034-0000-5000-9000-000000000009', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('7b8f0034-0001-5000-9000-000000000010', '7b8f0034-0000-5000-9000-000000000010', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
      ('48f5f86a-7396-50bf-92dd-a3a36fe63c20', '34000000-0000-0000-0000-000000000001', '1.0.0', 'Active', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    INSERT INTO "LobSchemaBundles" ("Id", "LobProductVersionId", "SchemaVersion", "Status", "DataSchemaJson", "UiSchemaJson", "RulesJson", "ProjectionMapJson", "ContentHash", "ActivatedAt", "ActivatedByUserId", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
    SELECT
      ('7b8f0034-0002-5000-9000-' || lpad(row_number() OVER ()::text, 12, '0'))::uuid,
      v."Id",
      '0.0.0',
      'Internal',
      '{"type":"object","additionalProperties":true}'::jsonb,
      '{"sections":[]}'::jsonb,
      '{"rules":[]}'::jsonb,
      '{}'::jsonb,
      'sha256:f0034-sentinel-' || v."Id"::text,
      TIMESTAMPTZ '2026-05-07T00:00:00Z',
      '00000000-0000-0000-0000-000000000000',
      TIMESTAMPTZ '2026-05-07T00:00:00Z',
      '00000000-0000-0000-0000-000000000000',
      TIMESTAMPTZ '2026-05-07T00:00:00Z',
      '00000000-0000-0000-0000-000000000000',
      FALSE
    FROM "LobProductVersions" v
    WHERE v."Version" = '0.0.0';

    INSERT INTO "LobSchemaBundles" ("Id", "LobProductVersionId", "SchemaVersion", "Status", "DataSchemaJson", "UiSchemaJson", "RulesJson", "ProjectionMapJson", "ContentHash", "ActivatedAt", "ActivatedByUserId", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
    VALUES (
        '34000000-0000-0000-0000-000000000201',
        '48f5f86a-7396-50bf-92dd-a3a36fe63c20',
        '1.0.0',
        'Active',
        $json${
          "type": "object",
          "required": ["revenueBand", "recordsHeld", "controls", "requestedLimit", "requestedRetention"],
          "properties": {
            "revenueBand": { "type": "string", "enum": ["0-10M", "10-50M", "50-250M", "250M+"] },
            "recordsHeld": { "type": "integer", "minimum": 0 },
            "controls": {
              "type": "object",
              "required": ["mfaEnabled", "edrEnabled", "backupEnabled", "trainingFrequency"],
              "properties": {
                "mfaEnabled": { "type": "boolean" },
                "mfaMaturity": { "type": ["string", "null"], "enum": ["Implemented", "Partial", "Planned", null] },
                "edrEnabled": { "type": "boolean" },
                "backupEnabled": { "type": "boolean" },
                "trainingFrequency": { "type": "string", "enum": ["Annual", "SemiAnnual", "Quarterly"] }
              },
              "additionalProperties": false
            },
            "requestedLimit": {
              "type": "object",
              "required": ["amountMinor", "currency"],
              "properties": {
                "amountMinor": { "type": "integer", "minimum": 1 },
                "currency": { "type": "string", "enum": ["USD"] }
              },
              "additionalProperties": false
            },
            "requestedRetention": {
              "type": "object",
              "required": ["amountMinor", "currency"],
              "properties": {
                "amountMinor": { "type": "integer", "minimum": 0 },
                "currency": { "type": "string", "enum": ["USD"] }
              },
              "additionalProperties": false
            }
          },
          "additionalProperties": true
        }$json$::jsonb,
        $json${
          "sections": [
            { "id": "exposure", "title": "Exposure", "fields": ["revenueBand", "recordsHeld"] },
            { "id": "controls", "title": "Controls", "fields": ["controls.mfaEnabled", "controls.mfaMaturity", "controls.edrEnabled", "controls.backupEnabled", "controls.trainingFrequency"] },
            { "id": "terms", "title": "Requested Terms", "fields": ["requestedLimit", "requestedRetention"] }
          ]
        }$json$::jsonb,
        $json${
          "rules": [
            { "id": "mfa_required_for_high_record_count", "when": "recordsHeld >= 1000000", "path": "controls.mfaEnabled" },
            { "id": "minimum_retention_not_met", "when": "requestedRetention.amountMinor < requestedLimit.amountMinor / 100" }
          ]
        }$json$::jsonb,
        $json${
          "submission": { "attributes": "LobAttributesJson", "productVersionId": "LobProductVersionId" },
          "policyVersion": { "attributes": "LobAttributesJson", "productVersionId": "LobProductVersionId" },
          "renewal": { "attributes": "LobAttributesJson", "productVersionId": "LobProductVersionId" }
        }$json$::jsonb,
        'sha256:f0034-cyber-1-0-0-seed',
        TIMESTAMPTZ '2026-05-07T00:00:00Z',
        '00000000-0000-0000-0000-000000000000',
        TIMESTAMPTZ '2026-05-07T00:00:00Z',
        '00000000-0000-0000-0000-000000000000',
        TIMESTAMPTZ '2026-05-07T00:00:00Z',
        '00000000-0000-0000-0000-000000000000',
        FALSE);

    INSERT INTO "LobBundleActivationEvents" ("Id", "LobSchemaBundleId", "FromStatus", "ToStatus", "ChangeNote", "ActorUserId", "OccurredAt", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
    VALUES ('34000000-0000-0000-0000-000000000301', '34000000-0000-0000-0000-000000000201', 'Draft', 'Active', 'Seed Cyber 1.0.0 bundle for F0034.', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    UPDATE "PolicyVersions" pv
    SET "LineOfBusiness" = COALESCE(p."LineOfBusiness", 'GeneralLiability')
    FROM "Policies" p
    WHERE pv."PolicyId" = p."Id";

    UPDATE "PolicyEndorsements" pe
    SET "LineOfBusiness" = COALESCE(p."LineOfBusiness", 'GeneralLiability')
    FROM "Policies" p
    WHERE pe."PolicyId" = p."Id";

    UPDATE "Submissions"
    SET "LobProductVersionId" = CASE
        WHEN "LineOfBusiness" IS NULL THEN 'aa901058-2402-5370-9978-66eb184066be'::uuid
        WHEN "LineOfBusiness" = 'Property' THEN '7b8f0034-0001-5000-9000-000000000001'::uuid
        WHEN "LineOfBusiness" = 'GeneralLiability' THEN '7b8f0034-0001-5000-9000-000000000002'::uuid
        WHEN "LineOfBusiness" = 'CommercialAuto' THEN '7b8f0034-0001-5000-9000-000000000003'::uuid
        WHEN "LineOfBusiness" = 'WorkersCompensation' THEN '7b8f0034-0001-5000-9000-000000000004'::uuid
        WHEN "LineOfBusiness" = 'ProfessionalLiability' THEN '7b8f0034-0001-5000-9000-000000000005'::uuid
        WHEN "LineOfBusiness" = 'Marine' THEN '7b8f0034-0001-5000-9000-000000000006'::uuid
        WHEN "LineOfBusiness" = 'Umbrella' THEN '7b8f0034-0001-5000-9000-000000000007'::uuid
        WHEN "LineOfBusiness" = 'Surety' THEN '7b8f0034-0001-5000-9000-000000000008'::uuid
        WHEN "LineOfBusiness" = 'Cyber' THEN '4ffc79e6-4e32-5d39-a82c-891b6034ab9e'::uuid
        WHEN "LineOfBusiness" = 'DirectorsOfficers' THEN '7b8f0034-0001-5000-9000-000000000010'::uuid
        ELSE 'aa901058-2402-5370-9978-66eb184066be'::uuid
    END;

    UPDATE "Renewals"
    SET "LobProductVersionId" = CASE
        WHEN "LineOfBusiness" IS NULL THEN 'aa901058-2402-5370-9978-66eb184066be'::uuid
        WHEN "LineOfBusiness" = 'Property' THEN '7b8f0034-0001-5000-9000-000000000001'::uuid
        WHEN "LineOfBusiness" = 'GeneralLiability' THEN '7b8f0034-0001-5000-9000-000000000002'::uuid
        WHEN "LineOfBusiness" = 'CommercialAuto' THEN '7b8f0034-0001-5000-9000-000000000003'::uuid
        WHEN "LineOfBusiness" = 'WorkersCompensation' THEN '7b8f0034-0001-5000-9000-000000000004'::uuid
        WHEN "LineOfBusiness" = 'ProfessionalLiability' THEN '7b8f0034-0001-5000-9000-000000000005'::uuid
        WHEN "LineOfBusiness" = 'Marine' THEN '7b8f0034-0001-5000-9000-000000000006'::uuid
        WHEN "LineOfBusiness" = 'Umbrella' THEN '7b8f0034-0001-5000-9000-000000000007'::uuid
        WHEN "LineOfBusiness" = 'Surety' THEN '7b8f0034-0001-5000-9000-000000000008'::uuid
        WHEN "LineOfBusiness" = 'Cyber' THEN '4ffc79e6-4e32-5d39-a82c-891b6034ab9e'::uuid
        WHEN "LineOfBusiness" = 'DirectorsOfficers' THEN '7b8f0034-0001-5000-9000-000000000010'::uuid
        ELSE 'aa901058-2402-5370-9978-66eb184066be'::uuid
    END;

    UPDATE "PolicyVersions"
    SET "LobProductVersionId" = CASE
        WHEN "LineOfBusiness" = 'Property' THEN '7b8f0034-0001-5000-9000-000000000001'::uuid
        WHEN "LineOfBusiness" = 'GeneralLiability' THEN '7b8f0034-0001-5000-9000-000000000002'::uuid
        WHEN "LineOfBusiness" = 'CommercialAuto' THEN '7b8f0034-0001-5000-9000-000000000003'::uuid
        WHEN "LineOfBusiness" = 'WorkersCompensation' THEN '7b8f0034-0001-5000-9000-000000000004'::uuid
        WHEN "LineOfBusiness" = 'ProfessionalLiability' THEN '7b8f0034-0001-5000-9000-000000000005'::uuid
        WHEN "LineOfBusiness" = 'Marine' THEN '7b8f0034-0001-5000-9000-000000000006'::uuid
        WHEN "LineOfBusiness" = 'Umbrella' THEN '7b8f0034-0001-5000-9000-000000000007'::uuid
        WHEN "LineOfBusiness" = 'Surety' THEN '7b8f0034-0001-5000-9000-000000000008'::uuid
        WHEN "LineOfBusiness" = 'Cyber' THEN '4ffc79e6-4e32-5d39-a82c-891b6034ab9e'::uuid
        WHEN "LineOfBusiness" = 'DirectorsOfficers' THEN '7b8f0034-0001-5000-9000-000000000010'::uuid
        ELSE '7b8f0034-0001-5000-9000-000000000002'::uuid
    END;

    UPDATE "PolicyEndorsements"
    SET "LobProductVersionId" = CASE
        WHEN "LineOfBusiness" = 'Property' THEN '7b8f0034-0001-5000-9000-000000000001'::uuid
        WHEN "LineOfBusiness" = 'GeneralLiability' THEN '7b8f0034-0001-5000-9000-000000000002'::uuid
        WHEN "LineOfBusiness" = 'CommercialAuto' THEN '7b8f0034-0001-5000-9000-000000000003'::uuid
        WHEN "LineOfBusiness" = 'WorkersCompensation' THEN '7b8f0034-0001-5000-9000-000000000004'::uuid
        WHEN "LineOfBusiness" = 'ProfessionalLiability' THEN '7b8f0034-0001-5000-9000-000000000005'::uuid
        WHEN "LineOfBusiness" = 'Marine' THEN '7b8f0034-0001-5000-9000-000000000006'::uuid
        WHEN "LineOfBusiness" = 'Umbrella' THEN '7b8f0034-0001-5000-9000-000000000007'::uuid
        WHEN "LineOfBusiness" = 'Surety' THEN '7b8f0034-0001-5000-9000-000000000008'::uuid
        WHEN "LineOfBusiness" = 'Cyber' THEN '4ffc79e6-4e32-5d39-a82c-891b6034ab9e'::uuid
        WHEN "LineOfBusiness" = 'DirectorsOfficers' THEN '7b8f0034-0001-5000-9000-000000000010'::uuid
        ELSE '7b8f0034-0001-5000-9000-000000000002'::uuid
    END;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "Submissions" ALTER COLUMN "LobProductVersionId" TYPE uuid;
    ALTER TABLE "Submissions" ALTER COLUMN "LobProductVersionId" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "Renewals" ALTER COLUMN "LobProductVersionId" TYPE uuid;
    ALTER TABLE "Renewals" ALTER COLUMN "LobProductVersionId" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "PolicyVersions" ALTER COLUMN "LineOfBusiness" TYPE character varying(50);
    ALTER TABLE "PolicyVersions" ALTER COLUMN "LineOfBusiness" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "PolicyVersions" ALTER COLUMN "LobProductVersionId" TYPE uuid;
    ALTER TABLE "PolicyVersions" ALTER COLUMN "LobProductVersionId" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "PolicyEndorsements" ALTER COLUMN "LineOfBusiness" TYPE character varying(50);
    ALTER TABLE "PolicyEndorsements" ALTER COLUMN "LineOfBusiness" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "PolicyEndorsements" ALTER COLUMN "LobProductVersionId" TYPE uuid;
    ALTER TABLE "PolicyEndorsements" ALTER COLUMN "LobProductVersionId" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE UNIQUE INDEX "UX_LobProducts_ProductKey" ON "LobProducts" ("ProductKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE INDEX "IX_LobProducts_LineOfBusiness_Status" ON "LobProducts" ("LineOfBusiness", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE UNIQUE INDEX "UX_LobProductVersions_Product_Version" ON "LobProductVersions" ("LobProductId", "Version");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE UNIQUE INDEX "UX_LobSchemaBundles_ProductVersion_SchemaVersion" ON "LobSchemaBundles" ("LobProductVersionId", "SchemaVersion");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE INDEX "IX_LobSchemaBundles_ProductVersion_Status" ON "LobSchemaBundles" ("LobProductVersionId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE INDEX "IX_LobBundleActivationEvents_Bundle_OccurredAt" ON "LobBundleActivationEvents" ("LobSchemaBundleId", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE INDEX "IX_Submissions_LobProductVersionId" ON "Submissions" ("LobProductVersionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE INDEX "IX_Renewals_LobProductVersionId" ON "Renewals" ("LobProductVersionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE INDEX "IX_PolicyVersions_LobProductVersionId" ON "PolicyVersions" ("LobProductVersionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE INDEX "IX_PolicyEndorsements_LobProductVersionId" ON "PolicyEndorsements" ("LobProductVersionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "Submissions" ADD CONSTRAINT "FK_Submissions_LobProductVersions_LobProductVersionId" FOREIGN KEY ("LobProductVersionId") REFERENCES "LobProductVersions" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "Renewals" ADD CONSTRAINT "FK_Renewals_LobProductVersions_LobProductVersionId" FOREIGN KEY ("LobProductVersionId") REFERENCES "LobProductVersions" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "PolicyVersions" ADD CONSTRAINT "FK_PolicyVersions_LobProductVersions_LobProductVersionId" FOREIGN KEY ("LobProductVersionId") REFERENCES "LobProductVersions" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    ALTER TABLE "PolicyEndorsements" ADD CONSTRAINT "FK_PolicyEndorsements_LobProductVersions_LobProductVersionId" FOREIGN KEY ("LobProductVersionId") REFERENCES "LobProductVersions" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    CREATE OR REPLACE FUNCTION nebula_lob_carrier_consistency()
    RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    DECLARE
        product_key text;
        product_lob text;
        old_product_key text;
    BEGIN
        SELECT p."ProductKey", p."LineOfBusiness"
        INTO product_key, product_lob
        FROM "LobProductVersions" v
        JOIN "LobProducts" p ON p."Id" = v."LobProductId"
        WHERE v."Id" = NEW."LobProductVersionId";

        IF product_key IS NULL THEN
            RAISE EXCEPTION 'LOB_SCHEMA_NOT_FOUND' USING ERRCODE = 'P0001';
        END IF;

        IF TG_OP = 'UPDATE'
           AND OLD."LobProductVersionId" IS DISTINCT FROM NEW."LobProductVersionId"
           AND COALESCE(current_setting('app.lob_migration_in_progress', true), 'false') <> 'true' THEN
            SELECT p."ProductKey"
            INTO old_product_key
            FROM "LobProductVersions" v
            JOIN "LobProducts" p ON p."Id" = v."LobProductId"
            WHERE v."Id" = OLD."LobProductVersionId";

            IF NOT (
                (
                    old_product_key = '_unspecified'
                    AND OLD."LineOfBusiness" IS NULL
                    AND NEW."LineOfBusiness" IS NOT NULL
                    AND NEW."LineOfBusiness" IS NOT DISTINCT FROM product_lob
                    AND NEW."LobAttributesJson" <> '{}'::jsonb
                )
                OR (
                    old_product_key LIKE '\_legacy\_%' ESCAPE '\'
                    AND OLD."LineOfBusiness" IS NOT NULL
                    AND OLD."LineOfBusiness" IS NOT DISTINCT FROM NEW."LineOfBusiness"
                    AND NEW."LineOfBusiness" IS NOT DISTINCT FROM product_lob
                    AND OLD."LobAttributesJson" = '{}'::jsonb
                    AND NEW."LobAttributesJson" <> '{}'::jsonb
                )
                OR (
                    TG_TABLE_NAME IN ('Submissions', 'Renewals')
                    AND (old_product_key = '_unspecified' OR old_product_key LIKE '\_legacy\_%' ESCAPE '\')
                    AND OLD."LobAttributesJson" = '{}'::jsonb
                    AND NEW."LobAttributesJson" = '{}'::jsonb
                    AND (
                        (product_key = '_unspecified' AND NEW."LineOfBusiness" IS NULL)
                        OR product_key LIKE '\_legacy\_%' ESCAPE '\'
                    )
                )
            ) THEN
                RAISE EXCEPTION 'LOB_PRODUCT_VERSION_IMMUTABLE' USING ERRCODE = 'P0001';
            END IF;
        END IF;

        IF product_key = '_unspecified' THEN
            IF TG_TABLE_NAME IN ('Submissions', 'Renewals')
               AND NEW."LineOfBusiness" IS NULL
               AND NEW."LobAttributesJson" = '{}'::jsonb THEN
                RETURN NEW;
            END IF;
            RAISE EXCEPTION 'LOB_PRODUCT_MISMATCH' USING ERRCODE = 'P0001';
        END IF;

        IF product_key LIKE '\_legacy\_%' ESCAPE '\' THEN
            IF product_lob IS NOT DISTINCT FROM NEW."LineOfBusiness"
               AND NEW."LobAttributesJson" = '{}'::jsonb THEN
                RETURN NEW;
            END IF;
            RAISE EXCEPTION 'LOB_PRODUCT_MISMATCH' USING ERRCODE = 'P0001';
        END IF;

        IF product_lob IS DISTINCT FROM NEW."LineOfBusiness" THEN
            RAISE EXCEPTION 'LOB_PRODUCT_MISMATCH' USING ERRCODE = 'P0001';
        END IF;

        RETURN NEW;
    END;
    $$;

    CREATE TRIGGER trg_lob_carrier_consistency
    BEFORE INSERT OR UPDATE OF "LineOfBusiness", "LobProductVersionId", "LobAttributesJson" ON "Submissions"
    FOR EACH ROW EXECUTE FUNCTION nebula_lob_carrier_consistency();

    CREATE TRIGGER trg_lob_carrier_consistency
    BEFORE INSERT OR UPDATE OF "LineOfBusiness", "LobProductVersionId", "LobAttributesJson" ON "Renewals"
    FOR EACH ROW EXECUTE FUNCTION nebula_lob_carrier_consistency();

    CREATE TRIGGER trg_lob_carrier_consistency
    BEFORE INSERT OR UPDATE OF "LineOfBusiness", "LobProductVersionId", "LobAttributesJson" ON "PolicyVersions"
    FOR EACH ROW EXECUTE FUNCTION nebula_lob_carrier_consistency();

    CREATE TRIGGER trg_lob_carrier_consistency
    BEFORE INSERT OR UPDATE OF "LineOfBusiness", "LobProductVersionId", "LobAttributesJson" ON "PolicyEndorsements"
    FOR EACH ROW EXECUTE FUNCTION nebula_lob_carrier_consistency();
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260507030000_F0034_ProductSchemaRegistryAndLobAttributes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260507030000_F0034_ProductSchemaRegistryAndLobAttributes', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603220000_F0019_SubmissionQuotingApproval') THEN
    ALTER TABLE "Submissions" ADD "ArchivedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603220000_F0019_SubmissionQuotingApproval') THEN
    ALTER TABLE "Submissions" ADD "ArchivedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603220000_F0019_SubmissionQuotingApproval') THEN
    ALTER TABLE "Submissions" ADD "IsArchived" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603220000_F0019_SubmissionQuotingApproval') THEN
    CREATE TABLE "SubmissionQuotePackets" (
        "Id" uuid NOT NULL,
        "SubmissionId" uuid NOT NULL,
        "Status" character varying(30) NOT NULL DEFAULT 'Draft',
        "LinkedDocumentRefsJson" jsonb NOT NULL DEFAULT ('[]'::jsonb),
        "RecordedPremiumAmount" numeric(18,2),
        "RecordedLimits" character varying(500),
        "RecordedDeductibles" character varying(500),
        "EffectiveDate" date,
        "CarrierMarket" character varying(200),
        "ReadinessState" character varying(40) NOT NULL DEFAULT 'Draft',
        "ReadyAt" timestamp with time zone,
        "ReadyByUserId" uuid,
        "ApprovedAt" timestamp with time zone,
        "ApprovedByUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        CONSTRAINT "PK_SubmissionQuotePackets" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SubmissionQuotePackets_Submissions_SubmissionId" FOREIGN KEY ("SubmissionId") REFERENCES "Submissions" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603220000_F0019_SubmissionQuotingApproval') THEN
    CREATE TABLE "SubmissionApprovalDecisions" (
        "Id" uuid NOT NULL,
        "SubmissionId" uuid NOT NULL,
        "Decision" character varying(30) NOT NULL,
        "ApproverUserId" uuid NOT NULL,
        "Reason" character varying(1000) NOT NULL,
        "AuthorityContextJson" jsonb NOT NULL DEFAULT ('{}'::jsonb),
        "DecidedAt" timestamp with time zone NOT NULL,
        "BlockingConditionsJson" jsonb NOT NULL DEFAULT ('[]'::jsonb),
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        CONSTRAINT "PK_SubmissionApprovalDecisions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SubmissionApprovalDecisions_Submissions_SubmissionId" FOREIGN KEY ("SubmissionId") REFERENCES "Submissions" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603220000_F0019_SubmissionQuotingApproval') THEN
    CREATE TABLE "SubmissionBindHandoffs" (
        "Id" uuid NOT NULL,
        "SubmissionId" uuid NOT NULL,
        "IdempotencyKey" character varying(120) NOT NULL,
        "Status" character varying(30) NOT NULL DEFAULT 'Pending',
        "CorrelationId" uuid NOT NULL,
        "PayloadSnapshotJson" jsonb NOT NULL DEFAULT ('{}'::jsonb),
        "RequestedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        CONSTRAINT "PK_SubmissionBindHandoffs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SubmissionBindHandoffs_Submissions_SubmissionId" FOREIGN KEY ("SubmissionId") REFERENCES "Submissions" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603220000_F0019_SubmissionQuotingApproval') THEN
    CREATE INDEX "IX_Submissions_IsArchived" ON "Submissions" ("IsArchived");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603220000_F0019_SubmissionQuotingApproval') THEN
    CREATE UNIQUE INDEX "IX_SubmissionQuotePackets_SubmissionId" ON "SubmissionQuotePackets" ("SubmissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603220000_F0019_SubmissionQuotingApproval') THEN
    CREATE INDEX "IX_SubmissionApprovalDecisions_SubmissionId_DecidedAt" ON "SubmissionApprovalDecisions" ("SubmissionId", "DecidedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603220000_F0019_SubmissionQuotingApproval') THEN
    CREATE UNIQUE INDEX "IX_SubmissionBindHandoffs_SubmissionId_IdempotencyKey" ON "SubmissionBindHandoffs" ("SubmissionId", "IdempotencyKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603220000_F0019_SubmissionQuotingApproval') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260603220000_F0019_SubmissionQuotingApproval', '10.0.5');
    END IF;
END $EF$;
COMMIT;

