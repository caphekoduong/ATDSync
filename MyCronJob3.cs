using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ATDSync
{
    public class MyCronJob3 : CronJobService
    {
        private readonly ILogger<MyCronJob3> _logger;
        private readonly IConfiguration _configuration;
        private SyncService syncService;

        public MyCronJob3(IScheduleConfig<MyCronJob3> config, ILogger<MyCronJob3> logger, IConfiguration configuration)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
            _configuration = configuration;
            syncService = new SyncService(_configuration.GetConnectionString("FromDB"), _configuration.GetConnectionString("ToDB"));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CronJob 3 starts.");
            return base.StartAsync(cancellationToken);
        }

        public override Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} CronJob 3 is working.");
            try
            {
                syncService.SyncAttendances();
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.Message);
            }
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CronJob 3 is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }

    public interface IScheduleConfig<T>
    {
        string CronExpression { get; set; }
        TimeZoneInfo TimeZoneInfo { get; set; }
    }

    public class ScheduleConfig<T> : IScheduleConfig<T>
    {
        public string CronExpression { get; set; }
        public TimeZoneInfo TimeZoneInfo { get; set; }
    }
}