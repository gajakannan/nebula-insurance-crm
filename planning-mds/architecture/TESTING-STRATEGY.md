# Nebula CRM - Testing Strategy

**Purpose:** Define the testing tools, frameworks, and patterns for Frontend, Backend, and AI layers.

**Last Updated:** 2026-02-01

---

## Testing Philosophy

1. **Test Pyramid:** Many unit tests → Fewer integration tests → Few E2E tests
2. **Coverage Target:** ≥80% code coverage for business logic (not configuration/boilerplate)
3. **Fast Feedback:** Unit tests run in <1 second, integration in <10 seconds
4. **Test in Production-Like Environment:** Use Testcontainers for databases, not mocks
5. **Shift Left:** Catch bugs early (unit tests), not late (E2E tests)
6. **Automate Everything:** All tests run in CI/CD, no manual testing for regressions

---

## 1. Frontend Testing (experience/)

### Tech Stack

| Test Type | Framework | Purpose |
|-----------|-----------|---------|
| **Unit/Component** | Vitest + React Testing Library | Test components in isolation |
| **E2E** | Playwright (with MCP) | Test full user flows across frontend + backend |
| **Accessibility** | @axe-core/playwright or jest-axe | WCAG 2.1 AA compliance |
| **Visual Regression** | Playwright (screenshots) | Detect unintended UI changes |
| **Performance** | Lighthouse CI, Web Vitals | Core Web Vitals, load times |
| **Integration** | Vitest + MSW (Mock Service Worker) | Test with mocked API responses |

### Unit & Component Tests (Vitest)

**Framework:** Vitest (faster Vite-native alternative to Jest)
**Utilities:** React Testing Library (test user interactions, not implementation)
**Coverage:** ≥80% for business logic components

**What to Test:**
- Component rendering with different props
- User interactions (click, type, submit)
- Conditional rendering
- State changes
- Form validation (with AJV/RJSF)
- Error handling and error boundaries
- Accessibility (ARIA labels, keyboard navigation)

**Example:**
```typescript
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { BrokerForm } from './BrokerForm';

describe('BrokerForm', () => {
  it('submits form with valid data', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();

    render(<BrokerForm onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText('Broker Name'), 'Test Broker');
    await user.type(screen.getByLabelText('Email'), 'test@example.com');
    await user.click(screen.getByRole('button', { name: /save/i }));

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith({
        name: 'Test Broker',
        email: 'test@example.com'
      });
    });
  });

  it('shows validation error for invalid email', async () => {
    render(<BrokerForm />);
    const user = userEvent.setup();

    await user.type(screen.getByLabelText('Email'), 'invalid-email');
    await user.click(screen.getByRole('button', { name: /save/i }));

    expect(screen.getByText('Invalid email address')).toBeInTheDocument();
  });
});
```

### Integration Tests (Vitest + MSW)

**Framework:** Vitest with MSW (Mock Service Worker)
**Purpose:** Test API integration without hitting real backend

**What to Test:**
- TanStack Query hooks (useQuery, useMutation)
- API error handling
- Loading states
- Cache invalidation
- Optimistic updates

**Example:**
```typescript
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import { BrokerList } from './BrokerList';

// Mock API
const server = setupServer(
  http.get('/brokers', () => {
    return HttpResponse.json([
      { id: '1', name: 'Broker A', email: 'a@example.com' },
      { id: '2', name: 'Broker B', email: 'b@example.com' }
    ]);
  })
);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

it('loads and displays brokers', async () => {
  const queryClient = new QueryClient();

  render(
    <QueryClientProvider client={queryClient}>
      <BrokerList />
    </QueryClientProvider>
  );

  expect(screen.getByText('Loading...')).toBeInTheDocument();

  await waitFor(() => {
    expect(screen.getByText('Broker A')).toBeInTheDocument();
    expect(screen.getByText('Broker B')).toBeInTheDocument();
  });
});
```

### E2E Tests (Playwright)

**Framework:** Playwright (with MCP for automation)
**Purpose:** Test critical user flows end-to-end

**What to Test:**
- Login flow
- CRUD operations (create broker, update, delete)
- Multi-step workflows (submission intake flow)
- Error scenarios (network failures, validation errors)
- Cross-browser compatibility (Chrome, Firefox, Safari)

**Example:**
```typescript
import { test, expect } from '@playwright/test';

test('create broker flow', async ({ page }) => {
  await page.goto('/brokers');

  // Click "Create Broker" button
  await page.getByRole('button', { name: 'Create Broker' }).click();

  // Fill form
  await page.getByLabel('Broker Name').fill('Test Broker');
  await page.getByLabel('Email').fill('test@example.com');
  await page.getByLabel('Phone').fill('1234567890');

  // Submit
  await page.getByRole('button', { name: 'Save' }).click();

  // Verify success
  await expect(page.getByText('Broker created successfully')).toBeVisible();
  await expect(page.getByText('Test Broker')).toBeVisible();
});
```

### Accessibility Tests (jest-axe / @axe-core/playwright)

**Framework:** @axe-core/playwright for E2E, jest-axe for component tests
**Purpose:** Ensure WCAG 2.1 AA compliance

**Example:**
```typescript
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test('broker form has no accessibility violations', async ({ page }) => {
  await page.goto('/brokers/new');

  const accessibilityScanResults = await new AxeBuilder({ page }).analyze();

  expect(accessibilityScanResults.violations).toEqual([]);
});
```

### Visual Regression Tests (Playwright)

**Framework:** Playwright screenshots
**Purpose:** Detect unintended UI changes

**Example:**
```typescript
test('broker card matches snapshot', async ({ page }) => {
  await page.goto('/brokers/123');

  await expect(page.getByTestId('broker-card')).toHaveScreenshot();
});
```

### Performance Tests (Lighthouse CI)

**Framework:** Lighthouse CI
**Purpose:** Monitor Core Web Vitals, bundle size, load time

**Metrics:**
- LCP (Largest Contentful Paint) < 2.5s
- FID (First Input Delay) < 100ms
- CLS (Cumulative Layout Shift) < 0.1
- Bundle size < 500KB (initial load)

**CI Integration:**
```yaml
# .github/workflows/lighthouse.yml
- name: Run Lighthouse
  uses: treosh/lighthouse-ci-action@v9
  with:
    urls: |
      http://localhost:3000
      http://localhost:3000/brokers
    uploadArtifacts: true
```

---

## 2. Backend Testing (engine/)

### Tech Stack

| Test Type | Framework | Purpose |
|-----------|-----------|---------|
| **Unit Tests** | xUnit + FluentAssertions | Test domain logic, services in isolation |
| **Integration Tests (In-Code)** | xUnit + WebApplicationFactory | Test API endpoints with in-memory server |
| **API Testing (Collections)** | Bruno CLI or curl scripts | Test API endpoints with request collections |
| **Database Tests** | xUnit + Testcontainers | Test repositories with real PostgreSQL |
| **Contract Tests** | Pact.NET | Verify API contracts match frontend expectations |
| **Load/Performance Tests** | k6 | Load testing, stress testing |
| **Mutation Tests** | Stryker.NET | Test the quality of tests |
| **Code Coverage** | Coverlet + ReportGenerator | Measure test coverage |

### Unit Tests (xUnit)

**Framework:** xUnit (most popular for .NET)
**Assertions:** FluentAssertions (more readable assertions)
**Coverage:** ≥80% for domain and application logic

**What to Test:**
- Domain entity business logic
- Application service logic
- Validation rules
- Workflow state transitions
- Authorization rules
- Timeline event creation
- Error handling

**Example:**
```csharp
using Xunit;
using FluentAssertions;
using Nebula.Domain.Entities;

public class BrokerTests
{
    [Fact]
    public void Activate_WhenInactive_ShouldSetStatusToActive()
    {
        // Arrange
        var broker = new Broker { Status = BrokerStatus.Inactive };

        // Act
        broker.Activate();

        // Assert
        broker.Status.Should().Be(BrokerStatus.Active);
    }

    [Fact]
    public void Activate_WhenDeleted_ShouldThrowException()
    {
        // Arrange
        var broker = new Broker { IsDeleted = true };

        // Act
        var act = () => broker.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot activate deleted broker");
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("valid@email.com", true)]
    [InlineData("invalid-email", false)]
    public void ValidateEmail_ShouldReturnExpectedResult(string email, bool expected)
    {
        // Arrange
        var broker = new Broker { Email = email };

        // Act
        var isValid = broker.IsEmailValid();

        // Assert
        isValid.Should().Be(expected);
    }
}
```

### Integration Tests (xUnit + WebApplicationFactory)

**Framework:** xUnit with WebApplicationFactory (ASP.NET Core in-memory test server)
**Purpose:** Test API endpoints without external dependencies

**What to Test:**
- API endpoint request/response
- JSON Schema validation
- Authorization enforcement
- Error responses (ProblemDetails)
- HTTP status codes
- Audit/timeline event creation

**Example:**
```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using System.Net.Http.Json;

public class BrokerEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BrokerEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateBroker_WithValidData_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateBrokerDto
        {
            Name = "Test Broker",
            Email = "test@example.com",
            Phone = "1234567890"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/brokers", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var broker = await response.Content.ReadFromJsonAsync<BrokerDto>();
        broker.Should().NotBeNull();
        broker!.Name.Should().Be("Test Broker");
    }

    [Fact]
    public async Task CreateBroker_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateBrokerDto
        {
            Name = "Test Broker",
            Email = "invalid-email",
            Phone = "1234567890"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/brokers", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Contain("Validation");
    }

    [Fact]
    public async Task GetBroker_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/brokers/123");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

### API Testing with Bruno CLI

**Framework:** Bruno CLI (open-source, git-friendly alternative to Postman/Newman)
**Purpose:** Test API endpoints with collections, scripts, and environments

**Why Bruno over Playwright for APIs:**
- ✅ **Purpose-built** for API testing (not browser automation)
- ✅ **Git-friendly** - collections stored as plain text files
- ✅ **Open source** - no vendor lock-in like Postman
- ✅ **Fast** - direct HTTP calls, no browser overhead
- ✅ **Scriptable** - JavaScript pre/post request scripts
- ✅ **Environments** - dev, staging, prod configs
- ✅ **CLI support** - run in CI/CD pipelines

**Why NOT Playwright for API testing:**
- ❌ Playwright is for browser automation, not API testing
- ❌ Overhead of browser instance
- ❌ Wrong tool for the job

**Bruno Collection Structure:**
```
bruno/
├── environments/
│   ├── dev.bru
│   ├── staging.bru
│   └── prod.bru
├── brokers/
│   ├── create-broker.bru
│   ├── get-broker.bru
│   ├── update-broker.bru
│   └── delete-broker.bru
├── accounts/
└── submissions/
```

**Example: Create Broker (create-broker.bru)**
```
meta {
  name: Create Broker
  type: http
  seq: 1
}

post {
  url: {{baseUrl}}/brokers
  body: json
  auth: bearer
}

auth:bearer {
  token: {{authToken}}
}

body:json {
  {
    "name": "Test Broker",
    "email": "test@example.com",
    "phone": "1234567890",
    "status": "Active"
  }
}

assert {
  res.status: eq 201
  res.body.id: isDefined
  res.body.name: eq "Test Broker"
}

tests {
  test("should create broker", function() {
    expect(res.getStatus()).to.equal(201);
    expect(res.getBody().name).to.equal("Test Broker");
  });
}
```

**Run in CLI:**
```bash
# Run all collections
bru run --env dev

# Run specific folder
bru run brokers --env dev

# Run in CI/CD
bru run --env ci --output junit --output-file test-results.xml
```

**Alternative: curl scripts (simpler, no dependencies)**
```bash
#!/bin/bash
# test-brokers-api.sh

BASE_URL="http://localhost:5000"
TOKEN="your-jwt-token"

# Test: Create Broker
echo "Testing: Create Broker"
RESPONSE=$(curl -s -w "%{http_code}" -X POST "$BASE_URL/brokers" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Broker","email":"test@example.com","phone":"1234567890"}')

HTTP_CODE="${RESPONSE: -3}"
BODY="${RESPONSE:0:${#RESPONSE}-3}"

if [ "$HTTP_CODE" -eq 201 ]; then
  echo "✓ Create Broker: PASS"
else
  echo "✗ Create Broker: FAIL (HTTP $HTTP_CODE)"
  exit 1
fi

# Test: Get Broker
BROKER_ID=$(echo "$BODY" | jq -r '.id')
echo "Testing: Get Broker"
curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/brokers/$BROKER_ID" \
  -H "Authorization: Bearer $TOKEN"
```

**When to use each approach:**

| Approach | When to Use |
|----------|-------------|
| **xUnit + WebApplicationFactory** | Integration tests in C# code, run in CI/CD, full coverage |
| **Bruno CLI** | API collections, manual testing, scripted E2E API flows |
| **curl scripts** | Simple smoke tests, health checks, quick validation |
| **Pact.NET** | Contract testing (verify frontend ↔ backend contracts) |
| **k6** | Load testing, performance testing |

### Database Tests (Testcontainers)

**Framework:** xUnit + Testcontainers (real PostgreSQL in Docker)
**Purpose:** Test repositories with real database (not in-memory)

**Why Testcontainers over In-Memory:**
- PostgreSQL-specific features (JSONB, full-text search, triggers)
- Production-like environment
- Catch migration issues early

**Example:**
```csharp
using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;

public class BrokerRepositoryTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer;
    private NebulaDbContext _context;
    private BrokerRepository _repository;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .Build();
        await _postgresContainer.StartAsync();

        // Create DbContext
        var options = new DbContextOptionsBuilder<NebulaDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _context = new NebulaDbContext(options);
        await _context.Database.MigrateAsync();

        _repository = new BrokerRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistBroker()
    {
        // Arrange
        var broker = new Broker
        {
            Id = Guid.NewGuid(),
            Name = "Test Broker",
            Email = "test@example.com"
        };

        // Act
        await _repository.AddAsync(broker);

        // Assert
        var saved = await _repository.GetByIdAsync(broker.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Test Broker");
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}
```

### Contract Tests (Pact.NET)

**Framework:** Pact.NET (consumer-driven contract testing)
**Purpose:** Verify API contracts between frontend (consumer) and backend (provider)

**Example:**
```csharp
// Provider test (backend verifies it meets contract)
[Fact]
public void EnsureProviderApiHonoursPactWithConsumer()
{
    var config = new PactVerifierConfig();

    IPactVerifier pactVerifier = new PactVerifier(config);
    pactVerifier
        .ProviderState($"{_pactServiceUri}/provider-states")
        .ServiceProvider("BrokerAPI", _providerUri)
        .HonoursPactWith("FrontendApp")
        .PactUri("../pacts/frontendapp-brokerapi.json")
        .Verify();
}
```

### Performance/Load Tests (NBomber or k6)

**Framework:** NBomber (C# native) or k6 (JavaScript, better UX)
**Purpose:** Load testing, stress testing, spike testing

**Metrics:**
- Requests per second (RPS)
- Response time (p50, p95, p99)
- Error rate
- Throughput

**Example (k6):**
```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 20 },  // Ramp up
    { duration: '1m', target: 100 },  // Steady load
    { duration: '30s', target: 0 },   // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests < 500ms
    http_req_failed: ['rate<0.01'],   // Error rate < 1%
  },
};

export default function () {
  const res = http.get('http://localhost:5000/brokers');
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });
  sleep(1);
}
```

### Mutation Testing (Stryker.NET)

**Framework:** Stryker.NET
**Purpose:** Test the quality of your tests (mutate code, tests should fail)

**Example:**
```bash
dotnet tool install -g dotnet-stryker
dotnet stryker
```

### Code Coverage (Coverlet)

**Framework:** Coverlet + ReportGenerator
**Purpose:** Measure test coverage

**Example:**
```bash
dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --collect:"XPlat Code Coverage"
reportgenerator -reports:"engine/tests/Nebula.Tests/TestResults/**/coverage.cobertura.xml" -targetdir:coverage-report
```

---

## 3. AI/Neuron Testing (neuron/)

### Tech Stack

| Test Type | Framework | Purpose |
|-----------|-----------|---------|
| **Unit Tests** | pytest | Test agent logic, tools, utilities |
| **LLM Tests** | pytest + mocking | Test prompt templates, output parsing |
| **Evaluation Tests** | pytest + custom metrics | Test accuracy, hallucination, cost |
| **Integration Tests** | pytest + real API | Test end-to-end agent flows (costly) |
| **MCP Server Tests** | pytest + FastAPI TestClient | Test MCP endpoints |
| **Performance Tests** | pytest-benchmark | Test latency, throughput |

### Unit Tests (pytest)

**Framework:** pytest
**Purpose:** Test agent logic without calling LLM APIs

**What to Test:**
- Tool implementations
- Prompt template formatting
- Output parsing
- Error handling
- Retry logic
- Cost tracking

**Example:**
```python
import pytest
from neuron.crm_agents.underwriter import UnderwriterAgent

def test_format_submission_prompt():
    agent = UnderwriterAgent()
    submission = {
        "business_type": "Restaurant",
        "revenue": 500000,
        "location": "CA"
    }

    prompt = agent.format_submission_prompt(submission)

    assert "Restaurant" in prompt
    assert "$500,000" in prompt
    assert "California" in prompt

def test_parse_underwriting_response():
    agent = UnderwriterAgent()
    response = """
    Risk Level: Medium
    Suggested Premium: $5,000
    Reasoning: Restaurant in high-risk area but good revenue.
    """

    result = agent.parse_response(response)

    assert result["risk_level"] == "Medium"
    assert result["premium"] == 5000
    assert "high-risk area" in result["reasoning"]
```

### LLM Tests (pytest + mocking)

**Framework:** pytest with unittest.mock
**Purpose:** Test LLM integration without API calls (fast, free)

**Example:**
```python
from unittest.mock import Mock, patch
import pytest
from neuron.crm_agents.underwriter import UnderwriterAgent

@patch('neuron.models.claude.ClaudeClient.generate')
def test_analyze_submission_returns_risk_assessment(mock_generate):
    # Mock LLM response
    mock_generate.return_value = {
        "content": "Risk Level: High\nPremium: $10,000\nReasoning: High risk industry"
    }

    agent = UnderwriterAgent()
    result = agent.analyze_submission({
        "business_type": "Nightclub",
        "revenue": 1000000
    })

    assert result["risk_level"] == "High"
    assert result["premium"] == 10000
    mock_generate.assert_called_once()
```

### Evaluation Tests (custom metrics)

**Framework:** pytest with custom evaluation metrics
**Purpose:** Measure agent quality (accuracy, hallucination, cost)

**Metrics:**
- **Accuracy:** Compare agent output to golden dataset
- **Hallucination Rate:** Detect when agent invents information
- **Latency:** Time to complete task (p50, p95, p99)
- **Cost:** Token usage, API cost per request
- **Success Rate:** % of tasks completed successfully

**Example:**
```python
import pytest
from neuron.crm_agents.underwriter import UnderwriterAgent
from neuron.evaluation.metrics import calculate_accuracy

@pytest.fixture
def golden_dataset():
    return [
        {
            "input": {"business_type": "Restaurant", "revenue": 500000},
            "expected_risk": "Medium",
            "expected_premium_range": (4000, 6000)
        },
        # ... more examples
    ]

def test_underwriting_accuracy(golden_dataset):
    agent = UnderwriterAgent()
    results = []

    for case in golden_dataset:
        output = agent.analyze_submission(case["input"])
        results.append({
            "correct_risk": output["risk_level"] == case["expected_risk"],
            "premium_in_range": (
                case["expected_premium_range"][0]
                <= output["premium"]
                <= case["expected_premium_range"][1]
            )
        })

    accuracy = calculate_accuracy(results)
    assert accuracy >= 0.85  # 85% accuracy threshold
```

### Integration Tests (pytest + real API)

**Framework:** pytest with real Claude API calls
**Purpose:** Test end-to-end agent flows (use sparingly, costly)

**Example:**
```python
import pytest
from neuron.crm_agents.underwriter import UnderwriterAgent

@pytest.mark.integration
@pytest.mark.slow
def test_underwriter_with_real_api():
    """Integration test - calls real Claude API"""
    agent = UnderwriterAgent(use_real_api=True)

    result = agent.analyze_submission({
        "business_type": "Restaurant",
        "revenue": 500000,
        "location": "CA",
        "claims_history": []
    })

    assert result["risk_level"] in ["Low", "Medium", "High"]
    assert result["premium"] > 0
    assert len(result["reasoning"]) > 50
```

### MCP Server Tests (FastAPI TestClient)

**Framework:** pytest + FastAPI TestClient
**Purpose:** Test MCP server endpoints

**Example:**
```python
from fastapi.testclient import TestClient
from neuron.mcp.crm_server import app

client = TestClient(app)

def test_get_broker():
    response = client.get("/mcp/brokers/123")
    assert response.status_code == 200
    assert response.json()["id"] == "123"

def test_mcp_tool_invoke():
    response = client.post("/mcp/tools/analyze-submission", json={
        "submission_id": "456"
    })
    assert response.status_code == 200
    assert "risk_level" in response.json()
```

### Performance Tests (pytest-benchmark)

**Framework:** pytest-benchmark
**Purpose:** Measure agent latency

**Example:**
```python
import pytest
from neuron.crm_agents.underwriter import UnderwriterAgent

def test_underwriting_performance(benchmark):
    agent = UnderwriterAgent(use_mock=True)  # Mock for speed
    submission = {"business_type": "Restaurant", "revenue": 500000}

    result = benchmark(agent.analyze_submission, submission)

    assert benchmark.stats.stats.mean < 0.1  # < 100ms average
```

---

## 4. Cross-Cutting Tests

### Contract Testing (Pact)

**Purpose:** Verify frontend expectations match backend implementation
**Tool:** Pact (consumer-driven contracts)

**Process:**
1. Frontend defines contract (expected API shape)
2. Backend verifies it meets contract
3. Both sides stay in sync

### Security Testing

**All tools are 100% free and open source.**

| Tool | Purpose | License |
|------|---------|---------|
| **OWASP ZAP** | Dynamic security scanning (DAST) | Apache 2.0 (FREE) |
| **Trivy** | Vulnerability scanning (dependencies + containers) | Apache 2.0 (FREE) |
| **Grype** | Vulnerability scanning (alternative to Trivy) | Apache 2.0 (FREE) |
| **SonarQube Community** | Static code analysis (SAST) | LGPL v3 (FREE) |
| **Semgrep** | Pattern-based SAST | LGPL 2.1 (FREE) |

**Note:** We use **Trivy** instead of Snyk because Trivy is 100% free and open source with no limitations. Snyk has a limited free tier and requires payment for commercial use.

### Load Testing (k6)

**Tool:** k6 (modern, developer-friendly load testing)
**Purpose:** Stress test full system under load

**Example:**
```javascript
import http from 'k6/http';

export const options = {
  stages: [
    { duration: '5m', target: 100 },   // Ramp to 100 users
    { duration: '10m', target: 100 },  // Stay at 100 users
    { duration: '5m', target: 500 },   // Spike to 500 users
    { duration: '5m', target: 0 },     // Ramp down
  ],
};

export default function () {
  http.get('http://localhost:5000/brokers');
}
```

---

## 5. Testing Pyramid

```
         /\
        /  \  E2E Tests (Playwright) - Few, slow, expensive
       /    \
      /------\  Integration Tests (xUnit + Testcontainers, Vitest + MSW) - Some, medium speed
     /        \
    /----------\  Unit Tests (xUnit, Vitest, pytest) - Many, fast, cheap
   /______________\
```

**Distribution:**
- **70% Unit Tests** - Fast, isolated, test business logic
- **20% Integration Tests** - Test API contracts, database access
- **10% E2E Tests** - Test critical user flows only

---

## 6. CI/CD Integration

### GitHub Actions Workflow

```yaml
name: Tests

on: [push, pull_request]

jobs:
  frontend-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
      - run: npm ci
      - run: npm run test:unit
      - run: npm run test:e2e
      - run: npm run test:a11y

  backend-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
      - run: dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --collect:"XPlat Code Coverage"
      - run: dotnet stryker  # Mutation testing

  ai-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-python@v4
      - run: pip install -r requirements.txt
      - run: pytest tests/ --cov=neuron
```

---

## Summary

| Layer | Unit | Integration | E2E/API | Performance | Coverage Tool |
|-------|------|-------------|---------|-------------|---------------|
| **Frontend** | Vitest | Vitest + MSW | Playwright (browser E2E) | Lighthouse | Vitest |
| **Backend** | xUnit | xUnit + WebAppFactory | Bruno CLI / curl (API) | k6 | Coverlet |
| **AI/Neuron** | pytest | pytest + FastAPI | pytest (real API) | pytest-benchmark | pytest-cov |

**Cross-Cutting:** Pact (contracts), OWASP ZAP (security), k6 (load), Trivy (vulnerabilities)

**Notes:**
- Playwright is for browser-based E2E tests only. For API testing, use Bruno CLI, curl, or in-code integration tests (xUnit + WebApplicationFactory).
- **All tools are 100% free and open source.** See `TESTING-TOOLS-LICENSES.md` for license verification.

---

**Next Steps:**
1. Set up testing infrastructure (Testcontainers, MSW, Playwright)
2. Create test templates and examples
3. Configure CI/CD pipelines
4. Define coverage thresholds
5. Train team on testing patterns
