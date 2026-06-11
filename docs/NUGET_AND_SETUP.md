# AeroLens — Complete Project Setup Guide
## India Aerospace Industry Intelligence ERP
### Stack: .NET 8 Web API + Oracle DB + Bootstrap 5.3 + Chart.js

---

## DELIVERABLES SUMMARY

| # | File | Description |
|---|------|-------------|
| 1 | `sql/01_schema_ddl.sql` | Oracle DDL — 13 tables, sequences, indexes, 2 views |
| 2 | `sql/02_seed_data.sql` | 20 companies, tech profiles, contacts, jobs, opportunities, CRM activities |
| 3 | `frontend/login.html` | JWT login page with radar animation |
| 4 | `frontend/index.html` | Complete ERP shell — Dashboard + 10 modules |
| 5 | `frontend/js/api.js` | API service layer — all 60+ endpoints wrapped |
| 6 | `backend/AeroLens_API_Complete.cs` | All controllers, endpoint map, JWT auth structure |
| 7 | `backend/CompanyService.cs` | Full Dapper + Oracle: CompanyService, DashboardService, AuthService, AuditService |
| 8 | `docs/ARCHITECTURE.md` | ERD, folder structure, roles matrix, roadmap, AI integration table |
| 9 | `docs/NUGET.md` | This file — NuGet packages, .csproj, quick-start |

---

## 1. VISUAL STUDIO 2022 — PROJECT SETUP

```bash
# Create solution
dotnet new sln -n AeroLens
dotnet new webapi -n AeroLens.API --framework net8.0
dotnet sln add AeroLens.API
cd AeroLens.API
```

---

## 2. .csproj — NUGET PACKAGES

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>AeroLens.API</AssemblyName>
    <RootNamespace>AeroLens.API</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <!-- Oracle + Dapper -->
    <PackageReference Include="Oracle.ManagedDataAccess.Core"     Version="23.5.1" />
    <PackageReference Include="Dapper"                            Version="2.1.35" />

    <!-- JWT Authentication -->
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.6" />
    <PackageReference Include="Microsoft.IdentityModel.Tokens"   Version="7.5.1" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt"  Version="7.5.1" />

    <!-- Password hashing -->
    <PackageReference Include="BCrypt.Net-Next"                   Version="4.0.3" />

    <!-- Excel / PDF report generation -->
    <PackageReference Include="NPOI"                              Version="2.7.1" />
    <PackageReference Include="iTextSharp.LGPLv2.Core"           Version="3.4.5" />

    <!-- Swagger UI -->
    <PackageReference Include="Swashbuckle.AspNetCore"            Version="6.7.3" />

    <!-- Rate limiting -->
    <PackageReference Include="AspNetCoreRateLimit"               Version="5.0.0" />

    <!-- CORS / Logging -->
    <PackageReference Include="Serilog.AspNetCore"                Version="8.0.1" />
    <PackageReference Include="Serilog.Sinks.File"                Version="5.0.0" />
  </ItemGroup>
</Project>
```

---

## 3. INSTALL ALL NUGET (CLI)

```bash
cd AeroLens.API

dotnet add package Oracle.ManagedDataAccess.Core --version 23.5.1
dotnet add package Dapper --version 2.1.35
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.6
dotnet add package Microsoft.IdentityModel.Tokens --version 7.5.1
dotnet add package System.IdentityModel.Tokens.Jwt --version 7.5.1
dotnet add package BCrypt.Net-Next --version 4.0.3
dotnet add package NPOI --version 2.7.1
dotnet add package iTextSharp.LGPLv2.Core --version 3.4.5
dotnet add package Swashbuckle.AspNetCore --version 6.7.3
dotnet add package AspNetCoreRateLimit --version 5.0.0
dotnet add package Serilog.AspNetCore --version 8.0.1
dotnet add package Serilog.Sinks.File --version 5.0.0
```

---

## 4. appsettings.json

```json
{
  "ConnectionStrings": {
    "OracleDb": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));User Id=AEROLENS;Password=Aero@2025;"
  },
  "Jwt": {
    "Key":         "AeroLens_SuperSecretKey_2025_Mahin_Aerospace_IIITH!@#$",
    "Issuer":      "AeroLens",
    "Audience":    "AeroLensClients",
    "ExpiryHours": 8
  },
  "FileStorage": {
    "BasePath":    "C:\\AeroLens\\Documents",
    "MaxFileMB":   10
  },
  "RateLimit": {
    "RequestsPerMinute": 120
  },
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  }
}
```

---

## 5. ORACLE 21c XE — LOCAL SETUP

```sql
-- As SYSDBA:
CREATE USER AEROLENS IDENTIFIED BY "Aero@2025"
  DEFAULT TABLESPACE USERS TEMPORARY TABLESPACE TEMP;
GRANT CONNECT, RESOURCE, CREATE VIEW TO AEROLENS;
GRANT CREATE SEQUENCE, CREATE TABLE, CREATE PROCEDURE TO AEROLENS;
ALTER USER AEROLENS QUOTA UNLIMITED ON USERS;

-- Then run as AEROLENS:
-- @01_schema_ddl.sql
-- @02_seed_data.sql
```

---

## 6. PROGRAM.CS — COMPLETE

```csharp
using AeroLens.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Oracle.ManagedDataAccess.Client;
using System.Text;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/aerolens-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ── Oracle Dapper (scoped per request) ──────────────────────
builder.Services.AddScoped<OracleConnection>(_ =>
    new OracleConnection(builder.Configuration.GetConnectionString("OracleDb")));

// ── Services DI ──────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,        AuthService>();
builder.Services.AddScoped<IAuditService,       AuditService>();
builder.Services.AddScoped<ICompanyService,     CompanyService>();
builder.Services.AddScoped<IDashboardService,   DashboardService>();
// ... add all other services

// ── JWT ──────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt => {
        opt.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer   = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// ── Authorization Policies ───────────────────────────────────
builder.Services.AddAuthorization(opt => {
    opt.AddPolicy("AdminOnly",     p => p.RequireRole("SuperAdmin","Admin"));
    opt.AddPolicy("BDAccess",      p => p.RequireRole("SuperAdmin","Admin","BDManager"));
    opt.AddPolicy("AnalystAccess", p => p.RequireRole("SuperAdmin","Admin","BDManager","Analyst"));
    opt.AddPolicy("ViewerAccess",  p => p.RequireRole("SuperAdmin","Admin","BDManager","Analyst","Viewer"));
});

// ── CORS ─────────────────────────────────────────────────────
builder.Services.AddCors(opt => opt.AddPolicy("AeroPolicy", p =>
    p.WithOrigins("http://localhost:5500","http://127.0.0.1:5500","https://aerolens.in")
     .AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new() { Title = "AeroLens API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, Scheme = "bearer",
        BearerFormat = "JWT", Description = "Enter JWT token"
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AeroLens v1"));
app.UseCors("AeroPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

---

## 7. DEVELOPMENT PHASE CHECKLIST

### Phase 1 — Foundation (Week 1–2)
- [ ] Oracle 21c XE install → create AEROLENS user
- [ ] Run `01_schema_ddl.sql` → verify all 13 tables created
- [ ] Run `02_seed_data.sql` → verify 20 companies + tech profiles
- [ ] .NET 8 VS2022 solution → install all NuGet packages
- [ ] Implement `Program.cs`, `appsettings.json`, JWT config
- [ ] `AuthController.cs` + `AuthService.cs` → test login with Postman
- [ ] Swagger UI working at `https://localhost:7001/swagger`

### Phase 2 — Company Module (Week 3–4)
- [ ] `CompanyService.cs` → `GetFilteredAsync` (pagination + filters)
- [ ] `CompanyController.cs` → test all CRUD endpoints
- [ ] Frontend `index.html` → wire company grid to live API
- [ ] Company search endpoint → wire to top-bar search
- [ ] Tech Profile CRUD

### Phase 3 — BD Pipeline (Week 5–6)
- [ ] `OpportunityService.cs` + controller
- [ ] Kanban board → drag-and-drop status update via PATCH
- [ ] Dashboard KPI endpoint → wire 8 KPI widgets
- [ ] Chart.js → segment distribution + pipeline funnel from live data

### Phase 4 — CRM & Jobs (Week 7–8)
- [ ] CrmActivity CRUD + reminder query
- [ ] Contact management + activity log
- [ ] Job Tracker with personal application tracking
- [ ] Reminder notification (background service + SignalR or polling)

### Phase 5 — Analytics & Reports (Week 9–10)
- [ ] All 6 dashboard charts → live data
- [ ] Report generator: Excel (NPOI) + PDF (iTextSharp)
- [ ] 8 pre-built reports wired to Report endpoints
- [ ] Document upload → Azure Blob / local filesystem

### Phase 6 — Polish & Deploy (Week 11–12)
- [ ] Audit log middleware for all write operations
- [ ] Role permissions UI (Manage Users)
- [ ] Input validation (FluentValidation)
- [ ] IIS / Nginx deploy with SSL
- [ ] Oracle statistics + indexes tuned

### Phase 7 — AI (Future)
- [ ] GPT-4o: company URL summarizer
- [ ] Job matcher: your profile vs JD NLP scoring
- [ ] ML opportunity probability model

---

## 8. FOLDER STRUCTURE (quick reference)

```
AeroLens/
├── AeroLens.sln
├── sql/
│   ├── 01_schema_ddl.sql
│   └── 02_seed_data.sql
├── docs/
│   ├── ARCHITECTURE.md
│   └── NUGET.md  (this file)
├── frontend/
│   ├── login.html
│   ├── index.html
│   └── js/
│       └── api.js
└── backend/
    ├── AeroLens_API_Complete.cs  (controllers + endpoint map)
    └── CompanyService.cs        (full service implementations)
```

---

## 9. POSTMAN COLLECTION — QUICK TEST

```
# 1. Login
POST http://localhost:5000/api/Auth/login
Body: { "username": "admin", "password": "Aero@2025" }
→ Copy token from response

# 2. List companies
GET http://localhost:5000/api/Company?page=1&pageSize=10
Header: Authorization: Bearer <token>

# 3. Filter defence companies using AI
GET http://localhost:5000/api/Company?segment=DEFENCE&usesAI=true

# 4. Dashboard KPIs
GET http://localhost:5000/api/Dashboard/kpi

# 5. Create opportunity
POST http://localhost:5000/api/Opportunity
Body: { "companyId": 1, "projectName": "Test", "oppValue": 10000000, "status": "NEW", "probabilityPct": 50 }
```

---

## 10. FUTURE AI INTEGRATION — IMPLEMENTATION NOTES

### Job Matcher (Python microservice)
```python
# skills_match.py — expose as REST endpoint
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity
import numpy as np

def score_job_match(my_skills: str, jd_skills: str) -> float:
    vect = TfidfVectorizer()
    matrix = vect.fit_transform([my_skills, jd_skills])
    return float(cosine_similarity(matrix[0:1], matrix[1:2])[0][0])

# Mahin's skill profile for matching:
MY_SKILLS = """
Python, LLMs, RAG pipelines, LoRA fine-tuning, LangChain, HuggingFace,
FastAPI, PyTorch, TensorFlow, NLP, Vector DB, FAISS, Streamlit,
B.Tech Aerospace Engineering, AI/ML Post Graduate Certificate IIITH,
aerospace knowledge, STM32, embedded systems, composite manufacturing
"""
```

### Company Summarizer (GPT-4o)
```csharp
// CompanySummarizerService.cs
public async Task<string> SummarizeFromUrlAsync(string url)
{
    // 1. Scrape website text (Playwright or HtmlAgilityPack)
    // 2. Call GPT-4o with structured prompt
    var prompt = $"Summarize this aerospace company's: products, tech stack, AI use, hiring outlook. URL content: {scrapedText}";
    // 3. Store summary back to MST_COMPANY.NOTES
}
```
