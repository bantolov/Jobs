﻿using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Hangfire;
using Hangfire.States;

namespace Rhetos.Jobs.Hangfire
{
    public class BackgroundJob : IBackgroundJob
	{
		private readonly ISqlExecuter _sqlExecuter;
		private readonly IUserInfo _userInfo;
        private readonly RhetosHangfireInitialization _hangfireInitialization;
        private readonly ILogger _logger;
		private readonly ILogger _performanceLogger;

		private readonly List<JobSchedule> _jobInstances = new List<JobSchedule>();

		public BackgroundJob(
			ILogProvider logProvider,
			IPersistenceTransaction persistenceTransaction,
			ISqlExecuter sqlExecuter,
			IUserInfo userInfo,
			RhetosHangfireInitialization hangfireInitialization)
		{
			_sqlExecuter = sqlExecuter;
			_userInfo = userInfo;
            _hangfireInitialization = hangfireInitialization;
            _logger = logProvider.GetLogger(InternalExtensions.LoggerName);
			_performanceLogger = logProvider.GetLogger($"Performance.{InternalExtensions.LoggerName}");
			persistenceTransaction.BeforeClose += PersistenceTransactionOnBeforeClose;
		}

		private void PersistenceTransactionOnBeforeClose()
		{
			var stopWatch = Stopwatch.StartNew();

			foreach (var job in _jobInstances) 
				EnqueueToHangfire(job);

			_performanceLogger.Write(stopWatch, "Enqueue all jobs to Hangfire.");
		}

		private void EnqueueToHangfire(JobSchedule job)
		{
			_logger.Trace(()=> $"Enqueuing job in Hangfire.|{job.GetLogInfo()}");

			var commmand = $@"INSERT INTO RhetosJobs.HangfireJob (ID) VALUES('{job.Job.Id}')";

			_sqlExecuter.ExecuteSql(commmand);

			job.EnqueueJob.Invoke();
			_logger.Trace(() => $"Job enqueued in Hangfire.|{job.GetLogInfo()}");
		}

        public void AddJob<TExecuter, TParameter>(TParameter parameter, bool executeInUserContext, object aggregationGroup, JobAggregator<TParameter> jobAggregator, string queue = null)
			where TExecuter : IJobExecuter<TParameter>
        {
			_hangfireInitialization.InitializeGlobalConfiguration();

			var job = new JobSchedule
			{
				Job = new Job
				{
					Id = Guid.NewGuid(),
					ExecuteAsUser = executeInUserContext ? _userInfo.UserName : null,
					Parameter = parameter, // Might be updated later.
				},
				ExecuterType = typeof(TExecuter),
				ParameterType = typeof(TParameter),
				AggregationGroup = aggregationGroup,
				EnqueueJob = null, // Will be set later.
			};

			_logger.Trace(() => $"Enqueuing job.|{job.GetLogInfo()}");

			if (aggregationGroup != null)
			{
				var lastJobIndex = _jobInstances.FindLastIndex(oldJob =>
					job.ExecuterType == oldJob.ExecuterType
					&& job.ParameterType == oldJob.ParameterType
					&& job.Job.ExecuteAsUser == oldJob.Job.ExecuteAsUser
					&& job.AggregationGroup.Equals(oldJob.AggregationGroup));

				if (lastJobIndex >= 0)
				{
					if (jobAggregator == null)
						jobAggregator = DefaultAggregator;

					bool removeOld = jobAggregator((TParameter)_jobInstances[lastJobIndex].Job.Parameter, ref parameter);
					job.Job.Parameter = parameter;

					if (removeOld)
					{
						_logger.Trace(() => $"Previous instance of the same job removed from queue." +
							$"|New {job.GetLogInfo()}|Old {_jobInstances[lastJobIndex].GetLogInfo()}");
						_jobInstances.RemoveAt(lastJobIndex);
					}
				}
			}

			if (string.IsNullOrWhiteSpace(queue) || queue.ToLower() == "default")
			{
				// Not enqueuing immediately to Hangfire, to allow later duplicate jobs to suppress the current one.
				job.EnqueueJob = () => global::Hangfire.BackgroundJob.Enqueue<JobExecuter<TExecuter, TParameter>>(
					executer => executer.ExecuteUnitOfWork(job.Job));
			}
			else
			{
				// Not enqueuing immediately to Hangfire, to allow later duplicate jobs to suppress the current one.
				// Only way to use specific queue is to use new instance of BackgroundJobClient and set specific EnqueuedState.
				job.EnqueueJob = () =>
				{
					var client = new BackgroundJobClient();
					var state = new EnqueuedState(queue.ToLower());
					client.Create<JobExecuter<TExecuter, TParameter>>(e => e.ExecuteUnitOfWork(job.Job), state);
				};
			}

			_jobInstances.Add(job);
			_logger.Trace(() => $"Job enqueued.|{job.GetLogInfo()}");
		}

		/// <summary>
		/// By default, duplicate jobs in the same aggregation group are eliminated.
		/// </summary>
		private static bool DefaultAggregator<TParameter>(TParameter oldJob, ref TParameter newJob) => true;
	}
}