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
    /// Job parameters required for job execution.
    /// It is serialized to the job queue storage before executing it.
    /// </summary>
    internal class JobParameter<TParameter> : IJobParameter
	{
		public Guid Id { get; set; }
		public string ExecuteAsUser { get; set; }
		public TParameter Parameter { get; set; }

		public string GetLogInfo(Type executerType)
		{
			var userInfo = string.IsNullOrWhiteSpace(ExecuteAsUser) ? "User not specified" : $"ExecuteInUserContext: {ExecuteAsUser}";
			return $"JobId: {Id}|{userInfo}|{executerType}|{Parameter}";
		}
	}
}