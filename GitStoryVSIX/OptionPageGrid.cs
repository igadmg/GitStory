using GitStory.Core;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;

namespace GitStoryVSIX
{
	public class OptionPageGrid : DialogPage
	{
		[Category("General")]
		[DisplayName("Enabled")]
		[Description("Enable or disable Git Story for current project (repository)")]
		public bool enaled {
			get => GitStoryVSPackage.instance?.Repo?.GetEnabled() ?? false;
			set => GitStoryVSPackage.instance?.Repo?.SetEnabled(value);
		}

		[Category("General")]
		[DisplayName("Repository UUID")]
		[Description("Repository GUID used to construct story branch names. Identify a repository where story is written, can be used to identify commiter across different repositories.")]
		public string guid {
			get => GitStoryVSPackage.instance?.Repo?.GetUuid() ?? string.Empty;
			set => GitStoryVSPackage.instance?.Repo?.SetUuid(value);
		}

		[Category("General")]
		[DisplayName("Story banch pattern")]
		[Description("Project story banch name pattern.")]
		public string branchNamePattern {
			get => GitStoryVSPackage.instance?.Repo?.GetStoryBranchNamePattern();
		}
	}
}
