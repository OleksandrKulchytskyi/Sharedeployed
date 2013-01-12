
using ShareDeployed.Common.Models;
using ShareDeployed.DataAccess.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ShareDeployed.Controllers
{
	[Authorize]
	public class WordsController : Controller
	{
		IUnityOfWork _unity;

		public WordsController(IUnityOfWork unity)
		{
			if (null == unity)
				throw new ArgumentNullException("unity");
			_unity=unity;
		}

		public ActionResult Index()
		{
			return View();
		}

		public ActionResult Upload()
		{
			return View();
		}

		[HttpPost]
		public ActionResult Upload(IEnumerable<HttpPostedFileBase> files)
		{
			if (this.Request.Files == null || this.Request.Files.Count == 0)
				return View("Upload");

			string format = "{0}_{1}_{2}";
			List<string> tasks = new List<string>();
			for (int i = 0; i < this.Request.Files.Count; i++)
			{
				HttpPostedFileBase file = this.Request.Files[0];
				if (file != null && file.ContentLength > 0)
				{
					if (Session != null && Session["UserId"] != null)
					{
						var fileName = string.Format(format, (int)Session["UserId"], DateTime.UtcNow.ToString("ddMMyyyyhhmm"), Path.GetFileName(file.FileName));
						var path = Path.Combine(Server.MapPath("~/App_Data/Uploads"), fileName);

						file.SaveAs(path);
						tasks.Add(path);
					}
				}
			}

			if (tasks.Count > 0)
			{
				var parseTask = Task.Factory.StartNew(new Action<object>(PerformParseOperation), tasks);

				parseTask.ContinueWith(t => MvcApplication.Logger.Error("Error has occurred in PerformParseOperation", t.Exception),
									TaskContinuationOptions.NotOnRanToCompletion);

				parseTask.ContinueWith(t => MvcApplication.Logger.Info("Parse task ends"),
									TaskContinuationOptions.OnlyOnRanToCompletion);
			}

			return View("Index");
		}

		[NonAction]
		private void PerformParseOperation(object state)
		{
			if (state == null || !(state is List<string>))
				return;

			using (var db = _unity)
			{

				List<Word> wordsList = new List<Word>();

				foreach (string path in (state as List<string>))
				{
					int userId = Int32.Parse(Path.GetFileName(path).Split('_')[0]);
					wordsList.Clear();

					using (StreamReader sr = new StreamReader(path))
					{
						string line = string.Empty;
						while ((line = sr.ReadLine()) != null)
						{
							if (string.IsNullOrEmpty(line))
								continue;
							else if (line.Contains('-'))
							{
								int indx = line.IndexOf('-');
								string word = line.Substring(0, indx).Trim();
								string transl = line.Substring(indx + 1).Trim();
								wordsList.Add(new Word()
								{
									UserId = userId,
									Complicated = false,
									ForeignWord = word,
									Translation = transl
								});
							}
						}
					}

					using (var trans = db.BeginTransaction())
					{
						IRepository<Word> repository = db.GetRepository<Word>();

						repository.AddBulk(wordsList);
						db.Commit();
					}
				}
			}
		}

	}
}
