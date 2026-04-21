# Publication Profile — helloinsurance.dev

This file encodes the voice, domain, audience, and channel configuration for the helloinsurance.dev blog.
It overrides generic defaults in `SKILL.md` when present.

Forks of this repository should replace this file with their own publication profile or delete it to use the generic agent defaults.

---

## Publication Identity

- **Primary platform**: Substack — https://helloinsurance.substack.com
- **Brand**: helloinsurance.dev
- **Mission**: Share ideas, architectures, and working software that inspire innovation in the insurance sector — from underwriting and claims to care management and compliance.
- **Section tag**: Posts may fall under "Side quests" (experiments, explorations) or the main feed (core technical content).

---

## Domain

Every post is grounded in the insurance and InsurTech space.

Technology — LLMs, multi-agent systems, RAG, calculation engines, authentication, access control — is always introduced in relation to a real insurance or care management use case:
- Underwriting workbenches and premium calculation
- Claims intake and adjudication
- Care management and patient care plans
- Policy servicing and access control
- Agentic workflows in regulated environments

If a technical topic has no direct insurance application in the post, name the bridge explicitly: "Here is how this pattern applies to underwriting" or "In a claims context, this would look like..."

---

## Voice

**Style**: Builder's journal. First-person. Learning in public.

**Positioning**: Experienced practitioner writing from curiosity, not authority. The author has presented publicly and spoken as a guest in industry settings, and brings genuine depth — but deliberately writes from the learner's stance rather than the expert's podium. This is an intentional editorial choice: it keeps the writing accessible, honest, and inviting rather than declarative. Do not soften this into novice uncertainty, and do not inflate it into expert authority. The right register is: "I've built this, here's what I learned, here's what I still don't know."

**Pronouns**: "I built", "I introduced", "We reset the project at least 5 times." Use "we" when referring to the team or collaborative decisions; use "I" for personal experience and reflection.

**Tone markers**:
- Humble and curious — "I'm humbled at every step"
- Honest about failure — "What started as X quickly showed its limits"
- Admits uncertainty without apology — "I became less confident the more I learned"
- Celebrates small wins without overselling — "Even with basic support, it unlocked new use cases"
- Asks the reader into the thinking — "What happens when we give LLMs not just memory but also a perspective?"
- Shares conviction without claiming finality — "This is what worked for us. Your context may differ."

**What to avoid**:
- Corporate-neutral or marketing language ("leveraging cutting-edge AI solutions")
- Passive voice used to obscure who made a decision
- Overclaiming ("this changes everything", "I'm an expert in...")
- Defensive hedging ("this may or may not work depending on your context") — state what happened honestly instead
- Novice framing that undersells genuine depth ("I'm just learning this, but...")
- Authoritative declarations that close down the reader's own thinking ("the right way to do this is...")

---

## Audience

**Primary reader**: A practitioner at the intersection of technology and insurance.
- InsurTech engineers building underwriting, claims, or servicing platforms
- Insurance domain professionals curious about how modern software and AI can change their workflows
- Engineers applying AI, LLMs, or distributed systems patterns to regulated industries
- Builders who want to see real decisions and real code, not theoretical frameworks

**Assumed knowledge**:
- Comfortable with software concepts (APIs, services, databases)
- Comfortable with AI/LLM patterns across enterprise, hobby, and personal contexts — deeply curious, practically grounded, knowledgeable enough to have opinions, and humble enough to still be learning.
- Has domain context in insurance or is willing to learn it via analogy

**Not written for**:
- Pure academics
- Enterprise sales audiences
- Readers who need every term defined from scratch

---

## Hook Patterns (this publication)

Preferred opening styles in order of fit:

1. **Provocative question** — Ask the question the post answers. Make it feel personal or builder-specific.
   > "What if you could simulate a multi-agent discussion thread… but powered by LLMs?"

2. **Honest reflection** — Open with a moment of doubt, discovery, or surprise from the build.
   > "The more I worked on it, the less confident I became in what I thought I knew."

3. **Quote + personal pivot** — Use a short external quote, then immediately make it personal.
   > "The more I learn, the more I realize how much I don't know." — Albert Einstein
   > [Then: here is what that felt like during this build.]

4. **Counterintuitive claim** — State something that sounds wrong, earn it in the post.
   > "Simple is not better by default. Complexity takes the entire space given to it."

---

## Formatting Conventions

**Emojis**: Used as visual anchors for section headers and key concepts — not decorative.
- Place the emoji before or after the section heading, not mid-sentence.
- Examples from past posts: ⚖️ for tradeoffs, 📐 for architecture, 🧩 for abstractions, ♻️ for patterns, 🤝 for collaboration, ❓ for open questions, 🚀 for launches or new series.
- Use sparingly — one per major section heading, zero in body text.

**Paragraph length**: Short to medium. Two to four sentences is the target. Long paragraphs are broken up unless they carry a single dense technical explanation.

**Lists**: Use for genuinely enumerable items (features added, options compared, questions raised). Avoid converting narrative into bullets to avoid writing prose.

**Code blocks**: Show real code or config from the project. Annotate with comments if a line needs explanation. Do not sanitize examples into abstract pseudocode unless the real code is sensitive.

---

## Series Conventions

Active series on this publication:

- **Foundations series** (101 / 201 / 301): Educational deep dives on a domain topic (Security, Access Control, etc.). Each level assumes the prior. Use the format: `[Topic] – [Level]` in the title.
- **Choices We Made** (Architecture, Part N): Documents architectural decisions and the reasoning behind them. Each part covers one major decision area.
- **Demo series**: Shows a feature or system working, extended in subsequent posts. Use the format: `[System Name] Demo: [What This Post Shows]`.
- **Side quests**: One-off explorations and experiments that don't fit an active series. Tagged "Side quests" in Substack.

When writing a new post: check whether it belongs to an existing series or warrants starting a new one. Always note the series label in the editorial brief.

---

## CTA Patterns

End every primary post with a call to action appropriate to the post type:

- **Deep dive / tutorial**: Invite readers to try the approach and share what they find. Link to the repo or relevant planning doc if public.
- **Series post**: Preview the next part specifically — name what it will cover, not just "stay tuned."
- **Retrospective**: Invite readers to share how they handled a similar challenge.
- **Demo post**: Link to where readers can access or try the demo.

---

## Active Amplification Channels

### LinkedIn
- **Audience**: Insurance professionals, InsurTech founders, engineering leads
- **Tone**: Professional but personal. Skip the code. Lead with the human decision or insight.
- **Length**: 150–250 words
- **CTA**: "Full post on Substack — link in comments" (LinkedIn suppresses links in body)
- **Format note**: Use line breaks between short paragraphs. No markdown headers. One emoji per post maximum.

### Reddit
- **Target communities**: r/MachineLearning, r/softwarearchitecture, r/programming, r/insurance (when relevant)
- **Tone**: Peer-to-peer. Community member sharing something they built, not promoting a post.
- **Length**: 200–350 words
- **CTA**: Share the link naturally in the body. Never lead with "I wrote a blog post."
- **Format note**: Lead with the problem or question, then the approach. The post should stand on its own even without clicking the link.

### dev.to
- **Audience**: Developers, engineers, technical practitioners
- **Tone**: Technical-first. Code is welcome. Practitioner to practitioner.
- **Length**: 700–1000 words (condensed technical version of the primary)
- **CTA**: Set canonical URL to the Substack primary post. Note at top: "Originally published at helloinsurance.substack.com"
- **Format note**: Use markdown headers. Include one or two code snippets if they add value.

### Bluesky / X-Twitter Thread

Both platforms use the same thread structure. Apply the platform-specific notes below when producing each.

**Shared thread structure** (6–8 posts):
- Post 1: Hook — question, claim, or surprising fact from the post. End with 🧵
- Posts 2–6: One key insight, decision, or step per post
- Post 7: Takeaway or lesson
- Post 8: CTA + primary-post pointer, with link placement handled per platform rules below

Number each post (1/8, 2/8, etc.). One clear idea per post. No filler.

**Bluesky**
- **Audience**: Skews technical and developer — closest to the InsurTech builder reader. Genuine sharing over broadcasting.
- **Tone**: Conversational and curious. The hook can be a real question or honest reflection. Provocative for its own sake feels off here.
- **Character limit**: 300 per post
- **Links**: Link-friendly — put the Substack link directly in Post 8 body.
- **Hashtags**: Skip or use one max. Custom feeds matter more than hashtags on Bluesky.

**X / Twitter**
- **Audience**: Broader reach, noisier feed. Needs a sharper hook to cut through.
- **Tone**: Punchy. The opening post should create immediate tension or curiosity.
- **Character limit**: 280 per post
- **Links**: X suppresses reach when links appear in the post body — put the Substack link in a reply to Post 8, not in the post itself.
- **Hashtags**: 1–2 relevant hashtags on the final post (e.g. #InsurTech #LLM).
