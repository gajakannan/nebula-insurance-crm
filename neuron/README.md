# Neuron - AI Intelligence Layer

**The Brain of Nebula CRM**

This directory contains the AI intelligence layer - models, agentic workflows, MCP servers, and intelligent automation.

## Purpose

Neuron is the cognitive layer that powers intelligent features in Nebula CRM through:
- LLM model integrations (Claude, local models)
- Agentic workflows and orchestration
- Model Context Protocol (MCP) servers
- AI-powered automation and decision support

## Current Status

`neuron/` is currently scaffold-only. The application implementation has not started yet.

## Directory Structure

Target scaffold (planned structure for implementation; some files are not created yet):

```
neuron/
├── mcp/              # MCP (Model Context Protocol) servers
│   ├── server.py     # Main MCP server implementation
│   └── tools/        # MCP tool definitions
│
├── CRM agent definitions
│   ├── underwriter/  # Underwriting decision support agent
│   ├── broker/       # Broker interaction agent
│   └── renewal/      # Renewal processing agent
│
├── models/           # LLM model configurations
│   ├── claude.py     # Claude API integration
│   ├── ollama.py     # Local model integration (Ollama)
│   └── router.py     # Model routing logic
│
├── workflows/        # Agentic workflows
│   ├── submission/   # Submission processing workflows
│   ├── renewal/      # Renewal workflows
│   └── orchestrator/ # Workflow orchestration
│
├── prompts/          # Prompt templates
│   ├── system/       # System prompts
│   ├── tasks/        # Task-specific prompts
│   └── few-shot/     # Few-shot examples
│
├── tools/            # Agent tools and capabilities
│   ├── data_tools/   # Data access tools
│   ├── api_tools/    # API interaction tools
│   └── analysis/     # Analysis and reasoning tools
│
└── config/           # Configuration
    ├── models.yaml   # Model configurations
    ├── agents.yaml   # Agent configurations
    └── mcp.yaml      # MCP server configuration
```

## Technology Stack

- **Python 3.11+** - Primary language
- **LangChain / LlamaIndex** - Agent frameworks (TBD based on needs)
- **Anthropic SDK** - Claude API integration
- **Ollama** - Local model deployment
- **MCP SDK** - Model Context Protocol implementation
- **FastAPI** - API server for MCP
- **Temporal** - Workflow orchestration (if long-running)

## AI Engineer Responsibilities

The **AI Engineer** role (defined in the external agent framework) is responsible for:
1. Integrating LLM models (Claude API, Ollama)
2. Building agentic workflows
3. Implementing MCP servers
4. Configuring agent behaviors and prompts
5. Managing model deployments and routing
6. Implementing agent-to-agent communication

## Getting Started

### Prerequisites
```bash
# Python 3.11+
python --version

# Dependency setup will be added with the first runnable neuron implementation.
# For now, there is no pinned requirements file in this directory.
```

### Running MCP Server
```bash
# Not implemented yet.
# First implementation target:
#   neuron/mcp/server.py
```

### Configuration
When implementation begins, create:
- `neuron/config/models.yaml`
- `neuron/config/agents.yaml`
- `neuron/config/mcp.yaml`

Keep secrets in environment variables and never commit API keys.

## Key Concepts

### Agentic Workflows
Workflows that use LLMs to make decisions, process information, and take actions:
- **Underwriting Assistant** - Analyze submissions, suggest pricing
- **Renewal Intelligence** - Predict renewal likelihood, suggest outreach
- **Broker Insights** - Analyze broker patterns, performance

### MCP (Model Context Protocol)
Standardized protocol for connecting LLMs to data sources and tools:
- Expose CRM data to agents
- Provide agent tools (search, analyze, update)
- Enable agent-to-agent communication

### Model Routing
Intelligently route requests to appropriate models:
- Simple tasks → Haiku (fast, cheap)
- Complex reasoning → Opus (deep analysis)
- Local tasks → Ollama (privacy, cost)

## Development Workflow

1. **Define Agent** in the CRM agent definitions package
2. **Create Prompts** in `prompts/`
3. **Implement Tools** in `tools/`
4. **Configure Model** in `config/models.yaml`
5. **Build Workflow** in `workflows/`
6. **Test** with unit tests
7. **Deploy** MCP server

## Integration with Main Application

```
┌─────────────────────────────────────────┐
│  Nebula CRM Application (C# .NET)      │
│  (engine/)                              │
└─────────────────┬───────────────────────┘
                  │
                  │ HTTP/gRPC
                  ↓
┌─────────────────────────────────────────┐
│  Neuron - AI Intelligence Layer         │
│  (neuron/)                              │
│                                         │
│  ├─ MCP Server (FastAPI)                │
│  ├─ Agent Orchestrator                  │
│  ├─ Model Router                        │
│  └─ CRM Agents                          │
└─────────────────┬───────────────────────┘
                  │
                  │ API Calls
                  ↓
┌─────────────────────────────────────────┐
│  LLM Models                             │
│  - Claude (Anthropic API)               │
│  - Local Models (Ollama)                │
└─────────────────────────────────────────┘
```

## Security Considerations

- **API Keys** - Never commit API keys; use environment variables
- **Rate Limiting** - Implement rate limits on MCP endpoints
- **Input Validation** - Validate all inputs before sending to LLMs
- **Output Sanitization** - Sanitize LLM outputs before using in application
- **Access Control** - Secure MCP endpoints with authentication
- **Audit Logging** - Log all agent actions and decisions

## Performance Optimization

- **Caching** - Cache frequent prompts and responses
- **Streaming** - Use streaming for long responses
- **Batching** - Batch similar requests
- **Model Selection** - Use appropriate model for task complexity
- **Local Models** - Use Ollama for privacy-sensitive or high-volume tasks

## Testing

```bash
# Unit tests
pytest neuron/tests/

# Integration tests
pytest neuron/tests/integration/

# Agent behavior tests
pytest neuron/tests/agents/
```

## Monitoring

- **Model Usage** - Track token usage and costs
- **Latency** - Monitor response times
- **Error Rates** - Track failures and retries
- **Agent Performance** - Measure agent accuracy and effectiveness

## Future Enhancements

- [ ] Multi-agent collaboration
- [ ] RAG (Retrieval Augmented Generation) for CRM data
- [ ] Fine-tuned models for specific insurance tasks
- [ ] Agent memory and learning
- [ ] Automated workflow optimization

---

**Neuron** is where intelligence lives. The AI Engineer agent (from the builder framework) implements everything here.
