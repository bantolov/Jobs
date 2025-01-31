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

using System;

namespace Rhetos.Jobs.Hangfire
{
    /// <summary>
    /// Extended information on job, for job management before it is executed.
    /// </summary>
    internal class JobSchedule
	{
		public IJobParameter Job { get; set; }
		public Type ExecuterType { get; set; }
		public Type ParameterType { get; set; }
		public object AggregationGroup { get; set; }
		public Action EnqueueJob { get; set; }

		public string GetLogInfo() => Job.GetLogInfo(ExecuterType);
	}
}