using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Models;

namespace MyFinances.Pages.Organisations
{
    public class OrgIndexModel : PageModel
    {
        private readonly Data.AppDBContext _context;

        public List<OrganisationType> _orgTypes = [];

        public OrgIndexModel(Data.AppDBContext context)
        {
            _context = context;
        }

        public IList<Organisation> Organisation { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Organisation = await _context.Organisations.OrderBy(m => m.IdOrg).ToListAsync();
            _orgTypes = await _context.OrganisationTypes.ToListAsync();
        }
    }
}
