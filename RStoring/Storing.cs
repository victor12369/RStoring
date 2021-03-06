﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Web.Script.Serialization;

namespace RStoring {

	/// <summary>
	/// Mark a class to store.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
	public class Storable : Attribute {
		/// <summary>
		/// A unique id.
		/// </summary>
		public string Name { get; }
		public Storable(string name) { Name = name; }
	}

	/// <summary>
	/// Mark a field or auto-property to serialize.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public class Stored : Attribute {
		/// <summary>
		/// A unique id in one class.
		/// </summary>
		public int ID { get; }
		/// <summary>
		/// If can't find value when deserialize, use Defualt
		/// </summary>
		public object Defualt { get; set; }
		public Stored(int id) {
			ID = id;
		}
	}
	/// <summary>
	/// Mark a function to construct defualt value.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class StoringConstructer : Attribute {
		public int ID { get; }
		public StoringConstructer(int id) {
			ID = id;
		}
	}
	/// <summary>
	/// Make a class able to control serialization for itself.
	/// </summary>
	interface IStorable {
		void GetObjectData(SerializationInfo info, StreamingContext context);
		void SetObjectData(SerializationInfo info, StreamingContext context);
	}
	interface IExtractCompleted {
		void ExtractCompleted(SerializationInfo info, StreamingContext context);
	}

	public static class Storing {
		private class AutomaticInitializer {
			public AutomaticInitializer() {
				AppDomain.CurrentDomain.AssemblyLoad += OnCurrentDomainAssemblyLoad;
				var asses = AppDomain.CurrentDomain.GetAssemblies();
				foreach (var asse in asses) RegisterClassAll(asse);
			}
		}
		static public Dictionary<string, Type> RegisterList { get; private set; } = new Dictionary<string, Type>();
		static public HashSet<Type> Registered { get; private set; } = new HashSet<Type>();
		public const BindingFlags FindFlag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
		public const string StoringAssemblyName = ">Storing<";

		/// <summary>
		/// convert auto-properties' fields' name to auto-properties' name
		/// defualt:  "&lt;prop&gt;k_BackingField" -> "prop"
		/// </summary>
		public static Func<string, string> AutoPropConverter { get; set; } = (string name) => {
			string[] s = name.Split(new char[] { '<', '>' });
			if (s.Length != 3) return null;
			if (s[0] == "" && s[2] == "k__BackingField") {
				return s[1];
			}
			return null;
		};

		static private AutomaticInitializer __initializer = new AutomaticInitializer();
		/// <summary>
		/// Register a class with [Stored].
		/// Auto register, no need to call manually.
		/// </summary>
		public static void RegisterClass(Type c) {
			if (Registered.Contains(c)) return;
			if (!Attribute.IsDefined(c, typeof(Storable))) throw new Exception("Can not register a class without [Storable]");
			string name = c.GetCustomAttribute<Storable>().Name;
			if (RegisterList.ContainsKey(name))
				throw new Exception("Storing Name repeat");
			RegisterList.Add(name, c);
			Registered.Add(c);
		}
		/// <summary>
		/// Check a class with [Stored] if it breaked some rules.
		/// </summary>
		/// <exception cref="SerializationException">
		/// Throw if a class break some rules.
		/// </exception>
		public static void CheckClass(Type c) {
			if (!Attribute.IsDefined(c, typeof(Storable))) return;
			HashSet<int> hs = new HashSet<int>();
			HashSet<int> hs2 = new HashSet<int>();
			var sfis = GetStoredFields(c);
			foreach (var fi in sfis) {
				int id = GetStoredID(c, fi).Value;
				if (hs.Contains(id)) throw new SerializationException($"In {{{c}}}: Assign multiple [Stored({id})]");
				hs.Add(id);
			}
			var smis = GetStoringConstructer(c);
			foreach (var mi in smis) {
				int id = mi.GetCustomAttribute<StoringConstructer>().ID;
				if (!hs.Contains(id)) throw new SerializationException($"In {{{c}}}: Assign [StoringConstructer({id})] without [Stored({id})]");
				if (hs2.Contains(id)) throw new SerializationException($"In {{{c}}}: Assign multiple [StoringConstructer({id})]");
				hs2.Add(id);
			}
			PropertyInfo[] pis = c.GetProperties(FindFlag);
			var spis = from pi in pis where Attribute.IsDefined(pi, typeof(Stored)) select pi;
			foreach (var pi in spis) {
				if (c.GetField($"<{pi.Name}>k__BackingField", FindFlag) == null) throw new SerializationException($"In {{{c}}}: Assign [Stored(ID)] on a non-automatic property {{{pi.Name}}}");
			}
		}

		private static void OnCurrentDomainAssemblyLoad(object sender, AssemblyLoadEventArgs args) {
			RegisterClassAll(args.LoadedAssembly);
		}

		/// <summary>
		/// Auto register when an assembly loaded, no need to call manually.
		/// </summary>
		public static void RegisterClassAll(Assembly asse) {
			Type[] ts = asse.GetTypes();
			var sts = from t in ts where Attribute.IsDefined(t, typeof(Storable)) select t;
			foreach (var t in sts) RegisterClass(t);
		}

		public static void CheckClassAll(Assembly asse) {
			Type[] ts = asse.GetTypes();
			var sts = from t in ts where Attribute.IsDefined(t, typeof(Storable)) select t;
			foreach (var t in sts) CheckClass(t);
		}

		public static void CheckClassAllAssembly() {
			AppDomain.CurrentDomain.AssemblyLoad += OnCurrentDomainAssemblyLoad;
			var asses = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var asse in asses) CheckClassAll(asse);
		}
		/// <summary>
		/// Get a formatter that supports [Stored].
		/// [Serializable] is also supported.
		/// </summary>
		public static T GetFormatter<T>() where T : IFormatter, new() {
			T ret = new T();
			ret.Binder = new StoringBinder();
			ret.SurrogateSelector = new StoringSurrogateSelector();
			return ret;
		}
		/// <summary>
		/// Get a BinaryFormatter that supports [Stored].
		/// [Serializable] is also supported.
		/// </summary>
		public static BinaryFormatter GetBinaryFormatter() {
			return new BinaryFormatter() { Binder = new StoringBinder(), SurrogateSelector = new StoringSurrogateSelector() };
		}
		public static IEnumerable<FieldInfo> GetStoredFields(Type t) {
			FieldInfo[] fis = t.GetFields(FindFlag);
			var sfis = from fi in fis where GetStoredID(t, fi) != null select fi;
			return sfis;
		}

		public static IEnumerable<MethodInfo> GetStoringConstructer(Type t) {
			MethodInfo[] mis = t.GetMethods(FindFlag);
			var smis = from mi in mis where Attribute.IsDefined(mi, typeof(StoringConstructer)) select mi;
			return smis;
		}

		public static int? GetStoredID(Type t, FieldInfo fi) {
			if (Attribute.IsDefined(fi, typeof(Stored))) return fi.GetCustomAttribute<Stored>().ID;
			string[] s = fi.Name.Split(new char[] { '<', '>' });
			if (s.Length != 3) return null;

			if (s[0] == "" && s[2] == "k__BackingField") {
				PropertyInfo pi = t.GetProperty(s[1], FindFlag);
				if (Attribute.IsDefined(pi, typeof(Stored)))
					return pi.GetCustomAttribute<Stored>().ID;
				else return null;
			}
			return null;
		}
		public static object GetStoredDefualt(Type t, FieldInfo fi) {
			if (Attribute.IsDefined(fi, typeof(Stored))) return fi.GetCustomAttribute<Stored>().Defualt;
			string[] s = fi.Name.Split(new char[] { '<', '>' });
			if (s.Length != 3) return null;
			if (s[0] == "" && s[2] == "k__BackingField") {
				PropertyInfo pi = t.GetProperty(s[1], FindFlag);
				if (Attribute.IsDefined(pi, typeof(Stored)))
					return pi.GetCustomAttribute<Stored>().Defualt;
				else return null;
			}
			return null;
		}

		/// <summary>
		/// Get a formatter that can deserialize to Dictionary&lt;string, object&gt;.
		/// Not support serialize.
		/// </summary>
		public static T GetDictionaryFormatter<T>() where T : IFormatter, new() {
			T ret = new T();
			ret.Binder = new JsonBinder();
			ret.SurrogateSelector = new JsonSurrogateSelector();
			return ret;
		}

		public static Dictionary<string, object> DeserializeBinaryFormatToDictionary(System.IO.Stream stream) {
			BinaryFormatter bf = GetDictionaryFormatter<BinaryFormatter>();
			Dictionary<string, object> dict = (Dictionary<string, object>)bf.Deserialize(stream);
			return dict;
		}
		public static string DeserializeBinaryFormatToJson(System.IO.Stream stream) {
			JavaScriptSerializer jss = new JavaScriptSerializer();
			return jss.Serialize(DeserializeBinaryFormatToDictionary(stream));
		}
		public static string GetTypeStoringName(Type t) {
			if (!t.IsGenericType) {
				if (Attribute.IsDefined(t, typeof(Storable))) {
					return $"{t.GetCustomAttribute<Storable>().Name},{StoringAssemblyName}";
				} else {
					return t.AssemblyQualifiedName;
				}
			}
			if (Attribute.IsDefined(t, typeof(Storable))) {
				Type[] gts = t.GetGenericArguments();
				StringBuilder sb = new StringBuilder($"{t.GetCustomAttribute<Storable>().Name}`{gts.Length}[");
				foreach (var gt in gts)
					sb.Append('[').Append(GetTypeStoringName(gt)).Append("],");
				sb[sb.Length - 1] = ']';
				sb.Append($",{StoringAssemblyName}");
				return sb.ToString();
			} else {
				Type[] gts = t.GetGenericArguments();
				StringBuilder sb = new StringBuilder(t.AssemblyQualifiedName);
				foreach (var gt in gts)
					sb.Replace(gt.AssemblyQualifiedName, GetTypeStoringName(gt));
				//if (Type.GetType(sb.ToString()) == t) { Console.WriteLine("123123123"); }
				return sb.ToString();
			}
			//Type.GetType("",,,)
		}
		class StoringAssembly : Assembly { }
		public static Type GetType(string stroingName) {
			return Type.GetType(stroingName, (AssemblyName an) => {
				if (an.FullName == StoringAssemblyName)
					return new StoringAssembly();
				return Assembly.Load(an);
			}, (Assembly asse, string str, bool b) => {
				if (asse == null) return null;
				if (!(asse is StoringAssembly))
					return asse.GetType(str);
				int len = str.IndexOf('`');
				if (len == -1)
					return RegisterList[str];
				else
					return RegisterList[str.Substring(0, len)];
			});
		}
	}
}
