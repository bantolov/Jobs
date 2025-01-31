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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RhetosJobs;

namespace Rhetos.Jobs.Test
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void Happy()
		{
			using (var scope = RhetosProcessHelper.CreateScope())
			{
				var repository = scope.Resolve<Common.DomRepository>();
				repository.RhetosJobs.Happy.Execute(new Happy());
				scope.CommitChanges();
			}
		}

		[TestMethod]
		public void HappyWithWait()
		{
			using (var scope = RhetosProcessHelper.CreateScope())
			{
				var repository = scope.Resolve<Common.DomRepository>();
				repository.RhetosJobs.HappyWithWait.Execute(new HappyWithWait());
				scope.CommitChanges();
			}
		}
	}
}
