using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace GitStoryVSIX
{
	public class RunningDocTableEvents : IVsRunningDocTableEvents3, IVsRunningDocTableEvents4, IDisposable
	{
		RunningDocumentTable rdt;
		private uint cookie;

		public RunningDocTableEvents(IServiceProvider serviceProvider)
		{
			rdt = new RunningDocumentTable(serviceProvider);
			cookie = rdt.Advise(this);
		}

		public void Dispose()
		{
			rdt.Unadvise(cookie);
		}

		#region IVsRunningDocTableEvents4

		public int OnBeforeFirstDocumentLock(IVsHierarchy pHier, uint itemid, string pszMkDocument) => VSConstants.S_OK;
		public int OnAfterLastDocumentUnlock(IVsHierarchy pHier, uint itemid, string pszMkDocument, int fClosedWithoutSaving) => VSConstants.S_OK;

		public int OnAfterSaveAll()
		{
			return VSConstants.S_OK;
		}

#endregion IVsRunningDocTableEvents4

#region IVsRunningDocTableEvents3

		public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;
		public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;
		public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;
		public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) => VSConstants.S_OK;
		public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => VSConstants.S_OK;
		public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew) => VSConstants.S_OK;
		public int OnBeforeSave(uint docCookie) => VSConstants.S_OK;

		public int OnAfterSave(uint docCookie)
		{
			return VSConstants.S_OK;
		}

		#endregion IVsRunningDocTableEvents3
	}
}
