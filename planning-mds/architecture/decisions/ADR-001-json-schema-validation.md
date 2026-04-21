# ADR-001: Use JSON Schema for Cross-Tier Validation

**Status:** Accepted

**Date:** 2026-02-01

**Deciders:** Architecture Team

**Context:**

We need a validation strategy that ensures consistency between our TypeScript frontend and C# backend. The application has the following requirements:

1. **User Experience:** Users need immediate validation feedback in forms (frontend validation)
2. **Security:** Server must validate all incoming requests regardless of client validation (backend validation)
3. **Consistency:** Validation rules must be identical on frontend and backend to avoid confusion
4. **Maintainability:** Changes to validation rules should not require updating multiple codebases
5. **Type Safety:** We want compile-time type checking in both TypeScript and C#
6. **API Documentation:** API contracts (OpenAPI) should include validation rules

### Problem Statement

How do we maintain consistent validation rules across TypeScript frontend and C# backend without duplicating logic?

### Considered Options

**Option 1: Duplicate Validation Logic**
- Write validation in both TypeScript (frontend) and C# (backend) separately
- ✅ Native to each language
- ❌ Duplication and drift risk
- ❌ High maintenance burden
- ❌ Inconsistent error messages

**Option 2: TypeScript-Only Validation Library**
- Use a TypeScript-only schema library for frontend validation, rely on backend model validation
- ✅ Excellent TypeScript integration
- ✅ Type inference
- ❌ TypeScript-specific (can't share with C# backend)
- ❌ Backend validation still needed (duplication)
- ❌ Schemas can't be shared with OpenAPI

**Option 3: JSON Schema Validation (Selected)**
- Define validation schemas in JSON Schema format
- Frontend validates with AJV or RJSF
- Backend validates with NJsonSchema
- ✅ Language-agnostic (works with TypeScript, C#, Python)
- ✅ Single source of truth
- ✅ OpenAPI 3.x uses JSON Schema natively
- ✅ Type generation for both TypeScript and C#
- ✅ Industry standard
- ❌ Additional tooling required
- ❌ Learning curve for JSON Schema syntax

**Option 4: Backend-Driven Validation**
- Backend exposes validation rules via API
- Frontend queries validation rules dynamically
- ✅ True single source
- ❌ Network latency for validation
- ❌ Complex implementation
- ❌ Poor offline experience

## Decision

We will use **JSON Schema as the single source of truth for validation rules**, shared between frontend (TypeScript) and backend (C#).

### Implementation Details

**1. Schema Location:**
```
planning-mds/schemas/
├── broker.schema.json
├── account.schema.json
├── submission.schema.json
└── ...
```

All JSON Schemas are stored in `planning-mds/schemas/` and version-controlled alongside application code.

**2. Frontend Validation:**
- **Manual forms:** React Hook Form + AJV resolver (`@hookform/resolvers/ajv`)
- **Dynamic forms:** RJSF (React JSON Schema Form) with built-in validation
- **Library:** AJV (Another JSON Validator) - industry standard for JavaScript

**3. Backend Validation:**
- **Validation point:** API endpoints (before domain logic)
- **Library:** NJsonSchema for .NET
- **Error format:** RFC 7807 ProblemDetails

**4. OpenAPI Integration:**
- Reuse JSON Schemas in OpenAPI `components/schemas` section
- Single schema definition serves validation, documentation, and code generation

**5. Type Generation:**
- **Frontend:** `json-schema-to-typescript` generates TypeScript interfaces
- **Backend:** NJsonSchema generates C# classes
- Types stay in sync with schemas automatically

### Example Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://nebula-crm.com/schemas/broker.json",
  "title": "Broker",
  "description": "Broker entity for CRM",
  "type": "object",
  "properties": {
    "name": {
      "type": "string",
      "minLength": 1,
      "maxLength": 100,
      "description": "Broker or agency name"
    },
    "email": {
      "type": "string",
      "format": "email",
      "description": "Primary contact email"
    },
    "phone": {
      "type": "string",
      "pattern": "^\\d{10}$",
      "description": "10-digit phone number"
    },
    "status": {
      "type": "string",
      "enum": ["Active", "Inactive"],
      "description": "Broker status"
    }
  },
  "required": ["name", "email", "status"],
  "additionalProperties": false,
  "errorMessage": {
    "properties": {
      "name": "Name is required and must be at most 100 characters",
      "email": "Invalid email address",
      "phone": "Phone must be 10 digits"
    }
  }
}
```

### Frontend Implementation

```typescript
// Load schema
import brokerSchema from '@/schemas/broker.schema.json';
import { ajvResolver } from '@hookform/resolvers/ajv';
import { useForm } from 'react-hook-form';

// Use with React Hook Form
const { register, handleSubmit } = useForm({
  resolver: ajvResolver(brokerSchema)
});
```

### Backend Implementation

```csharp
// Load schema
var schema = await JsonSchema.FromFileAsync(
    "planning-mds/schemas/broker.schema.json");

// Validate request
var validator = new JsonSchemaValidator();
var errors = validator.Validate(requestJson, schema);

if (errors.Count > 0)
{
    return Results.ValidationProblem(
        errors.ToDictionary(e => e.Path, e => new[] { e.ToString() }));
}
```

## Consequences

### Positive

✅ **Single Source of Truth**
- Validation rules defined once in JSON Schema
- Guaranteed consistency between frontend and backend
- No risk of drift

✅ **Type Safety**
- TypeScript types generated from schemas
- C# classes generated from schemas
- Compile-time errors if code doesn't match schema

✅ **OpenAPI Integration**
- OpenAPI 3.x natively uses JSON Schema
- API documentation includes validation rules automatically
- Tools like Swagger UI show validation constraints

✅ **Developer Experience**
- Change schema once, frontend and backend update
- Clear error messages defined in schema
- Less code to write and maintain

✅ **Industry Standard**
- JSON Schema is widely adopted
- Large ecosystem of tools and libraries
- Future-proof (not tied to specific framework)

✅ **Multi-Language Support**
- Works with TypeScript, C#, Python (neuron/ AI layer)
- Easy to add new languages in the future

### Negative

❌ **Learning Curve**
- Developers need to learn JSON Schema syntax
- Not as intuitive as native language validation
- **Mitigation:** Provide templates and examples, training session

❌ **Additional Tooling**
- Requires AJV (frontend), NJsonSchema (backend)
- Build step for type generation
- **Mitigation:** Integrate into build pipeline, document setup

❌ **JSON Schema Limitations**
- Some complex validations harder to express
- Cross-field validation requires custom keywords
- **Mitigation:** Use custom validation in application layer for complex cases

❌ **Error Message Customization**
- Default error messages are technical
- Requires `ajv-errors` or custom formatting
- **Mitigation:** Use `errorMessage` keyword in schemas

### Neutral

⚠️ **Schema Versioning**
- Need strategy for schema evolution (breaking vs non-breaking changes)
- **Plan:** Use semantic versioning for schemas, maintain backwards compatibility

⚠️ **Performance**
- JSON Schema validation adds overhead
- **Impact:** Negligible for most use cases (<1ms per validation)

## Implementation Plan

### Phase 1: Setup (Week 1)
- [x] Create `planning-mds/schemas/` directory structure
- [ ] Install AJV and @hookform/resolvers/ajv in frontend
- [ ] Install NJsonSchema in backend
- [ ] Set up type generation scripts
- [ ] Add validation to CI/CD pipeline

### Phase 2: Core Entities (Week 2-3)
- [ ] Create JSON Schemas for Broker, Account, Contact
- [ ] Implement frontend validation (AJV)
- [ ] Implement backend validation (NJsonSchema)
- [ ] Update OpenAPI specs to reference schemas
- [ ] Write unit tests for validation

### Phase 3: Workflow Entities (Week 4)
- [ ] Create JSON Schemas for Submission, Renewal
- [ ] Implement validation for complex workflows
- [ ] Document custom validation patterns
- [ ] Create developer guide

### Phase 4: Rollout (Week 5)
- [ ] Developer training on JSON Schema
- [ ] Code review checklist updated
- [ ] Monitoring for validation errors
- [ ] Retrospective and adjustments

## References

- [JSON Schema Specification](https://json-schema.org/)
- [AJV Documentation](https://ajv.js.org/)
- [NJsonSchema Documentation](https://github.com/RicoSuter/NJsonSchema)
- [OpenAPI 3.0 Specification](https://swagger.io/specification/)
- [RFC 7807: Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc7807)

## Related Decisions

- **ADR-002:** OpenAPI as API Contract Standard (depends on this)
- **ADR-003:** Frontend Form Strategy (React Hook Form + AJV vs RJSF)
- **ADR-004:** Error Response Format (RFC 7807 ProblemDetails)

## Approval

- [x] Architecture Team
- [x] Frontend Lead
- [x] Backend Lead
- [x] QA Lead

---

**Last Updated:** 2026-02-01

**Supersedes:** None

**Superseded By:** None (current)
