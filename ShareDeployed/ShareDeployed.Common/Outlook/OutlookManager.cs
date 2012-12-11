using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Office.Interop.Outlook;

namespace ShareDeployed.Common.Outlook
{
	public class NewMailReceivedEventArgs : EventArgs
	{
		public NewMailReceivedEventArgs() : base() { }

		public NewMailReceivedEventArgs(string subject, string body, string fromEmail, string fromUser)
		{
			Subject = subject;
			FromEmail = fromEmail;
			FromUser = fromUser;
			Body = body;
		}

		public NewMailReceivedEventArgs(string subject, string body, string fromEmail)
		{
			Subject = subject;
			FromEmail = fromEmail;
			FromUser = string.Empty;
			Body = body;
		}

		public string FromEmail { get; private set; }
		public string FromUser { get; private set; }
		public string Subject { get; private set; }
		public string Body { get; private set; }
	}

	public sealed class OutlookManager : IDisposable
	{
		bool _disposed = false;
		Microsoft.Office.Interop.Outlook.Application m_outlookApp = null;
		Microsoft.Office.Interop.Outlook.MAPIFolder m_inboxFolder = null;
		Microsoft.Office.Interop.Outlook.MAPIFolder m_DicomFolder = null;
		Microsoft.Office.Interop.Outlook.Folders m_allFolders = null;

		Microsoft.Office.Interop.Outlook.NameSpace m_outNameSpace = null;
		System.Security.Principal.WindowsImpersonationContext m_impContext = null;

		private List<string> m_FoldersToMonitor = null;
		private Dictionary<string, MAPIFolder> m_foldToMonitorInstances = null;
		public List<string> FoldersInOutlook { get; set; }

		public event EventHandler<NewMailReceivedEventArgs> MailReceived;

		/// <summary>
		/// ctor
		/// </summary>
		/// <exception cref="System.Runtime.InteropServices.COMException"/>
		public OutlookManager()
		{
			var current = System.Security.Principal.WindowsIdentity.GetCurrent();
			m_impContext = current.Impersonate();

			System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName("OUTLOOK");
			int collCount = processes.Length;

			if (collCount != 0)
			{
				#region comment
				//try
				//{
				//    // create an application instance of Outlook
				//    oApp = new Microsoft.Office.Interop.Outlook.Application();
				//}
				//catch (System.Exception ex)
				//{
				//    try
				//    {
				//        // get Outlook in another way
				//        oApp = Marshal.GetActiveObject("Outlook.Application") as Microsoft.Office.Interop.Outlook.Application;
				//    }
				//    catch (System.Exception ex2)
				//    {
				//        // try some other way to get the object
				//        oApp = Activator.CreateInstance(Type.GetTypeFromProgID("Outlook.Application")) as Microsoft.Office.Interop.Outlook.Application;
				//    }
				//} 
				#endregion

				try
				{
					// Outlook already running, hook into the Outlook instance
					m_outlookApp = System.Runtime.InteropServices.Marshal.GetActiveObject("Outlook.Application") as Microsoft.Office.Interop.Outlook.Application;
				}
				catch (System.Runtime.InteropServices.COMException ex)
				{
					if (ex.ErrorCode == -2147221021)
						m_outlookApp = new Application();
					else
						throw;
				}
			}
			else
				m_outlookApp = new Microsoft.Office.Interop.Outlook.Application();

			m_outNameSpace = m_outlookApp.GetNamespace("MAPI");
			m_allFolders = m_outNameSpace.Folders;

			m_outlookApp.NewMailEx += new ApplicationEvents_11_NewMailExEventHandler(outLookApp_NewMailEx);

			this.FoldersInOutlook = new List<string>();

			m_inboxFolder = m_outNameSpace.GetDefaultFolder(Microsoft.Office.Interop.Outlook.OlDefaultFolders.olFolderInbox);
			m_FoldersToMonitor = new List<string>();
			m_foldToMonitorInstances = new Dictionary<string, MAPIFolder>(StringComparer.OrdinalIgnoreCase);

			MAPIFolder mUserFolder = m_inboxFolder.Parent;
			if (mUserFolder != null)
			{
				foreach (object folder in mUserFolder.Folders)
				{
					MAPIFolder mapiFolder = folder as MAPIFolder;
					if (mapiFolder != null)
					{
						this.FoldersInOutlook.Add(mapiFolder.Name);
					}
				}
			}
		}

		public void SetFoldersToMonitor(IEnumerable<string> data)
		{
			if (m_FoldersToMonitor.Count > 0)
				m_FoldersToMonitor.Clear();

			foreach (string item in data)
				m_FoldersToMonitor.Add(item);

			MAPIFolder root = m_inboxFolder.Parent;
			foreach (string item in m_FoldersToMonitor)
			{
				if (!m_foldToMonitorInstances.ContainsKey(item))
				{
					foreach (Microsoft.Office.Interop.Outlook.MAPIFolder folder in root.Folders)
					{
						if (folder.Name.Contains(item))
							m_foldToMonitorInstances[item] = folder;
					}
				}
			}

			if (!m_foldToMonitorInstances.ContainsKey(m_inboxFolder.Name))
				m_foldToMonitorInstances[m_inboxFolder.Name] = m_inboxFolder;
		}

		public List<OutlookMailInfo> GetEmailListFromFolder(string folderName)
		{
			if (m_foldToMonitorInstances.ContainsKey(folderName))
			{
				List<OutlookMailInfo> data = new List<OutlookMailInfo>();
				int count = m_foldToMonitorInstances[folderName].Items.Count;
				for (int i = count; i > 0; i--)
				{
					MailItem mail = m_foldToMonitorInstances[folderName].Items[i] as MailItem;
					if (mail != null)
					{
						OutlookMailInfo info = MailInfoFromMailItem(mail);
						data.Add(info);
					}

				}
				return data;
			}
			return null;
		}

		public OutlookMailInfo GetEmailByItEntryId(string entryId)
		{
			if (string.IsNullOrEmpty(entryId))
				throw new ArgumentNullException("entryId");

			MailItem item = m_outlookApp.Session.GetItemFromID(entryId, Type.Missing) as MailItem;
			if (item != null)
			{
				return MailInfoFromMailItem(item);
			}
			return null;
		}

		private OutlookMailInfo MailInfoFromMailItem(MailItem mail)
		{
			OutlookMailInfo info = new OutlookMailInfo();
			info.Subject = mail.Subject;
			info.Body = mail.Body;
			info.CC = mail.CC;
			info.SenderEmail = mail.SenderEmailAddress;
			info.SentDate = mail.SentOn;
			info.To = mail.To;
			info.EntryID = mail.EntryID;
			info.SenderName = mail.SenderName;

			if (mail.Attachments != null && mail.Attachments.Count > 0)
				info.HasAttachments = true;

			info.UnRead = mail.UnRead;
			return info;
		}

		public int GetUnreadCountFromFolder(string folderName)
		{
			if (m_foldToMonitorInstances.ContainsKey(folderName))
			{
				return m_foldToMonitorInstances[folderName].UnReadItemCount;
			}
			return 0;
		}

		#region events generators
		private void OnNewMailReceived(MailItem item)
		{
			OnMailReceivedExt(new NewMailReceivedEventArgs(item.Subject, item.Body, item.SenderEmailAddress, item.SenderName));
		}

		private void OnMailReceivedExt(NewMailReceivedEventArgs args)
		{
			if (args == null)
				throw new ArgumentNullException("args");

			EventHandler<NewMailReceivedEventArgs> handler = MailReceived;
			if (handler != null)
			{
				handler(this, args);
			}
		}
		#endregion

		private void outLookApp_NewMailEx(string EntryIDCollection)
		{
			MAPIFolder mFolder = m_outlookApp.Session.GetDefaultFolder(OlDefaultFolders.olFolderInbox);
			// Do something interesting when a new e-mail arrives. 
			MailItem newMail = m_outNameSpace.GetItemFromID(EntryIDCollection, mFolder.StoreID) as MailItem;
			if (newMail != null)
			{
				Console.WriteLine("----------------");
				Console.WriteLine(newMail.SenderName);
				Console.WriteLine(newMail.Subject);
				Console.WriteLine(newMail.Body);
				Console.WriteLine("----------------");
			}
			OnNewMailReceived(newMail);
		}

		public void DisplayInbox()
		{
			// Get items in my inbox. 
			MAPIFolder inboxFolder = m_outNameSpace.GetDefaultFolder(OlDefaultFolders.olFolderInbox);
			// Print out some basic info. 
			Console.WriteLine("You have {0} e-mails.", inboxFolder.Items.Count);
			Console.WriteLine();
			foreach (object obj in inboxFolder.Items)
			{
				MailItem item = obj as MailItem;
				if (item != null)
				{
					Console.WriteLine("-> Received: {0}",
					  item.ReceivedTime.ToString());
					Console.WriteLine("-> Sender: {0}", item.SenderName);
					Console.WriteLine("-> Subject: {0}", item.Subject);
					Console.WriteLine();
				}
			}
		}

		public void SendNewMail()
		{
			// Create a new MailItem.
			MailItem myMail = m_outlookApp.CreateItem(OlItemType.olMailItem) as MailItem;
			// Now gather input from user. 
			Console.Write("Receiver Name: ");
			myMail.Recipients.Add(Console.ReadLine());
			Console.Write("Subject: ");
			myMail.Subject = Console.ReadLine();
			Console.Write("Message Body: ");
			myMail.Body = Console.ReadLine();
			// Send it!
		}

		private void SearchInBoxBySubject(string subj)
		{
			MAPIFolder inbox = m_outlookApp.Session.GetDefaultFolder(OlDefaultFolders.olFolderInbox);
			Items items = inbox.Items;
			MailItem mailItem = null;
			object folderItem;
			string subjectName = string.Empty;
			string filter = "[Subject] > 's' And [Subject] <'u'";
			folderItem = items.Find(filter);
			while (folderItem != null)
			{
				mailItem = folderItem as MailItem;
				if (mailItem != null)
				{
					subjectName += "\n" + mailItem.Subject;
				}
				folderItem = items.FindNext();
			}
			subjectName = " The following e-mail messages were found: " + subjectName;
			Console.WriteLine(subjectName);
		}

		private void SetCurrentFolder()
		{
			string folderName = "TestFolder";
			MAPIFolder inBox = (MAPIFolder)m_outlookApp.Session.GetDefaultFolder(OlDefaultFolders.olFolderInbox);
			try
			{
				Folder fold = inBox.Folders[folderName] as Folder;
				fold.Display();
			}
			catch
			{
				Console.WriteLine("There is no folder named " + folderName + ".");
			}
		}

		public List<OutlookMailInfo> GetMailList()
		{
			Microsoft.Office.Interop.Outlook.MailItem item = null;
			List<OutlookMailInfo> listData = new List<OutlookMailInfo>();
			if (m_DicomFolder != null)
			{
				Console.WriteLine("Inbox counts: " + m_inboxFolder.Items.Count.ToString());
				for (int i = 1; i <= m_DicomFolder.Items.Count; i++)
				{
					OutlookMailInfo mail = new OutlookMailInfo();
					item = (Microsoft.Office.Interop.Outlook.MailItem)m_DicomFolder.Items[i];
					mail.SenderName = i.ToString();
					mail.Subject = item.Subject;
					mail.SentDate = item.SentOn;
					listData.Add(mail);
					//Console.WriteLine("Categories: {0}", item.Categories);
					//Console.WriteLine("Body: {0}", item.Body);
					//Console.WriteLine("HTMLBody: {0}", item.HTMLBody);
				}
			}
			else
			{
				Console.WriteLine("Inbox counts: " + m_inboxFolder.Items.Count.ToString());
				for (int i = 1; i <= m_inboxFolder.Items.Count; i++)
				{
					OutlookMailInfo mail = new OutlookMailInfo();
					item = (Microsoft.Office.Interop.Outlook.MailItem)m_inboxFolder.Items[i];
					mail.SenderName = i.ToString();
					mail.Subject = item.Subject;
					mail.SentDate = item.SentOn;
					listData.Add(mail);
					//Console.WriteLine("Categories: {0}", item.Categories);
					//Console.WriteLine("Body: {0}", item.Body);
					//Console.WriteLine("HTMLBody: {0}", item.HTMLBody);
				}
			}
			return listData;
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;

			if (m_outlookApp != null)
				m_outlookApp = null;

			if (m_inboxFolder != null)
				m_inboxFolder = null;

			if (m_impContext != null)
			{
				m_impContext.Undo();
				m_impContext.Dispose();
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.SuppressFinalize(this);
		}

		public static bool IsOutlookInstalled()
		{
			try
			{
				Type type = Type.GetTypeFromCLSID(new Guid("0006F03A-0000-0000-C000-000000000046")); //Outlook.Application
				if (type == null) return false;
				object obj = Activator.CreateInstance(type);
				System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
				return true;
			}
			catch (System.Runtime.InteropServices.COMException)
			{
				return false;
			}
		}

		public void SimulateMailRecieving()
		{
			OnMailReceivedExt(new NewMailReceivedEventArgs("Test subject", "hello...", "Sender@mail.ru"));
		}
	}

	public sealed class OutlookMailInfo
	{
		public string EntryID { get; set; }

		public string SenderName { get; set; }
		public string Subject { get; set; }
		public string CC { get; set; }
		public string To { get; set; }
		public string Body { get; set; }
		public string SenderEmail { get; set; }
		public DateTime SentDate { get; set; }
		public bool HasAttachments { get; set; }
		public bool UnRead { get; set; }
	}
}