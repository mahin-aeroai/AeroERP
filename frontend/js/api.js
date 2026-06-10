// ================================================================
// AeroERP — API Service Layer (api.js)
// All REST calls with JWT auth, error handling, and retry logic
// ================================================================

const AeroAPI = (() => {
  const BASE = 'https://api.aeroerp.local/api';

  // ── AUTH ────────────────────────────────────────────────────
  function getToken() {
    return localStorage.getItem('aeroerp_token') || '';
  }
  function getHeaders(isMultipart = false) {
    const h = { 'Authorization': `Bearer ${getToken()}` };
    if (!isMultipart) h['Content-Type'] = 'application/json';
    return h;
  }
  async function handleResponse(res) {
    if (res.status === 401) { localStorage.clear(); window.location.href = 'login.html'; return; }
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: res.statusText }));
      throw new Error(err.message || `HTTP ${res.status}`);
    }
    if (res.status === 204) return null;
    return res.json();
  }

  // ── GENERIC CRUD ────────────────────────────────────────────
  async function GET(path) {
    const res = await fetch(`${BASE}${path}`, { headers: getHeaders() });
    return handleResponse(res);
  }
  async function POST(path, body) {
    const res = await fetch(`${BASE}${path}`, { method:'POST', headers:getHeaders(), body: JSON.stringify(body) });
    return handleResponse(res);
  }
  async function PUT(path, body) {
    const res = await fetch(`${BASE}${path}`, { method:'PUT', headers:getHeaders(), body: JSON.stringify(body) });
    return handleResponse(res);
  }
  async function PATCH(path, body = {}) {
    const res = await fetch(`${BASE}${path}`, { method:'PATCH', headers:getHeaders(), body: JSON.stringify(body) });
    return handleResponse(res);
  }
  async function DELETE(path) {
    const res = await fetch(`${BASE}${path}`, { method:'DELETE', headers:getHeaders() });
    return handleResponse(res);
  }
  async function UPLOAD(path, formData) {
    const res = await fetch(`${BASE}${path}`, { method:'POST', headers:getHeaders(true), body: formData });
    return handleResponse(res);
  }

  // ── AUTH ENDPOINTS ──────────────────────────────────────────
  const Auth = {
    login:          (username, password) => POST('/Auth/login', { username, password }),
    refresh:        (token) => POST('/Auth/refresh', { token }),
    changePassword: (oldPw, newPw) => POST('/Auth/change-password', { oldPassword:oldPw, newPassword:newPw }),
  };

  // ── MODULE 1: COMPANY ───────────────────────────────────────
  const Company = {
    getAll:       (params = {}) => GET('/Company?' + new URLSearchParams(params)),
    getById:      (id) => GET(`/Company/${id}`),
    create:       (dto) => POST('/Company', dto),
    update:       (id, dto) => PUT(`/Company/${id}`, dto),
    softDelete:   (id) => DELETE(`/Company/${id}`),
    search:       (q, field='all') => GET(`/Company/search?q=${encodeURIComponent(q)}&field=${field}`),
    export:       (format='csv') => GET(`/Company/export?format=${format}`),
    getFacilities:(id) => GET(`/Company/${id}/facilities`),
    addFacility:  (id, fac) => POST(`/Company/${id}/facilities`, fac),
    getTechProfile:(id) => GET(`/Company/${id}/tech-profile`),
    upsertTechProfile:(id, dto) => PUT(`/Company/${id}/tech-profile`, dto),
    getContacts:  (id) => GET(`/Company/${id}/contacts`),
    getOpportunities:(id) => GET(`/Company/${id}/opportunities`),
    getJobs:      (id) => GET(`/Company/${id}/jobs`),
    getProjects:  (id) => GET(`/Company/${id}/projects`),
    getDocuments: (id) => GET(`/Company/${id}/documents`),
  };

  // ── MODULE 2: CONTACT ───────────────────────────────────────
  const Contact = {
    getAll:       (params = {}) => GET('/Contact?' + new URLSearchParams(params)),
    getById:      (id) => GET(`/Contact/${id}`),
    create:       (dto) => POST('/Contact', dto),
    update:       (id, dto) => PUT(`/Contact/${id}`, dto),
    delete:       (id) => DELETE(`/Contact/${id}`),
    search:       (q) => GET(`/Contact/search?q=${encodeURIComponent(q)}`),
    getActivities:(id) => GET(`/Contact/${id}/activities`),
  };

  // ── MODULE 3: OPPORTUNITY ───────────────────────────────────
  const Opportunity = {
    getAll:       (params = {}) => GET('/Opportunity?' + new URLSearchParams(params)),
    getById:      (id) => GET(`/Opportunity/${id}`),
    create:       (dto) => POST('/Opportunity', dto),
    update:       (id, dto) => PUT(`/Opportunity/${id}`, dto),
    delete:       (id) => DELETE(`/Opportunity/${id}`),
    updateStatus: (id, status) => PATCH(`/Opportunity/${id}/status?status=${status}`),
    getPipelineSummary: () => GET('/Opportunity/pipeline-summary'),
    getByCompany: (cid) => GET(`/Opportunity/by-company/${cid}`),
  };

  // ── MODULE 4: JOB ───────────────────────────────────────────
  const Job = {
    getAll:       (params = {}) => GET('/Job?' + new URLSearchParams(params)),
    getById:      (id) => GET(`/Job/${id}`),
    create:       (dto) => POST('/Job', dto),
    update:       (id, dto) => PUT(`/Job/${id}`, dto),
    updateMyApp:  (id, dto) => PATCH(`/Job/${id}/my-application`, dto),
    getOpen:      () => GET('/Job/open'),
    getByType:    (type) => GET(`/Job/by-type?type=${type}`),
  };

  // ── MODULE 5: TECHNOLOGY ────────────────────────────────────
  const Technology = {
    getAll:       () => GET('/Technology'),
    getAiCompanies: () => GET('/Technology/ai-companies'),
    getByCompany: (cid) => GET(`/Technology/by-company/${cid}`),
    create:       (dto) => POST('/Technology', dto),
    update:       (id, dto) => PUT(`/Technology/${id}`, dto),
  };

  // ── MODULE 6: SUPPLIER ──────────────────────────────────────
  const Supplier = {
    getAll:       (params = {}) => GET('/Supplier?' + new URLSearchParams(params)),
    getById:      (id) => GET(`/Supplier/${id}`),
    create:       (dto) => POST('/Supplier', dto),
    update:       (id, dto) => PUT(`/Supplier/${id}`, dto),
    approve:      (id, status) => PATCH(`/Supplier/${id}/approve?status=${status}`),
    updateAudit:  (id, dto) => PATCH(`/Supplier/${id}/audit`, dto),
  };

  // ── MODULE 7: PROJECT ───────────────────────────────────────
  const Project = {
    getAll:       (params = {}) => GET('/Project?' + new URLSearchParams(params)),
    getById:      (id) => GET(`/Project/${id}`),
    create:       (dto) => POST('/Project', dto),
    update:       (id, dto) => PUT(`/Project/${id}`, dto),
    updateStatus: (id, status) => PATCH(`/Project/${id}/status?status=${status}`),
  };

  // ── MODULE 8: DOCUMENT ──────────────────────────────────────
  const Document = {
    getAll:       (companyId) => GET(`/Document?companyId=${companyId}`),
    upload:       (formData) => UPLOAD('/Document/upload', formData),
    download:     (id) => window.open(`${BASE}/Document/${id}/download?token=${getToken()}`, '_blank'),
    delete:       (id) => DELETE(`/Document/${id}`),
    getTypes:     () => GET('/Document/types'),
  };

  // ── MODULE 10: CRM ACTIVITY ─────────────────────────────────
  const CrmActivity = {
    getAll:       (params = {}) => GET('/CrmActivity?' + new URLSearchParams(params)),
    getById:      (id) => GET(`/CrmActivity/${id}`),
    create:       (dto) => POST('/CrmActivity', dto),
    update:       (id, dto) => PUT(`/CrmActivity/${id}`, dto),
    complete:     (id) => PATCH(`/CrmActivity/${id}/complete`),
    getReminders: () => GET('/CrmActivity/reminders'),
    getUpcoming:  () => GET('/CrmActivity/upcoming'),
  };

  // ── MODULE 9: DASHBOARD ─────────────────────────────────────
  const Dashboard = {
    getKpi:                () => GET('/Dashboard/kpi'),
    getSegmentDist:        () => GET('/Dashboard/segment-distribution'),
    getRevenueByType:      () => GET('/Dashboard/revenue-by-type'),
    getHiringTrends:       () => GET('/Dashboard/hiring-trends'),
    getTechAdoption:       () => GET('/Dashboard/tech-adoption'),
    getOpportunityFunnel:  () => GET('/Dashboard/opportunity-funnel'),
    getUpcomingActivities: () => GET('/Dashboard/upcoming-activities'),
  };

  // ── REPORTS ─────────────────────────────────────────────────
  const Report = {
    topEmployers:    () => GET('/Report/top-employers'),
    aiCompanies:     () => GET('/Report/ai-companies'),
    defenceSuppliers:() => GET('/Report/defence-suppliers'),
    droneStartups:   () => GET('/Report/drone-startups'),
    spaceStartups:   () => GET('/Report/space-startups'),
    bizOpportunities:() => GET('/Report/biz-opportunities'),
    contactDirectory:() => GET('/Report/contact-directory'),
    generate:        (type, fmt) => GET(`/Report/generate?type=${type}&fmt=${fmt}`),
  };

  // ── LOOKUP ──────────────────────────────────────────────────
  const Lookup = {
    getByType: (type) => GET(`/Lookup?type=${type}`),
    addValue:  (dto) => POST('/Lookup', dto),
  };

  // ── USER / ROLE ─────────────────────────────────────────────
  const User = {
    getAll:      () => GET('/User'),
    create:      (dto) => POST('/User', dto),
    update:      (id, dto) => PUT(`/User/${id}`, dto),
    getRoles:    () => GET('/Role'),
    getPermissions:(roleId) => GET(`/Role/${roleId}/permissions`),
    setPermissions:(roleId, perms) => PUT(`/Role/${roleId}/permissions`, perms),
  };

  // ── SEARCH (global) ─────────────────────────────────────────
  const Search = {
    global: (q) => GET(`/Company/search?q=${encodeURIComponent(q)}&field=all`),
  };

  // ── EXPORT HELPERS ──────────────────────────────────────────
  function downloadBlob(data, filename, mime = 'text/csv') {
    const blob = new Blob([data], { type: mime });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href = url; a.download = filename; a.click();
    URL.revokeObjectURL(url);
  }

  function buildQueryString(filters) {
    return Object.entries(filters)
      .filter(([, v]) => v !== '' && v !== null && v !== undefined)
      .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`)
      .join('&');
  }

  // ── PUBLIC API ───────────────────────────────────────────────
  return { Auth, Company, Contact, Opportunity, Job, Technology, Supplier, Project, Document, CrmActivity, Dashboard, Report, Lookup, User, Search, downloadBlob, buildQueryString };
})();

// ================================================================
// USAGE EXAMPLES:
// ================================================================
/*
// 1. Login
const result = await AeroAPI.Auth.login('admin', 'Aero@2025');
// → { token, user: { userId, username, role } }

// 2. Load companies with filters
const companies = await AeroAPI.Company.getAll({ segment:'DEFENCE', usesAI:true, page:1, pageSize:20 });
// → { items: [...], totalCount: 22, page: 1 }

// 3. Create opportunity
const opp = await AeroAPI.Opportunity.create({
  companyId: 1, projectName: 'AESA Module SW', oppValue: 45000000,
  status: 'NEW', probability: 60
});

// 4. Log CRM activity
await AeroAPI.CrmActivity.create({
  companyId: 1, contactId: 5, activityType: 'CALL',
  subject: 'GET Programme inquiry', activityDate: new Date().toISOString(),
  outcome: 'HR confirmed Aug openings', nextAction: 'Send resume', nextActionDate: '2025-06-20'
});

// 5. Track job application
await AeroAPI.Job.updateMyApp(7, { myAppStatus: 'APPLIED', myAppDate: new Date().toISOString() });

// 6. Get dashboard KPIs
const kpi = await AeroAPI.Dashboard.getKpi();
// → { totalCompanies:58, defenceCompanies:22, ... }

// 7. Upload document
const fd = new FormData();
fd.append('file', fileInput.files[0]);
fd.append('docType', 'Brochure');
fd.append('companyId', 1);
fd.append('docTitle', 'HAL Annual Report 2024');
await AeroAPI.Document.upload(fd);

// 8. Generate report
await AeroAPI.Report.generate('top-employers', 'xlsx');
*/
