using System;
using System.Collections.Generic;

namespace CreatePipe.ViewFilters
{
    public static class EnumParseUtility<TEnum>
    {
        //将String解析为 列举
        public static TEnum Parse(string strValue)
        {
            if (!typeof(TEnum).IsEnum) return default(TEnum);
            return (TEnum)Enum.Parse(typeof(TEnum), strValue);
        }
        //将列举解析为 String
        public static String Parse(TEnum enumVal)
        {
            if (!typeof(TEnum).IsEnum) return String.Empty;
            return Enum.GetName(typeof(TEnum), enumVal);
        }
        //将列举从 Integer 值解析为 String
        public static String Parse(int enumValInt)
        {
            if (!typeof(TEnum).IsEnum) return String.Empty;
            return Enum.GetName(typeof(TEnum), enumValInt);
        }
    }
    //用于比较两个List是否相等，如果完全相同返回true
    public static class ListCompareUtility<T>
    {
        public static bool Equals(ICollection<T> coll1, ICollection<T> coll2)
        {
            if (coll1.Count != coll2.Count) return false;
            foreach (T val1 in coll1)
            {
                if (!coll2.Contains(val1)) return false;
            }
            foreach (T val2 in coll2)
            {
                if (!coll1.Contains(val2)) return false;
            }
            return true;
        }
    }
}
