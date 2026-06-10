# ✈ AeroERP — India Aerospace Industry Intelligence ERP

> A full-stack **Aerospace Industry Intelligence & Business Development Management System**  
> Built with **.NET 8 Web API + Oracle Database + Bootstrap 5.3 + Chart.js**

---

## 🚀 Modules

| # | Module | Description |
|---|--------|-------------|
| 1 | **Company Master** | 58+ Indian aerospace companies — Defence, Space, UAV, MFG, Software |
| 2 | **Contact Management** | Industry contact directory with CRM linking |
| 3 | **Business Opportunity Tracker** | Kanban pipeline — RFQ, Tender, Direct |
| 4 | **Job & Career Tracker** | Personal application status tracking |
| 5 | **Technology Tracker** | AI/ML, DO-178C, Digital Twin, Additive Mfg adoption map |
| 6 | **Supplier & Vendor Tracker** | Approval and audit workflow |
| 7 | **Project Tracker** | Milestone tracking across Defence/Space/UAV |
| 8 | **Document Management** | Brochures, Certs, NDAs, Drawings |
| 9 | **Research & Analytics** | Revenue, hiring, tech, drone/space growth charts |
| 10 | **CRM & Follow-Up** | Call logs, meeting notes, reminders |

---

## 🗂 Repository Structure

```
AeroERP/
├── sql/
│   ├── 01_schema_ddl.sql       # Oracle DDL — 13 tables, sequences, views
│   └── 02_seed_data.sql        # 20 companies, tech profiles, jobs, opportunities
├── frontend/
│   ├── login.html              # JWT login page
│   ├── index.html              # Full ERP shell — all 10 modules
│   └── js/
│       └── api.js              # API service layer (60+ endpoints)
├── backend/
│   ├── AeroERP_API_Complete.cs # All controllers + endpoint map
│   └── CompanyService.cs       # Dapper + Oracle: Service + Auth + Dashboard
└── docs/
    ├── ARCHITECTURE.md         # ERD, folder structure, roles matrix, roadmap
    └── NUGET_AND_SETUP.md      # NuGet packages, setup guide, quick-start
```

---

## ⚡ Quick Start

```bash
# 1. Oracle — run as AEROERP user
sqlplus AEROERP/Aero@2025@XEPDB1 @sql/01_schema_ddl.sql
sqlplus AEROERP/Aero@2025@XEPDB1 @sql/02_seed_data.sql

# 2. .NET 8 backend
cd backend && dotnet run --project AeroERP.API

# 3. Frontend — open with Live Server
# Demo: username=admin / password=Aero@2025
```

---

## 🛠 Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | HTML5, Bootstrap 5.3, Chart.js 4.4, Vanilla JS |
| Backend | .NET 8 Web API, Dapper, JWT HS256 |
| Database | Oracle 19c / 21c XE |
| Auth | JWT Bearer + BCrypt (cost 12) |
| ORM | Dapper + Oracle.ManagedDataAccess.Core |
| Reports | NPOI (Excel) + iTextSharp (PDF) |

---

## 🔐 User Roles

| Role | Access |
|------|--------|
| SuperAdmin | Full CRUD + Export all modules |
| Admin | Module admin, no user management |
| BDManager | BD pipeline full access |
| Analyst | Company/Tech/Job CRUD, BD read |
| Viewer | Read-only all modules |

---

## 🤖 Future AI Features

- GPT-4o company summarizer (URL → Intel card)
- NLP job-skill matcher (profile vs JD cosine similarity)
- ML opportunity probability scoring (XGBoost)
- NDA/contract risk analyser
- Competitor intelligence alerts

---

## 👤 Author

**Mahin Nandipa** — B.Tech Aerospace, VIT Bhopal | PG AI/ML, IIIT Hyderabad (TalentSprint × Accenture)  
GitHub: [@mahin-aeroai](https://github.com/mahin-aeroai)

---

*AeroERP v1.0 — Built June 2025*
