# Persona Examples

Use these examples when creating user personas for the insurance CRM. Personas should be stored in `planning-mds/BLUEPRINT.md` Section 3.2 or as separate files if needed.

**Best Practice:** Create 3-5 personas representing primary and secondary users of the system.

---

## Persona 1: Sarah Chen - Distribution & Marketing Manager

**Priority:** Primary

**Demographics:**
- Age: 35
- Experience: 8 years in commercial P&C insurance
- Location: Regional office, Charlotte, NC
- Education: BA in Business Administration, Minor in Risk Management
- Technical Proficiency: High (uses Salesforce daily, Excel power user)

**Background & Current Role:**
- Manages broker and MGA relationships for Southeast region
- Responsible for $25M annual premium across 50+ broker relationships
- Reports to VP of Distribution
- Team: 2 direct reports (broker relationship coordinators)
- Covers 6 states (NC, SC, GA, FL, TN, VA)

**Daily Responsibilities:**
- Review and triage 15-20 new submissions daily
- Conduct weekly broker performance reviews
- Follow up on pending quotes (20-30 active)
- Schedule quarterly broker business review meetings
- Manage renewal pipeline (60-90 day advance notice)
- Coordinate with underwriting team on complex submissions
- Track broker production metrics and commission payments
- Respond to broker inquiries and relationship issues

**Goals & Motivations:**
1. Increase quote-to-bind ratio from 15% to 20% within 12 months
2. Grow regional premium by 15% year-over-year
3. Reduce submission-to-quote turnaround time from 5 days to 3 days
4. Identify and develop relationships with 10 new high-quality brokers
5. Improve broker satisfaction scores from 7.5 to 8.5 (out of 10)
6. Reduce time spent on administrative tasks (email, Excel) by 30%
7. Provide faster, more informed responses to broker inquiries

**Pain Points & Frustrations:**
1. **Submission chaos** (Impact: High, Frequency: Daily)
   - Submissions arrive via email, phone, portal - no single source of truth
   - Lose track of submissions or duplicate work
   - Can't easily see submission status or what underwriter is working on it

2. **No broker performance visibility** (Impact: High, Frequency: Weekly)
   - Have to manually compile broker metrics from multiple sources
   - Don't know which brokers are sending quality business vs. junk
   - Can't identify top performers or underperformers quickly

3. **Renewal pipeline management** (Impact: High, Frequency: Daily)
   - Renewals fall through the cracks - no automated reminders
   - Hard to see what's expiring in next 60-90 days
   - Miss opportunities to proactively reach out to brokers

4. **Fragmented data** (Impact: High, Frequency: Daily)
   - Broker data in one system, submissions in another, policy info elsewhere
   - Have to check 3-4 systems to get full picture of broker relationship
   - Waste 2-3 hours daily searching for information

5. **Email overload** (Impact: Medium, Frequency: Daily)
   - Critical updates buried in email threads
   - No audit trail of broker communications
   - Miss follow-up tasks because they're in email

6. **Manual reporting** (Impact: Medium, Frequency: Weekly)
   - Spend 4-5 hours weekly building reports in Excel
   - Data is stale by the time report is done
   - Hard to spot trends or issues

**Jobs-to-be-Done:**

1. **When I receive a new submission from a broker...**
   - I need to quickly triage it (priority, line of business, complexity)
   - So I can assign it to the right underwriter within 2 hours
   - And ensure the broker gets a fast response

2. **When preparing for a quarterly broker review meeting...**
   - I need to see 6 months of broker activity (submissions, quotes, binds, premium)
   - So I can have a data-driven conversation about performance
   - And identify opportunities to grow the relationship

3. **When a renewal is approaching (60 days out)...**
   - I need to be automatically notified with renewal details
   - So I can proactively reach out to the broker with renewal strategy
   - And prevent non-renewals due to lack of communication

4. **When a broker calls asking about a submission...**
   - I need to instantly see the submission status, assigned underwriter, and recent activity
   - So I can give them an informed answer in 30 seconds
   - And not have to say "let me check and get back to you"

5. **When identifying which brokers to focus development efforts on...**
   - I need to see broker performance trends (submission quality score, quote rate, bind rate, premium growth)
   - So I can prioritize my time on high-potential relationships
   - And avoid wasting time on brokers who send poor-quality business

**Technology Comfort:**
- Advanced: Salesforce, Excel (pivot tables, VLOOKUP), Outlook
- Intermediate: Power BI, basic SQL queries
- Willing to learn new systems if they save time

**Success Metrics for This Persona:**
- Reduced time to triage submissions
- Improved broker satisfaction scores
- Increased visibility into broker performance
- Automated renewal reminders
- Single source of truth for broker relationships

---

## Persona 2: Marcus Rodriguez - Senior Underwriter

**Priority:** Primary

**Demographics:**
- Age: 42
- Experience: 15 years in commercial property & casualty underwriting
- Location: Home office, Hartford, CT
- Education: CPCU, AU designations; BA in Finance
- Technical Proficiency: Medium (prefers desktop apps, comfortable with modern web apps)

**Background & Current Role:**
- Senior Underwriter specializing in Construction and Real Estate
- Handles complex accounts ($500K-$5M premium)
- Reports to Chief Underwriting Officer
- Team: Works independently, consults with 2 other senior UWs
- Portfolio: ~120 active accounts, 40-50 new submissions per quarter

**Daily Responsibilities:**
- Review and underwrite 3-5 new submissions daily
- Request additional information from brokers (ACORD forms, loss runs, financials)
- Prepare quotes and proposals
- Negotiate terms and pricing with brokers
- Bind new and renewal policies
- Conduct risk assessments and site visits (2-3 per month)
- Review and approve endorsements
- Manage renewal book (quarterly focus)

**Goals & Motivations:**
1. Maintain combined ratio below 95% (currently 92%)
2. Hit $18M written premium target for the year
3. Reduce quote turnaround time from 7 days to 5 days
4. Improve quote-to-bind ratio from 20% to 25%
5. Minimize time on administrative tasks (data entry, status updates)
6. Make better-informed risk decisions with complete account context
7. Build stronger relationships with top-producing brokers

**Pain Points & Frustrations:**
1. **Incomplete submissions** (Impact: High, Frequency: Daily)
   - 60% of submissions missing key documents (loss runs, financials, ACORD forms)
   - Spend 30 minutes per submission chasing missing information
   - Delays quote process by 2-3 days

2. **No account context** (Impact: High, Frequency: Daily)
   - When reviewing a submission, can't see related policies, prior quotes, or claims history
   - Have to search multiple systems for account history
   - Miss cross-sell opportunities because data is siloed

3. **Manual data entry** (Impact: Medium, Frequency: Daily)
   - Re-enter same information from submission into quoting system
   - Prone to errors and inconsistencies
   - Waste 1-2 hours daily on data entry

4. **Communication gaps with brokers** (Impact: Medium, Frequency: Daily)
   - Email back-and-forth for missing info or questions
   - No visibility into whether broker received quote or has questions
   - Hard to track follow-ups

5. **Renewal surprise** (Impact: High, Frequency: Quarterly)
   - Renewals pop up with 30 days notice - not enough time for quality underwriting
   - Don't have visibility into upcoming renewals 60-90 days out
   - Rush to get renewal quotes out on time

6. **Approval bottlenecks** (Impact: Medium, Frequency: Weekly)
   - Large deals require multiple approvals - process is opaque
   - Don't know where quote is in approval chain
   - Brokers ask for status updates that I can't provide

**Jobs-to-be-Done:**

1. **When I receive a new submission to underwrite...**
   - I need to see if submission is complete (all required docs attached)
   - So I can immediately start underwriting or request missing info
   - And avoid wasting time on incomplete submissions

2. **When evaluating a risk for a new account...**
   - I need to see all available context (broker relationship quality, similar risks we've quoted, industry loss trends)
   - So I can make informed pricing and coverage decisions
   - And price competitively while maintaining profitability

3. **When preparing a renewal quote...**
   - I need to be notified 90 days before expiration with full account history
   - So I can review claims experience, adjust pricing, and prepare recommendations
   - And get the renewal quote out with 45 days remaining (broker expects this)

4. **When a broker calls about a quote I issued 2 weeks ago...**
   - I need to quickly pull up the quote, see what was proposed, and note any follow-up
   - So I can have an informed conversation and advance the opportunity
   - And not sound like I forgot about their submission

5. **When deciding which submissions to prioritize...**
   - I need to see submission size, complexity, broker quality score, and expected close date
   - So I can work on high-value, high-probability opportunities first
   - And maximize my time and portfolio profitability

**Technology Comfort:**
- Advanced: Proprietary underwriting systems, Excel
- Intermediate: Web-based CRMs, document management systems
- Resistant to: Mobile apps, overly complex interfaces

**Success Metrics for This Persona:**
- Reduced time spent on administrative tasks
- Faster access to account context
- Automated renewal notifications
- Improved quote turnaround time
- Higher quote-to-bind conversion

---

## Persona 3: Jennifer Lee - Broker Relationship Coordinator

**Priority:** Secondary

**Demographics:**
- Age: 28
- Experience: 3 years in insurance operations
- Location: Regional office, Dallas, TX
- Education: BA in Communications
- Technical Proficiency: High (digital native, learns new software quickly)

**Background & Current Role:**
- Supports Distribution & Marketing Manager (Sarah's direct report)
- Manages day-to-day broker communications and administrative tasks
- Reports to Distribution & Marketing Manager
- Team: Works alongside 1 peer coordinator
- Supports 25 broker relationships

**Daily Responsibilities:**
- Log and track incoming submissions (20-25 daily)
- Send submission acknowledgment emails to brokers
- Follow up on missing documentation
- Update submission status in tracking spreadsheet
- Schedule broker meetings and calls
- Prepare broker performance reports
- Process broker appointments and contract paperwork
- Coordinate marketing materials and broker communications
- Handle routine broker inquiries

**Goals & Motivations:**
1. Keep submission pipeline organized and up-to-date
2. Respond to broker inquiries within 2 hours
3. Reduce manual data entry by 50%
4. Improve broker satisfaction with communication
5. Learn underwriting basics to advance career
6. Demonstrate value to earn promotion to Distribution Associate

**Pain Points & Frustrations:**
1. **Excel hell** (Impact: High, Frequency: Daily)
   - Maintaining submission tracking spreadsheet is time-consuming and error-prone
   - Hard to see real-time status when multiple people update different systems
   - Spreadsheet breaks or gets out of sync weekly

2. **Repetitive manual work** (Impact: High, Frequency: Daily)
   - Manually log every submission into tracking spreadsheet
   - Copy/paste information between systems
   - Send same acknowledgment emails over and over

3. **No visibility** (Impact: Medium, Frequency: Daily)
   - Don't know when underwriter updates submission status
   - Can't answer broker questions about underwriting timeline
   - Feel "out of the loop" on important updates

4. **Email overload** (Impact: Medium, Frequency: Daily)
   - Submissions come via email mixed with other communications
   - Miss urgent submissions buried in inbox
   - Hard to track which submissions have been acknowledged

5. **Lack of automation** (Impact: Medium, Frequency: Daily)
   - No automated acknowledgment emails
   - No automated reminders for missing docs
   - Everything is manual and time-consuming

**Jobs-to-be-Done:**

1. **When a submission arrives via email...**
   - I need to quickly log it and send an acknowledgment to the broker
   - So the broker knows we received it and what happens next
   - And I can track it through the pipeline

2. **When a broker calls asking about submission status...**
   - I need to instantly see where it is in the process and who's working on it
   - So I can give them an accurate update in real-time
   - And not have to ask around or say "I'll find out and call you back"

3. **When preparing the weekly submission status report...**
   - I need to pull current data from the system with a few clicks
   - So I can send the report in 15 minutes instead of 2 hours
   - And the data is accurate and up-to-date

4. **When following up on missing documentation...**
   - I need to see which submissions are incomplete and what's missing
   - So I can send targeted follow-up requests to brokers
   - And move submissions through the pipeline faster

**Technology Comfort:**
- Advanced: Social media, consumer apps (Amazon, Uber), Slack, modern web apps
- Intermediate: Excel, Outlook, CRM systems
- Prefers: Clean, intuitive interfaces; mobile-friendly

**Success Metrics for This Persona:**
- Time saved on manual data entry
- Faster response to broker inquiries
- Real-time submission status visibility
- Automated acknowledgment and follow-up emails
- Easier reporting

---

## Platform & Engineering Role Archetypes

These are **internal platform/engineering roles** (not insurance CRM business end-users). They are referenced by platform-foundation features such as F0034 (Product Schema Registry / Dynamic LOB Attributes) and F0036 (Form Engine and Form-State Preservation). They are intentionally lighter-weight than the business personas above — no demographics — because they describe a job function, not a market segment.

### Archetype A: Schema Steward

**Priority:** Secondary (platform-internal)
**Type:** Product/operations role that governs LOB schema bundles

**Background & Role:**
- Owns the lifecycle of LOB product schema bundles (e.g. `cyber/1.0.0`): the data schema, `ui-schema` layout/labels, cross-field rules, and example payloads.
- Publishes and activates new bundle versions through the governed registry (F0034); does not write frontend or backend code.

**Goals & Motivations:**
1. Add or change a product attribute (within the approved widget vocabulary) by publishing a schema-bundle version — **without** requiring a frontend code change.
2. Trust that a published attribute renders and validates consistently across client and backend.
3. Keep bundle changes auditable and reversible (versioned, activation-gated).

**Pain Points & Frustrations:**
1. Today the Cyber panel is hardcoded, so any attribute change requires a frontend deploy — defeating the registry's purpose (the exact gap F0036 closes).
2. No fast feedback that a bundle will render correctly before activation.

**Jobs-to-be-Done:**
1. **When I publish a new attribute that uses an approved widget...** I need it to render from the bundle through the widget registry, so no engineer has to hand-edit a panel.
2. **When I change a cross-field rule...** I need client and backend validation to stay in agreement, so underwriters don't see contradictory errors.

**Success Metrics:** schema-only changes ship without a frontend deploy; bundle activation fails closed on unknown widget/option; 0 client/backend validation disagreements on published examples.

### Archetype B: Frontend Platform Engineer

**Priority:** Secondary (platform-internal)
**Type:** Engineering role that builds and maintains shared frontend foundations

**Background & Role:**
- Builds shared frontend infrastructure: the dynamic form engine (RHF + AJV + widget registry, ADR-021), the F0035 form-state preservation registry, and the shared form-registration helper.
- Maintains consistency across product forms so feature teams don't reinvent form plumbing.

**Goals & Motivations:**
1. One governed form engine and one preservation-registration path, not bespoke per-screen panels and hand-rolled controlled forms.
2. Reduce regression risk when forms change (RHF field-state, parity tests, snapshot/restore behavior).
3. Keep ADRs and shipped code in sync (ADR-021 drift is what triggered F0036).

**Pain Points & Frustrations:**
1. Divergent form implementations (hardcoded attribute panel + hand-rolled CRUD forms) create maintenance and governance drift.
2. F0035 preservation was wired to zero forms, so unsaved input is lost on a forced re-auth across the app.

**Jobs-to-be-Done:**
1. **When I build a new product form...** I want to use the governed engine + the shared registration helper, so field state, validation, and preservation behave consistently.
2. **When a session boundary forces re-auth...** I want every in-scope mutation form to preserve and restore unsaved input via one shared mechanism, so I don't solve it per form.

**Success Metrics:** all in-scope mutation forms are RHF-managed and registered with F0035 preservation; one shared registration helper owns the dirty-state contract; ADR-021 matches the shipped engine.

---

## How to Use These Personas

### During Requirements Gathering:
- Ask: "Which persona(s) will use this feature?"
- Validate: "Does this solve a real pain point for Sarah/Marcus/Jennifer?"
- Prioritize: Primary personas (Sarah, Marcus) get priority over secondary (Jennifer)

### During Story Writing:
- Format: "As [Persona Name - Role], I want [capability], so that [benefit from persona's goals/jobs-to-be-done]"
- Example: "As Sarah (Distribution Manager), I want to see all submissions from a broker in one view, so I can quickly assess broker performance before our quarterly review meeting"

### During Design:
- Consider persona's technical proficiency when designing UI
- Use persona's daily workflow to inform navigation and information architecture
- Address persona's pain points explicitly in feature design

### During Prioritization:
- Features that solve "High Impact, High Frequency" pain points = MVP
- Features that address multiple personas' needs = higher priority
- Features that only serve secondary personas = Phase 1 or later

---

**Last Updated:** 2026-01-31
**Version:** 2.0
