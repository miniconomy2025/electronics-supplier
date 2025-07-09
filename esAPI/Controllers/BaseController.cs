
using esAPI.Data;
using esAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

#nullable enable
namespace esAPI.Controllers
{
    public partial class BaseController(AppDbContext context) : ControllerBase
    {

        protected readonly AppDbContext _context = context;

        protected async Task<Company?> GetOrganizationalUnitFromCertificateAsync()
        {
            var cert = await HttpContext.Connection.GetClientCertificateAsync();
            if (cert == null)
                return null;

            var dn = new System.Security.Cryptography.X509Certificates.X500DistinguishedName(cert.SubjectName.RawData);
            var dnString = dn.Format(true);

            var match = OrganizationalUnitRegex().Match(dnString);
            var organizationalUnit = match.Success ? match.Groups[1].Value.Trim() : null;

            if (organizationalUnit == null)
                return null;

            return await _context.Companies.FirstOrDefaultAsync(c => c.CompanyName == organizationalUnit);
        }

        [GeneratedRegex(@"OU=([^,\r\n]+)")]
        private static partial Regex OrganizationalUnitRegex();
    }
}