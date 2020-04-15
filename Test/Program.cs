using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using RStoring;
namespace EmpriseDesign {
	[Storable("R_TEST")]
	public class Test {
		[Stored(0)] int x;
	}
	[Storable("R_GTEST")]
	public class GTest<T1, T2> {
		[Stored(0)] T1 x;
		[Stored(1)] T2 y;
	}



	class Program {
		static void Store() {
			//var obj = new List<int>();
			var obj = new List<GTest<GTest<int, Test>, List<Test>>>();
			BinaryFormatter bf = Storing.GetBinaryFormatter();
			FileStream fs = new FileStream("E:\\a.txt", FileMode.Create);
			bf.Serialize(fs, obj);
			fs.Close();
		}
		static void Read() {
			object obj;
			BinaryFormatter bf = Storing.GetBinaryFormatter();
			FileStream fs = new FileStream("E:\\a.txt", FileMode.Open);
			obj = bf.Deserialize(fs);
			fs.Close();
		}
		static void Main(string[] args) {
			Storing.CheckClassAllAssembly();
			Store();
			Read();
			//var obj = new Dictionary<int, Test>();
			var obj = new List<GTest<GTest<int, Test>, List<Test>>>();
			string s = Storing.GetTypeStoringName(obj.GetType());
			Console.WriteLine(s);
			Type tt = Type.GetType(s);
			Type t = Storing.GetType(s);
			Console.ReadLine();
		}
	}
}
