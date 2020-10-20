using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ATDSync.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ATDSync.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;
        private SyncService syncService;

        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            syncService = new SyncService(_configuration.GetConnectionString("FromDB"), _configuration.GetConnectionString("ToDB"));
        }

        public List<Attendance> atts { get; set; }
        public string fromDb { get; set; }
        public string toDb { get; set; }

        public void OnGet(int sync)
        {
            atts = new List<Attendance>();
            try
            {
                fromDb = syncService.getNpgDbName();
                toDb = syncService.getSqlDbName();

                if(string.IsNullOrEmpty(fromDb) || string.IsNullOrEmpty(toDb))
                {
                    _logger.LogInformation("Cannot connect to database");
                }
                else
                {
                    atts = syncService.GetAttendances(sync);
                }
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.Message);
            }
        }

        public IActionResult OnPost()
        {
            try
            {
                syncService.SyncAttendances();
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.Message);
            }

            return RedirectToPage("Index", new { sync = 1 });
        }
    }
}
