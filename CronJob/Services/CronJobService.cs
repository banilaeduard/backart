namespace CronJob
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Quartz;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class CronJobService<T> : BackgroundService, IDisposable
    {
        private System.Timers.Timer _timer;
        private bool completed = true;
        private readonly CronExpression _expression;
        protected readonly IServiceProvider services;

        protected CronJobService(string cronExpression, IServiceProvider services)
        {
            _expression = new CronExpression(cronExpression);
            this.services = services;
        }

        protected virtual async Task ScheduleJob(CancellationToken cancellationToken)
        {
            var next = _expression.GetNextValidTimeAfter(DateTimeOffset.Now);
            if (next.HasValue)
            {
                var delay = next.Value - DateTimeOffset.Now;
                if (delay.TotalMilliseconds <= 0 || !completed)   // prevent non-positive values from being passed into Timer
                {
                    await ScheduleJob(cancellationToken);
                }
                _timer = new System.Timers.Timer(delay.TotalMilliseconds);
                completed = false;
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
                            await DoWork(cancellationToken, scopedProcessingService, scope);

                            completed = true;
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

        public abstract Task DoWork(CancellationToken cancellationToken, IProcessor<T> processor, IServiceScope scope);

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
