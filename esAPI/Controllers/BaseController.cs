using esAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Controllers
{
    public class BaseController(AppDbContext context) : ControllerBase
    {
        protected readonly AppDbContext _context = context;
    }
}