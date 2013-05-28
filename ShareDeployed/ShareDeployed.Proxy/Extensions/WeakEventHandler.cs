using System;
using System.Reflection;
using System.Reflection.Emit;

namespace ShareDeployed.Proxy.Extensions
{
	public class WeakEventHandler<E> where E : EventArgs
	{
		private delegate void EventHandlerThunk(object @this, object sender, E e);

		private static int _NextThunkID = 1;

		private WeakReference _TargetRef;
		private EventHandlerThunk _Thunk;
		private EventHandler<E> _Handler;

		public WeakEventHandler(EventHandler<E> eventHandler)
		{
			_TargetRef = new WeakReference(eventHandler.Target);
			_Thunk = CreateDynamicThunk(eventHandler);
			_Handler = Invoke;
		}

		public void Invoke(object sender, E e)
		{
			object target = _TargetRef.Target;

			if (target != null)
				_Thunk(target, sender, e);
		}

		public bool IsAlive()
		{
			object target = _TargetRef.Target;
			return (_TargetRef.IsAlive && target != null);
		}

		public static implicit operator EventHandler<E>(WeakEventHandler<E> weh)
		{
			return weh._Handler;
		}

		private EventHandlerThunk CreateDynamicThunk(EventHandler<E> eventHandler)
		{
			MethodInfo method = eventHandler.Method;
			Type declaringType = method.DeclaringType;

			int id = System.Threading.Interlocked.Increment(ref _NextThunkID);
			DynamicMethod dm = new DynamicMethod("EventHandlerThunk" + id, typeof(void),
			  new Type[] { typeof(object), typeof(object), typeof(E) }, declaringType);

			ILGenerator il = dm.GetILGenerator();

			// load and cast "this" pointer...
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Castclass, declaringType);
			// load arguments...
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			// call method...
			il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);
			// done...
			il.Emit(OpCodes.Ret);

			return (EventHandlerThunk)dm.CreateDelegate(typeof(EventHandlerThunk));
		}
	}
}
