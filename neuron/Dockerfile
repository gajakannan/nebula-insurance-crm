# Neuron Companion — stateless FastAPI runtime (F0038-S0008 deployability slice).
# Small by design (ADR-027): a thin orchestration service, no heavy agent frameworks.
FROM python:3.12-slim AS runtime

# Non-root runtime user (least privilege).
RUN useradd --create-home --uid 10001 neuron

WORKDIR /app
# Copy the source tree, then install the runtime deps declared in pyproject. Editable
# install keeps the orchestration assets (crm_agents/cards, orchestration/plans,
# app/contracts) resolvable from source via NEURON_ROOT at runtime.
COPY . ./
RUN pip install --no-cache-dir -e .

ENV NEURON_ENV=production \
    PYTHONUNBUFFERED=1
EXPOSE 8200
USER neuron

# Liveness: /health always 200 once the validated runtime is built (fail-fast at startup).
HEALTHCHECK --interval=10s --timeout=3s --start-period=20s --retries=5 \
  CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8200/health', timeout=2).read()"

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8200"]
