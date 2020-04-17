using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace RStoring {
	public class StoringSurrogate : ISerializationSurrogate {
		private void GetObjectData(object obj, Type t, SerializationInfo info, int cnt) {
			if (t == null) return;
			var sfis = Storing.GetStoredFields(t);
			foreach (var fi in sfis) {
				string name = $"{cnt}_{Storing.GetStoredID(t, fi)}";
				info.AddValue(name, fi.GetValue(obj));
			}
			GetObjectData(obj, t.BaseType, info, cnt + 1);
		}

		public void GetObjectData(object obj, SerializationInfo info, StreamingContext context) {
			Type it = obj.GetType().GetInterface("IStorable");
			if (it != null)
				it.GetMethod("GetObjectData").Invoke(obj, new object[] { info, context });
			else
				GetObjectData(obj, obj.GetType(), info, 0);
		}

		private void SetObjectData(object obj, Type t, SerializationInfo info, int cnt) {
			if (t == null) return;
			var smis = Storing.GetStoringConstructer(t);
			var sfis = Storing.GetStoredFields(t);
			foreach (var fi in sfis) {
				int id = Storing.GetStoredID(t, fi).Value;
				try {
					string name = $"{cnt}_{id}";
					fi.SetValue(obj, info.GetValue(name, fi.FieldType));
				} catch (SerializationException e) {
					var getters = from x in smis where x.GetCustomAttribute<StoringConstructer>().ID == id select x;
					if (!getters.Any()) fi.SetValue(obj, Storing.GetStoredDefualt(t, fi));
					else fi.SetValue(obj, getters.First().Invoke(obj, null));
				}
			}
			SetObjectData(obj, t.BaseType, info, cnt + 1);
		}

		public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) {
			Type ist = obj.GetType().GetInterface("IStorable");
			if (ist != null)
				ist.GetMethod("SetObjectData").Invoke(obj, new object[] { info, context });
			else
				SetObjectData(obj, obj.GetType(), info, 0);

			Type iect = obj.GetType().GetInterface("IExtractCompleted");
			if (iect != null)
				iect.GetMethod("ExtractCompleted").Invoke(obj, new object[] { info, context });
			return obj;
		}
	}
	public class StoringSurrogateSelector : SurrogateSelector {
		public StoringSurrogate ss = new StoringSurrogate();
		public override ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector) {
			if (Attribute.IsDefined(type, typeof(Storable))) {
				selector = this;
				return ss;
			}
			return base.GetSurrogate(type, context, out selector);
		}
	}
	public class StoringBinder : SerializationBinder {
		public override void BindToName(Type serializedType, out string assemblyName, out string typeName) {
			if (Storing.Registered.Contains(serializedType))
				assemblyName = Storing.StoringAssemblyName;
			else
				assemblyName = serializedType.Assembly.FullName;
			typeName = Storing.GetTypeStoringName(serializedType);
		}
		public override Type BindToType(string assemblyName, string typeName) {
			return Storing.GetType(typeName);
			//if (assemblyName == Storing.StoringAssemblyName && Storing.RegisterList.ContainsKey(typeName)) {
			//	return Storing.RegisterList[typeName];
			//}
			//var asse = from x in AppDomain.CurrentDomain.GetAssemblies() where x.FullName == assemblyName select x;
			//if (!asse.Any()) throw new SerializationException($"Can not identify assembly: {{{assemblyName}}}");
			//Type type = asse.Single().GetType(typeName);
			//if (type == null) throw new SerializationException($"Can not identify type: {{{typeName}}}");
			//return type;
		}
	}









	public class JsonSurrogate : ISerializationSurrogate {
		public void GetObjectData(object obj, SerializationInfo info, StreamingContext context) {
			throw new NotImplementedException();
		}
		public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) {
			Dictionary<string, object> dict = new Dictionary<string, object>();
			foreach (var x in info) {
				dict.Add(x.Name, x.Value);
			}
			return dict;
		}
	}
	public class JsonSurrogateSelector : SurrogateSelector {
		JsonSurrogate js = new JsonSurrogate();
		public override ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector) {
			selector = this;
			return js;
		}
	}
	public class JsonBinder : SerializationBinder {
		public override void BindToName(Type serializedType, out string assemblyName, out string typeName) {
			throw new NotImplementedException();
		}
		public override Type BindToType(string assemblyName, string typeName) {
			return typeof(object);
		}
	}
}
