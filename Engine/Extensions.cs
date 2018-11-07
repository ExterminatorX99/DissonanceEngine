using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GameEngine
{
	//TODO: Move regions into their own extension classes
	public static class Extensions
	{
		public static CultureInfo customCulture;
		
		public static void Init()
		{
			customCulture = (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
			customCulture.NumberFormat.NumberDecimalSeparator = ".";
		}

		#region Linq/Arrays
		public static void CopyTo<TKey,TValue>(this Dictionary<TKey,TValue> from,Dictionary<TKey,TValue> to)
		{
			foreach(var pair in from) {
				to.Add(pair.Key,pair.Value);
			}
		}

		public static IEnumerable<TResult> SelectIgnoreNull<TSource,TResult>(this IEnumerable<TSource> source,Func<TSource,TResult> selector)
		{
			if(source==null)	{ throw new ArgumentNullException (nameof(source)); }
			if(selector==null)	{ throw new ArgumentNullException (nameof(selector)); }
			return SelectIgnoreNullIterator(source,selector);
		}
		private static IEnumerable<TResult> SelectIgnoreNullIterator<TSource,TResult>(IEnumerable<TSource> source,Func<TSource,TResult> selector)
		{
			foreach(var element in source) {
				var result = selector(element);
				if(result!=null) {
					yield return result;
				}
			}
		}
		public static bool TryFirst<T>(this IEnumerable<T> array,Func<T,bool> func,out T result)
		{
			foreach(var val in array) {
				if(func(val)) {
					result = val;
					return true;
				}
			}
			result = default;
			return false;
		}

		public static string AllToString<T>(this T[] array,string separator = ",")
		{
			if(array==null) {
				return "NULL";
			}
			string str = "";
			for(int i=0;i<array.Length;i++) {
				str += array[i].ToString();
				if(i<array.Length-1) {
					str += separator;
				}
			}
			return str;
		}
		#endregion
		#region Textures
		public static void CopyPixels(this Pixel[,] from,Rect? sourceRect,Pixel[,] to,Vector2Int destPoint)
		{
			var srcRect = sourceRect ?? new Rect(0,0,@from.GetLength(0),@from.GetLength(1));
			int xLength1 = from.GetLength(0);
			int yLength1 = from.GetLength(1);
			int xLength2 = to.GetLength(0);
			int yLength2 = to.GetLength(1);

			for(int y=0;y<srcRect.Height;y++) {
				for(int x=0;x<srcRect.Width;x++) {
					int X1 = srcRect.X+x;
					int Y1 = srcRect.Y+y;
					int X2 = destPoint.x+x;
					int Y2 = destPoint.y+y;

					if(X1>=0 && Y1>=0 && X2>=0 && Y2>=0 && X1<xLength1 && Y1<yLength1 && X2<xLength2 && Y2<yLength2) {
						to[X2,Y2] = from[X1,Y1];
					}
				}
			}
		}
		#endregion
		#region BitConversion
		#region Byte
		public static byte ToByte(this bool[] array)
		{
			if(array.Length>8) {
				throw new ArgumentException("Array's length shouldn't be bigger than 8. Use ToShort() or ToInt() instead");
			}

			byte result = 0;
			for(int i=0;i<array.Length;i++) {
				if(array[i]) {
					result |= (byte)(1<<i);
				}
			}
			return result;
		}
		public static bool[] ToBitBooleans(this byte b)
		{
			var result = new bool[8];
			for(int i=0;i<8;i++) {
				result[i] = (b & 1 << i)!=0;
			}
			return result;
		}
		#endregion
		#region ULong
		public static ulong ToULong(this bool[] array)
		{
			if(array.Length!=64) {
				throw new ArgumentException("Array's length must be exactly 64");
			}

			ulong result = 0;
			for(int i=0;i<64;i++) {
				if(array[i]) {
					result |= (byte)(1<<i);
				}
			}
			return result;
		}
		public static bool[] ToBitBooleans(this ulong b)
		{
			var result = new bool[64];
			for(int i=0;i<64;i++) {
				result[i] = (b & (ulong)1 << i)!=0;
			}
			return result;
		}
		public static string BitsToString(this ulong b)
		{
			string result = "";
			for(int i=0;i<64;i++) {
				result += (b & (ulong)1 << i)==0 ? "0" : "1";
			}
			return result;
		}
		#endregion
		#endregion

		//Other
		#region Strings
		public static int SizeInBytes(this string str) => Encoding.ASCII.GetByteCount(str);
		public static bool IsEmptyOrNull(this string str) => string.IsNullOrEmpty(str);

		public static string ReplaceCaseInsensitive(this string str,string oldValue,string newValue)
			=> Regex.Replace(str,Regex.Escape(oldValue),newValue.Replace("$","$$"),RegexOptions.IgnoreCase);
		public static string ReplaceCaseInsensitive(this string str,params (string oldValue,string newValue)[] replacements)
		{
			for(int i=0;i<replacements.Length;i++) {
				(string oldValue,string newValue) = replacements[i];
				str = str.ReplaceCaseInsensitive(oldValue,newValue);
			}
			return str;
		}
		#endregion
		#region IO 
		//BinaryWriter
		public static void Write(this BinaryWriter writer,Vector2 vec)
		{
			writer.Write(vec.x);
			writer.Write(vec.y);
		}
		public static void Write(this BinaryWriter writer,Vector3 vec)
		{
			writer.Write(vec.x);
			writer.Write(vec.y);
			writer.Write(vec.z);
		}
		public static void Write(this BinaryWriter writer,Vector4 vec)
		{
			writer.Write(vec.x);
			writer.Write(vec.y);
			writer.Write(vec.z);
			writer.Write(vec.w);
		}
		public static void Write(this BinaryWriter writer,Quaternion q)
		{
			writer.Write(q.x);
			writer.Write(q.y);
			writer.Write(q.z);
			writer.Write(q.w);
		}
		//BinaryReader
		public static Vector2 ReadVector2(this BinaryReader reader) => new Vector2(reader.ReadSingle(),reader.ReadSingle());
		public static Vector3 ReadVector3(this BinaryReader reader) => new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
		public static Vector4 ReadVector4(this BinaryReader reader) => new Vector4(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
		public static Quaternion ReadQuaternion(this BinaryReader reader) => new Quaternion(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
		#endregion
	}
}