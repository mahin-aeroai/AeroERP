# 🔭 AeroLens — India Aerospace Industry Intelligence Platform

> A free, open-source **Aerospace Industry Intelligence Platform** — your lens into India's entire aerospace ecosystem.  
> covering Defence, Space, UAV, Avionics, Manufacturing & Software companies.

## 🌐 Live Links

| | URL |
|---|---|
| 🚀 **Live Intelligence Database** | **https://aerolens-production.up.railway.app/data.html** |
| 🏠 **Landing Page** | **https://mahin-aeroai.github.io/AeroLens/** |
| 💻 **GitHub Repo** | **https://github.com/mahin-aeroai/AeroLens** |

---

## 📊 Database Stats

| Metric | Count |
|--------|-------|
| 🏢 Total Companies | **87** |
| 🛡️ Defence Companies | 30 |
| 🚀 Space Companies | 16 |
| 🚁 UAV/Drone Companies | 22 |
| 🤖 Companies Using AI/ML | 35 |
| 💼 Active Job Openings | 80+ |
| 🚀 Major Projects Tracked | 150+ |

---

## 🗂 What's Inside

**Click any company card to see:**
- CEO / Leadership details
- Revenue, employees, founded year
- All major projects with values & status
- Technology stack (CAD, simulation, languages)
- AI/ML use cases
- Open job openings with direct apply links
- Export markets & strategic partners

**4 tabs:**
- 🏢 **Companies** — searchable grid + table view
- 🚀 **Projects** — Tejas Mk1A, Gaganyaan, AESA Radar, BrahMos-NG and more
- 💼 **Jobs** — GET programmes, fresher roles, internships
- 🤖 **Technology** — AI/ML, DO-178C, Digital Twin per company

---

## 🏢 Companies Covered

**Defence PSUs:** HAL · BEL · DRDO · BDL · MIDHANI  
**Space:** ISRO · NSIL · Skyroot · Agnikul · Pixxel · Dhruva Space · Bellatrix · Kawa Space  
**UAV/Drone:** ideaForge · Garuda · BotLab · Asteria · Raphe · Skye Air · Skylark · EndureAir · Indrajaal  
**MNCs:** Honeywell · GE Aerospace · Collins · Thales · Rolls-Royce · Safran · Airbus · Adani  
**Private Large:** Cyient · KPIT · Wipro · TASL · Bharat Forge · L&T · Dynamatic · Unimech  
**Avionics:** Astra Microwave · Ananth Technologies · Hical · Rangsons  
**Missiles:** BrahMos · Zen Technologies · NRT  
**MRO:** GMR Aero Technic · Air Works

---

## 🛠 Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | HTML5, Bootstrap 5.3, Chart.js 4.4, Vanilla JS |
| Backend | .NET 8 Web API, Dapper ORM |
| Database | Oracle 19c / 21c |
| Auth | JWT Bearer + BCrypt |
| Hosting | Railway.app (frontend) + GitHub Pages |
| CI/CD | GitHub → Railway auto-deploy |

---

## 📁 Repository Structure

```
AeroLens/
├── index.html              ← Public landing page (GitHub Pages)
├── data.html               ← Live intelligence database (87 companies)
├── frontend/
│   ├── index.html          ← Full ERP dashboard (demo)
│   └── login.html          ← Login screen
├── sql/
│   ├── 01_schema_ddl.sql   ← Oracle DDL — 13 tables
│   └── 02_seed_data.sql    ← Seed data
├── backend/
│   ├── AeroLens_API_Complete.cs
│   └── CompanyService.cs
└── docs/
    ├── ARCHITECTURE.md
    └── NUGET_AND_SETUP.md
```

---

## ⚡ Quick Start (Local)

```bash
# 1. Clone
git clone https://github.com/mahin-aeroai/AeroLens.git

# 2. Open data.html in browser — works immediately, no server needed
open data.html

# 3. For full ERP backend:
# Oracle setup + .NET 8 — see docs/NUGET_AND_SETUP.md
```

---

## 👤 Author

**Mahin Nandipa**  
B.Tech Aerospace Engineering — VIT Bhopal  
PG Certificate AI & ML — IIIT Hyderabad (TalentSprint × Accenture)  
📍 Hyderabad, India  

[![GitHub](https://img.shields.io/badge/GitHub-mahin--aeroai-blue?logo=github)](https://github.com/mahin-aeroai)
[![Live Demo](https://img.shields.io/badge/Live-AeroLens_Database-green)](https://aerolens-production.up.railway.app/data.html)

---

*AeroLens v1.0 — Built June 2025 — Open Source — Free Forever*
