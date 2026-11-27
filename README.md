# Al-Alamia

**Team 4 · Graduation Project**

A managed outsourcing platform that brings businesses and freelancers together through structured project management, controlled task assignment, and quality review.

Menna Reda · Merna Abdelfattah · Mohamed Tawfik · Abdelrahman Khaled · Nahed Magdy

---

## Contents

1. [What Al-Alamia is](#what-al-alamia-is)
2. [The Problem](#the-problem)
3. [Our Approach](#our-approach)
4. [How It Works](#how-it-works)
5. [Technology Stack](#technology-stack)
6. [System Architecture](#system-architecture)
7. [Data Model](#data-model)
8. [Security and Access Control](#security-and-access-control)
9. [Task Assignment and Review](#task-assignment-and-review)
10. [Challenges and Engineering Decisions](#challenges-and-engineering-decisions)
11. [The Team](#the-team)
12. [Delivery Plan](#delivery-plan)
13. [Documentation](#documentation)
14. [What Comes Next](#what-comes-next)

---

## What Al-Alamia is

Al-Alamia is a managed outsourcing platform designed to help businesses request digital services while giving administrators a structured way to manage projects, assign work, review submissions, and track freelancer activity.

Unlike a traditional freelance marketplace, clients do not directly select freelancers. Instead, the platform follows a centralized workflow in which administrators review requests, organize the required work, and assign tasks to eligible freelancers.

The goal is simple: make freelance service delivery more organized, manageable, and quality-focused.

| | |
|---|---|
| **3** | Main user groups: Admins, Clients, and Freelancers |
| **Managed** | Project and task assignment controlled by administrators |
| **End-to-end** | A structured workflow from project request to work review |

## The Problem

Delivering digital services involves more than finding someone who can do the work. Projects also require coordination, clear requirements, deadline management, progress tracking, and quality assurance.

When these activities are handled through disconnected tools and informal processes, it can become difficult to keep everyone aligned.

Al-Alamia is designed around three key challenges:

- **Coordination:** Keeping project requests, requirements, and communication organized.
- **Accountability:** Tracking task ownership, deadlines, submissions, and revisions.
- **Quality control:** Providing a consistent review process before completed work is approved.

These challenges define the problem the platform aims to address.

## Our Approach

Al-Alamia brings project coordination and freelance task management into one managed workflow.

- **Centralized project management:** Clients submit service requests through the platform.
- **Admin-controlled assignment:** Administrators review projects, determine pricing, create tasks, and offer them to eligible freelancers.
- **Structured delivery:** Freelancers receive task requirements and submit completed work for review.
- **Quality assurance:** Administrators approve submissions or request revisions.
- **Financial tracking:** Freelancer earnings and deductions are recorded through a ledger.

The key difference is that Al-Alamia does not simply connect clients with freelancers. It manages the process through which work is assigned, completed, reviewed, and delivered.

## How It Works

The platform follows a defined service delivery lifecycle.

```text
Client
  |
  v
Submit a Project Request
  |
  v
Admin Reviews the Request
and Determines the Price
  |
  v
Admin Creates Tasks
and Defines Requirements
  |
  v
Offers Sent to Eligible Freelancers
  |
  v
First Eligible Freelancer Accepts
  |
  v
Freelancer Completes the Task
and Submits the Work
  |
  v
Admin Reviews the Submission
  |
  +------ Request Revisions
  |              |
  |              v
  |       Freelancer Revises
  |       and Resubmits
  |              |
  |              v
  |         Admin Review
  |
  +------ Approve the Work
                 |
                 v
          Client Delivery
```

### The main participants

| Participant | Responsibility |
|---|---|
| **Client** | Submits project requests and provides the required details. |
| **Admin** | Reviews requests, determines prices, creates tasks, manages assignments, and reviews submissions. |
| **Freelancer** | Receives eligible task offers, completes assigned work, and submits deliverables. |

## Technology Stack

The planned technology stack is organized around a .NET backend, a relational database, and a React frontend.

| Technology | Purpose |
|---|---|
| **C#** | Backend programming language |
| **ASP.NET Core Web API** | Exposes backend endpoints for frontend and application operations |
| **Entity Framework Core** | Object-relational mapping and database access |
| **ASP.NET Core Identity** | User management, authentication support, and role integration |
| **JWT** | Token-based authentication for API requests |
| **Microsoft SQL Server** | Relational database for application data |
| **React** | Frontend user interface |
| **HTML, CSS, and JavaScript** | Web interface structure, styling, and browser-side behavior |
| **Git and GitHub** | Version control and collaborative development |
| **Postman** | API testing |
| **Swagger / OpenAPI** | API documentation and exploration |

Technology implementation status may vary during development. Update this section to distinguish completed components from those still in progress.

## System Architecture

Al-Alamia is being designed as a layered application, separating business rules from infrastructure and presentation concerns.

The intended responsibilities are:

- **Domain:** Core entities and business concepts.
- **Application:** Business operations, service contracts, and use-case logic.
- **Infrastructure:** Database access and external infrastructure implementations.
- **API:** HTTP endpoints, request handling, authentication configuration, and dependency registration.
- **Frontend:** React pages and components that communicate with the backend API.

A conceptual dependency view:

```text
React Frontend
      |
      v
ASP.NET Core Web API
      |
      v
Application Services
      |
      v
Domain and Business Rules
      ^
      |
Infrastructure Implementations
      |
      v
SQL Server
```

The final project structure and dependency directions should match the architecture actually implemented in the repository.

## Data Model

The database is designed to support the complete managed outsourcing workflow, from user registration to project delivery and financial tracking.

| Area | Main entities |
|---|---|
| **Identity and Profiles** | ApplicationUser, ClientProfile, FreelancerProfile |
| **Tracks and Skills** | Track, Skill, TrackSkill, FreelancerSkill |
| **Portfolio** | PortfolioItem, PortfolioItemFile |
| **Services and Projects** | Service, Project, ProjectService, ProjectStatusHistory |
| **Meetings** | Meeting |
| **Tasks and Assignment** | Task, TaskRequiredSkill, TaskOffer, TaskStatusHistory, TaskReassignment, TaskFile |
| **Submissions and Reviews** | TaskSubmission, TaskReview, TaskRevision, SubmissionFile |
| **Financial Records** | FreelancerLedgerEntry, FreelancerBlock |
| **Marketing Packages** | MarketingPackage, MarketingPackageItem, ProjectMarketingPackage, ProjectMarketingPackageItem |
| **Files and Support** | File, ProjectFile, SupportTicket, SupportMessage, SupportMessageFile |
| **Notifications** | Notification, EmailNotificationLog |

The final entity list should be synchronized with the actual database model and migrations.

### Important data relationships

- A client can create multiple projects.
- A project can request multiple predefined services.
- A freelancer belongs to a track and can have multiple skills.
- A task belongs to a track and may require specific skills.
- A task can receive offers from eligible freelancers, but only one freelancer should successfully claim it.
- A task can have multiple submissions and review or revision records over its lifecycle.
- A published portfolio item must have exactly three images.
- Financial ledger entries provide a traceable record of freelancer earnings and deductions.

## Security and Access Control

The API is protected in layers. Each layer is covered by automated tests that run against a real SQL Server in CI.

| Area | What is in place |
|---|---|
| **Authentication** | ASP.NET Core Identity with JWT access tokens and rotating refresh tokens with replay detection. Passwords need at least 8 characters and a digit. |
| **Brute-force protection** | Five failed sign-ins lock the account for 5 minutes. The login error is the same for a wrong password and an unknown e-mail, so accounts cannot be enumerated. |
| **Rate limiting** | Per client IP: 10 requests a minute on register and login, 20 a minute on file upload. Limits are configuration values. |
| **Authorization** | Role policies (Admin, Client, Freelancer) plus resource checks: a freelancer sees a task only when it is assigned or offered to them, and only an approved freelancer can take part in tasks. Identity always comes from the token, never from the request body. |
| **Task integrity** | Concurrent acceptance is decided atomically; a blocked freelancer receives no new offers; a missed final deadline returns the task to the pool and blocks the freelancer for 30 days (a background monitor enforces this). |
| **File upload** | One upload endpoint with an extension allowlist, a 10 MB limit and a content check (a renamed program is refused). The server decides the content type, files are stored under a generated name, and downloads go through an authorized endpoint as attachments. There is no public file link. |
| **Browser protection** | CORS denies every origin unless it is listed in configuration; security headers on every response (`nosniff`, frame denial, referrer policy, content security policy); HSTS outside Development; API responses are not cached. |
| **Errors** | Rule violations return a consistent `ProblemDetails` response (400, 403, 404, 409) with a clear message; unexpected failures return a plain 500 with no internal details, and are logged on the server. |
| **Delivery pipeline** | GitHub Actions builds and runs all tests on every pull request, with a SQL Server service container. A run fails if any test is skipped. |

Authentication alone is not enough: every operation also checks that the signed-in user may act on that specific resource.

**Before a real deployment:** replace the development JWT key and connection string with secrets from the environment, list the front-end origin under `Cors:AllowedOrigins`, and configure forwarded headers if the API runs behind a reverse proxy.

## Task Assignment and Review

Task assignment and review are central parts of Al-Alamia's workflow.

### 1. Eligibility

Task offers are sent to freelancers who meet the platform's eligibility requirements, including the relevant track and approval status.

### 2. First valid acceptance

Multiple freelancers may receive an offer, but the system must ensure that only the first valid acceptance successfully claims the task. This requires consistent handling of concurrent requests.

### 3. Submission and review

The assigned freelancer submits the completed work. The administrator reviews it and can approve the submission or request revisions.

### 4. Reassignment and accountability

When a task cannot proceed with its current assignment, the platform records the relevant status changes and reassignment history.

### 5. Financial records

Applicable earnings and deductions are recorded with their associated reasons and tasks, allowing the platform to maintain an auditable financial history.

The purpose of these rules is to make task ownership, work quality, and responsibility traceable throughout the process.

## Challenges and Engineering Decisions

### Designing a connected data model

The platform contains several related business areas: identity, projects, freelancer skills, tasks, submissions, support, and financial records.

**Engineering focus:** Define clear entity relationships and preserve referential integrity without introducing unnecessary duplication.

### Handling concurrent task acceptance

More than one freelancer may try to accept the same task at nearly the same time.

**Engineering focus:** Enforce a single successful assignment using appropriate database constraints and transaction-safe application logic.

### Managing task lifecycle transitions

Tasks can move through offers, assignment, submission, review, revision, approval, and possible reassignment.

**Engineering focus:** Define valid status transitions and preserve the history of important changes.

### Protecting resources and operations

Different users have different permissions, and access to a resource must depend on both the user's role and ownership or assignment.

**Engineering focus:** Combine authentication with resource-level authorization and request validation.

### Maintaining financial traceability

Penalties and earnings must remain associated with the correct task and reason.

**Engineering focus:** Record financial changes consistently and avoid applying the same applicable penalty twice to one task.

### Integrating frontend and backend

The React application must communicate reliably with the API while handling authentication, validation, loading states, and errors.

**Engineering focus:** Establish consistent API contracts and predictable response handling.

## The Team

Al-Alamia is a collaborative graduation project developed by Team 4.

| Team Member |
|---|
| Menna Reda |
| Merna Abdelfattah |
| Mohamed Tawfik |
| Abdelrahman Khaled |
| Nahed Magdy |

Each team member contributes to building and refining the platform as part of the shared project.

## Delivery Plan

Hand-in: **Monday 30 November 2026**. Work runs from 28 October with a milestone every week and a buffer at the end.

| Week | Dates | Focus | Milestone |
|---|---|---|---|
| 1 | 28 Oct – 3 Nov | Foundation: CI, migrations, Identity, design system, OpenAPI contracts | M1: login for 3 roles, DB built from migrations |
| 2 | 4 – 10 Nov | Flows: freelancer approval, projects, pricing, meetings, tasks and offers | M2: request → priced → tasks → offers |
| 3 | 11 – 17 Nov | Engine: concurrent accept and monthly cap, delivery and review, notifications, support | M3: accept engine passes the 50-request test |
| 4 | 18 – 24 Nov | Rules and money: deadline monitor, deductions, blocks, ledger, marketing packages | M4: full journey including penalties |
| 5 | 25 – 29 Nov | Polish: performance, E2E tests, security, accessibility, demo data | M5: release candidate |
| – | 29 – 30 Nov | Code freeze, fixes only, rehearsal, hand-in | Delivery |

Each member owns a vertical module (database, API, UI, tests). The proposed split is in section 12 of the design document.

## Documentation

| Document | Contents |
|---|---|
| [System design document (PDF)](docs/Alamia-System-Design-v1.1.pdf) | Scope, business rules BR-01 to BR-20, architecture, state machines, critical flows, database, API, UI, plan |
| [Presentation (PPTX)](docs/Alamia-Presentation.pptx) | 15 editable slides |
| [ERD](docs/erd/) | Mermaid source and per-module diagrams |
| [UI prototype](prototype/) | Interactive client, freelancer and admin screens (see below) |
| [Brand assets](brand/) | Logos (SVG and PNG) |

### Run the prototype locally

```bash
cd prototype
python -m http.server 8080
# http://localhost:8080/Main.dc.html        (client)
# http://localhost:8080/Freelancer.dc.html  (freelancer)
# http://localhost:8080/Admin.dc.html       (admin)
```

## Running the API and the Tests

Requirements: .NET 10 SDK and SQL Server (a local instance or a container).

```bash
# API: set the connection string and JWT settings (user secrets or environment variables), then
dotnet run --project src/alealamia.API
# Swagger UI (Development): https://localhost:<port>/swagger

# Tests: unit tests need nothing; the end-to-end tests need SQL Server
export ALEALAMIA_TEST_CONNECTION="Server=localhost,1433;User Id=sa;Password=<password>;Encrypt=True;TrustServerCertificate=True;"
dotnet test alealamia.slnx
```

The end-to-end tests create a throw-away database, drive the real HTTP pipeline (registration, tasks, offers, assignment, blocks, files, security headers) and delete the database afterwards. Without a reachable SQL Server they skip themselves; the CI workflow makes a skipped test a failure.

## What Comes Next

The next steps depend on the current implementation status and project priorities. Potential areas for continued development include:

- Completing and validating the database model and migrations.
- Implementing the application services and API endpoints.
- Integrating React pages with the backend.
- Testing role-based permissions and task assignment under concurrent requests.
- Testing the full project-to-delivery workflow.
- Improving error handling, validation, and user experience.
- Completing documentation and preparing a reproducible setup for developers.

The roadmap should be updated as features are implemented and tested.

---

**Al-Alamia — A structured workflow for managed freelance services.**
