﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace ShareDeployed.Proxy
{
	#region MethodCallInfo struct
	public struct MethodCallInfo : IEquatable<MethodCallInfo>
	{
		public MethodCallInfo(string methodName, int argsCount, IEnumerable<string> argsName)
		{
			_methodName = methodName;
			_argsCount = argsCount;
			_argumentsName = new List<string>(argsCount);
			if (argsCount > 0)
				ArgumentsName.AddRange(argsName);
		}

		private string _methodName;
		public string MethodName
		{
			get { return _methodName; }
			set { _methodName = value; }
		}

		private int _argsCount;
		public int ArgumentsCount
		{
			get { return _argsCount; }
			set { _argsCount = value; }
		}

		private List<string> _argumentsName;
		public List<string> ArgumentsName
		{
			get { return _argumentsName; }
			set { _argumentsName = value; }
		}

		public override bool Equals(object obj)
		{
			return (obj is MethodCallInfo) ? Equals((MethodCallInfo)obj) : false;
		}

		public bool Equals(MethodCallInfo other)
		{
			return (string.Equals(MethodName, other.MethodName, StringComparison.InvariantCulture) &&
					ArgumentsCount == other.ArgumentsCount &&
					IsArgumentsNameEquals(other));
		}

		private bool IsArgumentsNameEquals(MethodCallInfo compare)
		{
			bool result = true;
			if (ArgumentsName.Count != compare.ArgumentsName.Count)
				return false;

			foreach (string agrument in ArgumentsName)
			{
				if (!compare.ArgumentsName.Contains(agrument))
				{
					result = false;
					break;
				}
			}
			return result;
		}

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 31 + _methodName.GetHashCode();
			hash = hash * 31 + _argsCount;
			return hash;
			//return _methodName.GetHashCode() + _argsCount;
		}

		public override string ToString()
		{
			return string.Format("{0} - {1} - {3}", MethodName, ArgumentsCount, string.Join(",", ArgumentsName));
		}

		public static bool operator ==(MethodCallInfo left, MethodCallInfo right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(MethodCallInfo left, MethodCallInfo right)
		{
			return !left.Equals(right);
		}
	}
	#endregion

	public sealed class TypeMethodsMapper
	{
		//TODO: check here
		private static ConcurrentDictionary<int, ConcurrentDictionary<MethodCallInfo, MethodInfo>> _mappings;
		private static ConcurrentDictionary<MethodInfo, FastReflection.DynamicMethodDelegate> _dynamicDelMap;
		private static Lazy<TypeMethodsMapper> _instance;

		#region ctors
		static TypeMethodsMapper()
		{
			_instance = new Lazy<TypeMethodsMapper>(() => new TypeMethodsMapper(), true);
			_mappings = new ConcurrentDictionary<int, ConcurrentDictionary<MethodCallInfo, MethodInfo>>();
			_dynamicDelMap = new ConcurrentDictionary<MethodInfo, FastReflection.DynamicMethodDelegate>();
		}

		private TypeMethodsMapper()
		{
		}
		#endregion

		public static TypeMethodsMapper Instance
		{
			get
			{
				return _instance.Value;
			}
		}

		#region public methods
		public void Add(int type, MethodCallInfo mci, MethodInfo mi)
		{
			if (!_mappings.ContainsKey(type))
			{
				if (_mappings.TryAdd(type, new ConcurrentDictionary<MethodCallInfo, MethodInfo>()))
				{
					if (_mappings[type].TryAdd(mci, mi))
						_dynamicDelMap.TryAdd(mi, FastReflection.DynamicMethodDelegateFactory.Create(mi));
				}
			}
			else if (!_mappings[type].ContainsKey(mci))
			{
				if (_mappings[type].TryAdd(mci, mi))
				{
					FastReflection.DynamicMethodDelegate del = FastReflection.DynamicMethodDelegateFactory.Create(mi);
					_dynamicDelMap.AddOrUpdate(mi, del, (key, oldVal) => oldVal = del);
				}
			}
		}

		public MethodInfo Get(int type, MethodCallInfo mci)
		{
			if (_mappings.ContainsKey(type))
			{
				MethodInfo mi = null;
				_mappings[type].TryGetValue(mci, out mi);
				return mi;
			}
			return null;
		}

		public bool Contains(int type, MethodCallInfo mci)
		{
			return (_mappings.ContainsKey(type) && _mappings[type].ContainsKey(mci));
		}

		public FastReflection.DynamicMethodDelegate GetDynamicDelegate(int type, MethodCallInfo mci)
		{
			if (_mappings.ContainsKey(type))
			{
				MethodInfo mi = null;
				if (_mappings[type].TryGetValue(mci, out mi))
				{
					return _dynamicDelMap[mi];
				}
			}
			return null;
		}

		#endregion
	}
}
