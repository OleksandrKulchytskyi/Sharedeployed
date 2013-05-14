using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Proxy.Logging;
using ShareDeployed.Proxy;

namespace ShareDeployed.Test.Ioc
{
	[TestClass]
	public class ProxyIocTest
	{
		private System.Threading.ManualResetEvent _event;

		[TestInitialize]
		public void OnInitialize()
		{
			_event = new System.Threading.ManualResetEvent(false);
			DynamicProxyPipeline.Instance.Initialize(true);
		}

		[TestCleanup]
		public void OnUninitialize()
		{
			_event.Dispose();
		}

		public class TypeForResolving
		{
			[Instantiate]
			public ILogAggregator Logger { get; set; }

			public void DoLog()
			{
				if (Logger != null)
				{
					Logger.DoLog(LogSeverity.Info, "Hello", null);
				}
			}
		}

		public interface IWorker
		{
			void DoAnything();
		}

		public class ChildClass: TypeForResolving
		{
			[Instantiate]
			public IWorker Worker { get; set; }
		}

		public class ChildClass2 : TypeForResolving
		{
			[Instantiate]
			public Object Worker { get; set; }
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void TestPerThreadMethodFail()
		{
			typeof(TypeForResolving).InPerThreadScope();
		}

		[TestMethod]
		public void TestPerThreadMethodProper()
		{
			var type = typeof(TypeForResolving);
			type.BindToSelf().InPerThreadScope();
			Assert.IsTrue(ServicesMapper.GetFullMappingInfo(type).Value == ServiceLifetime.PerThread);

			TypeForResolving obj1 = DynamicProxyPipeline.Instance.ContracResolver.Resolve<TypeForResolving>();
			TypeForResolving obj2 = DynamicProxyPipeline.Instance.ContracResolver.Resolve<TypeForResolving>();
			Assert.IsTrue(object.ReferenceEquals(obj1, obj2));

			TypeForResolving obj3 = null;
			System.Threading.Tasks.Task.Factory.StartNew(() =>
			{
				obj3 = DynamicProxyPipeline.Instance.ContracResolver.Resolve<TypeForResolving>();
				Assert.IsFalse(object.ReferenceEquals(obj1, obj3));

				_event.Set();
			}).
			ContinueWith(prevTask =>
			{
				System.Diagnostics.Debug.WriteLine(prevTask.Exception);
				System.Diagnostics.Debug.WriteLine(prevTask.Exception.StackTrace);
				prevTask.Dispose();
			}, System.Threading.Tasks.TaskContinuationOptions.NotOnRanToCompletion);

			if (!_event.WaitOne(TimeSpan.FromSeconds(5)))
				Assert.Fail();
		}

		[TestMethod]
		public void SingletonScopeTestMethod()
		{
			var type = typeof(TypeForResolving);
			type.BindToSelf().InSingletonScope();
			Assert.IsTrue(ServicesMapper.GetFullMappingInfo(type).Value == ServiceLifetime.Singleton);

			TypeForResolving obj1 = DynamicProxyPipeline.Instance.ContracResolver.Resolve<TypeForResolving>();
			TypeForResolving obj2 = DynamicProxyPipeline.Instance.ContracResolver.Resolve<TypeForResolving>();
			Assert.IsTrue(object.ReferenceEquals(obj1, obj2));

			TypeForResolving obj3;
			System.Threading.Tasks.Task.Factory.StartNew(() =>
			{
				obj3 = DynamicProxyPipeline.Instance.ContracResolver.Resolve<TypeForResolving>();
				Assert.IsTrue(object.ReferenceEquals(obj1, obj3));
				obj3.DoLog();

				_event.Set();
			}).
				ContinueWith(prevTask =>
				{
					System.Diagnostics.Debug.WriteLine(prevTask.Exception);
					System.Diagnostics.Debug.WriteLine(prevTask.Exception.StackTrace);
					prevTask.Dispose();
				}, System.Threading.Tasks.TaskContinuationOptions.NotOnRanToCompletion);

			if (!_event.WaitOne(TimeSpan.FromSeconds(5)))
				Assert.Fail();
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void HierrachyMethod()
		{
			Type childType=typeof(ChildClass);
			childType.BindToSelf();
			DynamicProxyPipeline.Instance.ContracResolver.OmitNotRegistred = true;
			ChildClass obj= DynamicProxyPipeline.Instance.ContracResolver.Resolve<ChildClass>();
			if (obj == null )
				Assert.Fail();
			Assert.IsNull(obj.Worker);
		}

	}
}
