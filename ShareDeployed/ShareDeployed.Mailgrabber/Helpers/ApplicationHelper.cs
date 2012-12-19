using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Windows.Threading;

namespace ShareDeployed.Mailgrabber.Helpers
{
	public static class ApplicationHelper
	{
		[SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public static void DoEvents(DispatcherPriority priority)
		{
			DispatcherFrame frame = new DispatcherFrame();
			DispatcherOperation oper = Dispatcher.CurrentDispatcher.BeginInvoke(priority,
				new DispatcherOperationCallback(ExitFrameOperation), frame);

			Dispatcher.PushFrame(frame);
			if (oper.Status != DispatcherOperationStatus.Completed)
			{
				oper.Abort();
			}
		}

		private static object ExitFrameOperation(object obj)
		{
			((DispatcherFrame)obj).Continue = false;
			return null;
		}

		[SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public static void DoEvents()
		{
			DoEvents(DispatcherPriority.Background);
		}

	}
}
