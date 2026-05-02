using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// 四维齐次坐标向量类 - 用于向量存储与运算
    /// 齐次坐标: W=1表示点, W=0表示方向向量
    /// </summary>
    public class Vector4
    {
        #region 私有字段
        private float _x;
        private float _y;
        private float _z;
        private float _w = 1.0f;
        #endregion

        #region 属性（C# 7.0 表达式体）
        /// <summary>X坐标分量</summary>
        public float X { get => _x; set => _x = value; }

        /// <summary>Y坐标分量</summary>
        public float Y { get => _y; set => _y = value; }

        /// <summary>Z坐标分量</summary>
        public float Z { get => _z; set => _z = value; }

        /// <summary>W齐次坐标分量（W=1表示点，W=0表示方向向量）</summary>
        public float W { get => _w; set => _w = value; }
        #endregion

        #region 构造函数（C# 7.0 元组解构/构造函数链）
        /// <summary>使用XYZ构造向量（W默认为1.0）</summary>
        public Vector4(float x, float y, float z) => (X, Y, Z) = (x, y, z);

        /// <summary>从Revit的XYZ类型构造</summary>
        public Vector4(XYZ v) => (X, Y, Z) = ((float)v.X, (float)v.Y, (float)v.Z);

        /// <summary>完整构造器（指定所有分量）</summary>
        public Vector4(float x, float y, float z, float w) : this(x, y, z) => W = w;
        #endregion

        #region 运算符重载（C# 7.0 表达式体）
        /// <summary>向量加法</summary>
        public static Vector4 operator +(Vector4 a, Vector4 b) =>
            new Vector4(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        /// <summary>向量减法</summary>
        public static Vector4 operator -(Vector4 a, Vector4 b) =>
            new Vector4(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        /// <summary>标量乘法</summary>
        public static Vector4 operator *(Vector4 v, float factor) =>
            new Vector4(v.X * factor, v.Y * factor, v.Z * factor);

        /// <summary>标量除法</summary>
        public static Vector4 operator /(Vector4 v, float factor) =>
            new Vector4(v.X / factor, v.Y / factor, v.Z / factor);

        /// <summary>反向向量</summary>
        public static Vector4 operator -(Vector4 v) => new Vector4(-v.X, -v.Y, -v.Z);
        #endregion

        #region 向量运算（C# 7.0 表达式体）
        /// <summary>点积运算</summary>
        public float Dot(Vector4 v) => X * v.X + Y * v.Y + Z * v.Z;

        /// <summary>静态点积</summary>
        public static float Dot(Vector4 a, Vector4 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        /// <summary>叉积运算（结果向量的W=0，表示方向向量）</summary>
        public Vector4 Cross(Vector4 v) => new Vector4(
            Y * v.Z - Z * v.Y,  // X分量
            Z * v.X - X * v.Z,  // Y分量
            X * v.Y - Y * v.X,  // Z分量
            0f);                // 叉积结果应为方向向量

        /// <summary>静态叉积</summary>
        public static Vector4 Cross(Vector4 a, Vector4 b) => a.Cross(b);

        /// <summary>向量长度（模）</summary>
        public float Length() => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        /// <summary>长度平方（避免开方运算，性能更高）</summary>
        public float LengthSquared() => X * X + Y * Y + Z * Z;

        /// <summary>向量归一化（转换为单位向量）</summary>
        public void Normalize()
        {
            float len = Length();
            if (Math.Abs(len) < float.Epsilon) return;  // C# 7.0: 使用float.Epsilon
            (X, Y, Z) = (X / len, Y / len, Z / len);    // 元组批量赋值
        }

        /// <summary>返回归一化后的新向量（不改变原向量）</summary>
        public Vector4 Normalized()
        {
            float len = Length();
            return Math.Abs(len) < float.Epsilon
                ? new Vector4(0, 0, 0, W)
                : new Vector4(X / len, Y / len, Z / len, W);
        }
        #endregion

        #region 实用方法
        /// <summary>转换为Revit XYZ类型</summary>
        public XYZ ToXYZ() => new XYZ(X, Y, Z);

        /// <summary>字符串表示</summary>
        public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3}, {W:F3})";

        /// <summary>判断是否为方向向量（W=0）</summary>
        public bool IsDirectionVector => Math.Abs(W) < float.Epsilon;

        /// <summary>判断是否为位置向量（W=1）</summary>
        public bool IsPositionVector => Math.Abs(W - 1f) < float.Epsilon;
        #endregion
    }

    /// <summary>
    /// 4x4变换矩阵类 - 用于3D到2D投影、平移、旋转、缩放
    /// 矩阵存储方式：行主序（row-major）
    /// </summary>
    public class Matrix4
    {
        #region 矩阵类型枚举
        /// <summary>矩阵类型，用于快速求逆矩阵</summary>
        public enum MatrixType
        {
            Normal,                 // 普通矩阵
            Rotation,              // 旋转矩阵
            Translation,           // 平移矩阵
            Scale,                 // 缩放矩阵
            RotationAndTranslation // 旋转+平移矩阵（刚体变换）
        }
        #endregion

        #region 私有字段
        private readonly float[,] _matrix = new float[4, 4];  // C# 7.0: 只读字段
        private MatrixType _type;
        #endregion

        #region 索引器（C# 7.0 表达式体）
        /// <summary>矩阵元素索引器</summary>
        public float this[int row, int col]
        {
            get => _matrix[row, col];
            set => _matrix[row, col] = value;
        }
        #endregion

        #region 静态属性（常用矩阵）
        /// <summary>4x4单位矩阵</summary>
        public static Matrix4 Identity { get; } = new Matrix4();

        /// <summary>零矩阵</summary>
        public static Matrix4 Zero { get; } = new Matrix4 { _type = MatrixType.Normal };
        #endregion

        #region 构造函数（C# 7.0 优化）
        /// <summary>默认构造（单位矩阵）</summary>
        public Matrix4()
        {
            _type = MatrixType.Normal;
            SetIdentity();
        }

        ///// <summary>旋转矩阵（原点为(0,0,0)）</summary>
        public Matrix4(Vector4 xAxis, Vector4 yAxis, Vector4 zAxis)
        {
            _type = MatrixType.Rotation;
            SetIdentity();
            (_matrix[0, 0], _matrix[0, 1], _matrix[0, 2]) = (xAxis.X, xAxis.Y, xAxis.Z);
            (_matrix[1, 0], _matrix[1, 1], _matrix[1, 2]) = (yAxis.X, yAxis.Y, yAxis.Z);
            (_matrix[2, 0], _matrix[2, 1], _matrix[2, 2]) = (zAxis.X, zAxis.Y, zAxis.Z);
        }

        /// <summary>平移矩阵</summary>
        public Matrix4(Vector4 origin) : this()
        {
            _type = MatrixType.Translation;
            (_matrix[3, 0], _matrix[3, 1], _matrix[3, 2]) = (origin.X, origin.Y, origin.Z);
        }

        ///// <summary>旋转+平移矩阵（刚体变换）</summary>
        public Matrix4(Vector4 xAxis, Vector4 yAxis, Vector4 zAxis, Vector4 origin)
        {
            _type = MatrixType.RotationAndTranslation;
            SetIdentity();
            // 设置旋转部分
            (_matrix[0, 0], _matrix[0, 1], _matrix[0, 2]) = (xAxis.X, xAxis.Y, xAxis.Z);
            (_matrix[1, 0], _matrix[1, 1], _matrix[1, 2]) = (yAxis.X, yAxis.Y, yAxis.Z);
            (_matrix[2, 0], _matrix[2, 1], _matrix[2, 2]) = (zAxis.X, zAxis.Y, zAxis.Z);
            // 设置平移部分
            (_matrix[3, 0], _matrix[3, 1], _matrix[3, 2]) = (origin.X, origin.Y, origin.Z);
        }

        /// <summary>缩放矩阵</summary>
        public Matrix4(float scale) : this()
        {
            _type = MatrixType.Scale;
            (_matrix[0, 0], _matrix[1, 1], _matrix[2, 2]) = (scale, scale, scale);
        }

        /// <summary>深拷贝构造函数</summary>
        public Matrix4(Matrix4 other) : this()
        {
            _type = other._type;
            Array.Copy(other._matrix, _matrix, 16);
        }
        #endregion

        #region 矩阵初始化
        /// <summary>设置当前矩阵为单位矩阵</summary>
        public void SetIdentity()
        {
            Array.Clear(_matrix, 0, 16);           // C# 7.0: 使用Array.Clear
            (_matrix[0, 0], _matrix[1, 1], _matrix[2, 2], _matrix[3, 3]) = (1f, 1f, 1f, 1f);
        }
        #endregion

        #region 矩阵运算
        /// <summary>矩阵乘法（左乘右）</summary>
        public static Matrix4 Multiply(Matrix4 left, Matrix4 right)
        {
            var result = new Matrix4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result[i, j] = left[i, 0] * right[0, j] + left[i, 1] * right[1, j]
                                  + left[i, 2] * right[2, j] + left[i, 3] * right[3, j];
                }
            }
            return result;
        }

        /// <summary>矩阵乘法运算符重载</summary>
        public static Matrix4 operator *(Matrix4 left, Matrix4 right) => Multiply(left, right);

        /// <summary>变换向量/点</summary>
        //public Vector4 Transform(Vector4 point) => new Vector4(
        //    point.X * this[0, 0] + point.Y * this[1, 0] + point.Z * this[2, 0] + point.W * this[3, 0],
        //    point.X * this[0, 1] + point.Y * this[1, 1] + point.Z * this[2, 1] + point.W * this[3, 1],
        //    point.X * this[0, 2] + point.Y * this[1, 2] + point.Z * this[2, 2] + point.W * this[3, 2],
        //    point.W);
        #endregion

        #region 逆矩阵计算
        /// <summary>旋转矩阵的逆（转置）</summary>
        private Matrix4 RotationInverse() => new Matrix4(
            new Vector4(this[0, 0], this[1, 0], this[2, 0]),
            new Vector4(this[0, 1], this[1, 1], this[2, 1]),
            new Vector4(this[0, 2], this[1, 2], this[2, 2]));

        /// <summary>平移矩阵的逆</summary>
        private Matrix4 TranslationInverse() => new Matrix4(new Vector4(-this[3, 0], -this[3, 1], -this[3, 2]));

        /// <summary>缩放矩阵的逆</summary>
        private Matrix4 ScaleInverse() => new Matrix4(1f / _matrix[0, 0]);

        /// <summary>获取逆矩阵（根据矩阵类型优化）</summary>
        public Matrix4 Inverse()
        {
            switch (_type)
            {
                case MatrixType.Rotation:
                    return RotationInverse();
                case MatrixType.Translation:
                    return TranslationInverse();
                case MatrixType.RotationAndTranslation:
                    return Multiply(TranslationInverse(), RotationInverse());
                case MatrixType.Scale:
                    return ScaleInverse();
                default:
                    return new Matrix4();   // 普通矩阵返回单位矩阵
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>获取旋转部分（3x3子矩阵）</summary>
        public Matrix4 GetRotationPart() => new Matrix4(
            new Vector4(this[0, 0], this[0, 1], this[0, 2]),
            new Vector4(this[1, 0], this[1, 1], this[1, 2]),
            new Vector4(this[2, 0], this[2, 1], this[2, 2]));

        /// <summary>获取平移部分</summary>
        public Vector4 GetTranslationPart() => new Vector4(this[3, 0], this[3, 1], this[3, 2]);

        /// <summary>转换为数组</summary>
        public float[] ToArray()
        {
            var arr = new float[16];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    arr[i * 4 + j] = _matrix[i, j];
            return arr;
        }

        /// <summary>字符串表示</summary>
        public override string ToString() =>
            $"[{_matrix[0, 0]:F2},{_matrix[0, 1]:F2},{_matrix[0, 2]:F2},{_matrix[0, 3]:F2}]\n" +
            $"[{_matrix[1, 0]:F2},{_matrix[1, 1]:F2},{_matrix[1, 2]:F2},{_matrix[1, 3]:F2}]\n" +
            $"[{_matrix[2, 0]:F2},{_matrix[2, 1]:F2},{_matrix[2, 2]:F2},{_matrix[2, 3]:F2}]\n" +
            $"[{_matrix[3, 0]:F2},{_matrix[3, 1]:F2},{_matrix[3, 2]:F2},{_matrix[3, 3]:F2}]";
        #endregion
    }

    /// <summary>
    /// 二维双精度点结构
    /// </summary>
    public struct PointD
    {
        #region 私有字段
        private double _x;
        private double _y;
        #endregion

        #region 属性（C# 7.0 表达式体）
        public double X { get => _x; set => _x = value; }
        public double Y { get => _y; set => _y = value; }
        #endregion

        #region 构造函数
        public PointD(double x, double y) => (_x, _y) = (x, y);
        #endregion

        #region 运算符重载
        public static PointD operator +(PointD a, PointD b) => new PointD(a.X + b.X, a.Y + b.Y);
        public static PointD operator -(PointD a, PointD b) => new PointD(a.X - b.X, a.Y - b.Y);
        public static PointD operator *(PointD p, double factor) => new PointD(p.X * factor, p.Y * factor);
        public static bool operator ==(PointD a, PointD b) => Math.Abs(a.X - b.X) < 1e-9 && Math.Abs(a.Y - b.Y) < 1e-9;
        public static bool operator !=(PointD a, PointD b) => !(a == b);
        #endregion

        #region 实用方法
        public double DistanceTo(PointD other) => Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));
        public PointD Normalize() { double len = Math.Sqrt(X * X + Y * Y); return len < 1e-9 ? this : new PointD(X / len, Y / len); }
        public override bool Equals(object obj) => obj is PointD other && this == other;
        public override int GetHashCode() => (X.GetHashCode() * 397) ^ Y.GetHashCode();
        public override string ToString() => $"({X:F3}, {Y:F3})";
        #endregion
    }
}
