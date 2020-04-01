using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitStory.Core
{
	public class SourceHeatmap
	{
		private string path;

		public SourceHeatmap(string path)
		{
			this.path = path;
		}

		public void Prepend(PatchEntryChanges changes)
		{

		}
	}
}
