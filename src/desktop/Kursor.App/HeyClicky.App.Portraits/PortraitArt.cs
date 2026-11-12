using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HeyClicky.App.Portraits;

public static class PortraitArt
{
	public const double CaretStrokeWidth = 22.400000000000002;

	private const double CaretWidth = 48.0;

	private const double CaretRise = 16.0;

	public static readonly (double X, double Y, double W, double H) ClassicBodyRect = (X: 0.0, Y: 45.6, W: 320.0, H: 236.8);

	private const string WideSvg = "M19.6865 9.83594C20.3452 9.83594 20.9381 10.1139 21.3574 10.5576C21.8768 10.2445 22.4861 10.0654 23.1367 10.0654C23.9457 10.0655 24.6891 10.3442 25.2773 10.8105C26.026 10.2684 26.9327 9.95117 27.9092 9.95117C29.2042 9.95127 30.3756 10.5095 31.2197 11.4102C31.4299 11.3589 31.649 11.3291 31.875 11.3291C33.0851 11.3291 34.1101 12.1096 34.4824 13.1934C35.6073 13.3436 36.4756 14.3047 36.4756 15.4707C36.4755 16.1923 36.1421 16.8353 35.6221 17.2568C36.8521 18.3914 37.6229 20.016 37.623 21.8213C37.623 24.8523 35.4514 27.3744 32.5791 27.9199C30.8959 29.5883 28.5806 30.6201 26.0234 30.6201C24.3464 30.6201 22.7743 30.1742 21.415 29.3984C21.1092 29.675 20.7056 29.8457 20.2607 29.8457C19.8105 29.8457 19.4019 29.6712 19.0947 29.3887C18.591 29.6766 18.0085 29.8427 17.3867 29.8428C15.7829 29.8428 14.4343 28.7484 14.0479 27.2656C11.4247 25.947 9.62321 23.2337 9.62305 20.0977C9.62305 18.0525 10.3894 16.1863 11.6494 14.7695C11.6415 14.6592 11.6367 14.5479 11.6367 14.4355C11.6367 11.8953 13.6961 9.83609 16.2363 9.83594C16.9716 9.83594 17.6668 10.0086 18.2832 10.3154C18.6716 10.0154 19.1578 9.83599 19.6865 9.83594Z";

	private const string CompactSvg = "M22.2256 28.0938C21.5862 28.0938 21.0107 27.8241 20.6035 27.3936C20.0988 27.698 19.5064 27.8721 18.874 27.8721C18.0884 27.872 17.3662 27.6013 16.7949 27.1484C16.0678 27.6748 15.1876 27.9824 14.2393 27.9824C12.9805 27.9824 11.8425 27.4393 11.0225 26.5635C10.8182 26.6133 10.6053 26.6426 10.3857 26.6426C9.2111 26.6426 8.21581 25.8856 7.85352 24.834C6.76095 24.6881 5.91805 23.7545 5.91797 22.6221C5.91797 22.4279 5.94563 22.2402 5.99219 22.0605C4.76526 20.8306 3.99906 19.0976 3.99902 17.1758C3.99902 13.5625 6.70149 10.6126 10.0996 10.4307C11.7254 8.86944 13.9313 7.90723 16.3633 7.90723C17.9208 7.90729 19.3864 8.30097 20.666 8.99414C20.9455 8.78527 21.2913 8.65918 21.667 8.65918C22.1035 8.6593 22.4997 8.82798 22.7979 9.10156C23.2876 8.82135 23.8543 8.65918 24.459 8.65918C26.0168 8.65934 27.3269 9.72277 27.7021 11.1631C30.2501 12.4439 31.999 15.0797 31.999 18.126C31.999 20.8569 30.5935 23.2584 28.4668 24.6494C27.8521 25.9656 26.5173 26.8778 24.9688 26.8779C24.7215 26.8779 24.4795 26.854 24.2451 26.8096C23.8881 27.568 23.1193 28.0936 22.2256 28.0938Z";

	private static readonly PPath WideTraced = PPath.ParseSvg("M19.6865 9.83594C20.3452 9.83594 20.9381 10.1139 21.3574 10.5576C21.8768 10.2445 22.4861 10.0654 23.1367 10.0654C23.9457 10.0655 24.6891 10.3442 25.2773 10.8105C26.026 10.2684 26.9327 9.95117 27.9092 9.95117C29.2042 9.95127 30.3756 10.5095 31.2197 11.4102C31.4299 11.3589 31.649 11.3291 31.875 11.3291C33.0851 11.3291 34.1101 12.1096 34.4824 13.1934C35.6073 13.3436 36.4756 14.3047 36.4756 15.4707C36.4755 16.1923 36.1421 16.8353 35.6221 17.2568C36.8521 18.3914 37.6229 20.016 37.623 21.8213C37.623 24.8523 35.4514 27.3744 32.5791 27.9199C30.8959 29.5883 28.5806 30.6201 26.0234 30.6201C24.3464 30.6201 22.7743 30.1742 21.415 29.3984C21.1092 29.675 20.7056 29.8457 20.2607 29.8457C19.8105 29.8457 19.4019 29.6712 19.0947 29.3887C18.591 29.6766 18.0085 29.8427 17.3867 29.8428C15.7829 29.8428 14.4343 28.7484 14.0479 27.2656C11.4247 25.947 9.62321 23.2337 9.62305 20.0977C9.62305 18.0525 10.3894 16.1863 11.6494 14.7695C11.6415 14.6592 11.6367 14.5479 11.6367 14.4355C11.6367 11.8953 13.6961 9.83609 16.2363 9.83594C16.9716 9.83594 17.6668 10.0086 18.2832 10.3154C18.6716 10.0154 19.1578 9.83599 19.6865 9.83594Z");

	private static readonly PPath CompactTraced = PPath.ParseSvg("M22.2256 28.0938C21.5862 28.0938 21.0107 27.8241 20.6035 27.3936C20.0988 27.698 19.5064 27.8721 18.874 27.8721C18.0884 27.872 17.3662 27.6013 16.7949 27.1484C16.0678 27.6748 15.1876 27.9824 14.2393 27.9824C12.9805 27.9824 11.8425 27.4393 11.0225 26.5635C10.8182 26.6133 10.6053 26.6426 10.3857 26.6426C9.2111 26.6426 8.21581 25.8856 7.85352 24.834C6.76095 24.6881 5.91805 23.7545 5.91797 22.6221C5.91797 22.4279 5.94563 22.2402 5.99219 22.0605C4.76526 20.8306 3.99906 19.0976 3.99902 17.1758C3.99902 13.5625 6.70149 10.6126 10.0996 10.4307C11.7254 8.86944 13.9313 7.90723 16.3633 7.90723C17.9208 7.90729 19.3864 8.30097 20.666 8.99414C20.9455 8.78527 21.2913 8.65918 21.667 8.65918C22.1035 8.6593 22.4997 8.82798 22.7979 9.10156C23.2876 8.82135 23.8543 8.65918 24.459 8.65918C26.0168 8.65934 27.3269 9.72277 27.7021 11.1631C30.2501 12.4439 31.999 15.0797 31.999 18.126C31.999 20.8569 30.5935 23.2584 28.4668 24.6494C27.8521 25.9656 26.5173 26.8778 24.9688 26.8779C24.7215 26.8779 24.4795 26.854 24.2451 26.8096C23.8881 27.568 23.1193 28.0936 22.2256 28.0938Z");

	public static IReadOnlyList<HairPiece> Back(Hairstyle style, bool faceOnly = true)
	{
		PortraitPack portraitPack = style.Pack();
		if (portraitPack.IsCloud())
		{
			return Array.Empty<HairPiece>();
		}
		switch (portraitPack)
		{
		case PortraitPack.StorybookSpirits:
			return StorybookBack(style, faceOnly);
		case PortraitPack.ArcadeAllStars:
			return ArcadeBack(style);
		case PortraitPack.KiCrew:
			return Array.Empty<HairPiece>();
		default:
			switch (style)
			{
			case Hairstyle.Bob:
				return new HairPiece[1] { P(delegate(PPath p)
				{
					p.M(39.0, 144.0).C(38.0, 79.0, 80.0, 35.0, 127.0, 35.0).C(155.0, 33.0, 169.0, 43.0, 182.0, 58.0)
						.C(225.0, 54.0, 263.0, 76.0, 269.0, 121.0)
						.C(274.0, 154.0, 288.0, 168.0, 288.0, 186.0)
						.C(288.0, 230.0, 250.0, 248.0, 220.0, 244.0)
						.C(174.0, 251.0, 114.0, 260.0, 72.0, 255.0)
						.C(50.0, 255.0, 35.0, 241.0, 37.0, 224.0)
						.C(17.0, 212.0, 10.0, 199.0, 20.0, 179.0)
						.C(33.0, 165.0, 36.0, 163.0, 39.0, 144.0)
						.Z();
				}) };
			case Hairstyle.Topknot:
				return new HairPiece[3]
				{
					E(134.0, 5.0, 88.0, 86.0),
					P(delegate(PPath p)
					{
						p.M(43.0, 144.0).C(40.0, 96.0, 85.0, 66.0, 151.0, 66.0).C(227.0, 63.0, 282.0, 98.0, 282.0, 145.0)
							.C(282.0, 165.0, 275.0, 186.0, 264.0, 199.0)
							.C(245.0, 161.0, 222.0, 134.0, 164.0, 132.0)
							.C(108.0, 132.0, 79.0, 162.0, 60.0, 199.0)
							.C(48.0, 185.0, 43.0, 165.0, 43.0, 144.0)
							.Z();
					}),
					new HairPiece(PPath.Oval(154.0, 71.0, 53.0, 13.0), PieceFill.Accent)
				};
			case Hairstyle.Pixie:
				return new HairPiece[1] { P(delegate(PPath p)
				{
					p.M(45.0, 209.0).C(32.0, 182.0, 31.0, 154.0, 45.0, 126.0).C(67.0, 70.0, 116.0, 43.0, 182.0, 52.0)
						.C(234.0, 49.0, 280.0, 90.0, 278.0, 151.0)
						.C(275.0, 178.0, 269.0, 199.0, 259.0, 214.0)
						.C(186.0, 231.0, 109.0, 231.0, 45.0, 209.0)
						.Z();
				}) };
			case Hairstyle.TwinBuns:
				return new HairPiece[7]
				{
					E(10.0, 46.0, 77.0, 80.0),
					E(233.0, 46.0, 77.0, 80.0),
					E(10.0, 183.0, 77.0, 76.0),
					E(233.0, 182.0, 77.0, 76.0),
					E(43.0, 58.0, 236.0, 190.0),
					new HairPiece(PPath.Oval(67.0, 90.0, 15.0, 30.0), PieceFill.Accent),
					new HairPiece(PPath.Oval(238.0, 90.0, 15.0, 30.0), PieceFill.Accent)
				};
			case Hairstyle.Waves:
				if (!faceOnly)
				{
					return new HairPiece[1] { P(delegate(PPath p)
					{
						p.M(41.0, 144.0).C(32.0, 56.0, 95.0, 25.0, 163.0, 35.0).C(235.0, 25.0, 283.0, 75.0, 277.0, 143.0)
							.C(270.0, 195.0, 309.0, 217.0, 287.0, 250.0)
							.C(263.0, 277.0, 300.0, 295.0, 271.0, 315.0)
							.C(244.0, 340.0, 226.0, 314.0, 209.0, 330.0)
							.L(112.0, 325.0)
							.C(89.0, 337.0, 76.0, 311.0, 51.0, 325.0)
							.C(22.0, 317.0, 47.0, 288.0, 28.0, 270.0)
							.C(9.0, 246.0, 46.0, 224.0, 30.0, 204.0)
							.C(17.0, 182.0, 44.0, 169.0, 41.0, 144.0)
							.Z();
					}) };
				}
				return new HairPiece[1] { P(delegate(PPath p)
				{
					p.M(41.0, 144.0).C(32.0, 56.0, 95.0, 25.0, 163.0, 35.0).C(235.0, 25.0, 283.0, 75.0, 277.0, 143.0)
						.C(271.0, 177.0, 293.0, 189.0, 289.0, 212.0)
						.C(287.0, 228.0, 277.0, 229.0, 284.0, 244.0)
						.C(270.0, 255.0, 254.0, 243.0, 250.0, 227.0)
						.C(246.0, 207.0, 257.0, 189.0, 257.0, 166.0)
						.L(70.0, 166.0)
						.C(67.0, 190.0, 83.0, 208.0, 69.0, 225.0)
						.C(62.0, 240.0, 49.0, 245.0, 34.0, 237.0)
						.C(43.0, 222.0, 24.0, 211.0, 31.0, 193.0)
						.C(35.0, 174.0, 44.0, 166.0, 41.0, 144.0)
						.Z();
				}) };
			case Hairstyle.Curls:
				return new HairPiece[1]
				{
					new HairPiece(Scallop(160.0, 132.0, 133.0, 110.0, 11, 6.0), PieceFill.Hair)
				};
			case Hairstyle.Ponytail:
				return new HairPiece[3]
				{
					P(delegate(PPath p)
					{
						p.M(241.0, 89.0).C(286.0, 56.0, 316.0, 98.0, 293.0, 143.0).C(276.0, 178.0, 299.0, 219.0, 307.0, 237.0)
							.C(279.0, 249.0, 270.0, 273.0, 244.0, 248.0)
							.C(223.0, 225.0, 236.0, 188.0, 245.0, 160.0)
							.C(257.0, 132.0, 216.0, 122.0, 241.0, 89.0)
							.Z();
					}),
					E(45.0, 53.0, 233.0, 188.0),
					new HairPiece(PPath.Oval(243.0, 86.0, 25.0, 21.0), PieceFill.Accent)
				};
			case Hairstyle.Undercut:
				return new HairPiece[1] { P(delegate(PPath p)
				{
					p.M(43.0, 200.0).L(41.0, 120.0).C(42.0, 66.0, 81.0, 39.0, 137.0, 47.0)
						.L(211.0, 29.0)
						.L(201.0, 43.0)
						.L(244.0, 34.0)
						.L(233.0, 54.0)
						.C(271.0, 58.0, 285.0, 94.0, 274.0, 147.0)
						.L(267.0, 207.0)
						.Z();
				}) };
			case Hairstyle.Braids:
			{
				List<HairPiece> list = new List<HairPiece> { E(42.0, 55.0, 237.0, 192.0) };
				double[] array = new double[2] { 48.0, 246.0 };
				foreach (double x in array)
				{
					for (int j = 0; j < 5; j++)
					{
						double num = ((j % 2 == 0) ? (-3) : 3);
						list.Add(E(x + num, 180 + j * 24, 36.0, 39.0));
					}
					list.Add(new HairPiece(PPath.Oval(x + 1.0, 292.0, 27.0, 14.0), PieceFill.Accent));
					list.Add(P(delegate(PPath p)
					{
						p.M(x + 3.0, 306.0).L(x + 27.0, 306.0).L(x + 28.0, 332.0)
							.Q(x + 13.0, 339.0, x + 2.0, 329.0)
							.Z();
					}));
				}
				return list;
			}
			case Hairstyle.Puff:
				return new HairPiece[1]
				{
					new HairPiece(Scallop(160.0, 128.0, 139.0, 112.0, 16, 5.0), PieceFill.Hair)
				};
			default:
				return Array.Empty<HairPiece>();
			}
		}
	}

	public static IReadOnlyList<HairPiece> Front(Hairstyle style)
	{
		PortraitPack portraitPack = style.Pack();
		if (!portraitPack.IsCloud())
		{
			switch (portraitPack)
			{
			case PortraitPack.KiCrew:
				break;
			case PortraitPack.StorybookSpirits:
				return StorybookFront(style);
			case PortraitPack.ArcadeAllStars:
				return ArcadeFront(style);
			default:
				switch (style)
				{
				case Hairstyle.Bob:
					return Parted(182.0, 58.0, 148.0, 147.0);
				case Hairstyle.Topknot:
				{
					List<HairPiece> list = Parted(161.0, 82.0, 133.0, 135.0, 66.0).ToList();
					list.Add(new HairPiece(new PPath().M(174.0, 24.0).C(204.0, 22.0, 210.0, 61.0, 183.0, 61.0).C(161.0, 62.0, 160.0, 41.0, 178.0, 39.0)
						.C(191.0, 38.0, 193.0, 51.0, 184.0, 52.0), null));
					return list;
				}
				case Hairstyle.TwinBuns:
					return Parted(160.0, 72.0, 130.0, 132.0, 54.0);
				case Hairstyle.Waves:
					return new HairPiece[1] { P(delegate(PPath p)
					{
						p.M(42.0, 153.0).C(31.0, 70.0, 95.0, 25.0, 163.0, 35.0).C(230.0, 30.0, 276.0, 67.0, 278.0, 147.0)
							.C(248.0, 144.0, 215.0, 112.0, 194.0, 77.0)
							.C(171.0, 122.0, 124.0, 110.0, 108.0, 142.0)
							.C(93.0, 173.0, 73.0, 171.0, 73.0, 210.0)
							.C(40.0, 190.0, 59.0, 173.0, 42.0, 153.0)
							.Z();
					}) };
				case Hairstyle.Pixie:
					return new HairPiece[1] { P(delegate(PPath p)
					{
						p.M(28.0, 133.0).C(56.0, 76.0, 109.0, 42.0, 181.0, 50.0).C(186.0, 37.0, 179.0, 23.0, 177.0, 19.0)
							.C(194.0, 24.0, 203.0, 36.0, 206.0, 47.0)
							.C(215.0, 42.0, 229.0, 42.0, 240.0, 45.0)
							.C(230.0, 50.0, 225.0, 55.0, 223.0, 61.0)
							.C(263.0, 63.0, 283.0, 99.0, 278.0, 151.0)
							.C(271.0, 132.0, 257.0, 113.0, 238.0, 102.0)
							.C(214.0, 111.0, 192.0, 125.0, 177.0, 142.0)
							.C(177.0, 126.0, 174.0, 117.0, 174.0, 112.0)
							.C(137.0, 129.0, 97.0, 164.0, 85.0, 213.0)
							.Q(81.0, 232.0, 79.0, 249.0)
							.C(61.0, 231.0, 52.0, 203.0, 53.0, 180.0)
							.C(43.0, 186.0, 36.0, 192.0, 31.0, 203.0)
							.C(34.0, 177.0, 41.0, 151.0, 50.0, 137.0)
							.C(39.0, 143.0, 33.0, 146.0, 21.0, 150.0)
							.Q(22.0, 142.0, 28.0, 133.0)
							.Z();
					}) };
				case Hairstyle.Curls:
					return new HairPiece[4]
					{
						new HairPiece(new PPath().M(39.0, 131.0).C(39.0, 68.0, 80.0, 23.0, 154.0, 24.0).C(222.0, 23.0, 278.0, 61.0, 280.0, 130.0)
							.L(244.0, 110.0)
							.Q(225.0, 132.0, 202.0, 118.0)
							.Q(182.0, 139.0, 163.0, 119.0)
							.Q(143.0, 136.0, 125.0, 120.0)
							.Q(104.0, 137.0, 87.0, 113.0)
							.Q(70.0, 129.0, 53.0, 111.0)
							.L(39.0, 131.0)
							.Z(), PieceFill.Hair, Stroked: false),
						new HairPiece(PPath.Oval(39.0, 100.0, 33.0, 71.0), PieceFill.Hair, Stroked: false),
						new HairPiece(PPath.Oval(249.0, 100.0, 32.0, 68.0), PieceFill.Hair, Stroked: false),
						new HairPiece(new PPath().M(53.0, 111.0).Q(70.0, 129.0, 87.0, 113.0).Q(104.0, 137.0, 125.0, 120.0)
							.Q(143.0, 136.0, 163.0, 119.0)
							.Q(182.0, 139.0, 202.0, 118.0)
							.Q(225.0, 132.0, 244.0, 110.0), null)
					};
				case Hairstyle.Ponytail:
					return new HairPiece[1] { P(delegate(PPath p)
					{
						p.M(46.0, 149.0).C(40.0, 88.0, 80.0, 48.0, 150.0, 51.0).C(224.0, 45.0, 274.0, 73.0, 279.0, 145.0)
							.C(252.0, 139.0, 226.0, 99.0, 220.0, 82.0)
							.C(197.0, 128.0, 153.0, 128.0, 125.0, 148.0)
							.L(138.0, 115.0)
							.C(113.0, 138.0, 86.0, 145.0, 46.0, 149.0)
							.Z();
					}) };
				case Hairstyle.Undercut:
					return new HairPiece[2]
					{
						P(delegate(PPath p)
						{
							p.M(53.0, 122.0).C(50.0, 70.0, 85.0, 45.0, 137.0, 47.0).L(211.0, 29.0)
								.L(201.0, 43.0)
								.L(244.0, 34.0)
								.L(233.0, 54.0)
								.C(276.0, 58.0, 286.0, 96.0, 274.0, 147.0)
								.L(251.0, 115.0)
								.L(257.0, 158.0)
								.C(214.0, 146.0, 174.0, 112.0, 157.0, 78.0)
								.C(132.0, 110.0, 92.0, 113.0, 53.0, 122.0)
								.Z();
						}),
						new HairPiece(PPath.Oval(44.0, 128.0, 14.0, 40.0), PieceFill.Outfit, Stroked: false)
					};
				case Hairstyle.Braids:
					return Parted(162.0, 68.0, 125.0, 125.0, 54.0);
				case Hairstyle.Puff:
					return new HairPiece[2]
					{
						new HairPiece(new PPath().M(54.0, 113.0).C(53.0, 55.0, 94.0, 35.0, 159.0, 35.0).C(221.0, 30.0, 267.0, 63.0, 267.0, 113.0)
							.Q(249.0, 125.0, 237.0, 105.0)
							.Q(214.0, 125.0, 198.0, 101.0)
							.Q(177.0, 120.0, 161.0, 100.0)
							.Q(140.0, 121.0, 123.0, 102.0)
							.Q(99.0, 126.0, 84.0, 108.0)
							.Q(68.0, 127.0, 54.0, 113.0)
							.Z(), PieceFill.Hair, Stroked: false),
						new HairPiece(new PPath().M(267.0, 113.0).Q(249.0, 125.0, 237.0, 105.0).Q(214.0, 125.0, 198.0, 101.0)
							.Q(177.0, 120.0, 161.0, 100.0)
							.Q(140.0, 121.0, 123.0, 102.0)
							.Q(99.0, 126.0, 84.0, 108.0)
							.Q(68.0, 127.0, 54.0, 113.0), null)
					};
				default:
					return Array.Empty<HairPiece>();
				}
			}
		}
		return Array.Empty<HairPiece>();
	}

	private static IReadOnlyList<HairPiece> Parted(double x, double y, double left, double right, double top = 35.0)
	{
		return new HairPiece[2]
		{
			P(delegate(PPath p)
			{
				p.M(43.0, 144.0).C(39.0, 83.0, 82.0, top, x - 48.0, top).C(x - 27.0, top, x - 12.0, top + (y - top) * 0.3, x, y)
					.C(x - 20.0, y + 35.0, 94.0, left - 3.0, 53.0, left)
					.L(43.0, 144.0)
					.Z();
			}),
			P(delegate(PPath p)
			{
				p.M(x, y).C(228.0, top + 12.0, 266.0, 82.0, 273.0, right).C(241.0, right - 10.0, x + 20.0, y + 34.0, x, y)
					.Z();
			})
		};
	}

	private static PPath Scallop(double cx, double cy, double rx, double ry, int bumps, double depth)
	{
		int num = bumps * 8;
		Vector2[] array = new Vector2[num];
		for (int i = 0; i < num; i++)
		{
			double num2 = (double)i * Math.PI * 2.0 / (double)num;
			double num3 = depth * Math.Cos(num2 * (double)bumps);
			array[i] = new Vector2((float)(cx + (rx + num3) * Math.Cos(num2)), (float)(cy + (ry + num3) * Math.Sin(num2)));
		}
		PPath pPath = new PPath().M(array[0].X, array[0].Y);
		for (int j = 0; j < num; j++)
		{
			Vector2 vector = array[(j + num - 1) % num];
			Vector2 vector2 = array[j];
			Vector2 vector3 = array[(j + 1) % num];
			Vector2 vector4 = array[(j + 2) % num];
			pPath.C(vector2.X + (vector3.X - vector.X) / 6f, vector2.Y + (vector3.Y - vector.Y) / 6f, vector3.X - (vector4.X - vector2.X) / 6f, vector3.Y - (vector4.Y - vector2.Y) / 6f, vector3.X, vector3.Y);
		}
		return pPath.Z();
	}

	private static IReadOnlyList<HairPiece> ArcadeBack(Hairstyle style)
	{
		return style switch
		{
			Hairstyle.HeroCap => new HairPiece[2]
			{
				O(39.0, 72.0, 242.0, 166.0),
				P(delegate(PPath p)
				{
					p.M(35.0, 118.0).C(36.0, 6.0, 271.0, 4.0, 275.0, 116.0).Z();
				})
			}, 
			Hairstyle.FoxHood => new HairPiece[3]
			{
				Poly(43.0, 133.0, 24.0, 9.0, 111.0, 65.0, 217.0, 65.0, 294.0, 10.0, 277.0, 148.0, 281.0, 220.0, 246.0, 252.0, 70.0, 252.0, 38.0, 222.0),
				PolyF(PieceFill.Accent, 40.0, 35.0, 82.0, 77.0, 49.0, 97.0),
				PolyF(PieceFill.Accent, 280.0, 36.0, 270.0, 99.0, 238.0, 78.0)
			}, 
			Hairstyle.KnightHelm => new HairPiece[4]
			{
				O(37.0, 47.0, 248.0, 213.0),
				PolyF(PieceFill.Accent, 131.0, 55.0, 130.0, 24.0, 173.0, 8.0, 223.0, 24.0, 202.0, 52.0),
				R(21.0, 121.0, 34.0, 80.0, 12.0),
				R(269.0, 121.0, 34.0, 80.0, 12.0)
			}, 
			Hairstyle.Crown => new HairPiece[1] { P(delegate(PPath p)
			{
				p.M(44.0, 143.0).C(21.0, 18.0, 292.0, 18.0, 279.0, 140.0).Q(265.0, 203.0, 277.0, 253.0)
					.L(243.0, 235.0)
					.L(72.0, 235.0)
					.Q(40.0, 271.0, 41.0, 255.0)
					.Z();
			}) }, 
			Hairstyle.Robot => new HairPiece[5]
			{
				R(34.0, 45.0, 254.0, 205.0, 48.0),
				R(13.0, 125.0, 31.0, 68.0, 10.0),
				R(278.0, 125.0, 31.0, 68.0, 10.0),
				R(155.0, 12.0, 14.0, 41.0, 6.0, PieceFill.Accent),
				O(149.0, 5.0, 26.0, 22.0, PieceFill.Accent)
			}, 
			Hairstyle.LeafCap => new HairPiece[2]
			{
				O(43.0, 73.0, 233.0, 169.0),
				P(delegate(PPath p)
				{
					p.M(36.0, 113.0).C(51.0, 32.0, 132.0, 29.0, 231.0, 41.0).Q(272.0, 37.0, 301.0, 18.0)
						.Q(305.0, 74.0, 264.0, 113.0)
						.Z();
				})
			}, 
			Hairstyle.Pilot => new HairPiece[4]
			{
				O(34.0, 35.0, 254.0, 230.0),
				O(12.0, 116.0, 54.0, 87.0),
				O(259.0, 116.0, 49.0, 87.0),
				R(154.0, 37.0, 18.0, 63.0, 8.0, PieceFill.Accent)
			}, 
			Hairstyle.BearHood => new HairPiece[5]
			{
				O(12.0, 40.0, 89.0, 93.0),
				O(220.0, 40.0, 89.0, 93.0),
				O(34.0, 64.0, 251.0, 196.0),
				O(32.0, 60.0, 49.0, 51.0, PieceFill.Accent),
				O(240.0, 60.0, 49.0, 51.0, PieceFill.Accent)
			}, 
			Hairstyle.Jester => new HairPiece[5]
			{
				O(41.0, 70.0, 237.0, 171.0),
				P(delegate(PPath p)
				{
					p.M(36.0, 112.0).C(45.0, 61.0, 42.0, 28.0, 17.0, 46.0).C(71.0, 3.0, 124.0, 22.0, 133.0, 58.0)
						.Q(146.0, 5.0, 186.0, 14.0)
						.L(215.0, 58.0)
						.Q(284.0, 10.0, 303.0, 46.0)
						.Q(272.0, 44.0, 280.0, 117.0)
						.Z();
				}),
				O(5.0, 42.0, 25.0, 26.0, PieceFill.Accent),
				O(175.0, 7.0, 24.0, 25.0, PieceFill.Accent),
				O(289.0, 42.0, 25.0, 26.0, PieceFill.Accent)
			}, 
			Hairstyle.Pirate => new HairPiece[2]
			{
				O(40.0, 61.0, 240.0, 188.0),
				Poly(12.0, 88.0, 47.0, 23.0, 92.0, 44.0, 162.0, 13.0, 227.0, 44.0, 274.0, 22.0, 308.0, 88.0, 274.0, 125.0, 43.0, 125.0)
			}, 
			_ => Array.Empty<HairPiece>(), 
		};
	}

	private static IReadOnlyList<HairPiece> ArcadeFront(Hairstyle style)
	{
		switch (style)
		{
		case Hairstyle.HeroCap:
			return new HairPiece[3]
			{
				P(delegate(PPath p)
				{
					p.M(34.0, 113.0).Q(151.0, 90.0, 276.0, 113.0).Q(321.0, 131.0, 292.0, 136.0)
						.Q(160.0, 116.0, 38.0, 132.0)
						.Z();
				}),
				O(135.0, 55.0, 53.0, 43.0, PieceFill.Accent),
				PolyF(PieceFill.Outfit, 153.0, 66.0, 168.0, 66.0, 175.0, 78.0, 162.0, 90.0, 149.0, 78.0)
			};
		case Hairstyle.FoxHood:
		case Hairstyle.BearHood:
			return new HairPiece[2]
			{
				P(delegate(PPath p)
				{
					p.M(37.0, 159.0).C(17.0, 40.0, 300.0, 39.0, 283.0, 159.0).L(255.0, 140.0)
						.Q(251.0, 96.0, 216.0, 117.0)
						.Q(187.0, 146.0, 162.0, 135.0)
						.Q(136.0, 147.0, 109.0, 117.0)
						.Q(74.0, 96.0, 65.0, 140.0)
						.Z();
				}),
				O(147.0, 100.0, 30.0, 20.0, PieceFill.Accent)
			};
		case Hairstyle.KnightHelm:
			return new HairPiece[4]
			{
				P(delegate(PPath p)
				{
					p.M(38.0, 142.0).Q(43.0, 40.0, 163.0, 51.0).Q(282.0, 42.0, 284.0, 142.0)
						.L(260.0, 156.0)
						.L(256.0, 117.0)
						.Q(162.0, 99.0, 67.0, 117.0)
						.L(62.0, 156.0)
						.Z();
				}),
				R(153.0, 52.0, 20.0, 51.0, 5.0, PieceFill.Accent),
				Line(34.0, 148.0, 42.0, 178.0),
				Line(288.0, 148.0, 280.0, 178.0)
			};
		case Hairstyle.Crown:
			return new HairPiece[3]
			{
				Sweep(177.0, 55.0, 151.0),
				PolyF(PieceFill.Accent, 108.0, 76.0, 99.0, 32.0, 132.0, 51.0, 160.0, 17.0, 190.0, 51.0, 222.0, 32.0, 214.0, 78.0),
				O(151.0, 47.0, 20.0, 22.0, PieceFill.Outfit)
			};
		case Hairstyle.Robot:
			return new HairPiece[7]
			{
				R(47.0, 53.0, 228.0, 65.0, 25.0),
				R(48.0, 220.0, 225.0, 29.0, 12.0),
				R(73.0, 73.0, 34.0, 16.0, 6.0, PieceFill.Accent),
				R(215.0, 73.0, 34.0, 16.0, 6.0, PieceFill.Accent),
				Line(129.0, 234.0, 195.0, 234.0),
				O(25.0, 142.0, 10.0, 12.0, PieceFill.Accent),
				O(285.0, 142.0, 10.0, 12.0, PieceFill.Accent)
			};
		case Hairstyle.LeafCap:
			return new HairPiece[3]
			{
				P(delegate(PPath p)
				{
					p.M(34.0, 119.0).Q(157.0, 74.0, 273.0, 118.0).L(266.0, 139.0)
						.Q(161.0, 111.0, 43.0, 141.0)
						.Z();
				}),
				PolyF(PieceFill.Accent, 219.0, 87.0, 262.0, 52.0, 257.0, 94.0, 222.0, 111.0),
				Line(227.0, 102.0, 252.0, 69.0)
			};
		case Hairstyle.Pilot:
			return new HairPiece[4]
			{
				P(delegate(PPath p)
				{
					p.M(37.0, 147.0).C(29.0, 20.0, 293.0, 21.0, 284.0, 147.0).L(261.0, 151.0)
						.Q(164.0, 62.0, 65.0, 151.0)
						.Z();
				}),
				R(154.0, 45.0, 18.0, 53.0, 8.0, PieceFill.Accent),
				O(26.0, 137.0, 27.0, 44.0, PieceFill.Accent),
				O(273.0, 137.0, 23.0, 44.0, PieceFill.Accent)
			};
		case Hairstyle.Jester:
			return new HairPiece[1] { PolyF(PieceFill.Accent, 37.0, 118.0, 72.0, 88.0, 118.0, 81.0, 165.0, 102.0, 210.0, 79.0, 256.0, 91.0, 281.0, 118.0, 257.0, 140.0, 236.0, 122.0, 210.0, 145.0, 185.0, 123.0, 159.0, 145.0, 132.0, 123.0, 103.0, 144.0, 79.0, 121.0, 58.0, 143.0) };
		case Hairstyle.Pirate:
			return new HairPiece[3]
			{
				P(delegate(PPath p)
				{
					p.M(19.0, 96.0).Q(158.0, 68.0, 301.0, 96.0).L(278.0, 130.0)
						.Q(159.0, 106.0, 43.0, 129.0)
						.Z();
				}),
				O(139.0, 49.0, 43.0, 40.0, PieceFill.Accent),
				PolyF(PieceFill.Outfit, 157.0, 56.0, 166.0, 56.0, 174.0, 69.0, 161.0, 82.0, 148.0, 69.0)
			};
		default:
			return Array.Empty<HairPiece>();
		}
	}

	private static HairPiece Sweep(double part, double top, double tip)
	{
		return P(delegate(PPath p)
		{
			p.M(41.0, 147.0).C(29.0, 59.0, 89.0, top - 7.0, part, top).C(261.0, top - 9.0, 282.0, 94.0, 280.0, 145.0)
				.Q(235.0, 137.0, part, top + 41.0)
				.Q(108.0, 156.0, 74.0, tip)
				.L(60.0, tip - 35.0)
				.Z();
		});
	}

	public static PPath StorybookEyePath()
	{
		return new PPath().M(19.0, 0.0).C(36.0, -1.0, 44.0, 11.0, 44.0, 27.0).C(44.0, 43.0, 37.0, 54.0, 23.0, 54.0)
			.C(8.0, 54.0, 0.0, 45.0, 0.0, 31.0)
			.C(0.0, 13.0, 6.0, 1.0, 19.0, 0.0)
			.Z();
	}

	private static IReadOnlyList<HairPiece> StorybookBack(Hairstyle style, bool faceOnly)
	{
		double hem = (faceOnly ? 242 : 274);
		return style switch
		{
			Hairstyle.SeedHood => new HairPiece[2]
			{
				O(32.0, 54.0, 256.0, 206.0),
				P(delegate(PPath p)
				{
					p.M(148.0, 65.0).C(99.0, 67.0, 92.0, 36.0, 111.0, 16.0).Q(154.0, 11.0, 162.0, 51.0)
						.Q(183.0, 8.0, 221.0, 18.0)
						.Q(233.0, 63.0, 166.0, 69.0)
						.Z();
				})
			}, 
			Hairstyle.RibbonBob => new HairPiece[3]
			{
				O(35.0, 47.0, 250.0, 207.0),
				O(43.0, 159.0, 49.0, 91.0),
				O(232.0, 157.0, 47.0, 93.0)
			}, 
			Hairstyle.SleepyEars => new HairPiece[5]
			{
				O(43.0, 16.0, 65.0, 130.0),
				O(225.0, 36.0, 66.0, 109.0),
				O(31.0, 72.0, 258.0, 188.0),
				O(61.0, 40.0, 29.0, 62.0, PieceFill.Accent),
				O(241.0, 55.0, 29.0, 52.0, PieceFill.Accent)
			}, 
			Hairstyle.MeadowHat => new HairPiece[3]
			{
				O(42.0, 67.0, 237.0, 187.0),
				O(82.0, 23.0, 157.0, 99.0),
				O(14.0, 79.0, 291.0, 51.0)
			}, 
			Hairstyle.CatKerchief => new HairPiece[3]
			{
				P(delegate(PPath p)
				{
					p.M(34.0, 153.0).Q(27.0, 58.0, 39.0, 24.0).Q(81.0, 32.0, 107.0, 67.0)
						.Q(165.0, 45.0, 222.0, 68.0)
						.Q(250.0, 32.0, 280.0, 29.0)
						.Q(295.0, 96.0, 284.0, 177.0)
						.Q(163.0, 307.0, 35.0, 191.0)
						.Z();
				}),
				O(51.0, 47.0, 27.0, 47.0, PieceFill.HairHighlight),
				O(250.0, 49.0, 23.0, 45.0, PieceFill.HairHighlight)
			}, 
			Hairstyle.WindPart => new HairPiece[2]
			{
				O(35.0, 44.0, 247.0, 209.0),
				P(delegate(PPath p)
				{
					p.M(248.0, 142.0).Q(269.0, 207.0, 294.0, 215.0).Q(265.0, 250.0, 231.0, 240.0)
						.Z();
				})
			}, 
			Hairstyle.CandleTuft => new HairPiece[1] { P(delegate(PPath p)
			{
				p.M(32.0, 177.0).C(23.0, 105.0, 97.0, 113.0, 108.0, 58.0).Q(121.0, 62.0, 117.0, 90.0)
					.C(161.0, 68.0, 142.0, 32.0, 183.0, 12.0)
					.C(178.0, 50.0, 229.0, 49.0, 234.0, 89.0)
					.Q(247.0, 81.0, 261.0, 64.0)
					.C(255.0, 116.0, 310.0, 141.0, 282.0, 194.0)
					.Q(153.0, 321.0, 32.0, 177.0)
					.Z();
			}) }, 
			Hairstyle.BerryBraids => new HairPiece[5]
			{
				O(39.0, 50.0, 241.0, 192.0),
				O(42.0, 164.0, 52.0, hem - 164.0),
				O(232.0, 164.0, 48.0, hem - 164.0),
				O(48.0, hem - 24.0, 36.0, 17.0, PieceFill.Accent),
				O(239.0, hem - 24.0, 33.0, 17.0, PieceFill.Accent)
			}, 
			Hairstyle.AcornCap => new HairPiece[3]
			{
				O(38.0, 64.0, 246.0, 189.0),
				R(151.0, 10.0, 20.0, 42.0, 9.0),
				O(32.0, 36.0, 255.0, 101.0)
			}, 
			Hairstyle.MoonMantle => new HairPiece[1] { P(delegate(PPath p)
			{
				p.M(25.0, hem).C(40.0, 85.0, 86.0, 17.0, 170.0, 19.0).C(250.0, 39.0, 275.0, 152.0, 297.0, hem)
					.Q(156.0, hem - 22.0, 25.0, hem)
					.Z();
			}) }, 
			_ => Array.Empty<HairPiece>(), 
		};
	}

	private static IReadOnlyList<HairPiece> StorybookFront(Hairstyle style)
	{
		return style switch
		{
			Hairstyle.SeedHood => new HairPiece[2]
			{
				Fringe(),
				Leaf(137.0, 63.0, PieceFill.Accent)
			}, 
			Hairstyle.RibbonBob => new HairPiece[3]
			{
				Fringe(),
				new HairPiece(new PPath().M(215.0, 65.0).C(175.0, 83.0, 164.0, 51.0, 177.0, 38.0).Q(209.0, 29.0, 219.0, 56.0)
					.C(243.0, 25.0, 280.0, 22.0, 268.0, 48.0)
					.Q(270.0, 85.0, 222.0, 72.0)
					.Z(), PieceFill.Accent),
				O(211.0, 53.0, 21.0, 23.0, PieceFill.Outfit)
			}, 
			Hairstyle.SleepyEars => new HairPiece[4]
			{
				Fringe(),
				O(124.0, 78.0, 20.0, 13.0, PieceFill.HairHighlight),
				O(158.0, 71.0, 22.0, 14.0, PieceFill.HairHighlight),
				O(194.0, 80.0, 20.0, 13.0, PieceFill.HairHighlight)
			}, 
			Hairstyle.MeadowHat => new HairPiece[4]
			{
				Fringe(),
				R(70.0, 67.0, 181.0, 22.0, 10.0, PieceFill.Accent),
				O(14.0, 84.0, 291.0, 36.0),
				Leaf(225.0, 53.0, PieceFill.Outfit)
			}, 
			Hairstyle.CatKerchief => new HairPiece[3]
			{
				Fringe(),
				new HairPiece(new PPath().M(34.0, 99.0).Q(152.0, 28.0, 282.0, 99.0).L(277.0, 119.0)
					.Q(158.0, 67.0, 40.0, 119.0)
					.Z(), PieceFill.Accent),
				O(254.0, 90.0, 24.0, 23.0, PieceFill.Outfit)
			}, 
			Hairstyle.WindPart => new HairPiece[2]
			{
				Fringe(),
				new HairPiece(new PPath().M(63.0, 104.0).Q(105.0, 40.0, 184.0, 62.0).Q(122.0, 67.0, 63.0, 104.0)
					.Z(), PieceFill.HairHighlight, Stroked: false)
			}, 
			Hairstyle.CandleTuft => new HairPiece[2]
			{
				Fringe(),
				Leaf(153.0, 48.0, PieceFill.Accent)
			}, 
			Hairstyle.BerryBraids => new HairPiece[4]
			{
				Fringe(),
				O(48.0, 119.0, 17.0, 18.0, PieceFill.Accent),
				O(61.0, 108.0, 17.0, 18.0, PieceFill.Accent),
				Leaf(57.0, 81.0, PieceFill.Outfit)
			}, 
			Hairstyle.AcornCap => new HairPiece[7]
			{
				Fringe(),
				R(29.0, 87.0, 260.0, 34.0, 16.0, PieceFill.HairShadow),
				Line(65.0, 57.0, 83.0, 80.0),
				Line(102.0, 43.0, 122.0, 77.0),
				Line(145.0, 39.0, 163.0, 76.0),
				Line(187.0, 43.0, 205.0, 78.0),
				Line(226.0, 57.0, 242.0, 81.0)
			}, 
			Hairstyle.MoonMantle => new HairPiece[3]
			{
				Fringe(),
				O(153.0, 51.0, 30.0, 31.0, PieceFill.Accent),
				O(166.0, 46.0, 25.0, 27.0)
			}, 
			_ => Array.Empty<HairPiece>(), 
		};
	}

	private static HairPiece Fringe()
	{
		return P(delegate(PPath p)
		{
			p.M(38.0, 145.0).C(15.0, 31.0, 289.0, 32.0, 280.0, 140.0).L(257.0, 143.0)
				.Q(245.0, 112.0, 204.0, 104.0)
				.Q(192.0, 126.0, 172.0, 128.0)
				.L(174.0, 105.0)
				.Q(153.0, 127.0, 121.0, 132.0)
				.L(125.0, 109.0)
				.Q(81.0, 122.0, 64.0, 147.0)
				.Z();
		});
	}

	private static HairPiece Leaf(double x, double y, PieceFill fill)
	{
		return new HairPiece(new PPath().M(x, y + 26.0).Q(x - 8.0, y, x + 30.0, y).Q(x + 37.0, y + 31.0, x, y + 26.0)
			.Z(), fill);
	}

	public static PPath CloudClubSilhouette(Hairstyle style)
	{
		PPath pPath = new PPath();
		switch (style)
		{
		case Hairstyle.CloudCumulus:
			pPath.M(54.0, 119.0).C(44.0, 80.0, 82.0, 59.0, 116.0, 85.0).C(124.0, 36.0, 192.0, 41.0, 203.0, 88.0)
				.C(236.0, 60.0, 279.0, 88.0, 267.0, 123.0)
				.C(310.0, 128.0, 310.0, 186.0, 276.0, 200.0)
				.C(278.0, 236.0, 246.0, 253.0, 217.0, 239.0)
				.C(194.0, 265.0, 151.0, 266.0, 132.0, 244.0)
				.C(98.0, 263.0, 63.0, 246.0, 63.0, 222.0)
				.C(22.0, 226.0, 13.0, 186.0, 34.0, 163.0)
				.C(17.0, 145.0, 28.0, 122.0, 54.0, 119.0);
			break;
		case Hairstyle.CloudCotton:
			pPath.M(58.0, 113.0).C(40.0, 79.0, 83.0, 56.0, 108.0, 77.0).C(111.0, 42.0, 157.0, 36.0, 178.0, 65.0)
				.C(211.0, 43.0, 252.0, 63.0, 250.0, 98.0)
				.C(285.0, 90.0, 301.0, 123.0, 282.0, 149.0)
				.C(310.0, 179.0, 295.0, 218.0, 264.0, 218.0)
				.C(262.0, 252.0, 223.0, 268.0, 198.0, 249.0)
				.C(169.0, 278.0, 122.0, 265.0, 112.0, 244.0)
				.C(76.0, 259.0, 49.0, 234.0, 55.0, 212.0)
				.C(19.0, 212.0, 17.0, 176.0, 37.0, 159.0)
				.C(19.0, 140.0, 33.0, 113.0, 58.0, 113.0);
			break;
		case Hairstyle.CloudRipple:
			pPath.M(45.0, 130.0).C(31.0, 99.0, 63.0, 77.0, 90.0, 97.0).C(92.0, 64.0, 126.0, 57.0, 147.0, 83.0)
				.C(168.0, 46.0, 218.0, 57.0, 223.0, 94.0)
				.C(253.0, 75.0, 284.0, 95.0, 277.0, 124.0)
				.C(309.0, 130.0, 310.0, 166.0, 287.0, 183.0)
				.C(305.0, 218.0, 268.0, 245.0, 242.0, 229.0)
				.C(229.0, 261.0, 187.0, 259.0, 172.0, 239.0)
				.C(150.0, 266.0, 111.0, 260.0, 99.0, 237.0)
				.C(67.0, 255.0, 40.0, 232.0, 45.0, 208.0)
				.C(9.0, 197.0, 12.0, 148.0, 45.0, 130.0);
			break;
		case Hairstyle.CloudBillow:
			pPath.M(47.0, 135.0).C(29.0, 96.0, 73.0, 68.0, 103.0, 89.0).C(92.0, 49.0, 146.0, 26.0, 174.0, 61.0)
				.C(205.0, 38.0, 243.0, 63.0, 242.0, 103.0)
				.C(285.0, 94.0, 307.0, 134.0, 281.0, 164.0)
				.C(311.0, 193.0, 287.0, 239.0, 251.0, 232.0)
				.C(233.0, 263.0, 196.0, 266.0, 171.0, 248.0)
				.C(141.0, 273.0, 96.0, 255.0, 91.0, 234.0)
				.C(55.0, 246.0, 27.0, 218.0, 40.0, 188.0)
				.C(12.0, 180.0, 17.0, 140.0, 47.0, 135.0);
			break;
		case Hairstyle.CloudBloom:
			pPath.M(50.0, 120.0).C(35.0, 92.0, 60.0, 65.0, 91.0, 78.0).C(95.0, 43.0, 137.0, 32.0, 161.0, 62.0)
				.C(184.0, 31.0, 226.0, 43.0, 232.0, 78.0)
				.C(261.0, 65.0, 288.0, 91.0, 274.0, 120.0)
				.C(310.0, 129.0, 306.0, 168.0, 284.0, 183.0)
				.C(300.0, 214.0, 271.0, 243.0, 243.0, 229.0)
				.C(235.0, 263.0, 189.0, 268.0, 163.0, 242.0)
				.C(137.0, 268.0, 94.0, 263.0, 85.0, 232.0)
				.C(53.0, 246.0, 23.0, 220.0, 37.0, 188.0)
				.C(10.0, 166.0, 18.0, 126.0, 50.0, 120.0);
			break;
		case Hairstyle.CloudTuft:
			pPath.M(53.0, 128.0).C(35.0, 98.0, 65.0, 70.0, 94.0, 88.0).C(95.0, 51.0, 128.0, 34.0, 161.0, 55.0)
				.C(177.0, 66.0, 194.0, 60.0, 203.0, 50.0)
				.C(213.0, 38.0, 225.0, 47.0, 222.0, 62.0)
				.C(219.0, 81.0, 205.0, 89.0, 211.0, 99.0)
				.C(245.0, 76.0, 280.0, 96.0, 270.0, 128.0)
				.C(308.0, 139.0, 303.0, 184.0, 276.0, 196.0)
				.C(287.0, 231.0, 249.0, 254.0, 223.0, 239.0)
				.C(197.0, 265.0, 151.0, 260.0, 134.0, 239.0)
				.C(104.0, 257.0, 68.0, 248.0, 60.0, 220.0)
				.C(22.0, 224.0, 12.0, 184.0, 35.0, 163.0)
				.C(23.0, 144.0, 33.0, 128.0, 53.0, 128.0);
			break;
		case Hairstyle.CloudDrift:
			pPath.M(43.0, 138.0).C(25.0, 108.0, 60.0, 83.0, 87.0, 103.0).C(87.0, 69.0, 133.0, 53.0, 157.0, 83.0)
				.C(180.0, 62.0, 214.0, 76.0, 222.0, 102.0)
				.C(244.0, 85.0, 272.0, 95.0, 278.0, 123.0)
				.C(309.0, 125.0, 310.0, 164.0, 284.0, 183.0)
				.C(304.0, 211.0, 277.0, 240.0, 246.0, 227.0)
				.C(221.0, 253.0, 191.0, 247.0, 174.0, 236.0)
				.C(138.0, 260.0, 104.0, 246.0, 91.0, 231.0)
				.C(59.0, 248.0, 27.0, 225.0, 39.0, 200.0)
				.C(8.0, 186.0, 15.0, 149.0, 43.0, 138.0);
			break;
		case Hairstyle.CloudPillow:
			pPath.M(60.0, 111.0).C(45.0, 65.0, 97.0, 46.0, 126.0, 75.0).C(149.0, 46.0, 194.0, 51.0, 211.0, 80.0)
				.C(248.0, 50.0, 288.0, 82.0, 271.0, 118.0)
				.C(309.0, 142.0, 301.0, 183.0, 278.0, 198.0)
				.C(290.0, 235.0, 250.0, 260.0, 218.0, 238.0)
				.C(189.0, 270.0, 148.0, 263.0, 130.0, 241.0)
				.C(93.0, 268.0, 51.0, 244.0, 57.0, 216.0)
				.C(18.0, 212.0, 16.0, 164.0, 41.0, 149.0)
				.C(30.0, 132.0, 39.0, 113.0, 60.0, 111.0);
			break;
		case Hairstyle.CloudNimbus:
			pPath.M(50.0, 124.0).C(35.0, 101.0, 56.0, 77.0, 82.0, 86.0).C(77.0, 53.0, 115.0, 40.0, 140.0, 65.0)
				.C(161.0, 32.0, 206.0, 43.0, 212.0, 77.0)
				.C(242.0, 57.0, 279.0, 84.0, 266.0, 117.0)
				.C(300.0, 119.0, 310.0, 154.0, 284.0, 180.0)
				.C(307.0, 209.0, 275.0, 243.0, 247.0, 230.0)
				.C(237.0, 261.0, 197.0, 265.0, 177.0, 241.0)
				.C(158.0, 270.0, 113.0, 260.0, 104.0, 240.0)
				.C(72.0, 257.0, 45.0, 236.0, 49.0, 211.0)
				.C(17.0, 216.0, 6.0, 181.0, 31.0, 161.0)
				.C(16.0, 142.0, 31.0, 124.0, 50.0, 124.0);
			break;
		case Hairstyle.CloudWisp:
			pPath.M(50.0, 126.0).C(34.0, 98.0, 68.0, 76.0, 94.0, 96.0).C(96.0, 60.0, 139.0, 49.0, 162.0, 83.0)
				.C(184.0, 59.0, 214.0, 77.0, 221.0, 99.0)
				.C(246.0, 101.0, 266.0, 90.0, 277.0, 77.0)
				.C(287.0, 65.0, 300.0, 76.0, 294.0, 90.0)
				.C(288.0, 106.0, 280.0, 114.0, 280.0, 126.0)
				.C(310.0, 143.0, 303.0, 180.0, 278.0, 195.0)
				.C(289.0, 228.0, 251.0, 246.0, 226.0, 233.0)
				.C(201.0, 259.0, 157.0, 255.0, 139.0, 237.0)
				.C(106.0, 258.0, 65.0, 240.0, 60.0, 219.0)
				.C(21.0, 217.0, 13.0, 183.0, 35.0, 160.0)
				.C(22.0, 142.0, 32.0, 128.0, 50.0, 126.0);
			break;
		default:
			return PPath.Oval(24.0, 60.0, 272.0, 204.0);
		}
		return pPath.Z();
	}

	public static PPath CaretEyePath(PortraitExpression e, int index)
	{
		double num;
		switch (e)
		{
		case PortraitExpression.Surprised:
			num = 20.0;
			break;
		case PortraitExpression.Excited:
		case PortraitExpression.Loving:
			num = 18.0;
			break;
		case PortraitExpression.Listening:
			num = 17.0;
			break;
		case PortraitExpression.Thinking:
			num = 9.0;
			break;
		case PortraitExpression.Skeptical:
			num = ((index == 0) ? 4.0 : 16.0);
			break;
		case PortraitExpression.Shy:
			num = 12.0;
			break;
		case PortraitExpression.Sad:
			num = -14.0;
			break;
		case PortraitExpression.Determined:
			num = 8.0;
			break;
		default:
			num = 16.0;
			break;
		}
		double rise = num;
		return Caret(36.0, 43.0, rise);
	}

	public static PPath CloudClosedEyePath(PortraitExpression e, double centerX)
	{
		return Caret(centerX, 164.0, e switch
		{
			PortraitExpression.Sleepy => -16.0, 
			PortraitExpression.Laughing => 16.0, 
			_ => 0.0, 
		});
	}

	private static PPath Caret(double cx, double cy, double rise)
	{
		return new PPath().M(cx - 24.0, cy + rise / 2.0).L(cx, cy - rise / 2.0).L(cx + 24.0, cy + rise / 2.0);
	}

	public static PPath CloudShape(bool wide, bool mirrored, (double X, double Y, double W, double H) rect)
	{
		PPath pPath = (wide ? WideTraced : CompactTraced);
		(double, double, double, double) tuple = pPath.Bounds();
		if (tuple.Item3 <= 0.0 || tuple.Item4 <= 0.0)
		{
			return new PPath();
		}
		double num = Math.Min(rect.W / tuple.Item3, rect.H / tuple.Item4);
		Matrix3x2 m = Matrix3x2.CreateTranslation((float)(0.0 - (tuple.Item1 + tuple.Item3 / 2.0)), (float)(0.0 - (tuple.Item2 + tuple.Item4 / 2.0))) * Matrix3x2.CreateScale((float)(mirrored ? (0.0 - num) : num), (float)num) * Matrix3x2.CreateTranslation((float)(rect.X + rect.W / 2.0), (float)(rect.Y + rect.H / 2.0));
		return pPath.Transformed(m);
	}

	public static CloudFaceTraits ClassicTraits(PortraitAppearance a)
	{
		return new CloudFaceTraits(a.Hairstyle.RawValue(), a.HairColor.ClassicCloudHue());
	}

	public static PPath ClassicSilhouette(PortraitAppearance a)
	{
		CloudFaceTraits cloudFaceTraits = ClassicTraits(a);
		return CloudShape(cloudFaceTraits.IsWide, cloudFaceTraits.IsMirrored, ClassicBodyRect);
	}

	public static (double X, double Y) ClassicEyeCenter(int index, PortraitAppearance a)
	{
		double num = 16.0 * ClassicTraits(a).RestingGazeDirection * 1.6;
		return (X: 160.0 + ((index == 0) ? (-45.6) : 45.6) + num, Y: ClassicBodyRect.Y + ClassicBodyRect.H / 2.0 + 6.4);
	}

	public static PPath ClassicClosedEyePath(PortraitExpression e, (double X, double Y) c)
	{
		int num;
		if (e == PortraitExpression.Laughing)
		{
			num = 16;
		}
		else
		{
			bool flag = ((e == PortraitExpression.Sleepy || e == PortraitExpression.Sad) ? true : false);
			num = (flag ? (-16) : 0);
		}
		double num2 = num;
		return new PPath().M(c.X - 24.0, c.Y + num2 / 2.0).L(c.X, c.Y - num2 / 2.0).L(c.X + 24.0, c.Y + num2 / 2.0);
	}

	public static PPath MouthPath(PortraitExpression e)
	{
		(double, double, double) tuple;
		switch (e)
		{
		case PortraitExpression.Surprised:
			tuple = (17.0, 13.0, -13.0);
			break;
		case PortraitExpression.Excited:
		case PortraitExpression.Laughing:
			tuple = (32.0, 19.0, -3.0);
			break;
		case PortraitExpression.Happy:
		case PortraitExpression.Loving:
			tuple = (22.0, 5.0, 2.0);
			break;
		case PortraitExpression.Thinking:
		case PortraitExpression.Skeptical:
			tuple = (18.0, 0.0, -1.0);
			break;
		case PortraitExpression.Sleepy:
			tuple = (13.0, 1.0, 0.0);
			break;
		case PortraitExpression.Shy:
			tuple = (14.0, 5.0, 3.0);
			break;
		case PortraitExpression.Sad:
			tuple = (18.0, -5.0, -3.0);
			break;
		default:
			tuple = (18.0, 4.0, 2.0);
			break;
		}
		(double, double, double) tuple2 = tuple;
		double item = tuple2.Item1;
		double item2 = tuple2.Item2;
		double item3 = tuple2.Item3;
		double num = 162.0 - item / 2.0;
		double num2 = 162.0 + item / 2.0;
		return new PPath().M(num, 217.0).C(num + 2.0, 217.0 + item2, num2 - 2.0, 217.0 + item2, num2, 217.0).C(num2 - 2.0, 217.0 + item3, num + 2.0, 217.0 + item3, num, 217.0)
			.Z();
	}

	public static PPath FacePath(double foreheadTop)
	{
		return new PPath().M(53.0, 145.0).C(53.0, 110.0, 89.0, foreheadTop, 162.0, foreheadTop).C(235.0, foreheadTop, 273.0, 110.0, 273.0, 145.0)
			.C(273.0, 216.0, 222.0, 249.0, 164.0, 249.0)
			.C(99.0, 249.0, 53.0, 216.0, 53.0, 145.0)
			.Z();
	}

	public static PPath JawPath()
	{
		return new PPath().M(53.0, 145.0).C(53.0, 216.0, 99.0, 249.0, 164.0, 249.0).C(222.0, 249.0, 273.0, 216.0, 273.0, 145.0);
	}

	public static PPath HeartPath(double sx, double sy, double tx = 0.0, double ty = 0.0)
	{
		return new PPath().M(39.0, 73.0).C(25.0, 62.0, 11.0, 52.0, 11.0, 38.0).C(11.0, 20.0, 31.0, 20.0, 39.0, 32.0)
			.C(49.0, 20.0, 67.0, 20.0, 67.0, 38.0)
			.C(67.0, 52.0, 52.0, 62.0, 39.0, 73.0)
			.Z()
			.Transformed(Matrix3x2.CreateScale((float)sx, (float)sy) * Matrix3x2.CreateTranslation((float)tx, (float)ty));
	}

	public static PPath LedgeHand(double w, double h)
	{
		return new PPath().M(X(2.0), Y(13.0)).C(X(2.0), Y(7.0), X(6.0), Y(4.0), X(12.0), Y(4.0)).C(X(14.0), Y(4.0), X(15.0), Y(5.5), X(17.0), Y(5.5))
			.C(X(19.0), Y(5.5), X(20.0), Y(4.0), X(23.0), Y(4.0))
			.C(X(29.0), Y(4.0), X(32.0), Y(8.0), X(32.0), Y(14.0))
			.C(X(32.0), Y(20.0), X(27.0), Y(23.0), X(17.0), Y(23.0))
			.C(X(7.0), Y(23.0), X(2.0), Y(20.0), X(2.0), Y(13.0))
			.Z();
		double X(double x)
		{
			return w * x / 34.0;
		}
		double Y(double y)
		{
			return h * y / 26.0;
		}
	}

	private static HairPiece P(Action<PPath> draw)
	{
		PPath pPath = new PPath();
		draw(pPath);
		return new HairPiece(pPath, PieceFill.Hair);
	}

	private static HairPiece E(double x, double y, double w, double h)
	{
		return new HairPiece(PPath.Oval(x, y, w, h), PieceFill.Hair);
	}

	private static HairPiece O(double x, double y, double w, double h, PieceFill fill = PieceFill.Hair)
	{
		return new HairPiece(PPath.Oval(x, y, w, h), fill);
	}

	private static HairPiece R(double x, double y, double w, double h, double r, PieceFill fill = PieceFill.Hair)
	{
		return new HairPiece(PPath.Rounded(x, y, w, h, r), fill);
	}

	private static HairPiece Poly(params double[] c)
	{
		return PolyF(PieceFill.Hair, c);
	}

	private static HairPiece PolyF(PieceFill fill, params double[] c)
	{
		PPath pPath = new PPath().M(c[0], c[1]);
		for (int i = 2; i < c.Length; i += 2)
		{
			pPath.L(c[i], c[i + 1]);
		}
		return new HairPiece(pPath.Z(), fill);
	}

	private static HairPiece Line(params double[] c)
	{
		PPath pPath = new PPath().M(c[0], c[1]);
		for (int i = 2; i < c.Length; i += 2)
		{
			pPath.L(c[i], c[i + 1]);
		}
		return new HairPiece(pPath, null);
	}
}
