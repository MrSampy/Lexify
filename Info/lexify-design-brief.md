# Lexify — Design Brief

> This document describes all screens, user flows, UI components, and the design system needed to create a correct design for the application.

---

## Table of Contents

1. [Product Concept](#1-product-concept)
2. [User Roles](#2-user-roles)
3. [UI Tech Stack](#3-ui-tech-stack)
4. [Design System](#4-design-system)
5. [Screen Inventory](#5-screen-inventory)
6. [Screen Details](#6-screen-details)
7. [Core User Flows](#7-core-user-flows)
8. [Component Inventory](#8-component-inventory)
9. [Critical UI States](#9-critical-ui-states)
10. [Admin Panel](#10-admin-panel)
11. [Appendix: API Surface](#11-appendix-api-surface)

---

## 1. Product Concept

**Lexify** is a web application for self-directed foreign language vocabulary learning. The user pastes a raw word list → AI automatically structures it → quizzes are generated → spaced repetition drills the words to fluency.

### Core Value Propositions

| Value | Description |
|---|---|
| **Zero friction** | Paste raw text — AI parses, translates, and structures everything |
| **Smart repetition** | SM-2 algorithm: only review words that are actually due |
| **Varied testing** | 4 question types, LLM-generated per block |
| **Progress through stats** | Dashboard with metrics, test history |

### Target Audience

- Students learning foreign languages
- Self-learners (English, Norwegian, Ukrainian, etc.)
- Users maintaining personal vocabularies
- Teachers and moderators (via admin panel)

---

## 2. User Roles

### User (regular)

- Creates word blocks, imports via AI or manually
- Takes AI-generated quizzes
- Runs daily spaced-repetition review sessions (SM-2)
- **Sees only their own data** — isolation enforced via JWT + Global Query Filter at the DB level

### Admin

- Views all users, can suspend accounts
- Monitors AI provider calls (Ollama / OpenAI)
- Manages system settings and language list
- Views the audit log
- **Separate layout** with a sidebar, visually distinct from the user zone

---

## 3. UI Tech Stack

| Technology | Purpose |
|---|---|
| React 19 + TypeScript | Component architecture, strict typing |
| Vite | Bundler, dev server on port 5173 |
| Feature-Sliced Design (FSD) | Architecture: `app → pages → widgets → features → entities → shared` |
| shadcn/ui | Pre-built components: Button, Input, Dialog, Table, Badge, Select, Checkbox |
| Tailwind CSS | Utility-first styling, theming via CSS variables |
| Zustand | Client state (auth, import flow, test runner) |
| TanStack Query v5 | Server state, caching, mutation-driven invalidation |
| React Router v6 | Routing, route guards (AuthGuard, AdminGuard) |
| Lucide React | Icons (16px in buttons, 20px standalone) |

---

## 4. Design System

### 4.1 Color Palette

The application uses **dark mode** as its primary theme (shadcn/ui dark).

| Token | Hex | Usage |
|---|---|---|
| `background` | `#0a0a0a` | Page background |
| `card` | `#111111` | Card backgrounds |
| `border` | `#222222` | Element borders |
| `foreground` | `#ededed` | Primary text |
| `muted-foreground` | `#71717a` | Secondary text, captions |
| `primary` | `#60a5fa` (blue-400) | Primary CTA buttons, links |
| `success` | `#4ade80` (green-400) | Correct answer, success states |
| `destructive` | `#f87171` (red-400) | Delete, errors, wrong answer |
| `warning` | `#fb923c` (orange-400) | Confidence flag, warnings |
| `admin-accent` | `#fb923c` (orange-400) | Accent color for the admin zone |

### 4.2 Typography

| Level | Tailwind | Usage |
|---|---|---|
| Page title | `text-2xl font-bold` | Top-level page heading |
| Section heading | `text-sm font-semibold uppercase tracking-wider text-muted-foreground` | Section labels |
| Card title | `text-sm font-semibold` | Card headings |
| Body | `text-sm` | Main content text |
| Caption | `text-xs text-muted-foreground` | Meta info, timestamps |
| Mono | `font-mono text-xs` | Language codes, technical data |

### 4.3 Installed shadcn/ui Components

`Button` · `Input` · `Textarea` · `Badge` · `Dialog` · `Table` · `Select` · `Checkbox` · `Sonner (Toast)`

### 4.4 Word Type Badge System

A key visual language of the app — color-coded badges for word types:

| Type | Background | Text Color | Meaning |
|---|---|---|---|
| `Word` | `#1e3a5f` | `#60a5fa` | Regular vocabulary word |
| `Phrase` | `#2e1065` | `#c084fc` | Multi-word phrase |
| `Expression` | `#431407` | `#fb923c` | Fixed expression |
| `Idiom` | `#14532d` | `#4ade80` | Idiomatic expression |
| `⚠ Confidence Flag` | `#2a1a1a` | `#f87171` | Word marked for extra attention |

### 4.5 Layout Rules

**User layout:**
- Sticky header (`z-50`, `backdrop-blur`)
- Content width: `max-w-6xl mx-auto px-4`
- No sidebar — minimal navigation (logo + search + sign out)

**Admin layout:**
- Fixed left sidebar (`AdminNav`) + main content area
- Admin zone uses a visually distinct accent color (orange vs blue)

**Cards:** no `box-shadow`, only `border` + `border-radius: 8px`. Background `#111`.

---

## 5. Screen Inventory

### Public (no auth required)

| Route | Screen |
|---|---|
| `/login` | Sign in |
| `/register` | Create account |

### User (requires authentication)

| Route | Screen |
|---|---|
| `/` | Dashboard |
| `/blocks` | Word block list |
| `/blocks/:id` | Block detail (word table) |
| `/blocks/:id/import` | AI word import |
| `/tests` | Test list |
| `/tests/new` | Create new test |
| `/tests/:id/run` | Test runner |
| `/tests/:id/results` | Test results |
| `/review` | Spaced repetition session |
| `/search` | Word search |

### Admin (requires Admin role)

| Route | Screen |
|---|---|
| `/admin` | Admin dashboard |
| `/admin/users` | User management |
| `/admin/ai` | AI monitor |
| `/admin/settings` | System settings |
| `/admin/languages` | Language management |
| `/admin/audit` | Audit log |

---

## 6. Screen Details

### 6.1 `/login` — Sign In

**Purpose:** Authenticate the user.

**Elements:**
- Lexify logo
- Heading "Sign in to Lexify"
- Subheading with "Create one" link
- Email field (with inline validation)
- Password field (with inline validation)
- "Sign in" button

**States:**
- `idle` — empty form
- `loading` — button disabled, spinner
- `validation error` — red text below the field
- `server error` — toast "Invalid email or password"

**Notes:**
- Full-screen layout with no header or footer
- On success → redirect to `/`
- Centered card on a dark background

---

### 6.2 `/register` — Create Account

**Purpose:** Register a new user account.

**Elements:**
- Display Name (optional)
- Email, Password
- "Register" button
- "Already have an account? Sign in" link

**States:**
- `idle` / `loading` / `validation error` / `success → redirect /login`

**Notes:**
- After registration: no auto-login; redirect to `/login` with a success message

---

### 6.3 `/` — Dashboard

**Purpose:** Welcome screen with quick access to key sections and current stats.

**Elements:**
- Personalized greeting with the user's display name
- **ReviewDueBanner** — shown at the top when words are due for review today
- 2 large CTA cards: "Blocks" and "Tests"
- 4 stat cards: `Blocks` / `Words total` / `Answers this week` / `Tests this week`
- "Recent Blocks" section — last 3–5 blocks
- "Recent Tests" section — last 3–5 tests

**States:**
- `loading` — skeleton placeholders in place of cards
- `empty` (new user) — onboarding CTA "Create your first block"
- `filled` — standard view

---

### 6.4 `/blocks` — Word Block List

**Purpose:** Browse and manage word collections.

**Elements:**
- "Filter by tag..." input
- Language dropdown (`all` / `en` / `uk` / `no` / ...)
- List of `BlockCard` components
- "Import CSV" and "+ New Block" buttons

**BlockCard contains:**
- Block title
- `LanguageBadge` (EN / UK / NO / ...)
- Word count
- Click → navigate to `/blocks/:id`

**States:**
- `loading` / `empty` (prompt to create the first block) / `filled` / `filtered`

---

### 6.5 `/blocks/:id` — Block Detail

**Purpose:** View and edit the words in a specific block.

**Elements:**
- "← Back to blocks" link
- Title + `LanguageBadge` + word count
- "Export CSV" and "Delete block" buttons (destructive — requires confirm)
- Tag input (add/remove tags)
- "Confidence flagged only" checkbox
- "AI Import" and "+ Add word" buttons
- Words table

**Words table columns:**

| Term | Translation | Type | Notes | Action |
|---|---|---|---|---|
| suspense | tension | `Word` | irregular verb | Delete |
| on-demand | on request | `Expression` | fixed expression | Delete |

**Notes:**
- Rows with `confidenceFlag = true` — highlighted with a warning color (e.g. orange left border)
- "Delete block" — only if 0 words, otherwise show a confirm warning

---

### 6.6 `/blocks/:id/import` — AI Word Import

**Purpose:** Core flow — paste raw text → AI formatting → save to block.

**Steps (wizard):**

```
Step 1: Input  →  Step 2: Formatting (SSE)  →  Step 3: Preview  →  Step 4: Saving  →  Done
```

**Step 1 — Input:**
- Large textarea for raw text
- "Target language" dropdown (the language being learned)
- "Native language" dropdown (translation language)
- "Format" button

**Step 2 — Formatting (SSE streaming):**
- Progress indicator with three phases:
  - `parsing` → "Analyzing..."
  - `streaming` → animated progress bar + current streaming chunk
  - `done` → advance to Step 3
- AI error → inline banner with "Try again" button (input text is not lost)

**Step 3 — Preview (editable table):**
- Columns: Term / Translation / Type (select) / Confidence Flag / Notes
- Rows with `confidenceFlag = true` — highlighted in orange
- Block title field (AI suggests a title, user can edit)
- "Save" button

---

### 6.7 `/tests` — Test List

**Purpose:** Overview of created tests, run or delete them.

**Elements:**
- Status filter dropdown (`all` / `generating` / `ready` / `archived`)
- List of test cards
- "+ New test" button

**Test card contains:**
- Test title
- Status badge: `Generating` (with spinner) / `Ready` / `Archived`
- Question count + creation date
- "Run" button if `Ready`, "Delete" always visible

**Notes:**
- `Generating` — show spinner, poll status every 2 seconds
- `Archived` — test is completed; user can only view past results

---

### 6.8 `/tests/new` — Create Test

**Purpose:** Select blocks and parameters to generate a quiz via AI.

**Elements:**
- `BlockSelector` — list of blocks with checkboxes (showing word count per block)
- `QuestionTypeSelector` — question type checkboxes:
  - TranslateToNative — translate to your native language
  - TranslateToForeign — translate to the target language
  - FillInSentence — fill in the missing word in a sentence
  - MultiSelectTheme — select all words matching a theme
  - OpenAnswer — type the answer manually
- "Generate" button

**States:**
- Warning shown if fewer than 5 words are selected
- On submit → redirect to `/tests` with polling for the new test

---

### 6.9 `/tests/:id/run` — Test Runner

**Purpose:** Answer test questions one at a time.

**Elements:**
- `TestProgressBar` — "Question N of M"
- Question text + word context (which block it came from)
- Answer component (depends on question type — see below)
- "Submit" / "Next" button
- `AnswerFeedback` after each answer

**Question types:**

| Type | UI Component | Description |
|---|---|---|
| `SingleChoice` | 4 option buttons | Pick one correct answer |
| `MultiSelect` | Checkboxes | Select all correct answers |
| `FillInBlank` | Input + sentence context | Type the missing word |
| `OpenAnswer` | Free-text input | Type the translation (checked via Levenshtein distance) |

**AnswerFeedback:**
- Correct → green highlight + ✓
- Wrong → red highlight + show correct answer + word's `notes`

---

### 6.10 `/tests/:id/results` — Test Results

**Purpose:** Final screen showing the score and a breakdown of answers.

**Elements:**
- Overall result: `correct / total` + percentage
- Visual indicator (progress bar or circular gauge)
- Full question list: user's answer vs correct answer
- "Try Again" and "Back to Tests" buttons

**Notes:**
- Score < 60% → encouraging message
- Score ≥ 90% → congratulations message
- After the test, SM-2 intervals are updated server-side for incorrectly answered words; the UI shows the outcome

---

### 6.11 `/review` — Spaced Repetition Session

**Purpose:** Daily SM-2 flashcard session.

**Elements:**
- Counter "N words remaining"
- `ReviewCard` — flashcard front: `term` + language badge
- "Show translation" button (flips the card)
- Card back: `translation` + `notes` + `QualityRater`
- `QualityRater` — 6 buttons rated 0–5:
  - 0 — Complete blackout
  - 1 — Barely remembered
  - 2 — Recalled with effort
  - 3 — Recalled with hesitation
  - 4 — Recalled easily
  - 5 — Perfect, no hesitation

**States:**
- `front` — only term is shown
- `back` — translation + quality rater shown
- `saving` — brief state after rating, before next card
- `no words due` — empty state "No words due for review" + "Go to Blocks" button

**Notes:**
- Card flip animation is an important UX moment
- Each rating triggers a PATCH request that updates `next_review_at` for the word

---

### 6.12 `/search` — Word Search

**Purpose:** Full-text search across all of the user's words.

**Elements:**
- SearchBar in the header (already present)
- Results list grouped by block
- Match highlighting in the result text

**States:**
- `empty query` — hint to start typing
- `loading` — spinner
- `results` — grouped list
- `no results` — "Nothing found"

---

## 7. Core User Flows

### Flow 1 — AI Word Import

```
Navigate to /blocks/:id/import
    ↓
Paste raw text (e.g. "word - translation" per line)
    ↓
Select target language and native language
    ↓
Click "Format"
    ↓
SSE progress: parsing → streaming → done
    ↓  (if AI error → inline banner + retry, text is preserved)
Preview table with AI results
    ↓
Edit if needed (type, translation, block title)
    ↓
Click "Save"
    ↓
Redirect → /blocks/:id
```

**UX critical:** SSE progress must be visually clear — three distinct phases with animation. Words with `confidenceFlag=true` must be immediately highlighted in the Preview.

---

### Flow 2 — Create and Take a Test

```
Click "+ New test" → /tests/new
    ↓
Select blocks (checkboxes) + question types
    ↓
Click "Generate"
    ↓
Redirect → /tests (test status "Generating", polling every 2s)
    ↓
Status changes to "Ready"
    ↓
Click "Run" → /tests/:id/run
    ↓
Answer questions one by one (with per-answer feedback)
    ↓
Finish → /tests/:id/results
    ↓
Review score and answer breakdown
```

**UX critical:** Generation via Hangfire job takes 10–60 seconds. Clear waiting state is required — spinner + status badge polling.

---

### Flow 3 — Daily Spaced Repetition (SM-2)

```
Open /review (or click ReviewDueBanner)
    ↓
Flashcard showing the term
    ↓
Click "Show translation" (flip animation)
    ↓
See translation + notes
    ↓
Rate recall quality: buttons 0–5
    ↓
Next card (or session complete screen)
```

**UX critical:** Rating must be fast and instinctive. Buttons 0–5 should use a color gradient (red → green) so the user can click without reading labels every time.

---

## 8. Component Inventory

### Navigation

| Component | Description |
|---|---|
| `UserLayout` (header) | Sticky top bar: logo, SearchBar, "Sign out" button |
| `AdminNav` (sidebar) | Fixed side menu with admin section links |
| `AuthGuard` | Redirects to `/login` if no access token |
| `AdminGuard` | Redirects if user lacks the Admin role |

### Blocks & Words

| Component | Description |
|---|---|
| `BlockCard` | Block card: title + LanguageBadge + word count |
| `WordRow` | Table row: term / translation / WordTypeBadge / notes / delete |
| `WordTypeBadge` | Color-coded badge: Word / Phrase / Expression / Idiom |
| `LanguageBadge` | Language badge: EN / UK / NO / ... |
| `ConfidenceBadge` | "Needs review" indicator badge |
| `EditBlockModal` | Modal for editing block title and description |
| `CreateBlockModal` | Modal for creating a new block |

### AI Import

| Component | Description |
|---|---|
| `RawTextInput` | Large textarea for pasting raw vocabulary |
| `FormatProgress` | SSE progress indicator with phases |
| `WordPreviewTable` | Editable table of AI-formatted words |
| `ImportErrorBanner` | AI error banner with retry button |

### Tests

| Component | Description |
|---|---|
| `TestProgressBar` | "Question N of M" progress indicator |
| `SingleChoiceQuestion` | 4 answer option buttons |
| `MultiSelectQuestion` | Checkbox list of answer options |
| `FillInBlankQuestion` | Input with sentence context |
| `OpenAnswerQuestion` | Free-text input with Levenshtein-based checking |
| `AnswerFeedback` | Green/red feedback panel + correct answer display |
| `BlockSelector` | Block checkboxes for test creation |
| `QuestionTypeSelector` | Question type checkboxes for test creation |

### Review (Spaced Repetition)

| Component | Description |
|---|---|
| `ReviewCard` | Flashcard with flip animation |
| `QualityRater` | 6 buttons 0–5 with color gradient and labels |
| `ReviewDueBanner` | Dashboard banner: "N words due for review today" |

### General

| Component | Description |
|---|---|
| `StatCard` | Card with a large number and a label |
| `Spinner` | Loading indicator |
| `SearchBar` | Search input in the header |

---

## 9. Critical UI States

Every list and every screen must handle three states:

### Loading

- Skeleton placeholders for lists and cards
- Spinner on buttons during form submission
- Buttons disabled while a request is in flight

### Empty States

| Screen | Message | CTA |
|---|---|---|
| Block List | "You don't have any blocks yet" | "+ New Block" |
| Block Detail | "This block has no words" | "AI Import" |
| Test List | "No tests yet" | "+ New test" |
| Review | "No words due for review!" | "Go to Blocks" |
| Search | "Nothing found" | — |
| Dashboard (new user) | Onboarding message | "Create your first block" |

### Error States

| Error Type | Display |
|---|---|
| Network error | Toast "Check your connection" |
| AI unavailable | Inline banner "AI is currently unavailable, please try again later" + retry |
| Rate limit (429) | Toast "Too many requests — please wait N minutes" |
| 401 Unauthorized | Auto-refresh token → retry; on failure → logout |
| 5xx Server Error | Toast "Something went wrong" |
| Validation (422) | Inline errors below form fields |

---

## 10. Admin Panel

### Layout

- **Fixed left sidebar** (`AdminNav`)
- Main content area to the right
- **Visually distinct from the user zone**: orange accent color instead of blue

### Screens

#### `/admin` — Dashboard

**Elements:**
- Stat cards: Total users / Active users / Total blocks / Total words / Tests generated
- AI metrics: success rate, avg latency (ms), Ollama vs OpenAI ratio
- Activity charts (registrations, tests per day)

---

#### `/admin/users` — User Management

**Elements:**
- Search by email or display name
- Table: Email / Display name / Role / Status / Last active / Actions
- Action buttons: `Suspend` / `Activate` / `View details`
- `UserDetailModal`: user stats, their block list

**Notes:**
- Destructive `Suspend` action → always requires a confirm modal

---

#### `/admin/ai` — AI Monitor

**Elements:**
- `AiMetricsChart`: success rate and latency over time
- `AiLogTable`: Call type / Provider / Model / Duration (ms) / Success / Error message
- Filter by date range and provider

**Notes:**
- Ollama and OpenAI rows use distinct colors in the table and chart
- Failed calls highlighted in red

---

#### `/admin/settings` — System Settings

**Elements:**
- Key-value settings form
- Languages table with full CRUD (code / name / flag)
- Recent audit log entries

---

#### `/admin/audit` — Audit Log

**Elements:**
- Table: Date / User / Action / Details / IP address
- Filter by action type and user

---

## 11. Appendix: API Surface

| Group | Base path | Key operations |
|---|---|---|
| Auth | `/api/auth` | login, register, refresh, logout |
| Blocks | `/api/blocks` | CRUD with pagination and filters |
| Words | `/api/blocks/:id/words` | CRUD, bulk import, AI format (SSE) |
| Tests | `/api/tests` | generate, list, get by id |
| Attempts | `/api/attempts/:id` | start, submit answer, finish |
| Review | `/api/review` | get due words, submit quality rating |
| Admin | `/api/admin` | stats, users, AI logs, settings |

**Rate limits:**
- `POST /api/words/format` → 10 requests / minute / user
- `POST /api/tests/generate` → 5 requests / hour / user
- `POST /api/auth/login` → 10 attempts / 15 minutes / IP
