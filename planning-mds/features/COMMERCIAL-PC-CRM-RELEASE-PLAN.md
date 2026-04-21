# Commercial P&C CRM Release Plan

**Status:** Proposed
**Last Updated:** 2026-03-21
**Owner:** Product + Architecture

## Purpose

Define the proposed release boundary for Nebula as a marketable Commercial P&C CRM, extend the feature inventory beyond the currently reserved IDs, and provide a practical implementation sequence.

This plan is grounded in:
- Nebula's current blueprint, roadmap, and domain docs
- Insurance CRM patterns captured in local planning docs
- External product patterns from Salesforce, Vertafore, and Microsoft

## Current State

Completed foundation and core surfaces already cover:
- dashboard
- broker and contact management
- task API MVP
- authentication and authorization
- frontend quality gates

Still-active or already-reserved business features:
- F0004 - Task Center UI + Manager Assignment
- F0006 - Submission Intake Workflow
- F0007 - Renewal Pipeline
- F0008 - Broker Insights

## Release Framing

Nebula should be framed in three product waves:

### 1. CRM Release MVP

This is the minimum credible release boundary to call Nebula a Commercial P&C CRM.

Includes:
- F0004 - Task Center UI + Manager Assignment
- F0006 - Submission Intake Workflow
- F0016 - Account 360 & Insured Management
- F0020 - Document Management & ACORD Intake
- F0018 - Policy Lifecycle & Policy 360
- F0019 - Submission Quoting, Proposal & Approval Workflow
- F0007 - Renewal Pipeline
- F0021 - Communication Hub & Activity Capture
- F0022 - Work Queues, Assignment Rules & Coverage Management
- F0023 - Global Search, Saved Views & Operational Reporting
- F0031 - Data Import, Deduplication & Go-Live Migration

### 2. CRM Release MVP+

These features materially improve competitiveness and insurance fit, but are not all required for the first marketable CRM release.

Includes:
- F0008 - Broker Insights
- F0017 - Broker/MGA Hierarchy, Producer Ownership & Territory Management
- F0024 - Claims & Service Case Tracking
- F0027 - COI, ACORD & Outbound Document Generation
- F0028 - Carrier & Market Relationship Management

### 3. Brokerage Platform Expansion

These features extend Nebula from CRM into broader agency management / brokerage operations.

Includes:
- F0025 - Commission, Producer Splits & Revenue Tracking
- F0026 - Billing, Invoicing & Reconciliation
- F0029 - External Broker Collaboration Portal
- F0030 - Integration Hub & Data Exchange
- F0032 - Admin Configuration & Reference Data Console

## Proposed Now / Next / Later

### Now

- F0004 - Task Center UI + Manager Assignment
- F0014 - DevOps Smoke Test Automation
- F0006 - Submission Intake Workflow
- F0016 - Account 360 & Insured Management
- F0020 - Document Management & ACORD Intake

### Next

- F0018 - Policy Lifecycle & Policy 360
- F0019 - Submission Quoting, Proposal & Approval Workflow
- F0007 - Renewal Pipeline
- F0021 - Communication Hub & Activity Capture
- F0022 - Work Queues, Assignment Rules & Coverage Management
- F0023 - Global Search, Saved Views & Operational Reporting
- F0031 - Data Import, Deduplication & Go-Live Migration

### Later

- F0008 - Broker Insights
- F0017 - Broker/MGA Hierarchy, Producer Ownership & Territory Management
- F0024 - Claims & Service Case Tracking
- F0025 - Commission, Producer Splits & Revenue Tracking
- F0026 - Billing, Invoicing & Reconciliation
- F0027 - COI, ACORD & Outbound Document Generation
- F0028 - Carrier & Market Relationship Management
- F0029 - External Broker Collaboration Portal
- F0030 - Integration Hub & Data Exchange
- F0032 - Admin Configuration & Reference Data Console

## Feature Catalog

### F0016 - Account 360 & Insured Management

Purpose:
Give underwriters and distribution users a full insured-centered record with profile, contacts, submissions, renewals, policies, activity timeline, and key metrics.

Why it matters:
Current personas repeatedly require account context, related history, and a single place to understand the insured relationship.

Dependencies:
- F0002 broker management
- F0009 authentication

Suggested release:
- CRM Release MVP

### F0017 - Broker/MGA Hierarchy, Producer Ownership & Territory Management

Purpose:
Model MGA, sub-broker, producer, and territory relationships with ownership, visibility, and assignment rules.

Why it matters:
Important for regional/channel governance, producer accountability, and advanced broker performance workflows.

Dependencies:
- F0002 broker management
- F0023 reporting

Suggested release:
- CRM Release MVP+

### F0018 - Policy Lifecycle & Policy 360

Purpose:
Introduce policy records, terms, lines of business, carrier, premium, effective and expiration dates, versions, endorsements, cancellation events, and policy timeline.

Why it matters:
Commercial P&C CRM credibility requires policy-level truth, not only broker and submission records.

Dependencies:
- F0016 account management
- F0020 document management

Suggested release:
- CRM Release MVP

### F0019 - Submission Quoting, Proposal & Approval Workflow

Purpose:
Extend submission handling from intake into triage, underwriting assignment, quote preparation, approvals, proposal issuance, bind request, bind, decline, and withdrawal.

Why it matters:
Intake without quote/proposal workflow leaves the core underwriting lifecycle incomplete.

Dependencies:
- F0006 submission intake
- F0020 document management

Suggested release:
- CRM Release MVP

### F0020 - Document Management & ACORD Intake

Purpose:
Support upload, metadata, versioning, retrieval, and audit of ACORD forms, loss runs, financials, quotes, policy documents, and endorsements.

Why it matters:
Document completeness is a daily underwriting pain point and a prerequisite for structured intake and quoting.

Dependencies:
- F0016 account management
- F0006 submission intake

Suggested release:
- CRM Release MVP

### F0021 - Communication Hub & Activity Capture

Purpose:
Capture notes, calls, meetings, emails, follow-ups, and communication history as part of the relationship and submission record.

Why it matters:
Current personas explicitly suffer from email overload and missing communication audit trails.

Dependencies:
- F0004 task center UI
- F0016 account management

Suggested release:
- CRM Release MVP

### F0022 - Work Queues, Assignment Rules & Coverage Management

Purpose:
Add operational queues, routing rules, workload balancing, reassignment, backup coverage, and out-of-office continuity for submissions, renewals, and tasks.

Why it matters:
Modern insurance operations need more than personal task lists. They need managed work distribution and backup coverage.

Dependencies:
- F0004 task center UI
- F0006 submission intake
- F0007 renewal pipeline

Suggested release:
- CRM Release MVP

### F0023 - Global Search, Saved Views & Operational Reporting

Purpose:
Provide cross-object search, saved filters/views, workload reports, submission aging, renewals due, and daily operational KPI reporting.

Why it matters:
This is table-stakes CRM usability and required to eliminate spreadsheet-driven reporting.

Dependencies:
- F0016 account management
- F0018 policy lifecycle
- F0019 submission workflow

Suggested release:
- CRM Release MVP

### F0024 - Claims & Service Case Tracking

Purpose:
Track claims notices, service requests, claim status context, servicing tasks, and post-bind customer support history.

Why it matters:
Strengthens account and policy context and helps Nebula support the full customer relationship after binding.

Dependencies:
- F0018 policy lifecycle
- F0021 communication hub

Suggested release:
- CRM Release MVP+

### F0025 - Commission, Producer Splits & Revenue Tracking

Purpose:
Model commission plans, producer splits, expected revenue, receivables, and production attribution.

Why it matters:
Critical for brokerage economics and producer compensation, but beyond strict CRM scope.

Dependencies:
- F0017 producer ownership
- F0018 policy lifecycle
- F0028 carrier relationships

Suggested release:
- Brokerage Platform Expansion

### F0026 - Billing, Invoicing & Reconciliation

Purpose:
Support invoicing, billing events, payment tracking, reconciliation, and finance-facing policy transactions.

Why it matters:
Important for agencies and brokerages, but it pushes Nebula beyond CRM into AMS/accounting territory.

Dependencies:
- F0018 policy lifecycle
- F0025 commission tracking

Suggested release:
- Brokerage Platform Expansion

### F0027 - COI, ACORD & Outbound Document Generation

Purpose:
Generate COIs, outbound forms, proposals, and policy-facing documents from structured data and templates.

Why it matters:
Highly recognizable insurance-specific parity feature and valuable once documents and policies are modeled.

Dependencies:
- F0018 policy lifecycle
- F0020 document management

Suggested release:
- CRM Release MVP+

### F0028 - Carrier & Market Relationship Management

Purpose:
Track carriers, underwriter contacts, appetite notes, appointments, market access, and relationship activity.

Why it matters:
Commercial P&C distribution teams manage both broker relationships and market relationships.

Dependencies:
- F0019 submission workflow
- F0023 reporting

Suggested release:
- CRM Release MVP+

### F0029 - External Broker Collaboration Portal

Purpose:
Allow brokers to submit, track, and collaborate on business through a secure external surface.

Why it matters:
High strategic value, but explicitly outside Nebula's current MVP boundary.

Dependencies:
- F0009 broker user access boundaries
- F0030 integration hub

Suggested release:
- Brokerage Platform Expansion

### F0030 - Integration Hub & Data Exchange

Purpose:
Provide structured integration points for email, document storage, accounting, carrier data exchange, and future external systems.

Why it matters:
Necessary for scale and ecosystem connectivity, but not required to prove the core CRM.

Dependencies:
- F0020 document management
- F0021 communication hub
- F0026 billing

Suggested release:
- Brokerage Platform Expansion

### F0031 - Data Import, Deduplication & Go-Live Migration

Purpose:
Support initial customer migration, CSV import, duplicate detection, data quality review, and cutover readiness.

Why it matters:
A CRM that cannot ingest customer data cleanly is difficult to adopt in production.

Dependencies:
- F0016 account management
- F0002 broker management

Suggested release:
- CRM Release MVP

### F0032 - Admin Configuration & Reference Data Console

Purpose:
Expose admin management for statuses, queues, assignment rules, templates, lines of business, territories, and other configurable reference data.

Why it matters:
Important as Nebula becomes more configurable, but can follow an earlier release that uses seed data and constrained admin paths.

Dependencies:
- F0022 assignment rules
- F0023 reporting

Suggested release:
- Brokerage Platform Expansion

## Suggested Implementation Order

Primary sequence:

1. F0014 - DevOps Smoke Test Automation
2. F0004 - Task Center UI + Manager Assignment
3. F0006 - Submission Intake Workflow
4. F0016 - Account 360 & Insured Management
5. F0020 - Document Management & ACORD Intake
6. F0018 - Policy Lifecycle & Policy 360
7. F0019 - Submission Quoting, Proposal & Approval Workflow
8. F0007 - Renewal Pipeline
9. F0021 - Communication Hub & Activity Capture
10. F0022 - Work Queues, Assignment Rules & Coverage Management
11. F0023 - Global Search, Saved Views & Operational Reporting
12. F0031 - Data Import, Deduplication & Go-Live Migration
13. F0008 - Broker Insights
14. F0017 - Broker/MGA Hierarchy, Producer Ownership & Territory Management
15. F0028 - Carrier & Market Relationship Management
16. F0027 - COI, ACORD & Outbound Document Generation
17. F0024 - Claims & Service Case Tracking
18. F0032 - Admin Configuration & Reference Data Console
19. F0030 - Integration Hub & Data Exchange
20. F0025 - Commission, Producer Splits & Revenue Tracking
21. F0026 - Billing, Invoicing & Reconciliation
22. F0029 - External Broker Collaboration Portal

Parallelizable workstreams:
- F0031 can start during mid-MVP once account and broker models are stable.
- F0032 can begin once queues, templates, and reference data are defined.
- F0028 can begin in parallel with later submission and reporting hardening.

## Positioning Guidance

Nebula can credibly position itself as a Commercial P&C CRM once the CRM Release MVP wave is complete.

Nebula should not position itself as a full brokerage management system or agency management system until the Brokerage Platform Expansion wave is meaningfully delivered.

## External Product Grounding

This plan was informed by the following external sources and interpreted through Nebula's local constraints:
- Salesforce Insurance Brokerage Management
- Salesforce Financial Services Cloud for Insurance Brokerages
- Vertafore WorkSmart
- Vertafore AMS360 Renewal List Tool
- Vertafore AMS360 Out of Office Assistant
- Vertafore AMS360 Re-Assign Expiring Policies
- Microsoft Dynamics 365 activity Kanban and activity list patterns
- Microsoft assignment rules, queue priority, and least-privilege record sharing guidance
