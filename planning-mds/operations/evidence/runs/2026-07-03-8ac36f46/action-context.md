# Action Context

DEFECT_SUMMARY=Service Cases page shows unable-to-load error

OBSERVED_BEHAVIOR=The frontend `/service-cases` page renders "Unable to load service cases."

EXPECTED_BEHAVIOR=The frontend `/service-cases` page should retrieve service cases from the local backend and render either the list or the defined empty state.

REPRO_STEPS=Open `http://localhost:5173/service-cases` after logging in through local dev auth; observe the Service Cases page error panel.

AFFECTED_PATHS=[
  `experience/src/pages/ServiceCasesPage.tsx`,
  `experience/src/features/service-cases/hooks.ts`,
  backend service-case API surface if triage shows the API is failing
]

AGENT_ROLES=[architect, frontend-developer]

FEATURE_REFS=[F0024]

ALLOW_FEATURE_PROPOSAL=false

Lifecycle Authority=none
