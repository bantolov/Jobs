﻿/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Autofac;
using Hangfire;
using System;
using System.Collections.Generic;

namespace Rhetos.Jobs.Hangfire
{
    /// <summary>
    /// Initializes Hangfire server for background job processing in a Rhetos applications.
    /// </summary>
    /// <remarks>
    /// The jobs server initialization is called automatically in a Rhetos web application startup (<see cref="IService"/> implementation).
    /// In other processes, for example CLI utilities or unit tests, call <see cref="ConfigureHangfireJobServers"/> before creating 
    /// <see cref="BackgroundJobServer"/> to start job processing in the current application process.
    /// </remarks>
    public static class RhetosJobServer
    {
        /// <summary>Using WeakReference to avoid interfering with the DI container disposal, since this is a static field.</summary>
        private static WeakReference<ILifetimeScope> _containerReference;
        private static Action<ContainerBuilder> _customRegistrations;

        /// <summary>
        /// Initializes Hangfire for background job processing of Rhetos jobs.
        /// </summary>
        /// <remarks>
        /// Call this method before calling <see cref="CreateHangfireJobServer"/> in a CLI utility or unit tests.
        /// This method is automatically called in Rhetos web application startup.
        /// It calls <see cref="RhetosHangfireInitialization.InitializeGlobalConfiguration"/> and provides DI container to Hangfire for job instancing.
        /// </remarks>
        /// <param name="container">
        /// Provide the Rhetos DI container from the current application.
        /// It will be used by Hangfire to resolve infrastructure components for job execution (from the root scope).
        /// It will be used by Rhetos Hangfire integration components to create a new DI lifetime scope (unit of work)
        /// for each job executer (<see cref="IJobExecuter{TParameter}"/> implementations).
        /// </param>
        /// <param name="customRegistrations">
        /// The custom registrations are available for <see cref="IJobExecuter{TParameter}"/> implementations.
        /// </param>
        public static void ConfigureHangfireJobServers(ILifetimeScope container, Action<ContainerBuilder> customRegistrations = null)
        {
            if (container is null)
                throw new ArgumentNullException(nameof(container));
            _containerReference = new WeakReference<ILifetimeScope>(container);
            _customRegistrations = customRegistrations;

            container.Resolve<RhetosHangfireInitialization>().InitializeGlobalConfiguration();
            GlobalConfiguration.Configuration.UseAutofacActivator(container);
        }

        /// <summary>
        /// Create a new instance of Hangfire <see cref="BackgroundJobServer"/>.
        /// It uses application-specific configuration from <see cref="RhetosJobHangfireOptions"/> for <see cref="BackgroundJobServerOptions"/>.
        /// </summary>
        /// <remarks>
        /// The Hangfire BackgroundJobServer will start processing background jobs immediately.
        /// </remarks>
        public static BackgroundJobServer CreateHangfireJobServer()
        {
            var container = GetContainer();
            var options = container.Resolve<RhetosJobHangfireOptions>();
            return new BackgroundJobServer(new BackgroundJobServerOptions
            {
                WorkerCount = options.WorkerCount,
                ShutdownTimeout = TimeSpan.FromSeconds(options.ShutdownTimeout),
                StopTimeout = TimeSpan.FromSeconds(options.StopTimeout),
                SchedulePollingInterval = TimeSpan.FromSeconds(options.SchedulePollingInterval),
                HeartbeatInterval = TimeSpan.FromSeconds(options.HeartbeatInterval),
                ServerTimeout = TimeSpan.FromSeconds(options.ServerTimeout),
                ServerCheckInterval = TimeSpan.FromSeconds(options.ServerCheckInterval),
                CancellationCheckInterval = TimeSpan.FromSeconds(options.CancellationCheckInterval),
                Queues = options.Queues,
            });
        }

        /// <summary>
        /// Creates a new unit of work scope, before executing a <see cref="IJobExecuter{TParameter}"/> implementation.
        /// </summary>
        internal static TransactionScopeContainer CreateScope(Action<ContainerBuilder> customizeScope)
        {
            var container = GetContainer();
            return new TransactionScopeContainer((IContainer)container, customizeScope + _customRegistrations);
        }

        private static ILifetimeScope GetContainer()
        {
            if (_containerReference == null)
                throw new InvalidOperationException($"{nameof(RhetosJobServer)} not initialized. Call {nameof(RhetosJobServer)}.{nameof(ConfigureHangfireJobServers)} first, if directly executing background jobs.");
            if (!_containerReference.TryGetTarget(out var container))
                throw new InvalidOperationException($"The previously provided DI container has been disposed. Call {nameof(RhetosJobServer)}.{nameof(ConfigureHangfireJobServers)} again, if directly executing background jobs.");
            return container;
        }
    }
}
