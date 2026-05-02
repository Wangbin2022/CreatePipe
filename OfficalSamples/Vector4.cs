using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// 四维齐次坐标向量类，用于存储向量及向量运算
    /// 默认W=1.0表示点向量，W=0表示方向向量
    /// </summary>
    //public class Vector4
    //{
    //    #region 成员变量与属性

    //    // C# 7.3: 使用默认字面量简化double初始化
    //    private double m_x;
    //    private double m_y;
    //    private double m_z;
    //    private double m_w = 1.0d;

    //    /// <summary>
    //    /// X坐标分量
    //    /// </summary>
    //    public double X
    //    {
    //        get => m_x;           // C# 7.0: 表达式体属性
    //        set => m_x = value;
    //    }

    //    /// <summary>
    //    /// Y坐标分量
    //    /// </summary>
    //    public double Y
    //    {
    //        get => m_y;
    //        set => m_y = value;
    //    }

    //    /// <summary>
    //    /// Z坐标分量
    //    /// </summary>
    //    public double Z
    //    {
    //        get => m_z;
    //        set => m_z = value;
    //    }

    //    /// <summary>
    //    /// W齐次坐标分量（W=1表示点，W=0表示方向向量）
    //    /// </summary>
    //    public double W
    //    {
    //        get => m_w;
    //        set => m_w = value;
    //    }

    //    #endregion

    //    #region 构造函数

    //    /// <summary>
    //    /// 使用XYZ分量构造向量（W默认为1.0）
    //    /// </summary>
    //    /// <param name="x">X坐标</param>
    //    /// <param name="y">Y坐标</param>
    //    /// <param name="z">Z坐标</param>
    //    public Vector4(double x, double y, double z)
    //    {
    //        (X, Y, Z) = (x, y, z);  // C# 7.0: 元组解构赋值
    //    }

    //    /// <summary>
    //    /// 从Revit的XYZ类型构造Vector4（W默认为1.0）
    //    /// </summary>
    //    /// <param name="v">Revit中的XYZ向量</param>
    //    public Vector4(Autodesk.Revit.DB.XYZ v)
    //    {
    //        // C# 7.0: 简化类型转换（v.X本身就是double，无需显式转换）
    //        (X, Y, Z) = (v.X, v.Y, v.Z);
    //    }

    //    /// <summary>
    //    /// 完整构造器：指定所有四个分量
    //    /// </summary>
    //    public Vector4(double x, double y, double z, double w) : this(x, y, z)
    //    {
    //        W = w;  // 构造函数链调用后单独设置W
    //    }

    //    #endregion

    //    #region 叉积运算（法向量计算）

    //    /// <summary>
    //    /// 计算当前向量与另一向量的叉积（得到垂直于两向量的法向量）
    //    /// </summary>
    //    /// <param name="other">第二个向量</param>
    //    /// <returns>垂直于当前向量和other向量的法向量（W分量保持0）</returns>
    //    public Vector4 CrossProduct(Vector4 other)
    //    {
    //        // C# 7.0: 使用元组简化临时变量
    //        (double x, double y, double z) = (
    //            Y * other.Z - Z * other.Y,  // X分量公式
    //            Z * other.X - X * other.Z,  // Y分量公式
    //            X * other.Y - Y * other.X   // Z分量公式
    //        );

    //        // 叉积结果向量W=0表示方向向量（无限远点）
    //        return new Vector4(x, y, z, 0);
    //    }

    //    /// <summary>
    //    /// 静态方法：计算两个向量的叉积（得到法向量）
    //    /// </summary>
    //    /// <param name="a">第一个向量</param>
    //    /// <param name="b">第二个向量</param>
    //    /// <returns>垂直于向量a和b的法向量</returns>
    //    public static Vector4 CrossProduct(Vector4 a, Vector4 b)
    //    {
    //        // 使用元组解构和内联计算，避免冗余局部变量
    //        return new Vector4(
    //            a.Y * b.Z - a.Z * b.Y,  // 叉积X分量
    //            a.Z * b.X - a.X * b.Z,  // 叉积Y分量
    //            a.X * b.Y - a.Y * b.X,  // 叉积Z分量
    //            0                        // 方向向量的W=0
    //        );
    //    }

    //    #endregion

    //    #region 扩展功能（C# 7.3增强）

    //    /// <summary>
    //    /// 计算向量长度（模）
    //    /// </summary>
    //    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);

    //    /// <summary>
    //    /// 向量归一化（转换为单位向量）
    //    /// </summary>
    //    /// <returns>归一化后的向量，W分量保持原值</returns>
    //    public Vector4 Normalize()
    //    {
    //        double mag = Magnitude;
    //        if (mag < 1e-10)
    //            throw new InvalidOperationException("零向量无法归一化");

    //        return new Vector4(X / mag, Y / mag, Z / mag, W);
    //    }

    //    /// <summary>
    //    /// 重写ToString方法便于调试
    //    /// </summary>
    //    public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3}, {W:F3})";

    //    #endregion
    //}
}
