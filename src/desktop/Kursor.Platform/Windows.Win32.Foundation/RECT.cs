using System.CodeDom.Compiler;
using System.Drawing;

namespace Windows.Win32.Foundation;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct RECT
{
	internal int left;

	internal int top;

	internal int right;

	internal int bottom;

	internal readonly int Width => right - left;

	internal readonly int Height => bottom - top;

	internal readonly bool IsEmpty
	{
		get
		{
			if (left == 0 && top == 0 && right == 0)
			{
				return bottom == 0;
			}
			return false;
		}
	}

	internal readonly int X => left;

	internal readonly int Y => top;

	internal readonly Size Size => new Size(Width, Height);

	internal RECT(Rectangle value)
		: this(value.Left, value.Top, value.Right, value.Bottom)
	{
	}

	internal RECT(Point location, Size size)
		: this(location.X, location.Y, location.X + size.Width, location.Y + size.Height)
	{
	}

	internal RECT(int left, int top, int right, int bottom)
	{
		this.left = left;
		this.top = top;
		this.right = right;
		this.bottom = bottom;
	}

	internal static RECT FromXYWH(int x, int y, int width, int height)
	{
		return new RECT(x, y, x + width, y + height);
	}

	public static implicit operator Rectangle(RECT value)
	{
		return new Rectangle(value.left, value.top, value.Width, value.Height);
	}

	public static implicit operator RectangleF(RECT value)
	{
		return new RectangleF(value.left, value.top, value.Width, value.Height);
	}

	public static implicit operator RECT(Rectangle value)
	{
		return new RECT(value);
	}
}
