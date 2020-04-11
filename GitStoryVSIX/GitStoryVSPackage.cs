using EnvDTE80;
using GitStory.Core;
using LibGit2Sharp;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace GitStoryVSIX
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the
	/// IVsPackage interface and uses the registration attributes defined in the framework to
	/// register itself and its components with the shell. These attributes tell the pkgdef creation
	/// utility what data to put into .pkgdef file.
	/// </para>
	/// <para>
	/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
	/// </para>
	/// </remarks>
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
	[Guid(GitStoryVSPackage.PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	public sealed class GitStoryVSPackage : AsyncPackage
	{
		/// <summary>
		/// GitsVSPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "1ca85a6f-8929-4d41-adcf-8bbafbbb6740";
		private DTE2 dte;
		private RunningDocTableEvents rdte;
		public Repository repo;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitStoryVSPackage"/> class.
		/// </summary>
		public GitStoryVSPackage()
		{
			// Inside this method you can place any initialization code that does not require
			// any Visual Studio service because at this point the package object is created but
			// not sited yet inside Visual Studio environment. The place to do all the other
			// initialization is the Initialize method.
		}

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.
			await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			try
			{
				dte = GetGlobalService(typeof(SDTE)) as DTE2;
				var statusBar = await GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;

				Action<string> print = str => {
					statusBar.IsFrozen(out int frozen);
					bool unlocked = frozen == 0;
					if (unlocked)
					{
						statusBar.SetText(str);
					}
				};

				var solutionDir = Path.GetDirectoryName(dte.Solution.FileName);
				repo = new Repository(solutionDir);
				rdte = new RunningDocTableEvents(this,
					OnAfterSaveFn: () => {
						print("Saving Story...");
						try
						{
							repo?.Store();
						}
						catch { }
					});
			}
			catch
			{
				rdte?.Dispose();
				rdte = null;

				repo?.Dispose();
				repo = null;
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				rdte?.Dispose();
				repo?.Dispose();
			}
		}

		#endregion
	}
}
