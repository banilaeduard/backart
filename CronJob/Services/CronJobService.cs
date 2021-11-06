namespace CronJob
{
    using core;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Quartz;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class CronJobService<T> : BackgroundService, IDisposable
    {
        private DateTimeOffset lastRun = DateTimeOffset.MinValue;
        private System.Timers.Timer _timer;

        private readonly CronExpression _expression;
        protected readonly IServiceProvider services;
        private readonly int _defaultRetry;

        protected CronJobService(string cronExpression, IServiceProvider services, AppSettings appSettings)
        {
            _defaultRetry = int.Parse(appSettings.retrySeconds);
            _expression = new CronExpression(cronExpression);
            this.services = services;
        }

        protected virtual async Task ScheduleJob(CancellationToken cancellationToken)
        {
            var next = _expression.GetNextValidTimeAfter(lastRun);
            if (next.HasValue)
            {
                var delay = next.Value - DateTimeOffset.Now;
                _timer = new System.Timers.Timer(Math.Max(_defaultRetry, (int)delay.TotalMilliseconds));
                _timer.Elapsed += async (sender, args) =>
                {
                    _timer.Dispose();  // reset and dispose timer
                    _timer = null;
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        using (var scope = services.CreateScope())
                        {
                            var scopedProcessingService =
                                scope.ServiceProvider
                                    .GetRequiredService<IProcessor<T>>();
                            if (await DoWork(cancellationToken, scopedProcessingService, scope))
                            {
                                lastRun = DateTimeOffset.Now;
                            }
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await ScheduleJob(cancellationToken);    // reschedule next
                    }
                };
                _timer.Start();
            }
            await Task.CompletedTask;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ScheduleJob(stoppingToken);
        }

        public abstract Task<Boolean> DoWork(CancellationToken cancellationToken, IProcessor<T> processor, IServiceScope scope);

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Stop();
            await base.StopAsync(cancellationToken);
        }                    
        public override void Dispose()
        {
            base.Dispose();
            _timer?.Dispose();
        }
    }
}
