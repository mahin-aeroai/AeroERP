# AeroERP — Complete System Architecture & Development Roadmap
## India Aerospace Industry Intelligence ERP
### Author: Mahin | Stack: HTML5 + Bootstrap + .NET 8 Web API + Oracle DB

---

## 1. SYSTEM ARCHITECTURE

```
┌──────────────────────────────────────────────────────────────┐
│                     CLIENT TIER (Browser)                     │
│  HTML5 + Bootstrap 5.3 + Chart.js + Vanilla JS               │
│  Pages: Dashboard, 10 Modules, Reports, Admin                 │
└─────────────────┬────────────────────────────────────────────┘
                  │ HTTPS REST + JWT Bearer Token
┌─────────────────▼────────────────────────────────────────────┐
│               GATEWAY / REVERSE PROXY                         │
│  Nginx / IIS (SSL termination, rate limiting, CORS)           │
└─────────────────┬────────────────────────────────────────────┘
                  │
┌─────────────────▼────────────────────────────────────────────┐
│           .NET 8 WEB API (AeroERP.API)                        │
│  ┌──────────────┐  ┌─────────────────┐  ┌──────────────────┐ │
│  │ Controllers  │  │    Services     │  │    Middleware    │ │
│  │ (10 modules) │  │  (Business Lgc) │  │ JWT Auth, CORS,  │ │
│  │ + Auth       │  │  + Validation   │  │ Audit, Rate Lmt  │ │
│  └──────┬───────┘  └────────┬────────┘  └──────────────────┘ │
│         │                   │                                  │
│  ┌──────▼───────────────────▼────────────────────────────┐    │
│  │              Data Access Layer (Dapper + Oracle)       │    │
│  │  Repository pattern, Stored Procs, Views, Raw SQL      │    │
│  └───────────────────────┬────────────────────────────────┘    │
└──────────────────────────┼───────────────────────────────────┘
                           │ Oracle Net
┌──────────────────────────▼───────────────────────────────────┐
│              ORACLE DATABASE (19c / 21c)                       │
│  ┌────────────┐ ┌──────────────┐ ┌────────────────────────┐  │
│  │  MST_*     │ │  CRM_*       │ │  BIZ_* / JOB_* / PRJ_* │  │
│  │  (Master)  │ │  (Activity)  │ │  (Transactional)        │  │
│  └────────────┘ └──────────────┘ └────────────────────────┘  │
│  ┌────────────┐ ┌──────────────┐ ┌────────────────────────┐  │
│  │  TECH_*    │ │  DOC_*       │ │  ERP_USERS / AUD_LOG    │  │
│  │  (Profile) │ │  (Files)     │ │  (Security / Audit)     │  │
│  └────────────┘ └──────────────┘ └────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                           │
┌──────────────────────────▼───────────────────────────────────┐
│              FILE STORAGE (Local / Azure Blob)                 │
│  Company Brochures, Certs, NDAs, Drawings, Reports            │
└──────────────────────────────────────────────────────────────┘
```

---

## 2. ENTITY RELATIONSHIP DIAGRAM (ERD — Textual)

```
ERP_ROLES ──────< ERP_USERS ─────────────────────────────┐
                     │                                    │
ERP_MODULE_PERMISSIONS (role_id, module_code)             │
                                                          │
REF_LOOKUP (lookup_type, code, value)                     │
                                                          │
MST_COMPANY ──────┬──────────────────────────────────────┤
    │             │                                       │
    ├── MST_COMPANY_FACILITIES                           │
    ├── MST_COMPANY_SEGMENT                              │
    │             │                                       │
    ├──<── CRM_CONTACT ──────────────────────────────────┤
    │         │    │                                      │
    │         │    └──<── CRM_ACTIVITY (activity_type)   │
    │         │                                           │
    ├──<── BIZ_OPPORTUNITY ─────────────────────────────┤
    │         │                                           │
    │         └──<── DOC_DOCUMENT                        │
    │                                                     │
    ├──<── JOB_POSTING                                   │
    │                                                     │
    ├──── TECH_PROFILE (1:1 per company)                 │
    │                                                     │
    ├──<── PRJ_PROJECT                                   │
    │                                                     │
    └──── SUP_SUPPLIER (optional link)                   │
                                                          │
AUD_LOG ◄────────────────────────────────────────────────┘
(logs all INSERT/UPDATE/DELETE by USER_ID)

Cardinalities:
  ERP_ROLES        1 ──< ERP_USERS
  MST_COMPANY      1 ──< MST_COMPANY_FACILITIES (many)
  MST_COMPANY      1 ──< MST_COMPANY_SEGMENT   (many)
  MST_COMPANY      1 ──< CRM_CONTACT            (many)
  MST_COMPANY      1 ──< BIZ_OPPORTUNITY        (many)
  MST_COMPANY      1 ──< JOB_POSTING            (many)
  MST_COMPANY      1 ──  TECH_PROFILE           (1:1)
  MST_COMPANY      1 ──< PRJ_PROJECT            (many)
  MST_COMPANY      1 ──< DOC_DOCUMENT           (many)
  CRM_CONTACT      1 ──< CRM_ACTIVITY           (many)
  BIZ_OPPORTUNITY  1 ──< DOC_DOCUMENT           (many)
  ERP_USERS        1 ──< BIZ_OPPORTUNITY        (assigned)
  ERP_USERS        1 ──< CRM_ACTIVITY           (assigned)
```

---

## 3. VISUAL STUDIO .NET 8 FOLDER STRUCTURE

```
AeroERP.sln
│
├── AeroERP.API/                        ← ASP.NET Core 8 Web API
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── CompanyController.cs
│   │   ├── ContactController.cs
│   │   ├── OpportunityController.cs
│   │   ├── JobController.cs
│   │   ├── TechProfileController.cs
│   │   ├── SupplierController.cs
│   │   ├── ProjectController.cs
│   │   ├── DocumentController.cs
│   │   ├── CrmActivityController.cs
│   │   ├── DashboardController.cs
│   │   ├── ReportController.cs
│   │   ├── LookupController.cs
│   │   └── UserController.cs
│   │
│   ├── Models/                         ← DTOs, Request/Response
│   │   ├── Company/
│   │   │   ├── CompanyDto.cs
│   │   │   ├── CompanyFilter.cs
│   │   │   └── CompanyCreateRequest.cs
│   │   ├── Auth/
│   │   │   ├── LoginRequest.cs
│   │   │   ├── LoginResponse.cs
│   │   │   └── TokenClaims.cs
│   │   ├── Opportunity/
│   │   ├── Job/
│   │   ├── Contact/
│   │   ├── Technology/
│   │   ├── Supplier/
│   │   ├── Project/
│   │   ├── Document/
│   │   ├── CrmActivity/
│   │   └── Dashboard/
│   │       ├── KpiDto.cs
│   │       └── ChartDataDto.cs
│   │
│   ├── Services/
│   │   ├── ICompanyService.cs / CompanyService.cs
│   │   ├── IContactService.cs / ContactService.cs
│   │   ├── IOpportunityService.cs / OpportunityService.cs
│   │   ├── IJobService.cs / JobService.cs
│   │   ├── ITechProfileService.cs / TechProfileService.cs
│   │   ├── ISupplierService.cs / SupplierService.cs
│   │   ├── IProjectService.cs / ProjectService.cs
│   │   ├── IDocumentService.cs / DocumentService.cs
│   │   ├── ICrmActivityService.cs / CrmActivityService.cs
│   │   ├── IDashboardService.cs / DashboardService.cs
│   │   ├── IAuthService.cs / AuthService.cs (JWT)
│   │   ├── IAuditService.cs / AuditService.cs
│   │   └── IReportService.cs / ReportService.cs
│   │
│   ├── Data/
│   │   ├── OracleContext.cs            ← Dapper connection factory
│   │   ├── Repositories/
│   │   │   ├── ICompanyRepository.cs / CompanyRepository.cs
│   │   │   ├── IContactRepository.cs  / ContactRepository.cs
│   │   │   └── ... (one per entity)
│   │   └── SqlQueries/                 ← SQL string constants
│   │       ├── CompanySql.cs
│   │       └── ...
│   │
│   ├── Middleware/
│   │   ├── JwtMiddleware.cs
│   │   ├── AuditMiddleware.cs
│   │   └── ExceptionHandlingMiddleware.cs
│   │
│   ├── Helpers/
│   │   ├── JwtHelper.cs
│   │   ├── PasswordHelper.cs (BCrypt)
│   │   ├── PaginationHelper.cs
│   │   └── ExportHelper.cs (CSV/Excel)
│   │
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs
│
├── AeroERP.Frontend/                   ← Static frontend (or separate deploy)
│   ├── index.html                      ← Main ERP shell
│   ├── login.html
│   ├── css/
│   │   └── aeroerp.css
│   ├── js/
│   │   ├── app.js                      ← Main app logic
│   │   ├── api.js                      ← API call wrappers
│   │   ├── charts.js                   ← Chart.js configs
│   │   ├── auth.js                     ← JWT handling
│   │   └── modules/
│   │       ├── company.js
│   │       ├── contact.js
│   │       ├── opportunity.js
│   │       ├── jobs.js
│   │       ├── technology.js
│   │       └── crm.js
│   └── assets/
│       └── logo.svg
│
└── AeroERP.Tests/
    ├── UnitTests/
    │   ├── CompanyServiceTests.cs
    │   └── OpportunityServiceTests.cs
    └── IntegrationTests/
        └── CompanyApiTests.cs
```

---

## 4. USER ROLES & PERMISSIONS MATRIX

| Module           | SuperAdmin | Admin | BDManager | Analyst | Viewer |
|-----------------|:----------:|:-----:|:---------:|:-------:|:------:|
| Company Master   | CRUD+E     | CRUD+E| View+E    | CRU+E   | View   |
| Contacts         | CRUD+E     | CRUD+E| CRUD      | CRU     | View   |
| Opportunities    | CRUD+E     | CRUD+E| CRUD      | View    | View   |
| Job Tracker      | CRUD+E     | CRUD+E| CRU       | CRUD    | View   |
| Technology       | CRUD+E     | CRUD+E| View      | CRUD    | View   |
| Suppliers        | CRUD+E+Approve| CRUD+Approve| View | CRU   | View   |
| Projects         | CRUD+E     | CRUD+E| CRUD      | View    | View   |
| Documents        | CRUD+E+DL  | CRUD+DL| CRUD+DL | CRU+DL  | View+DL|
| CRM Activities   | CRUD+E     | CRUD+E| CRUD      | CRU     | View   |
| Analytics        | Full       | Full  | Read      | Full    | Read   |
| Reports          | All+Export | All+Export| All+Export| All+Export| Basic|
| Users/Roles      | Full       | Limited| None     | None    | None   |
| Settings         | Full       | Limited| None     | None    | None   |

C=Create, R=Read, U=Update, D=Delete, E=Export, DL=Download

---

## 5. JWT AUTHENTICATION FLOW

```
1. POST /api/Auth/login  { username, password }
   ↓
2. API validates → BCrypt hash compare → Oracle ERP_USERS
   ↓
3. Generate JWT: { userId, username, role, email }
   Signed with HS256 + secret key, expires 8h
   ↓
4. Client stores in localStorage: aeroerp_token
   ↓
5. All subsequent requests: Authorization: Bearer <token>
   ↓
6. JwtMiddleware validates token on every request
   ↓
7. Role-based policy checks via [Authorize(Policy="BDAccess")]
```

**Token Claims:**
```json
{
  "userId": "42",
  "username": "mahin",
  "email": "mahin@aeroerp.in",
  "role": "Analyst",
  "iat": 1749600000,
  "exp": 1749628800
}
```

---

## 6. DEVELOPMENT ROADMAP (Phase-wise)

### PHASE 1 — Foundation (Weeks 1–3)
- [ ] Oracle 19c schema: all 13 tables + sequences + indexes + views
- [ ] .NET 8 Web API project setup (VS 2022)
- [ ] Dapper + Oracle.ManagedDataAccess NuGet
- [ ] JWT Auth (login, refresh, change-password)
- [ ] ERP_USERS + ERP_ROLES seed data
- [ ] Swagger UI for API testing
- [ ] Frontend: login.html with JWT store

### PHASE 2 — Company Master (Weeks 4–5)
- [ ] CompanyController: CRUD + search + export
- [ ] Company frontend: grid + list view + filters
- [ ] Add/Edit modal with validation
- [ ] Company detail page (all related data)
- [ ] Tech Profile CRUD

### PHASE 3 — Contacts & CRM (Weeks 6–7)
- [ ] ContactController + frontend
- [ ] CRM Activity logging (call, email, meeting)
- [ ] Reminder system (background job / SignalR)
- [ ] Follow-up dashboard widget

### PHASE 4 — Business Development (Weeks 8–9)
- [ ] Opportunity Tracker: CRUD + Kanban view
- [ ] Pipeline analytics (funnel chart)
- [ ] Project Tracker: CRUD + milestone tracking

### PHASE 5 — Job & Supplier (Weeks 10–11)
- [ ] Job Tracker: personal application status
- [ ] Supplier/Vendor CRUD + audit workflow
- [ ] Document upload (multipart + Azure Blob)

### PHASE 6 — Analytics & Reports (Weeks 12–13)
- [ ] Dashboard KPI endpoint + Chart.js integration
- [ ] Segment distribution, tech adoption charts
- [ ] Report generator: Excel (NPOI/ClosedXML) + PDF (iTextSharp)
- [ ] Top-50 employers, AI companies, job reports

### PHASE 7 — AI Integration (Weeks 14–16, Future)
- [ ] GPT-4o API: Auto-summarize company from website URL
- [ ] ML opportunity scoring model (Python microservice → REST)
- [ ] NLP job-skill matching (your profile vs JD)
- [ ] Automated competitor alerts (scraper + AI summary)
- [ ] Recommendation engine: "Companies to target next"

---

## 7. FUTURE AI INTEGRATION FEATURES

| Feature | Description | Tech |
|---------|-------------|------|
| Company Summarizer | Input company URL → AI extracts key intel | GPT-4o + Playwright scraper |
| Job Matcher | Score your profile vs JD using NLP | Python + spaCy + cosine sim |
| Opportunity Scorer | ML model: probability based on history | scikit-learn, xgboost |
| Contract Analyser | Upload NDA/contract → AI extracts risks | GPT-4o + RAG |
| Competitor Alerts | Daily crawl → AI summary of competitor news | Python + Newspaper3k + GPT |
| Skill Gap Analyser | Compare your skills vs market demand | NLP + Oracle analytics |
| Smart Search | Natural language: "defence companies using AI in Hyderabad" | Vector DB + embedding search |
| Report Writer | Generate narrative reports from data | GPT-4o + Handlebars templates |

---

## 8. API SECURITY HEADERS & BEST PRACTICES

```csharp
// Program.cs additions
app.Use(async (ctx, next) => {
    ctx.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Add("X-Frame-Options", "DENY");
    ctx.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    ctx.Response.Headers.Add("Referrer-Policy", "no-referrer");
    await next();
});
// Rate limiting: 100 req/min per IP (AspNetCoreRateLimit NuGet)
// Oracle connection: use wallet + encrypted password in appsettings
// Passwords: BCrypt.Net-Next (cost factor 12)
// File upload: validate MIME type + max 10MB per file
// Audit: log all write operations to AUD_LOG via middleware
```

---

## 9. ORACLE PERFORMANCE TUNING

```sql
-- Partition COMPANY and ACTIVITY tables by year for large datasets
ALTER TABLE CRM_ACTIVITY MODIFY PARTITION BY RANGE (ACTIVITY_DATE) INTERVAL (NUMTOYMINTERVAL(1,'MONTH'))
(PARTITION p_initial VALUES LESS THAN (DATE '2024-01-01'));

-- Materialized view for dashboard KPIs (refresh every 5 min)
CREATE MATERIALIZED VIEW MV_DASHBOARD_KPI
REFRESH FAST ON COMMIT
AS SELECT * FROM V_DASHBOARD_KPI;

-- Statistics for query optimizer
EXEC DBMS_STATS.GATHER_SCHEMA_STATS('AEROERP');
```

---

## 10. DEPLOYMENT CHECKLIST

```
Local Dev:
  ✓ Oracle 21c XE free (localhost:1521/XEPDB1)
  ✓ VS 2022 + .NET 8 SDK
  ✓ Live Server / IIS Express for frontend
  ✓ Postman collection for API testing

Production:
  ✓ Oracle 19c Enterprise (or Oracle Cloud ADB)
  ✓ IIS 10 / Nginx reverse proxy + SSL (Let's Encrypt)
  ✓ Windows Service for background reminders
  ✓ Azure Blob Storage for documents
  ✓ Azure Application Insights for monitoring
  ✓ GitHub Actions CI/CD pipeline
  ✓ Oracle Data Masking for dev/test environments
```
