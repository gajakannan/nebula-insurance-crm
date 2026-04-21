# CRM Competitive Analysis & Inspiration

This document provides a baseline understanding of features and patterns in established CRM systems, particularly those used in commercial insurance. Use this as inspiration and competitive context when defining product requirements.

---

## Purpose

When building an insurance CRM, it's critical to understand:
- **Table-stakes features**: What users expect from any CRM
- **Industry-specific patterns**: How insurance CRMs differ from general B2B CRMs
- **UI/UX conventions**: Patterns users already know from existing systems
- **Terminology**: How competitors name similar concepts

---

## General CRM Systems

### Salesforce (Sales Cloud)

**Core Features:**
- Account & Contact Management (360-degree view)
- Opportunity/Pipeline Management
- Activity Timeline (emails, calls, meetings, tasks)
- Reports & Dashboards
- Task & Reminder Center
- Email Integration
- Mobile App

**Key Patterns:**
- **Related Lists**: Associated records shown as embedded tables (e.g., Contacts under Account)
- **Chatter Feed**: Activity stream for collaboration
- **Record Details**: Tab-based layout (Details, Related, Activity)
- **Global Search**: Unified search across all objects
- **Quick Actions**: Contextual buttons for common operations

**Insurance-Specific Salesforce Editions:**
- **Financial Services Cloud**: Relationship-based views, household hierarchy
- **Insurance-specific objects**: Policies, Claims, Submissions (via AppExchange)

**Terminology:**
- Account = Company/Broker
- Contact = Person
- Opportunity = Deal/Submission
- Lead = Prospect
- Activity = Task/Event

---

### Microsoft Dynamics 365 (Sales)

**Core Features:**
- Account/Contact Management
- Opportunity Management with Sales Stages
- Business Process Flows (visual workflow guides)
- Relationship Analytics
- LinkedIn Integration
- Power BI Dashboards

**Key Patterns:**
- **Business Process Flow**: Step-by-step progress bar at top of record
- **Relationship Assistant**: AI-driven suggestions and reminders
- **Timeline Control**: Unified activity feed
- **Forms & Views**: Highly customizable layouts
- **Hierarchical Relationships**: Parent/child account structures

**Insurance Extensions:**
- **Dynamics 365 for Insurance**: Policy lifecycle, claims, underwriting workflows

**Terminology:**
- Account = Company/Organization
- Contact = Individual
- Opportunity = Potential Sale
- Case = Service Request
- Activity = Phone Call/Appointment/Email

---

### HubSpot CRM

**Core Features:**
- Contact & Company Management
- Deal Pipeline (visual Kanban board)
- Email Tracking & Templates
- Meeting Scheduler
- Task Automation
- Reporting Dashboards

**Key Patterns:**
- **Pipeline Board**: Visual drag-and-drop deal stages
- **Contact Timeline**: All interactions in chronological feed
- **Properties**: Flexible custom fields
- **Workflows**: Automated task creation and notifications
- **Associations**: Many-to-many relationships between objects

**Strengths for SMB:**
- Simple, intuitive UI
- Fast setup
- Email-first design

**Terminology:**
- Company = Business/Broker
- Contact = Person
- Deal = Opportunity/Sale
- Ticket = Support Request
- Activity = Email/Call/Meeting/Note

---

## Insurance-Specific CRM Systems

### Applied Epic

**Target Users:** Insurance agencies and brokers

**Core Features:**
- Client Management (individuals, businesses)
- Policy Management (multi-policy per client)
- Submission Tracking
- Renewal Pipeline
- Commission Tracking
- Document Management
- Certificate of Insurance (COI) generation

**Key Patterns:**
- **Client 360**: All policies, submissions, renewals in one view
- **Policy-Centric**: Policy is the primary object (not just opportunity)
- **Renewal Workflows**: Automated renewal reminders and workflows
- **Carrier Integration**: Direct integration with carrier systems

**Insurance Domain Features:**
- Lines of Business (Commercial, Personal, Benefits)
- Policy types (GL, WC, Property, Auto, etc.)
- Producer/Agent assignment
- Carrier relationships

**Terminology:**
- Client = Insured/Policyholder
- Producer = Agent/Broker
- Submission = Quote Request
- Binder = Temporary Policy
- Renewal = Policy Renewal

---

### Vertafore AMS360 / Sagitta

**Target Users:** Independent insurance agencies

**Core Features:**
- Customer/Prospect Management
- Policy Administration
- Suspense/Task Management (follow-up system)
- Commission Processing
- Accounting Integration
- ACORD Forms integration

**Key Patterns:**
- **Suspense System**: Task/reminder system with due dates and escalation
- **Policy-First Design**: Navigation organized by policy type
- **ACORD Standards**: Uses ACORD XML for carrier communication
- **Activity Codes**: Standardized codes for tracking interactions

**Strengths:**
- Deep accounting integration
- Robust commission tracking
- Mature suspense/workflow system

**Terminology:**
- Customer = Client/Insured
- Policy = Insurance Contract
- Suspense = Task/Reminder
- Activity = Customer Interaction
- Producer = Agent

---

### Duck Creek (Distribution Management)

**Target Users:** Insurance carriers (underwriters, distribution teams)

**Core Features:**
- Agent/Broker Portal Management
- Submission Intake & Routing
- Underwriting Workflow
- Quote Generation
- Appointment Management (carrier-agent relationships)
- Commission Management

**Key Patterns:**
- **Workflow Engine**: State-based workflows with validation gates
- **Role-Based Views**: Different screens for underwriters vs. distribution users
- **Document Generation**: Automated quote and policy document creation
- **Hierarchy Management**: Managing MGA/broker/sub-broker relationships

**Carrier-Specific Features:**
- Underwriting rules engine
- Rating integration
- Policy binding workflows
- Reinsurance tracking

**Terminology:**
- Producer = Broker/MGA
- Submission = New Business Request
- Quote = Indication/Proposal
- Bind = Issue Policy
- Appointment = Carrier-Agent Authorization

---

## Common CRM Features (Table-Stakes)

Regardless of industry or vendor, users expect these baseline features:

### 1. **Account/Company Management**
- Hierarchical relationships (parent/child companies)
- Contact list (people at the company)
- Activity timeline (all interactions)
- Related records (deals, policies, submissions)
- Custom fields

### 2. **Contact Management**
- Personal details (name, email, phone, title)
- Association to multiple companies (many-to-many)
- Communication history
- Notes and attachments

### 3. **Activity Timeline**
- Chronological feed of all interactions
- Emails, calls, meetings, notes, tasks
- System-generated events (record created, updated)
- Filterable by activity type

### 4. **Task & Reminder Center**
- Create tasks with due dates
- Assign tasks to users
- Task status (open, completed, overdue)
- Email/dashboard reminders
- Task queue view (my tasks, team tasks)

### 5. **Search & Filter**
- Global search across all entities
- Advanced filters (by status, date range, assigned user)
- Saved searches/views
- Recent items

### 6. **Reports & Dashboards**
- Summary metrics (e.g., open submissions, renewals due)
- Charts and graphs
- Exportable data
- Scheduled email reports

### 7. **Permissions & Security**
- Role-based access control
- Record-level permissions (owner, team, read-only)
- Field-level security (hide sensitive data)
- Audit logs

### 8. **Mobile Access**
- Responsive web app or native mobile app
- Offline access to key records
- Mobile-optimized views

---

## Insurance CRM-Specific Features

Beyond general CRM features, insurance CRMs typically include:

### 1. **Broker/MGA Hierarchy**
- Parent MGA → Sub-broker relationships
- Hierarchical commission splits
- Territorial assignments

### 2. **Submission Workflow**
- Intake → Triage → Underwriter Assignment → Quote → Bind
- Status tracking with validation gates
- Document upload (ACORD forms, loss runs)

### 3. **Renewal Pipeline**
- Automated renewal reminders (60/90/120 days out)
- Renewal status tracking (renewed, non-renewed, declined)
- Premium comparison (expiring vs. renewal)

### 4. **Policy Tracking**
- Policy number, effective/expiration dates
- Lines of business (GL, WC, Property, etc.)
- Premium amounts
- Carrier information

### 5. **Producer/Agent Assignment**
- Assign brokers to accounts
- Track producer code for commission purposes
- Producer performance metrics

### 6. **Document Management**
- ACORD forms (125, 126, 130, 140)
- Certificate of Insurance (COI) generation
- Policy documents, endorsements
- Loss runs, financial statements

### 7. **Commission Tracking**
- Commission rates by carrier/product
- Commission receivables
- Producer splits

---

## UI/UX Patterns to Consider

### 1. **360-Degree View**
All CRMs use a "single pane of glass" approach:
- Header: Record name, key fields (status, owner, dates)
- Tabs: Details, Related Records, Activity, Notes
- Related Lists: Embedded tables of associated records
- Quick Actions: Buttons for common operations

**Example (Broker 360):**
```
┌─────────────────────────────────────────────────────┐
│ Broker: ABC Insurance Group         [Edit] [Delete] │
│ Status: Active  |  Owner: Sarah J.  |  License: XYZ │
├─────────────────────────────────────────────────────┤
│ [Details] [Accounts] [Submissions] [Activity]       │
├─────────────────────────────────────────────────────┤
│ Contact Information                                 │
│ - Email: abc@example.com                            │
│ - Phone: 555-1234                                   │
│                                                     │
│ Related Accounts (5)           [View All] [Add New] │
│ ┌─────────────────────────────────────────────────┐│
│ │ Account Name       | Status  | Premium YTD     ││
│ │ Acme Corp          | Active  | $125,000        ││
│ │ XYZ Manufacturing  | Pending | $50,000         ││
│ └─────────────────────────────────────────────────┘│
│                                                     │
│ Activity Timeline                                   │
│ ○ Today - Email sent: Renewal reminder             │
│ ○ Yesterday - Task completed: Follow up call       │
│ ○ 2 days ago - Submission created: Acme GL quote   │
└─────────────────────────────────────────────────────┘
```

### 2. **Pipeline/Kanban Board**
HubSpot and many modern CRMs use visual pipelines:
- Columns = Stages (Intake, Triage, Quoted, Bound)
- Cards = Submissions/Opportunities
- Drag-and-drop to change stage
- Summary metrics per stage

### 3. **Business Process Flow**
Dynamics 365 uses a visual progress bar:
```
[Intake] → [Triage] → [Underwriting] → [Quoted] → [Bound]
   ✓          ✓           ●              ○           ○
```

### 4. **Quick Create Forms**
Modal dialogs for fast data entry without leaving current page:
- "Quick Create Contact"
- "Quick Log Activity"
- "Quick Add Note"

### 5. **Inline Editing**
Click-to-edit fields directly in the detail view (no separate edit mode)

---

## Feature Prioritization for MVP

Based on competitive analysis, here's a suggested MVP feature priority:

### Must-Have (Table-Stakes)
1. ✅ Broker/MGA CRUD + 360 view
2. ✅ Account (Insured) CRUD + 360 view
3. ✅ Contact management
4. ✅ Activity timeline (system events)
5. ✅ Task center (create, assign, complete)
6. ✅ Basic search and filtering
7. ✅ User permissions (RBAC)

### Should-Have (Competitive Parity)
8. Submission intake workflow
9. Renewal pipeline
10. Email logging/tracking
11. Basic reporting (submission count, renewals due)
12. Document upload/storage
13. Broker hierarchy

### Nice-to-Have (Differentiators)
14. Mobile app
15. Email integration (send from CRM)
16. Advanced analytics/dashboards
17. Automated workflows/reminders
18. ACORD form generation
19. Carrier integrations

---

## Key Takeaways for Product Managers

1. **Don't Reinvent Core CRM Patterns**: Users expect standard layouts (360 view, timeline, related lists). Innovate on insurance-specific workflows, not basic CRM UX.

2. **Insurance-Specific is Key**: The differentiator is NOT "another CRM" but "CRM built for insurance workflows" (submissions, renewals, broker hierarchies, policies).

3. **Progressive Disclosure**: Start with simple CRUD and 360 views. Add advanced features (workflows, analytics) in later phases.

4. **Use Industry Terminology**: Don't call it "Opportunity" if the industry calls it "Submission." Use insurance domain language.

5. **Audit Trail is Non-Negotiable**: Insurance is a regulated industry. Every mutation must be logged (who, what, when).

6. **Mobile Access is Expected**: Even if MVP is web-only, design with mobile in mind. Sales/distribution users work in the field.

---

## Reference Links

### General CRM Resources
- [Salesforce CRM Overview](https://www.salesforce.com/products/sales-cloud/overview/)
- [Microsoft Dynamics 365 Sales](https://dynamics.microsoft.com/en-us/sales/overview/)
- [HubSpot CRM Features](https://www.hubspot.com/products/crm)

### Insurance CRM Resources
- [Applied Epic Overview](https://www.appliedsystems.com/en-us/solutions/applied-epic/)
- [Vertafore AMS360](https://www.vertafore.com/products/ams360)
- [Duck Creek Distribution Management](https://www.duckcreek.com/products/distribution-management)

### Industry Standards
- [ACORD Standards](https://www.acord.org/standards-architecture/acord-forms) - Insurance data exchange formats
- [Insurance Glossary](https://www.iii.org/insuranceindustryblog/glossary/) - Industry terminology

---

## When to Use This Document

- **Before Phase A**: Understand competitive landscape and baseline features
- **During Story Writing**: Reference UI patterns and terminology
- **During Prioritization**: Distinguish table-stakes from differentiators
- **During Acceptance Criteria**: Ensure feature parity with industry standards
- **When Clarifying Terms**: Use consistent insurance domain language

---

**Last Updated:** 2026-01-31
