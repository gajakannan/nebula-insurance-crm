START TRANSACTION;
ALTER TABLE "Submissions" ADD "ArchivedAt" timestamp with time zone;

ALTER TABLE "Submissions" ADD "ArchivedByUserId" uuid;

ALTER TABLE "Submissions" ADD "IsArchived" boolean NOT NULL DEFAULT FALSE;

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

CREATE INDEX "IX_Submissions_IsArchived" ON "Submissions" ("IsArchived");

CREATE UNIQUE INDEX "IX_SubmissionQuotePackets_SubmissionId" ON "SubmissionQuotePackets" ("SubmissionId");

CREATE INDEX "IX_SubmissionApprovalDecisions_SubmissionId_DecidedAt" ON "SubmissionApprovalDecisions" ("SubmissionId", "DecidedAt");

CREATE UNIQUE INDEX "IX_SubmissionBindHandoffs_SubmissionId_IdempotencyKey" ON "SubmissionBindHandoffs" ("SubmissionId", "IdempotencyKey");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260603220000_F0019_SubmissionQuotingApproval', '10.0.5');

COMMIT;

