using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Helper;
using System.Text;
using System.Linq;
using ShareDeployed.Proxy;

namespace ShareDeployed.Test
{
	[TestClass]
	public class UnitTest4
	{
		[TestMethod]
		public void LazyWeakReferenceTest()
		{
			LazyWeakReference<StringBuilder> builder = new LazyWeakReference<StringBuilder>(
															new StringBuilder("A builder"),
															() => new StringBuilder("Acopy"));
			StringBuilder aTarget = builder.Target;
			aTarget = null;
			GC.Collect();

			Console.WriteLine("Reinitialized {0} times", builder.Reinitialized);
			aTarget = builder.Target;
			aTarget = null;
			GC.Collect();
			Console.WriteLine("Reinitialized {0} times", builder.Reinitialized);
			aTarget = builder.Target;
			aTarget = null;
			GC.Collect();
			Console.WriteLine("Reinitialized {0} times", builder.Reinitialized);
			aTarget = builder.Target;
			aTarget = null;
			Console.WriteLine("Reinitialized {0} times", builder.Reinitialized);

			System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
		}

		[TestMethod]
		public void TetsTypeGetHashCode()
		{
			SafeCollection<int> coll = new SafeCollection<int>();

			var types = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
						 where !assembly.FullName.Contains("System")
						 let tps =
						 (from type in assembly.GetTypes()
						  where !type.IsAbstract && !type.IsInterface && type.IsPublic
						  select type)

						 select new { Asm = assembly, Types = tps });

			//var types = from a in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
			//			let tps = a.GetTypes()
			//			from t in tps
			//			where t.IsPublic && t != null & !t.IsNested && !a.FullName.Contains("System")
			//			select new { Asm = a, Types = t };

			foreach (var item in types)
			{
				if (item.Asm == null)
					continue;
				foreach (Type t in item.Types)
				{
					if (t != null)
						coll.Add(t.GetHashCode());
				}
			}

			SafeCollection<int> coll2 = new SafeCollection<int>();

			var types2 = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
						  where !assembly.FullName.Contains("System")
						  let tps =
						  (from type in assembly.GetTypes()
						   where !type.IsAbstract && !type.IsInterface && !type.IsNested && type.IsPublic
						   select type)
						  select new { Asm = assembly, Types = tps });

			foreach (var item in types2)
			{
				if (item.Asm == null)
					continue;
				foreach (Type t in item.Types)
				{
					if (t != null)
						coll2.Add(t.GetHashCode());
				}
			}

			foreach (int hash1 in coll)
			{
				if (!coll2.Contains(hash1))
					Assert.Fail();
			}

			foreach (int hash2 in coll2)
			{
				if (!coll.Contains(hash2))
					Assert.Fail();
			}

			Assert.IsTrue(coll2.Count == coll.Count);
		}
	}
}
