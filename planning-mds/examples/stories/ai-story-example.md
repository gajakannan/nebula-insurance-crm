# Story: AI-Powered Customer Risk Assessment

**Story ID:** AI-1
**Feature:** F-AI - Intelligent Customer Insights
**Title:** AI-powered customer risk assessment with recommendations
**Priority:** High
**Phase:** Post-MVP (AI Features)
**AI Pattern:** AI-Embedded (Pattern 2)

## User Story

**As a** Account Manager
**I want** to get an AI-powered risk assessment for a customer
**So that** I can proactively identify issues and take recommended actions before problems escalate

## Context & Background

Account managers currently rely on manual review of customer history to identify risks (late payments, declining engagement, complaints). This is time-consuming and inconsistent.

This story introduces the first AI-powered insight feature, which analyzes customer data and provides:
- Risk classification (Low/Medium/High)
- Key risk indicators
- Recommended next actions

**AI Integration Pattern:** This story uses **AI-Embedded (Pattern 2)** because:
- Moderate AI complexity (multi-step workflow with data analysis)
- Backend proxy pattern for authorization and rate limiting
- AI logic isolated in neuron/ layer
- No need for real-time streaming (batch analysis is acceptable)

## Acceptance Criteria

### Functional Requirements

- **Given** I am an authorized user viewing a customer's detail page
- **When** I click "Analyze Risk" button
- **Then** an AI-powered risk assessment is displayed within 5 seconds
- **And** the assessment includes: risk level, key indicators, and 2-3 recommended actions

- **Given** the AI analysis completes successfully
- **When** the response is returned
- **Then** the risk level is one of: Low, Medium, High
- **And** key indicators list 3-5 specific data points (e.g., "3 late payments in last 6 months")
- **And** recommendations are actionable (e.g., "Schedule check-in call", "Review payment terms")

- **Given** the customer has insufficient data for analysis
- **When** I request a risk assessment
- **Then** I see a message "Insufficient data for analysis (need at least 3 months of history)"
- **And** no risk assessment is stored

- **Given** the AI service (neuron/) is unavailable
- **When** I request a risk assessment
- **Then** I see a user-friendly error message "Risk analysis temporarily unavailable"
- **And** the application remains functional for other features

### AI-Specific Acceptance Criteria

- **Given** a customer risk assessment is generated
- **When** the AI response is received
- **Then** the response includes a confidence score (0.0 - 1.0)
- **And** confidence below 0.6 displays a warning "Low confidence - manual review recommended"

- **Given** the AI generates a risk assessment
- **When** the analysis completes
- **Then** the prompt and response are logged for audit purposes
- **And** token usage and cost are tracked

- **Given** a user requests risk assessments for multiple customers in quick succession
- **When** the rate limit (10 requests/minute) is exceeded
- **Then** subsequent requests are rejected with "Rate limit exceeded - try again in X seconds"

### Authorization

- **Given** I am not authorized (`customer:analyze-risk` permission)
- **When** I attempt to request a risk assessment
- **Then** access is denied with a 403 response
- **And** the neuron/ service is never called

### Audit & Timeline

- **Given** a risk assessment is successfully generated
- **When** the analysis completes
- **Then** an audit timeline event is created with:
  - Event type: "CustomerRiskAssessed"
  - Actor: current user
  - Metadata: risk level, confidence, model used
  - Timestamp: UTC

## AI Integration Contract

### Backend → Neuron API Contract

**Endpoint:** `POST http://neuron:8000/assess-customer-risk`

**Request:**
```json
{
  "customer_id": "uuid",
  "context": {
    "include_payment_history": true,
    "include_engagement_metrics": true,
    "lookback_months": 6
  }
}
```

**Response (Success):**
```json
{
  "risk_level": "High" | "Medium" | "Low",
  "confidence": 0.85,
  "key_indicators": [
    "3 late payments in last 6 months",
    "50% decrease in engagement",
    "2 support complaints filed"
  ],
  "recommendations": [
    {
      "action": "Schedule check-in call",
      "priority": "High",
      "reasoning": "Customer engagement has declined significantly"
    },
    {
      "action": "Review payment terms",
      "priority": "Medium",
      "reasoning": "Payment delays suggest cash flow issues"
    }
  ],
  "model_used": "llm-model",
  "token_count": 450,
  "cost_usd": 0.0023,
  "generated_at": "2026-02-07T10:30:00Z"
}
```

**Response (Insufficient Data):**
```json
{
  "error": "insufficient_data",
  "message": "Need at least 3 months of customer history",
  "min_data_points_required": 10,
  "actual_data_points": 2
}
```

**Response (Error):**
```json
{
  "error": "ai_processing_failed",
  "message": "LLM request failed",
  "details": "Rate limit exceeded on LLM API"
}
```

### Backend Proxy Implementation

**File:** `engine/Services/AiProxyService.cs`

**Responsibilities:**
- Call neuron/ API with customer context
- Enforce authorization before calling AI
- Apply rate limiting (10 requests/minute per user)
- Handle neuron/ failures gracefully with fallback messaging
- Log all AI requests for audit

**Error Handling:**
- Timeout after 10 seconds → "Analysis timed out, try again"
- 503 from neuron/ → "Risk analysis temporarily unavailable"
- 429 from neuron/ → "Rate limit exceeded, try again in X seconds"

### Neuron Implementation

**File:** `neuron/domain_agents/risk_assessment_agent.py`

**Responsibilities:**
- Fetch customer data (via internal API call to engine/)
- Analyze payment history, engagement, support tickets
- Generate structured risk assessment using LLM
- Return standardized response format
- Track token usage and cost

**Data Access:**
The neuron/ agent calls engine/ internal API to fetch customer data:
```python
# neuron/domain_agents/risk_assessment_agent.py
async def fetch_customer_context(customer_id: str) -> dict:
    """Fetch customer data from engine/ internal API."""
    async with httpx.AsyncClient() as client:
        # Service-to-service auth token
        headers = {"Authorization": f"Bearer {SERVICE_TOKEN}"}

        # Fetch customer details
        customer = await client.get(
            f"http://engine:5000/internal/customers/{customer_id}",
            headers=headers
        )

        # Fetch payment history
        payments = await client.get(
            f"http://engine:5000/internal/customers/{customer_id}/payments",
            headers=headers
        )

        return {
            "customer": customer.json(),
            "payments": payments.json()
        }
```

**Prompt Structure:**
```python
system_prompt = """
You are a customer risk analyst for a CRM system.

Your task: Analyze customer data and assess risk level.

Input:
- Customer profile
- Payment history (last 6 months)
- Engagement metrics
- Support ticket history

Output (JSON):
{
  "risk_level": "High" | "Medium" | "Low",
  "confidence": 0.0-1.0,
  "key_indicators": ["indicator1", "indicator2", ...],
  "recommendations": [
    {"action": "...", "priority": "High/Medium/Low", "reasoning": "..."}
  ]
}

Rules:
- High risk: payment issues + declining engagement + complaints
- Medium risk: one major issue or multiple minor issues
- Low risk: healthy payment + engagement patterns
- Confidence <0.6 if data is sparse or contradictory
"""
```

## Data Requirements

**Customer Data Inputs (from engine/):**
- Customer profile (name, status, account age)
- Payment history (last 6 months, amounts, on-time vs late)
- Engagement metrics (login frequency, feature usage)
- Support ticket history (count, severity, resolution time)

**Minimum Data Requirements:**
- At least 3 months of customer history
- At least 10 data points (payments, tickets, or engagement events)

**Output Storage:**
- Risk assessments stored in `CustomerRiskAssessment` table
- Fields: CustomerId, RiskLevel, Confidence, Indicators (JSON), Recommendations (JSON), AssessedAt, AssessedBy, ModelUsed

## Non-Functional Requirements

### Performance
- AI response time p95 < 5 seconds (user-facing timeout: 10 seconds)
- Backend → neuron/ timeout: 8 seconds
- Neuron/ → LLM timeout: 6 seconds

### Cost Controls
- Rate limit: 10 risk assessments per user per minute
- Rate limit: 100 risk assessments per organization per hour
- Use lightweight model for simple analysis, balanced model for complex (cost-aware model routing)

### Security
- Authorization checked in backend before calling neuron/
- Service-to-service auth token for neuron/ → engine/ calls
- No customer PII sent to LLM logs (sanitize before logging)
- AI responses sanitized before display (prevent prompt injection attacks)

### Reliability
- Backend gracefully handles neuron/ failures (no crash)
- Fallback message displayed if AI unavailable
- Retry logic: 1 retry with exponential backoff for transient errors

### Observability
- Log every AI request with: user, customer_id, timestamp, model, tokens, cost
- Track metrics: request count, latency, error rate, cost per assessment
- Alert on: error rate > 5%, cost per day > $10, p95 latency > 10s

## Testing Strategy

### Backend Tests (engine/)

**Unit Tests:**
- `AiProxyService` calls neuron/ with correct payload
- Authorization is enforced before AI call
- Rate limiting rejects requests correctly
- Timeouts handled gracefully

**Integration Tests:**
- Mock neuron/ API and verify full flow
- Verify audit timeline event created
- Verify error responses mapped correctly

### AI Tests (neuron/)

**Unit Tests:**
- Risk assessment agent parses customer data correctly
- Prompt generation includes all required context
- Response parsing handles all LLM output variations

**Evaluation Tests:**
```python
# neuron/tests/test_risk_agent_evaluation.py
def test_risk_agent_accuracy():
    """Evaluate agent with known customer scenarios."""
    test_cases = [
        {
            "customer": high_risk_customer_fixture,
            "expected_risk": "High",
            "min_confidence": 0.7
        },
        {
            "customer": low_risk_customer_fixture,
            "expected_risk": "Low",
            "min_confidence": 0.8
        }
    ]

    for case in test_cases:
        result = agent.assess(case["customer"])
        assert result.risk_level == case["expected_risk"]
        assert result.confidence >= case["min_confidence"]
```

**Mock Tests:**
- Mock LLM provider API to avoid costs during testing
- Use deterministic LLM responses for reproducible tests

### E2E Tests

**Playwright Test:**
```typescript
test('AI risk assessment displays results', async ({ page }) => {
  await page.goto('/customers/123');
  await page.click('button:has-text("Analyze Risk")');

  // Should show loading state
  await expect(page.locator('.loading-spinner')).toBeVisible();

  // Should display risk level within 10 seconds
  await expect(page.locator('.risk-level')).toHaveText(/High|Medium|Low/, {
    timeout: 10000
  });

  // Should display key indicators
  await expect(page.locator('.key-indicators li')).toHaveCount.greaterThan(2);

  // Should display recommendations
  await expect(page.locator('.recommendations li')).toHaveCount.greaterThan(1);
});
```

## Dependencies

**Depends On:**
- Customer data model and repository (engine/)
- Payment history API (engine/)
- Engagement metrics API (engine/)
- Authorization policy for `customer:analyze-risk`
- neuron/ service deployed and accessible from engine/
- Service-to-service auth token provisioning

**Related Stories:**
- AI-2 - Batch risk assessment for all customers
- AI-3 - Risk trend visualization over time

## Out of Scope

- Real-time streaming risk updates (use AI-Centric pattern for that)
- Customer-facing risk score display (internal use only in MVP)
- Automated action execution based on risk (recommendations only)
- Risk prediction (forecasting future risk, not just current)
- Multi-language support for AI recommendations

## Technical Decisions

### ADR: Use AI-Embedded Pattern (Pattern 2)

**Context:** Need to implement AI-powered customer risk assessment.

**Decision:** Use AI-Embedded pattern with neuron/ layer accessed via backend proxy.

**Rationale:**
- ✅ Moderate complexity (multi-step workflow, prompt management)
- ✅ Backend controls authorization and rate limiting
- ✅ AI logic isolated for testing and reusability
- ✅ No need for real-time streaming (batch is fine)
- ✅ Clear separation of business logic (engine/) and AI logic (neuron/)

**Alternatives Considered:**
- ❌ AI-Optional: Too complex for embedded LLM calls in backend services
- ❌ AI-Centric: Overkill for batch analysis (no streaming needed)

### ADR: Model Selection Strategy

**Decision:** Use lightweight model by default, balanced model for edge cases.

**Rationale:**
- Most risk assessments are straightforward → Lightweight model is sufficient and cost-effective
- Complex scenarios (sparse data, contradictory signals) → Balanced model for better reasoning
- Cost optimization: Lightweight model is 10-20x cheaper than balanced model

**Implementation:**
```python
def select_model(customer_data: dict) -> str:
    """Route to appropriate model based on complexity."""
    data_quality = assess_data_quality(customer_data)

    if data_quality == "poor" or data_quality == "contradictory":
        return "llm-balanced"  # Better reasoning for edge cases
    else:
        return "llm-lightweight"   # Fast and cost-effective
```

## Questions & Assumptions

**Open Questions:**
- [ ] Should risk assessments be cached? (e.g., valid for 24 hours)
- [ ] Should we notify account managers when risk level changes?
- [ ] What happens if customer data changes during analysis?

**Assumptions (to be validated):**
- Users understand that AI assessments are recommendations, not guarantees
- 5-second response time is acceptable for users
- $0.002-0.005 per assessment is acceptable cost
- Risk assessments are point-in-time (no historical tracking in MVP)

## Definition of Done

- [ ] All acceptance criteria met
- [ ] Backend → neuron/ API contract implemented and tested
- [ ] Authorization enforced in backend before AI call
- [ ] Rate limiting prevents abuse
- [ ] Error handling graceful (no user-facing crashes)
- [ ] Audit timeline event created for all assessments
- [ ] Token usage and cost tracked
- [ ] Unit tests pass (backend and neuron/)
- [ ] Evaluation tests pass (accuracy > 80% on test cases)
- [ ] E2E test passes (UI → backend → neuron/ → LLM)
- [ ] Performance requirements met (p95 < 5s)
- [ ] Security review passed (no PII leakage, authorization enforced)
- [ ] Documentation updated (API contract, integration guide)
- [ ] Cost monitoring dashboard configured

---

**AI Pattern Reference:** See `docs/AI-INTEGRATION-PATTERNS.md` Pattern 2 (AI-Embedded)
**Architect Reference:** The external agent framework's architect role provides AI-architecture pattern references.
