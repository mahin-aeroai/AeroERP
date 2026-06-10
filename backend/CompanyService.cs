// ================================================================
// AeroERP — CompanyService.cs (full Dapper + Oracle implementation)
// Copy-paste ready for Visual Studio .NET 8
// ================================================================

using Dapper;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace AeroERP.API.Services;

// ── INTERFACES ───────────────────────────────────────────────────
public interface ICompanyService
{
    Task<PagedResult<CompanyDto>>  GetFilteredAsync(CompanyFilter filter);
    Task<CompanyDetailDto?>        GetByIdAsync(int id);
    Task<int>                      CreateAsync(CompanyCreateRequest req, int userId);
    Task                           UpdateAsync(int id, CompanyUpdateRequest req, int userId);
    Task                           SoftDeleteAsync(int id, int userId);
    Task<IEnumerable<CompanySearchResult>> GlobalSearchAsync(string q, string field);
    Task<byte[]>                   ExportCsvAsync(CompanyFilter filter);
    Task<TechProfileDto?>          GetTechProfileAsync(int companyId);
    Task                           UpsertTechProfileAsync(int companyId, TechProfileDto dto, int userId);
    Task<IEnumerable<FacilityDto>> GetFacilitiesAsync(int companyId);
    Task<int>                      AddFacilityAsync(int companyId, FacilityDto dto);
}

// ── DTOs ────────────────────────────────────────────────────────
public record CompanyDto(
    int     CompanyId,
    string  CompanyName,
    string  CompanyType,
    string? Headquarters,
    string? Website,
    string? IndustrySegment,
    string? AnnualRevenue,
    int?    EmployeeCount,
    string? Certifications,
    string? ExportCountries,
    bool    InternshipAvail,
    bool    GetProgram,
    int     ContactCount,
    int     OpenJobs,
    bool    UsesAI,
    bool    UsesML,
    bool    UsesDigitalTwin
);

public record CompanyDetailDto(
    CompanyDto                 Company,
    TechProfileDto?            TechProfile,
    IEnumerable<FacilityDto>   Facilities,
    IEnumerable<object>        RecentActivities
);

public record CompanyCreateRequest(
    string  CompanyName,
    string  CompanyType,
    string? Headquarters,
    string? Website,
    string? IndustrySegment,
    string? ParentCompany,
    int?    EmployeeCount,
    string? AnnualRevenue,
    int?    RevenueYear,
    string? Certifications,
    string? ExportCountries,
    string? StrategicPartners,
    string? CareersUrl,
    bool    InternshipAvail,
    bool    GetProgram,
    string? Notes
);

public record CompanyUpdateRequest : CompanyCreateRequest(
    string CompanyName, string CompanyType, string? Headquarters, string? Website,
    string? IndustrySegment, string? ParentCompany, int? EmployeeCount, string? AnnualRevenue,
    int? RevenueYear, string? Certifications, string? ExportCountries, string? StrategicPartners,
    string? CareersUrl, bool InternshipAvail, bool GetProgram, string? Notes
);

public record CompanyFilter(
    string? Name        = null,
    string? Type        = null,
    string? Segment     = null,
    string? City        = null,
    bool?   UsesAI      = null,
    bool?   HasGetProgram=null,
    bool?   HasInternship=null,
    int     Page        = 1,
    int     PageSize    = 20,
    string  SortBy      = "COMPANY_NAME",
    string  SortDir     = "ASC"
);

public record CompanySearchResult(int CompanyId, string CompanyName, string CompanyType, string? Headquarters, string MatchedField, string? Snippet);
public record TechProfileDto(int? TechId, int CompanyId, bool UsesAI, string? AiDetails, bool UsesMl, string? MlDetails, bool UsesEmbedded, bool UsesAvionics, bool UsesDigitalTwin, bool UsesAdditiveMfg, string? CadTools, string? SimulationTools, string? ProgrammingLangs, string? SoftwareCerts);
public record FacilityDto(int? FacilityId, int CompanyId, string? FacilityName, string? City, string? State, string? Country, string? FacilityType);
public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize);

// ── IMPLEMENTATION ────────────────────────────────────────────
public class CompanyService : ICompanyService
{
    private readonly OracleConnection _db;
    private readonly IAuditService   _audit;

    public CompanyService(OracleConnection db, IAuditService audit)
    { _db = db; _audit = audit; }

    // ── GET FILTERED (with pagination) ───────────────────────
    public async Task<PagedResult<CompanyDto>> GetFilteredAsync(CompanyFilter f)
    {
        // Safelist sortable columns
        var allowedSort = new HashSet<string> { "COMPANY_NAME","ANNUAL_REVENUE","EMPLOYEE_COUNT","COMPANY_ID" };
        var sortCol = allowedSort.Contains(f.SortBy.ToUpper()) ? f.SortBy.ToUpper() : "COMPANY_NAME";
        var sortDir = f.SortDir.ToUpper() == "DESC" ? "DESC" : "ASC";

        var conditions = new List<string> { "c.IS_ACTIVE = 'Y'" };
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(f.Name)) {
            conditions.Add("UPPER(c.COMPANY_NAME) LIKE UPPER(:name)");
            p.Add("name", $"%{f.Name}%");
        }
        if (!string.IsNullOrWhiteSpace(f.Type)) {
            conditions.Add("c.COMPANY_TYPE = :ctype"); p.Add("ctype", f.Type);
        }
        if (!string.IsNullOrWhiteSpace(f.Segment)) {
            conditions.Add("c.INDUSTRY_SEGMENT LIKE :seg"); p.Add("seg", $"%{f.Segment}%");
        }
        if (!string.IsNullOrWhiteSpace(f.City)) {
            conditions.Add("UPPER(c.HEADQUARTERS) LIKE UPPER(:city)"); p.Add("city", $"%{f.City}%");
        }
        if (f.UsesAI == true)      conditions.Add("tp.USES_AI = 'Y'");
        if (f.HasGetProgram == true) conditions.Add("c.GET_PROGRAM = 'Y'");
        if (f.HasInternship == true) conditions.Add("c.INTERNSHIP_AVAIL = 'Y'");

        var where = string.Join(" AND ", conditions);
        var offset = (f.Page - 1) * f.PageSize;

        var sql = $@"
            SELECT * FROM (
                SELECT c.COMPANY_ID, c.COMPANY_NAME, c.COMPANY_TYPE, c.HEADQUARTERS, c.WEBSITE,
                       c.INDUSTRY_SEGMENT, c.ANNUAL_REVENUE, c.EMPLOYEE_COUNT,
                       c.CERTIFICATIONS, c.EXPORT_COUNTRIES,
                       CASE WHEN c.INTERNSHIP_AVAIL='Y' THEN 1 ELSE 0 END AS INTERNSHIP_AVAIL,
                       CASE WHEN c.GET_PROGRAM='Y'      THEN 1 ELSE 0 END AS GET_PROGRAM,
                       (SELECT COUNT(*) FROM CRM_CONTACT cc WHERE cc.COMPANY_ID=c.COMPANY_ID AND cc.IS_ACTIVE='Y') AS CONTACT_COUNT,
                       (SELECT COUNT(*) FROM JOB_POSTING jp WHERE jp.COMPANY_ID=c.COMPANY_ID AND jp.STATUS='OPEN') AS OPEN_JOBS,
                       CASE WHEN tp.USES_AI='Y'          THEN 1 ELSE 0 END AS USES_AI,
                       CASE WHEN tp.USES_ML='Y'          THEN 1 ELSE 0 END AS USES_ML,
                       CASE WHEN tp.USES_DIGITAL_TWIN='Y' THEN 1 ELSE 0 END AS USES_DIGITAL_TWIN,
                       ROW_NUMBER() OVER (ORDER BY c.{sortCol} {sortDir}) AS RN,
                       COUNT(*) OVER () AS TOTAL_COUNT
                FROM MST_COMPANY c
                LEFT JOIN TECH_PROFILE tp ON tp.COMPANY_ID = c.COMPANY_ID
                WHERE {where}
            ) WHERE RN > :offset AND RN <= :limit";

        p.Add("offset", offset);
        p.Add("limit",  offset + f.PageSize);

        var rows = await _db.QueryAsync(sql, p);
        var items = rows.Select(r => new CompanyDto(
            (int)r.COMPANY_ID, r.COMPANY_NAME, r.COMPANY_TYPE, r.HEADQUARTERS, r.WEBSITE,
            r.INDUSTRY_SEGMENT, r.ANNUAL_REVENUE, (int?)r.EMPLOYEE_COUNT,
            r.CERTIFICATIONS, r.EXPORT_COUNTRIES,
            r.INTERNSHIP_AVAIL == 1, r.GET_PROGRAM == 1,
            (int)r.CONTACT_COUNT, (int)r.OPEN_JOBS,
            r.USES_AI == 1, r.USES_ML == 1, r.USES_DIGITAL_TWIN == 1
        )).ToList();

        var totalCount = rows.Any() ? (int)rows.First().TOTAL_COUNT : 0;
        return new PagedResult<CompanyDto>(items, totalCount, f.Page, f.PageSize);
    }

    // ── GET BY ID ─────────────────────────────────────────────
    public async Task<CompanyDetailDto?> GetByIdAsync(int id)
    {
        var sql = @"SELECT c.*, tp.USES_AI, tp.USES_ML, tp.CAD_TOOLS, tp.SIMULATION_TOOLS, tp.PROGRAMMING_LANGS
                    FROM MST_COMPANY c
                    LEFT JOIN TECH_PROFILE tp ON tp.COMPANY_ID = c.COMPANY_ID
                    WHERE c.COMPANY_ID = :id AND c.IS_ACTIVE = 'Y'";
        var row = await _db.QuerySingleOrDefaultAsync(sql, new { id });
        if (row == null) return null;

        var company = new CompanyDto(
            (int)row.COMPANY_ID, row.COMPANY_NAME, row.COMPANY_TYPE, row.HEADQUARTERS,
            row.WEBSITE, row.INDUSTRY_SEGMENT, row.ANNUAL_REVENUE, (int?)row.EMPLOYEE_COUNT,
            row.CERTIFICATIONS, row.EXPORT_COUNTRIES,
            row.INTERNSHIP_AVAIL == "Y", row.GET_PROGRAM == "Y",
            0, 0, row.USES_AI == "Y", row.USES_ML == "Y", false
        );

        var techProfile = row.USES_AI == null ? null :
            new TechProfileDto(null, id, row.USES_AI == "Y", null, row.USES_ML == "Y",
                null, false, false, false, false, row.CAD_TOOLS, row.SIMULATION_TOOLS, row.PROGRAMMING_LANGS, null);

        var facilities = await GetFacilitiesAsync(id);
        return new CompanyDetailDto(company, techProfile, facilities, Enumerable.Empty<object>());
    }

    // ── CREATE ────────────────────────────────────────────────
    public async Task<int> CreateAsync(CompanyCreateRequest req, int userId)
    {
        var sql = @"
            INSERT INTO MST_COMPANY
              (COMPANY_ID, COMPANY_NAME, COMPANY_TYPE, HEADQUARTERS, WEBSITE, INDUSTRY_SEGMENT,
               PARENT_COMPANY, EMPLOYEE_COUNT, ANNUAL_REVENUE, REVENUE_YEAR, CERTIFICATIONS,
               EXPORT_COUNTRIES, STRATEGIC_PARTNERS, CAREERS_URL, INTERNSHIP_AVAIL,
               GET_PROGRAM, NOTES, IS_ACTIVE, CREATED_ON, CREATED_BY)
            VALUES
              (SEQ_COMPANY.NEXTVAL, :cn, :ct, :hq, :web, :seg, :parent, :emp, :rev, :ryr,
               :certs, :exp, :partners, :carUrl,
               CASE WHEN :intern = 1 THEN 'Y' ELSE 'N' END,
               CASE WHEN :get    = 1 THEN 'Y' ELSE 'N' END,
               :notes, 'Y', SYSDATE, :uid)
            RETURNING COMPANY_ID INTO :newId";

        var p = new DynamicParameters();
        p.Add("cn",      req.CompanyName);
        p.Add("ct",      req.CompanyType);
        p.Add("hq",      req.Headquarters);
        p.Add("web",     req.Website);
        p.Add("seg",     req.IndustrySegment);
        p.Add("parent",  req.ParentCompany);
        p.Add("emp",     req.EmployeeCount);
        p.Add("rev",     req.AnnualRevenue);
        p.Add("ryr",     req.RevenueYear);
        p.Add("certs",   req.Certifications);
        p.Add("exp",     req.ExportCountries);
        p.Add("partners",req.StrategicPartners);
        p.Add("carUrl",  req.CareersUrl);
        p.Add("intern",  req.InternshipAvail ? 1 : 0);
        p.Add("get",     req.GetProgram ? 1 : 0);
        p.Add("notes",   req.Notes);
        p.Add("uid",     userId);
        p.Add("newId",   dbType: DbType.Int32, direction: ParameterDirection.Output);

        await _db.ExecuteAsync(sql, p);
        var newId = p.Get<int>("newId");
        await _audit.LogAsync("MST_COMPANY", newId, "INSERT", null, req, userId);
        return newId;
    }

    // ── UPDATE ────────────────────────────────────────────────
    public async Task UpdateAsync(int id, CompanyUpdateRequest req, int userId)
    {
        var sql = @"
            UPDATE MST_COMPANY SET
                COMPANY_NAME     = :cn,
                COMPANY_TYPE     = :ct,
                HEADQUARTERS     = :hq,
                WEBSITE          = :web,
                INDUSTRY_SEGMENT = :seg,
                PARENT_COMPANY   = :parent,
                EMPLOYEE_COUNT   = :emp,
                ANNUAL_REVENUE   = :rev,
                CERTIFICATIONS   = :certs,
                EXPORT_COUNTRIES = :exp,
                STRATEGIC_PARTNERS=:partners,
                CAREERS_URL      = :carUrl,
                INTERNSHIP_AVAIL = CASE WHEN :intern = 1 THEN 'Y' ELSE 'N' END,
                GET_PROGRAM      = CASE WHEN :get    = 1 THEN 'Y' ELSE 'N' END,
                NOTES            = :notes,
                UPDATED_ON       = SYSDATE,
                UPDATED_BY       = :uid
            WHERE COMPANY_ID = :id AND IS_ACTIVE = 'Y'";

        var p = new DynamicParameters();
        p.Add("cn", req.CompanyName); p.Add("ct", req.CompanyType); p.Add("hq", req.Headquarters);
        p.Add("web", req.Website); p.Add("seg", req.IndustrySegment); p.Add("parent", req.ParentCompany);
        p.Add("emp", req.EmployeeCount); p.Add("rev", req.AnnualRevenue); p.Add("certs", req.Certifications);
        p.Add("exp", req.ExportCountries); p.Add("partners", req.StrategicPartners); p.Add("carUrl", req.CareersUrl);
        p.Add("intern", req.InternshipAvail ? 1 : 0); p.Add("get", req.GetProgram ? 1 : 0);
        p.Add("notes", req.Notes); p.Add("uid", userId); p.Add("id", id);

        await _db.ExecuteAsync(sql, p);
        await _audit.LogAsync("MST_COMPANY", id, "UPDATE", null, req, userId);
    }

    // ── SOFT DELETE ───────────────────────────────────────────
    public async Task SoftDeleteAsync(int id, int userId)
    {
        await _db.ExecuteAsync(
            "UPDATE MST_COMPANY SET IS_ACTIVE='N', UPDATED_ON=SYSDATE, UPDATED_BY=:uid WHERE COMPANY_ID=:id",
            new { uid=userId, id });
        await _audit.LogAsync("MST_COMPANY", id, "DELETE", null, null, userId);
    }

    // ── GLOBAL SEARCH ─────────────────────────────────────────
    public async Task<IEnumerable<CompanySearchResult>> GlobalSearchAsync(string q, string field)
    {
        var sql = @"
            SELECT COMPANY_ID, COMPANY_NAME, COMPANY_TYPE, HEADQUARTERS,
                   'Company Name' AS MATCHED_FIELD, COMPANY_NAME AS SNIPPET
            FROM MST_COMPANY
            WHERE IS_ACTIVE='Y' AND UPPER(COMPANY_NAME) LIKE UPPER(:q)
            UNION ALL
            SELECT c.COMPANY_ID, c.COMPANY_NAME, c.COMPANY_TYPE, c.HEADQUARTERS,
                   'Technology' AS MATCHED_FIELD, tp.CAD_TOOLS AS SNIPPET
            FROM MST_COMPANY c JOIN TECH_PROFILE tp ON tp.COMPANY_ID=c.COMPANY_ID
            WHERE c.IS_ACTIVE='Y' AND UPPER(tp.CAD_TOOLS||' '||tp.PROGRAMMING_LANGS) LIKE UPPER(:q)
            UNION ALL
            SELECT c.COMPANY_ID, c.COMPANY_NAME, c.COMPANY_TYPE, c.HEADQUARTERS,
                   'Certification' AS MATCHED_FIELD, c.CERTIFICATIONS AS SNIPPET
            FROM MST_COMPANY c
            WHERE c.IS_ACTIVE='Y' AND UPPER(c.CERTIFICATIONS) LIKE UPPER(:q)
            FETCH FIRST 30 ROWS ONLY";

        var rows = await _db.QueryAsync(sql, new { q = $"%{q}%" });
        return rows.Select(r => new CompanySearchResult(
            (int)r.COMPANY_ID, r.COMPANY_NAME, r.COMPANY_TYPE, r.HEADQUARTERS,
            r.MATCHED_FIELD, r.SNIPPET));
    }

    // ── EXPORT CSV ────────────────────────────────────────────
    public async Task<byte[]> ExportCsvAsync(CompanyFilter filter)
    {
        var result = await GetFilteredAsync(filter with { Page=1, PageSize=9999 });
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Company ID,Company Name,Type,Headquarters,Segment,Revenue,Employees,Certifications,GET,Internship,Open Jobs");
        foreach (var c in result.Items)
            sb.AppendLine($"{c.CompanyId},\"{c.CompanyName}\",{c.CompanyType},\"{c.Headquarters}\",\"{c.IndustrySegment}\",\"{c.AnnualRevenue}\",{c.EmployeeCount},\"{c.Certifications}\",{c.GetProgram},{c.InternshipAvail},{c.OpenJobs}");
        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ── TECH PROFILE ──────────────────────────────────────────
    public async Task<TechProfileDto?> GetTechProfileAsync(int companyId)
    {
        var sql = "SELECT * FROM TECH_PROFILE WHERE COMPANY_ID = :id";
        var row = await _db.QuerySingleOrDefaultAsync(sql, new { id=companyId });
        if (row == null) return null;
        return new TechProfileDto((int)row.TECH_ID, companyId, row.USES_AI=="Y", row.AI_DETAILS,
            row.USES_ML=="Y", row.ML_DETAILS, row.USES_EMBEDDED=="Y", row.USES_AVIONICS=="Y",
            row.USES_DIGITAL_TWIN=="Y", row.USES_ADDITIVE_MFG=="Y", row.CAD_TOOLS,
            row.SIMULATION_TOOLS, row.PROGRAMMING_LANGS, row.CERTIFICATIONS_SW);
    }

    public async Task UpsertTechProfileAsync(int companyId, TechProfileDto dto, int userId)
    {
        var exists = await _db.QuerySingleOrDefaultAsync<int?>(
            "SELECT TECH_ID FROM TECH_PROFILE WHERE COMPANY_ID=:cid", new { cid=companyId }) != null;

        if (exists) {
            await _db.ExecuteAsync(@"UPDATE TECH_PROFILE SET
                USES_AI=:ai, AI_DETAILS=:aiD, USES_ML=:ml, ML_DETAILS=:mlD,
                USES_EMBEDDED=:emb, USES_AVIONICS=:avi, USES_DIGITAL_TWIN=:dt,
                USES_ADDITIVE_MFG=:am, CAD_TOOLS=:cad, SIMULATION_TOOLS=:sim,
                PROGRAMMING_LANGS=:pl, CERTIFICATIONS_SW=:csw, UPDATED_ON=SYSDATE, UPDATED_BY=:uid
                WHERE COMPANY_ID=:cid",
                new { ai=Y(dto.UsesAI), aiD=dto.AiDetails, ml=Y(dto.UsesMl), mlD=dto.MlDetails,
                      emb=Y(dto.UsesEmbedded), avi=Y(dto.UsesAvionics), dt=Y(dto.UsesDigitalTwin),
                      am=Y(dto.UsesAdditiveMfg), cad=dto.CadTools, sim=dto.SimulationTools,
                      pl=dto.ProgrammingLangs, csw=dto.SoftwareCerts, uid=userId, cid=companyId });
        } else {
            await _db.ExecuteAsync(@"INSERT INTO TECH_PROFILE (TECH_ID,COMPANY_ID,USES_AI,AI_DETAILS,USES_ML,ML_DETAILS,
                USES_EMBEDDED,USES_AVIONICS,USES_DIGITAL_TWIN,USES_ADDITIVE_MFG,CAD_TOOLS,
                SIMULATION_TOOLS,PROGRAMMING_LANGS,CERTIFICATIONS_SW,UPDATED_BY)
                VALUES (SEQ_TECHNOLOGY.NEXTVAL,:cid,:ai,:aiD,:ml,:mlD,:emb,:avi,:dt,:am,:cad,:sim,:pl,:csw,:uid)",
                new { cid=companyId, ai=Y(dto.UsesAI), aiD=dto.AiDetails, ml=Y(dto.UsesMl), mlD=dto.MlDetails,
                      emb=Y(dto.UsesEmbedded), avi=Y(dto.UsesAvionics), dt=Y(dto.UsesDigitalTwin),
                      am=Y(dto.UsesAdditiveMfg), cad=dto.CadTools, sim=dto.SimulationTools,
                      pl=dto.ProgrammingLangs, csw=dto.SoftwareCerts, uid=userId });
        }
    }

    // ── FACILITIES ────────────────────────────────────────────
    public async Task<IEnumerable<FacilityDto>> GetFacilitiesAsync(int companyId)
    {
        var rows = await _db.QueryAsync("SELECT * FROM MST_COMPANY_FACILITIES WHERE COMPANY_ID=:cid AND IS_ACTIVE='Y'", new { cid=companyId });
        return rows.Select(r => new FacilityDto((int)r.FACILITY_ID, companyId, r.FACILITY_NAME, r.CITY, r.STATE, r.COUNTRY, r.FACILITY_TYPE));
    }
    public async Task<int> AddFacilityAsync(int companyId, FacilityDto dto)
    {
        return await _db.ExecuteScalarAsync<int>(@"
            INSERT INTO MST_COMPANY_FACILITIES (COMPANY_ID,FACILITY_NAME,CITY,STATE,COUNTRY,FACILITY_TYPE)
            VALUES (:cid,:fn,:city,:state,:cntry,:ft) RETURNING FACILITY_ID INTO :id",
            new { cid=companyId, fn=dto.FacilityName, city=dto.City, state=dto.State, cntry=dto.Country???"India", ft=dto.FacilityType });
    }

    private static string Y(bool b) => b ? "Y" : "N";
}

// ================================================================
// DashboardService.cs — KPIs & Chart data from Oracle views
// ================================================================
public class DashboardService : IDashboardService
{
    private readonly OracleConnection _db;
    public DashboardService(OracleConnection db) => _db = db;

    public async Task<KpiDto> GetKpiAsync()
    {
        var row = await _db.QuerySingleAsync("SELECT * FROM V_DASHBOARD_KPI");
        return new KpiDto((int)row.TOTAL_COMPANIES, (int)row.DEFENCE_COMPANIES, (int)row.SPACE_COMPANIES,
            (int)row.DRONE_COMPANIES, (int)row.TOTAL_CONTACTS, (int)row.ACTIVE_OPPS,
            (decimal)row.PIPELINE_VALUE, (int)row.OPEN_JOBS, (int)row.UPCOMING_FOLLOWUPS, (int)row.AI_COMPANIES);
    }

    public async Task<IEnumerable<SegmentCountDto>> GetSegmentDistributionAsync()
    {
        var sql = @"SELECT INDUSTRY_SEGMENT AS SEGMENT, COUNT(*) AS CNT
                    FROM MST_COMPANY WHERE IS_ACTIVE='Y'
                    GROUP BY INDUSTRY_SEGMENT ORDER BY CNT DESC";
        return (await _db.QueryAsync(sql)).Select(r => new SegmentCountDto(r.SEGMENT, (int)r.CNT));
    }

    public async Task<IEnumerable<ChartPoint>> GetTechAdoptionAsync()
    {
        var sql = @"SELECT 'AI/ML' AS LABEL, COUNT(*) AS VALUE FROM TECH_PROFILE WHERE USES_AI='Y'
                    UNION ALL SELECT 'Digital Twin', COUNT(*) FROM TECH_PROFILE WHERE USES_DIGITAL_TWIN='Y'
                    UNION ALL SELECT 'Additive Mfg', COUNT(*) FROM TECH_PROFILE WHERE USES_ADDITIVE_MFG='Y'
                    UNION ALL SELECT 'Embedded SW',  COUNT(*) FROM TECH_PROFILE WHERE USES_EMBEDDED='Y'
                    UNION ALL SELECT 'Avionics SW',  COUNT(*) FROM TECH_PROFILE WHERE USES_AVIONICS='Y'";
        return (await _db.QueryAsync(sql)).Select(r => new ChartPoint(r.LABEL, (int)r.VALUE));
    }

    public async Task<IEnumerable<PipelineStageDto>> GetOpportunityFunnelAsync()
    {
        var sql = @"SELECT STATUS, COUNT(*) AS CNT, NVL(SUM(OPP_VALUE),0) AS TOTAL_VALUE
                    FROM BIZ_OPPORTUNITY WHERE IS_ACTIVE='Y'
                    GROUP BY STATUS ORDER BY MIN(ROWNUM)";
        return (await _db.QueryAsync(sql)).Select(r => new PipelineStageDto(r.STATUS, (int)r.CNT, (decimal)r.TOTAL_VALUE));
    }

    public async Task<IEnumerable<ActivityDto>> GetUpcomingActivitiesAsync()
    {
        var sql = @"SELECT ca.ACTIVITY_ID, ca.SUBJECT, ca.ACTIVITY_TYPE, ca.ACTIVITY_DATE,
                           c.COMPANY_NAME, ct.CONTACT_NAME
                    FROM CRM_ACTIVITY ca
                    JOIN MST_COMPANY c ON c.COMPANY_ID = ca.COMPANY_ID
                    LEFT JOIN CRM_CONTACT ct ON ct.CONTACT_ID = ca.CONTACT_ID
                    WHERE ca.STATUS='OPEN' AND ca.ACTIVITY_DATE <= SYSDATE + 7
                    ORDER BY ca.ACTIVITY_DATE FETCH FIRST 10 ROWS ONLY";
        return (await _db.QueryAsync(sql)).Select(r => new ActivityDto(
            (int)r.ACTIVITY_ID, r.SUBJECT, r.ACTIVITY_TYPE, (DateTime)r.ACTIVITY_DATE,
            r.COMPANY_NAME, r.CONTACT_NAME));
    }
}

// ── RECORDS ────────────────────────────────────────────────────
public record KpiDto(int TotalCompanies, int DefenceCompanies, int SpaceCompanies,
    int DroneCompanies, int TotalContacts, int ActiveOpps, decimal PipelineValue,
    int OpenJobs, int UpcomingFollowUps, int AiCompanies);
public record SegmentCountDto(string Segment, int Count);
public record ChartPoint(string Label, int Value);
public record PipelineStageDto(string Stage, int Count, decimal TotalValue);
public record ActivityDto(int Id, string Subject, string ActivityType, DateTime Date, string CompanyName, string? ContactName);

public interface IDashboardService {
    Task<KpiDto> GetKpiAsync();
    Task<IEnumerable<SegmentCountDto>> GetSegmentDistributionAsync();
    Task<IEnumerable<ChartPoint>> GetTechAdoptionAsync();
    Task<IEnumerable<PipelineStageDto>> GetOpportunityFunnelAsync();
    Task<IEnumerable<ActivityDto>> GetUpcomingActivitiesAsync();
}

// ================================================================
// AuthService.cs — JWT Authentication
// ================================================================
public class AuthService : IAuthService
{
    private readonly OracleConnection _db;
    private readonly IConfiguration   _cfg;

    public AuthService(OracleConnection db, IConfiguration cfg) { _db=db; _cfg=cfg; }

    public async Task<LoginResponse?> AuthenticateAsync(string username, string password)
    {
        var user = await _db.QuerySingleOrDefaultAsync(@"
            SELECT u.*, r.ROLE_NAME FROM ERP_USERS u
            JOIN ERP_ROLES r ON r.ROLE_ID = u.ROLE_ID
            WHERE (UPPER(u.USERNAME)=UPPER(:un) OR UPPER(u.EMAIL)=UPPER(:un))
              AND u.IS_ACTIVE='Y'", new { un=username });

        if (user == null) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, (string)user.PASSWORD_HASH)) return null;

        // Update last login
        await _db.ExecuteAsync("UPDATE ERP_USERS SET LAST_LOGIN=SYSTIMESTAMP WHERE USER_ID=:id", new { id=(int)user.USER_ID });

        var token = GenerateJwt((int)user.USER_ID, user.USERNAME, user.EMAIL, user.ROLE_NAME);
        return new LoginResponse(token, new UserInfo((int)user.USER_ID, user.USERNAME, user.FULL_NAME, user.EMAIL, user.ROLE_NAME));
    }

    private string GenerateJwt(int userId, string username, string email, string role)
    {
        var key   = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                      System.Text.Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var expiry= DateTime.UtcNow.AddHours(double.Parse(_cfg["Jwt:ExpiryHours"] ?? "8"));

        var claims = new[] {
            new System.Security.Claims.Claim("userId",   userId.ToString()),
            new System.Security.Claims.Claim("username", username),
            new System.Security.Claims.Claim("email",    email),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role),
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer:   _cfg["Jwt:Issuer"],
            audience: _cfg["Jwt:Audience"],
            claims:   claims,
            expires:  expiry,
            signingCredentials: creds);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    public Task<LoginResponse?> RefreshTokenAsync(string oldToken) => throw new NotImplementedException("Implement sliding expiry");
    public async Task ChangePasswordAsync(int userId, string oldPw, string newPw)
    {
        var hash = await _db.QuerySingleAsync<string>("SELECT PASSWORD_HASH FROM ERP_USERS WHERE USER_ID=:id", new{id=userId});
        if (!BCrypt.Net.BCrypt.Verify(oldPw, hash)) throw new UnauthorizedAccessException("Current password incorrect");
        var newHash = BCrypt.Net.BCrypt.HashPassword(newPw, 12);
        await _db.ExecuteAsync("UPDATE ERP_USERS SET PASSWORD_HASH=:h WHERE USER_ID=:id", new{h=newHash, id=userId});
    }
}

public record LoginResponse(string Token, UserInfo User);
public record UserInfo(int UserId, string Username, string FullName, string Email, string Role);
public interface IAuthService {
    Task<LoginResponse?> AuthenticateAsync(string username, string password);
    Task<LoginResponse?> RefreshTokenAsync(string token);
    Task ChangePasswordAsync(int userId, string oldPw, string newPw);
}

// ================================================================
// AuditService.cs — Write to AUD_LOG
// ================================================================
public class AuditService : IAuditService
{
    private readonly OracleConnection _db;
    public AuditService(OracleConnection db) => _db = db;

    public async Task LogAsync(string tableName, int recordId, string action,
        object? oldValues, object? newValues, int changedBy, string? ip = null)
    {
        await _db.ExecuteAsync(@"
            INSERT INTO AUD_LOG (LOG_ID,TABLE_NAME,RECORD_ID,ACTION,OLD_VALUES,NEW_VALUES,CHANGED_BY,CHANGED_ON,IP_ADDRESS)
            VALUES (SEQ_AUDIT.NEXTVAL,:tbl,:rid,:act,:old,:new,:uid,SYSTIMESTAMP,:ip)",
            new {
                tbl = tableName, rid = recordId, act = action,
                old = oldValues != null ? System.Text.Json.JsonSerializer.Serialize(oldValues) : (string?)null,
                new_ = newValues != null ? System.Text.Json.JsonSerializer.Serialize(newValues) : (string?)null,
                uid = changedBy, ip
            });
    }
}
public interface IAuditService {
    Task LogAsync(string tableName, int recordId, string action, object? oldValues, object? newValues, int changedBy, string? ip = null);
}
