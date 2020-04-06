using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitStory.Core
{
	public static class LibGit2SharpEx
	{
		public static IEnumerable<Submodule> ForEachSubmodule(this IEnumerable<Submodule> submodules, Action<Submodule> fn)
		{
			var exceptions =
				submodules.Select(sm => {
					try { fn(sm); }
					catch (Exception e) { return e; }
					return null;
				})
				.Where(e => e != null)
				.ToArray();

			if (exceptions.Length != 0)
				throw new AggregateException(exceptions);

			return submodules;
		}
	}
}
