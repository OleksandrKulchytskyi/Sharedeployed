using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace ShareDeployed.Mailgrabber.Helpers
{
	public class WeakEventHandlerAdvanced<E> where E : EventArgs
	{
		private delegate void EventHandlerThunk(object @this, object sender, E e);

		private static int g_NextThunkID = 1;

		private WeakReference m_TargetRef;
		private EventHandlerThunk m_Thunk;
		private EventHandler<E> m_Handler;

		public WeakEventHandlerAdvanced(EventHandler<E> eventHandler)
		{
			m_TargetRef = new WeakReference(eventHandler.Target);
			m_Thunk = CreateDynamicThunk(eventHandler);
			m_Handler = Invoke;
		}

		private EventHandlerThunk CreateDynamicThunk(EventHandler<E> eventHandler)
		{
			MethodInfo method = eventHandler.Method;
			Type declaringType = method.DeclaringType;

			int id = Interlocked.Increment(ref g_NextThunkID);
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

		public void Invoke(object sender, E e)
		{
			object target = m_TargetRef.Target;

			if (target != null)
				m_Thunk(target, sender, e);
		}

		public bool IsAlive()
		{
			object target = m_TargetRef.Target;
			return (m_TargetRef.IsAlive && target != null);
		}

		public static implicit operator EventHandler<E>(WeakEventHandlerAdvanced<E> weh)
		{
			return weh.m_Handler;
		}
	}
}
