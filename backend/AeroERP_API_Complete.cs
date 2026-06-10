// ================================================================
// AEROERP – .NET 8 WEB API – COMPLETE BACKEND
// File: AeroERP.API – Program.cs + Controllers + Models
// ================================================================

// ── Program.cs ─────────────────────────────────────────────────
/*
using AeroERP.API.Data;
using AeroERP.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Oracle.ManagedDataAccess.Client;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Oracle DB via Dapper
builder.Services.AddScoped<OracleConnection>(sp =>
    new OracleConnection(builder.Configuration.GetConnectionString("OracleDb")));

// Services DI
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IOpportunityService, OpportunityService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<ITechProfileService, TechProfileService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICrmActivityService, CrmActivityService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// JWT Auth
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt => {
        opt.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(opt => {
    opt.AddPolicy("AdminOnly",    p => p.RequireRole("SuperAdmin", "Admin"));
    opt.AddPolicy("BDAccess",     p => p.RequireRole("SuperAdmin", "Admin", "BDManager"));
    opt.AddPolicy("AnalystAccess",p => p.RequireRole("SuperAdmin", "Admin", "BDManager", "Analyst"));
    opt.AddPolicy("ViewerAccess", p => p.RequireRole("SuperAdmin", "Admin", "BDManager", "Analyst", "Viewer"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(opt => opt.AddDefaultPolicy(
    p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseSwagger(); app.UseSwaggerUI();
app.UseCors(); app.UseAuthentication(); app.UseAuthorization();
app.MapControllers();
app.Run();
*/

// ── appsettings.json ────────────────────────────────────────────
/*
{
  "ConnectionStrings": {
    "OracleDb": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=AERODB)));User Id=AEROERP;Password=Aero@2025;"
  },
  "Jwt": {
    "Key":      "AeroERP_SuperSecretKey_2025_Mahin!@#",
    "Issuer":   "AeroERP",
    "Audience": "AeroERPClients",
    "ExpiryHours": 8
  },
  "FileStorage": {
    "BasePath": "C:\\AeroERP\\Documents"
  }
}
*/

// ================================================================
// MODELS (DTOs)
// ================================================================

// -- CompanyDto.cs --
/*
namespace AeroERP.API.Models;
public class CompanyDto {
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public string CompanyType { get; set; } = "";
    public string? Headquarters { get; set; }
    public string? Website { get; set; }
    public string? IndustrySegment { get; set; }
    public string? ParentCompany { get; set; }
    public int? EmployeeCount { get; set; }
    public string? AnnualRevenue { get; set; }
    public int? RevenueYear { get; set; }
    public string? Certifications { get; set; }
    public string? ExportCountries { get; set; }
    public string? StrategicPartners { get; set; }
    public string? CareersUrl { get; set; }
    public bool InternshipAvail { get; set; }
    public bool GetProgram { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    // Computed
    public int ContactCount { get; set; }
    public int OppCount { get; set; }
    public int OpenJobs { get; set; }
    public int ActiveProjects { get; set; }
    public bool UsesAI { get; set; }
    public string? CadTools { get; set; }
}

public class CompanyFilter {
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Segment { get; set; }
    public string? City { get; set; }
    public bool? UsesAI { get; set; }
    public bool? HasGetProgram { get; set; }
    public bool? HasInternship { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "COMPANY_NAME";
    public string SortDir { get; set; } = "ASC";
}
*/

// ================================================================
// CONTROLLERS — all 10 modules
// ================================================================

// ── CompanyController.cs ────────────────────────────────────────
/*
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompanyController : ControllerBase {
    private readonly ICompanyService _svc;
    public CompanyController(ICompanyService svc) => _svc = svc;

    // GET api/Company?page=1&pageSize=20&name=HAL&segment=DEFENCE
    [HttpGet]
    [Authorize(Policy = "ViewerAccess")]
    public async Task<IActionResult> GetAll([FromQuery] CompanyFilter filter) {
        var result = await _svc.GetFilteredAsync(filter);
        return Ok(result);
    }

    // GET api/Company/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id) {
        var co = await _svc.GetByIdAsync(id);
        return co == null ? NotFound() : Ok(co);
    }

    // POST api/Company
    [HttpPost]
    [Authorize(Policy = "AnalystAccess")]
    public async Task<IActionResult> Create([FromBody] CompanyDto dto) {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var id = await _svc.CreateAsync(dto, GetUserId());
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    // PUT api/Company/5
    [HttpPut("{id}")]
    [Authorize(Policy = "AnalystAccess")]
    public async Task<IActionResult> Update(int id, [FromBody] CompanyDto dto) {
        await _svc.UpdateAsync(id, dto, GetUserId());
        return NoContent();
    }

    // DELETE api/Company/5 (soft delete)
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id) {
        await _svc.SoftDeleteAsync(id, GetUserId());
        return NoContent();
    }

    // GET api/Company/search?q=ANSYS&field=all
    [HttpGet("search")]
    public async Task<IActionResult> GlobalSearch([FromQuery] string q, [FromQuery] string field = "all") {
        var result = await _svc.GlobalSearchAsync(q, field);
        return Ok(result);
    }

    // GET api/Company/export?format=csv
    [HttpGet("export")]
    [Authorize(Policy = "AnalystAccess")]
    public async Task<IActionResult> Export([FromQuery] string format = "csv") {
        var bytes = await _svc.ExportAsync(format);
        return File(bytes, "text/csv", "companies.csv");
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst("userId")?.Value ?? "0");
}
*/

// ── OpportunityController.cs ────────────────────────────────────
/*
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpportunityController : ControllerBase {
    private readonly IOpportunityService _svc;
    public OpportunityController(IOpportunityService svc) => _svc = svc;

    [HttpGet]                      public async Task<IActionResult> GetAll([FromQuery] OppFilter f) => Ok(await _svc.GetFilteredAsync(f));
    [HttpGet("{id}")]              public async Task<IActionResult> GetById(int id)                 => Ok(await _svc.GetByIdAsync(id));
    [HttpPost]    [Authorize(Policy="BDAccess")]  public async Task<IActionResult> Create([FromBody] OppDto dto)         => Ok(await _svc.CreateAsync(dto, GetUserId()));
    [HttpPut("{id}")] [Authorize(Policy="BDAccess")] public async Task<IActionResult> Update(int id, [FromBody] OppDto dto) { await _svc.UpdateAsync(id, dto, GetUserId()); return NoContent(); }
    [HttpPatch("{id}/status")]     public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status) { await _svc.UpdateStatusAsync(id, status, GetUserId()); return NoContent(); }
    [HttpGet("pipeline-summary")]  public async Task<IActionResult> PipelineSummary() => Ok(await _svc.GetPipelineSummaryAsync());
    [HttpGet("by-company/{cid}")]  public async Task<IActionResult> ByCompany(int cid) => Ok(await _svc.GetByCompanyAsync(cid));
    private int GetUserId() => int.Parse(User.FindFirst("userId")?.Value ?? "0");
}
*/

// ── DashboardController.cs ─────────────────────────────────────
/*
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase {
    private readonly IDashboardService _svc;
    public DashboardController(IDashboardService svc) => _svc = svc;

    // GET api/Dashboard/kpi
    [HttpGet("kpi")]
    public async Task<IActionResult> Kpi() => Ok(await _svc.GetKpiAsync());

    // GET api/Dashboard/segment-distribution
    [HttpGet("segment-distribution")]
    public async Task<IActionResult> SegmentDist() => Ok(await _svc.GetSegmentDistributionAsync());

    // GET api/Dashboard/revenue-by-type
    [HttpGet("revenue-by-type")]
    public async Task<IActionResult> RevByType() => Ok(await _svc.GetRevenueByTypeAsync());

    // GET api/Dashboard/hiring-trends
    [HttpGet("hiring-trends")]
    public async Task<IActionResult> HiringTrends() => Ok(await _svc.GetHiringTrendsAsync());

    // GET api/Dashboard/tech-adoption
    [HttpGet("tech-adoption")]
    public async Task<IActionResult> TechAdoption() => Ok(await _svc.GetTechAdoptionAsync());

    // GET api/Dashboard/opportunity-funnel
    [HttpGet("opportunity-funnel")]
    public async Task<IActionResult> OppFunnel() => Ok(await _svc.GetOpportunityFunnelAsync());

    // GET api/Dashboard/upcoming-activities
    [HttpGet("upcoming-activities")]
    public async Task<IActionResult> UpcomingActivities() => Ok(await _svc.GetUpcomingActivitiesAsync());
}
*/

// ── AuthController.cs ──────────────────────────────────────────
/*
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase {
    private readonly IAuthService _svc;
    public AuthController(IAuthService svc) => _svc = svc;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req) {
        var result = await _svc.AuthenticateAsync(req.Username, req.Password);
        return result == null ? Unauthorized(new { message = "Invalid credentials" }) : Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req) =>
        Ok(await _svc.RefreshTokenAsync(req.Token));

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req) {
        await _svc.ChangePasswordAsync(int.Parse(User.FindFirst("userId")!.Value), req.OldPassword, req.NewPassword);
        return Ok();
    }
}
*/

// ================================================================
// FULL REST ENDPOINT MAP
// ================================================================
/*
BASE URL: https://api.aeroerp.local/api/

─── AUTH ───────────────────────────────────────────────────────
POST   /Auth/login                    Login → JWT token
POST   /Auth/refresh                  Refresh JWT
POST   /Auth/change-password          Change own password

─── COMPANY ─────────────────────────────────────────────────────
GET    /Company                       List with filters & pagination
GET    /Company/{id}                  Company details
POST   /Company                       Create company
PUT    /Company/{id}                  Update company
DELETE /Company/{id}                  Soft delete
GET    /Company/search?q=&field=      Global search
GET    /Company/export?format=csv     Export
GET    /Company/{id}/facilities       Company facilities
POST   /Company/{id}/facilities       Add facility
GET    /Company/{id}/tech-profile     Get tech profile
PUT    /Company/{id}/tech-profile     Upsert tech profile
GET    /Company/{id}/contacts         Contacts for company
GET    /Company/{id}/opportunities    Opportunities for company
GET    /Company/{id}/jobs             Jobs for company
GET    /Company/{id}/projects         Projects for company
GET    /Company/{id}/documents        Documents for company

─── CONTACT ─────────────────────────────────────────────────────
GET    /Contact                       List contacts
GET    /Contact/{id}                  Contact detail
POST   /Contact                       Create
PUT    /Contact/{id}                  Update
DELETE /Contact/{id}                  Delete
GET    /Contact/search?q=             Search contacts
GET    /Contact/{id}/activities       CRM activities for contact

─── OPPORTUNITY ─────────────────────────────────────────────────
GET    /Opportunity                   List pipeline
GET    /Opportunity/{id}              Detail
POST   /Opportunity                   Create
PUT    /Opportunity/{id}              Update
DELETE /Opportunity/{id}              Delete
PATCH  /Opportunity/{id}/status       Update status
GET    /Opportunity/pipeline-summary  Kanban / funnel summary
GET    /Opportunity/by-company/{cid}  By company

─── JOB ────────────────────────────────────────────────────────
GET    /Job                           List jobs
GET    /Job/{id}                      Detail
POST   /Job                           Create
PUT    /Job/{id}                      Update
PATCH  /Job/{id}/my-application       Track personal application
GET    /Job/open                      Open positions only
GET    /Job/by-type?type=GET          Filter by type

─── TECHNOLOGY ─────────────────────────────────────────────────
GET    /Technology                    List all tech profiles
GET    /Technology/ai-companies       Companies using AI
GET    /Technology/by-company/{cid}   By company
POST   /Technology                    Create
PUT    /Technology/{id}               Update

─── SUPPLIER ────────────────────────────────────────────────────
GET    /Supplier                      List
GET    /Supplier/{id}                 Detail
POST   /Supplier                      Create
PUT    /Supplier/{id}                 Update
PATCH  /Supplier/{id}/approve         Approve supplier
PATCH  /Supplier/{id}/audit           Update audit status

─── PROJECT ─────────────────────────────────────────────────────
GET    /Project                       List
GET    /Project/{id}                  Detail
POST   /Project                       Create
PUT    /Project/{id}                  Update
PATCH  /Project/{id}/status           Update status

─── DOCUMENT ────────────────────────────────────────────────────
GET    /Document?companyId=           List docs
POST   /Document/upload               Upload file (multipart)
GET    /Document/{id}/download        Download file
DELETE /Document/{id}                 Delete
GET    /Document/types                Document type list

─── CRM ACTIVITY ────────────────────────────────────────────────
GET    /CrmActivity?companyId=        Activity list
GET    /CrmActivity/{id}              Detail
POST   /CrmActivity                   Create activity
PUT    /CrmActivity/{id}              Update
PATCH  /CrmActivity/{id}/complete     Mark complete
GET    /CrmActivity/reminders         My pending reminders
GET    /CrmActivity/upcoming          Next 7 days activities

─── DASHBOARD ───────────────────────────────────────────────────
GET    /Dashboard/kpi                 Top-level KPIs
GET    /Dashboard/segment-distribution
GET    /Dashboard/revenue-by-type
GET    /Dashboard/hiring-trends
GET    /Dashboard/tech-adoption
GET    /Dashboard/opportunity-funnel
GET    /Dashboard/upcoming-activities

─── REPORTS ─────────────────────────────────────────────────────
GET    /Report/top-employers          Top 50 employers
GET    /Report/ai-companies           AI/ML companies
GET    /Report/defence-suppliers      Defence suppliers
GET    /Report/drone-startups         Drone startups
GET    /Report/space-startups         Space startups
GET    /Report/biz-opportunities      Pipeline report
GET    /Report/contact-directory      Full contact list
GET    /Report/generate?type=&fmt=    Generic report generator

─── LOOKUP ──────────────────────────────────────────────────────
GET    /Lookup?type=COMPANY_TYPE      Lookup values
POST   /Lookup                        Add lookup (Admin only)

─── USER/ROLE ───────────────────────────────────────────────────
GET    /User                          User list (Admin)
POST   /User                          Create user (Admin)
PUT    /User/{id}                     Update
GET    /Role                          Roles
GET    /Role/{id}/permissions         Role permissions
PUT    /Role/{id}/permissions         Update permissions
*/
