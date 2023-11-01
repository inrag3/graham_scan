using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Timers;

namespace Drawer
{
    public class Hull
    {
        private IList<Point> _points;

        public IList<Point> Points { get; private set; } = new List<Point>();

        public Hull(IList<Point> points)
        {
            _points = (List<Point>)points;
            Create();
        }


        private void Create()
        {
            _points = _points
                .OrderBy(p => p.Y) //Сортировка по Y координате (находим самую нижнюю точку).
                .ToList();

            _points = _points
                .OrderBy(p => Math.Atan2(p.Y - _points[0].Y, p.X - _points[0].X)) // Затем сортируем по углу от начальной точки.
                .ToList();
            
            List<int> indexes = new()
            {
                0,
                1,
            };

            for (int i = 2; i < _points.Count; i++)
            {
                while (Point.Rotate(_points[indexes[^2]], _points[indexes[^1]], _points[i]) < 0)
                {
                    indexes.RemoveAt(indexes.Count - 1);
                }
                indexes.Add(i);
            }

            foreach (var index in indexes)
            {
                Points.Add(_points[index]);
            }
        }
    }

    public class Cursors
    {
        private static Lazy<Cursor> _pen = new Lazy<Cursor>(() =>
            new Cursor(Application.GetResourceStream(new Uri("/Resources/pencil.cur", UriKind.Relative)).Stream));
        private static Lazy<Cursor> _fill = new Lazy<Cursor>(() =>
            new Cursor(Application.GetResourceStream(new Uri("/Resources/fill.cur", UriKind.Relative)).Stream));

        public static Cursor Pen => _pen.Value;
        public static Cursor Fill => _fill.Value;
    }
    public class Colors
    {

        private static readonly Lazy<Color> _black = new Lazy<Color>(() => new Color(0, 0, 0));
        private static readonly Lazy<Color> _red = new Lazy<Color>(() => new Color(0, 0, 255));
        private static readonly Lazy<Color> _green = new Lazy<Color>(() => new Color(0, 255, 0));
        private static readonly Lazy<Color> _blue = new Lazy<Color>(() => new Color(255, 0, 0));
        private static readonly Lazy<Color> _white = new Lazy<Color>(() => new Color(255, 255, 255));

        private static readonly Lazy<Color>[] _colors = new Lazy<Color>[] { _red, _green, _blue };
        public static Color Black => _black.Value;
        public static Color Red => _red.Value;
        public static Color Green => _green.Value;
        public static Color Blue => _blue.Value;
        public static Color White => _white.Value;


        public static Color Random() => _colors[new Random().Next() % _colors.Length].Value;
    }

    public class Color
    {
        double blue;
        double green;
        double red;
        double alpha;

        public Color(System.Windows.Media.Color color)
        {
            Blue = color.B;
            Green = color.G;
            Red = color.R;
            Alpha = color.A;
        }

        public Color(double blue, double green, double red, double alpha = 255)
        {
            Blue = blue;
            Green = green;
            Red = red;
            Alpha = alpha;
        }

        public double Blue { get => blue; set => blue = value; }
        public double Green { get => green; set => green = value; }
        public double Red { get => red; set => red = value; }
        public double Alpha { get => alpha; set => alpha = value; }

        public Color GetDiffWith(Color other)
        {
            return new Color(Blue - other.Blue, Green - other.Green, Red - other.Red);
        }

        public Color GetSumWith(Color other)
        {
            return new Color(Blue + other.Blue, Green + other.Green, Red + other.Red);
        }

        public Color GetMultBy(double k)
        {
            return new Color(Blue * k, Green * k, Red * k);
        }

        private byte Normalize(double v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return (byte)v;
        }

        public byte[] ToBgra()
        {
            return new byte[] { Normalize(Blue), Normalize(Green), Normalize(Red), Normalize(Alpha) };
        }

        static public void Swap(Color c1, Color c2)
        {
            double c1Blue = c1.Blue;
            double c1Green = c1.Green;
            double c1Red = c1.Red;

            c1.Blue = c2.Blue;
            c1.Green = c2.Green;
            c1.Red = c2.Red;
            c2.Blue = c1Blue;
            c2.Green = c1Green;
            c2.Red = c1Red;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Color other = (Color)obj;
            return Blue == other.Blue && Red == other.Red && Green == other.Green && Alpha == other.Alpha;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public bool IsSimilar(Color other, double threshold = 50)
        {
            double rDiff = Red - other.Red;
            double gDiff = Green - other.Green;
            double bDiff = Blue - other.Blue;
            double distance = Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
            return distance <= threshold;
        }

    }

    public class Line
    {

        private readonly IList<Point> _points = new Point[2];

        public int Id { get; set; }
        public string Title => $"Line #{Id}";

        public int X1 { get => _points[0].X; }
        public int Y1 { get => _points[0].Y; }
        public int X2 { get => _points[1].X; }
        public int Y2 { get => _points[1].Y; }

        public Point Last { set => _points[1] = value; }

        public IList<Point> Points => _points;

        public Point Center => new((X1 + X2) / 2, (Y2 + Y1) / 2);

        public Line(int x1, int y1)
        {
            _points[0] = new(x1, y1);
        }

        public Line(int x1, int y1, int x2, int y2) : this(x1, y1)
        {
            _points[1] = new(x2, y2);
        }

        public Point? Intersection(Line line)
        {
            Point A = _points[0];
            Point B = _points[1];
            Point C = line._points[0];
            Point D = line._points[1];

            double determinant = (B.X - A.X) * (D.Y - C.Y) - (B.Y - A.Y) * (D.X - C.X);

            if (determinant == 0)
                return null;

            double t = ((C.X - A.X) * (D.Y - C.Y) - (C.Y - A.Y) * (D.X - C.X)) / determinant;

            if (!(t >= 0 && t <= 1))
                return null;

            int x = (int)(A.X + t * (B.X - A.X));
            int y = (int)(A.Y + t * (B.Y - A.Y));
            return new Point(x, y);
        }
    }

    public class Polygon
    {
        private IList<Point> _points = new List<Point>();

        public IList<Point> Points => _points;


        public List<Edge> Edges
        {
            get
            {
                List<Edge> edges = new List<Edge>();
                for (int i = 0; i < _points.Count; i++)
                {
                    edges.Add(new Edge(_points[i], _points[(i + 1) % _points.Count]));
                }
                return edges;
            }
        }

        public Point Center
        {
            get
            {
                var xSum = _points.Sum(point => point.X);
                var ySum = _points.Sum(point => point.Y);
                var count = _points.Count();
                return new Point(xSum / count, ySum / count);
            }
        }

        public int Id { get; set; }
        public string Title => $"Polygon #{Id}";

        public Polygon(int x, int y)
        {
            _points.Add(new Point(x, y));
        }

        public void Add(int x, int y) => _points.Add(new Point(x, y));

        public void Add(Point point) => _points.Add(point);

        public bool Contains(Point point)
        {
            int intersections = 0;
            var edges = Edges;
            for (int i = 0; i < edges.Count; i++)
            {
                var edge = edges[i];

                if (edge.IsHorizontal || point.Y == edge.Down.Y)
                    continue;

                // Почему > Up ? Если вспомнить, что мы работаем от левого верхнего угла, сразу становится понятно)
                if (point.Y < edge.Down.Y && point.Y >= edge.Up.Y && edge.IsRight(point))
                {
                    if (edge.Direction == Edge.MyDirection.LEFT && edge.Down.X < point.X)
                        intersections++;
                    if (edge.Direction == Edge.MyDirection.RIGHT && edge.Up.X < point.X)
                        intersections++;
                }
            }
            return intersections % 2 == 1;
        }

    }



    public class Edge
    {

        public enum MyDirection
        {
            RIGHT,
            LEFT,
        }

        private Point _up;
        private Point _down;

        private MyDirection _direction;
        public MyDirection Direction => _direction;


        public Point Up => _up;
        public Point Down => _down;
        public bool IsHorizontal => _up.Y == _down.Y;


        public Edge(Point p1, Point p2)
        {
            if (p1.Y > p2.Y)
            {
                _up = p2;
                _down = p1;
            }
            else
            {
                _up = p1;
                _down = p2;
            }
            if (_up.X > _down.X)
                _direction = MyDirection.LEFT;
            else
                _direction = MyDirection.RIGHT;
        }
        public bool IsRight(Point q)
        {
            return (Up.X - Down.X) * (q.Y - Down.Y) - (Up.Y - Down.Y) * (q.X - Down.X) > 0;
        }
    }



    public class Point
    {
        int x;
        int y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get => x; set => x = value; }
        public int Y { get => y; set => y = value; }

        public double XDouble { get => (double)x; }
        public double YDouble { get => (double)y; }

        static public void Swap(Point p1, Point p2)
        {
            int p2X = p2.X;
            int p2Y = p2.Y;

            p2.X = p1.X;
            p2.Y = p1.Y;
            p1.X = p2X;
            p1.Y = p2Y;
        }


        public int Distance(Point p2)
        {
            return (int)Math.Sqrt(Math.Pow(X - p2.X, 2) + Math.Pow(Y - p2.Y, 2));
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Point otherPoint = (Point)obj;
            return X == otherPoint.X && Y == otherPoint.Y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static int Rotate(Point a, Point b, Point c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y);
        }
    }




    //TODO сделать отдельную обёртку для битмапа.
    public class Drawer
    {
        WriteableBitmap _bitmap;
        byte[] _pixels;
        int _width;
        int _height;
        int _stride;
        private Color _base;

        public WriteableBitmap Bitmap
        {
            get => _bitmap;
            set => _bitmap = new WriteableBitmap(value);
        }
        public int Height => (int)_bitmap.Height;

        public Drawer(int width, int height)
        {
            _width = width;
            _height = height;
            _base = Colors.White;
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            int pixelWidth = _bitmap.PixelWidth;
            int pixelHeight = _bitmap.PixelHeight;
            _stride = pixelWidth * ((_bitmap.Format.BitsPerPixel + 7) / 8);
            _pixels = new byte[pixelHeight * _stride];
            Clear();
        }

        public bool IsPointInTriangle(Point p, Point p1, Point p2, Point p3)
        {
            int p1Side = (p1.Y - p2.Y) * p.X + (p2.X - p1.X) * p.Y + (p1.X * p2.Y - p2.X * p1.Y);
            int p2Side = (p2.Y - p3.Y) * p.X + (p3.X - p2.X) * p.Y + (p2.X * p3.Y - p3.X * p2.Y);
            int p3Side = (p3.Y - p1.Y) * p.X + (p1.X - p3.X) * p.Y + (p3.X * p1.Y - p1.X * p3.Y);

            return (p1Side >= 0 && p2Side >= 0 && p3Side >= 0) || (p1Side <= 0 && p2Side <= 0 && p3Side <= 0);
        }

        private void SwapTrianglePoints(Point p1, Color c1, Point p2, Color c2, Point p3, Color c3)
        {
            if (p1.Y > p2.Y)
            {
                if (p2.Y > p3.Y)
                {
                    Point.Swap(p1, p3);
                    Color.Swap(c1, c3);
                }
                else
                {
                    Point.Swap(p1, p2);
                    Color.Swap(c1, c2);
                }
            }
            if (p1.Y == p2.Y)
            {
                Point.Swap(p1, p3);
                Color.Swap(c1, c3);
            }
            if (p1.Y == p3.Y)
            {
                Point.Swap(p1, p2);
                Color.Swap(c1, c2);
            }
            if (p2.X > p3.X)
            {
                Point.Swap(p2, p3);
                Color.Swap(c2, c3);
            }
        }

        private Color InterpolateColor(double coord, double coord1, Color c1, double coord2, Color c2)
        {
            Color cc1 = c1.GetMultBy(1);
            Color cc2 = c2.GetMultBy(1);
            if (coord1 > coord2)
            {
                (coord1, coord2) = (coord2, coord1);
                Color.Swap(cc1, cc2);
            }

            double diff = coord2 - coord1;
            Color cDiff = cc2.GetDiffWith(cc1);

            return cDiff.GetMultBy(1 / diff).GetMultBy(coord - coord1).GetSumWith(cc1);
        }

        public void DrawTriangleLinear(Point p1, Color c1, Point p2, Color c2, Point p3, Color c3)
        {
            SwapTrianglePoints(p1, c1, p2, c2, p3, c3);

            int yMin = Math.Min(p1.Y, Math.Min(p2.Y, p3.Y));
            int yMax = Math.Max(p1.Y, Math.Max(p2.Y, p3.Y));
            int xMin = Math.Min(p1.X, Math.Min(p2.X, p3.X));
            int xMax = Math.Max(p1.X, Math.Max(p2.X, p3.X));

            for (int y = yMin; y <= yMax; y++)
            {
                int xLeft = xMin;
                while (!IsPointInTriangle(new Point(xLeft, y), p1, p2, p3)) xLeft++;
                int xRight = xMax;
                while (!IsPointInTriangle(new Point(xRight, y), p1, p2, p3)) xRight--;
                Color cLeft = InterpolateColor(y, p1.Y, c1, p2.Y, c2);
                Color cRight = InterpolateColor(y, p1.Y, c1, p3.Y, c3);

                for (int x = xLeft; x <= xRight; x++)
                {
                    Color c = InterpolateColor(x, xLeft, cLeft, xRight, cRight);
                    DrawPoint(x, y, c.ToBgra());
                }
            }
        }

        public void DrawTriangleVectors(Point p1, Color c1, Point p2, Color c2, Point p3, Color c3)
        {
            Point np1 = new Point(0, 0);
            Point np2 = new Point(p2.X - p1.X, p2.Y - p1.Y);
            Point np3 = new Point(p3.X - p1.X, p3.Y - p1.Y);

            if (np3.Y == 0)
            {
                Point.Swap(np2, np3);
                Color.Swap(c2, c3);
            }

            Color diff1 = c2.GetDiffWith(c1);
            Color diff2 = c3.GetDiffWith(c1);

            int xMin = Math.Min(np1.X, Math.Min(np2.X, np3.X));
            int yMin = Math.Min(np1.Y, Math.Min(np2.Y, np3.Y));
            int xMax = Math.Max(np1.X, Math.Max(np2.X, np3.X));
            int yMax = Math.Max(np1.Y, Math.Max(np2.Y, np3.Y));

            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    double w1 = (y * np3.XDouble - x * np3.YDouble) / (np2.YDouble * np3.XDouble - np2.XDouble * np3.YDouble);

                    if (w1 >= 0 && w1 <= 1)
                    {
                        double w2 = (y - w1 * np2.YDouble) / np3.YDouble;

                        if (w2 >= 0 && ((w1 + w2) <= 1))
                        {
                            Color diff1w1 = diff1.GetMultBy(w1);
                            Color diff2w2 = diff2.GetMultBy(w2);
                            Color resultedColor = c1.GetSumWith(diff1w1).GetSumWith(diff2w2);
                            DrawPoint(x + p1.X, y + p1.Y, resultedColor.ToBgra());
                        }
                    }
                }
            }
        }

        private double GetFractionPart(double f)
        {
            return f - Math.Truncate(f);
        }

        private double GetRFractionPart(double f)
        {
            return 1 - GetFractionPart(f);
        }

        public void DrawLineWu(int x1, int y1, int x2, int y2, byte[] colorData)
        {
            bool steep = Math.Abs(y2 - y1) > Math.Abs(x2 - x1);

            if (steep)
            {
                (x1, y1) = (y1, x1);
                (x2, y2) = (y2, x2);
            }

            if (x1 > x2)
            {
                (x1, x2) = (x2, x1);
                (y1, y2) = (y2, y1);
            }

            double dx = x2 - x1;
            double dy = y2 - y1;
            double gradient = dx == 0 ? 1 : dy / dx;

            double xend = x1;
            double yend = y1 + gradient * (xend - x1);

            double xgap = GetRFractionPart(x1 + 0.5);
            double xpxl1 = xend;
            double ypxl1 = Math.Truncate(yend);

            if (steep)
            {
                DrawPoint(ypxl1, xpxl1, new byte[] { colorData[0], colorData[1], colorData[2], (byte)(GetRFractionPart(yend) * xgap * 255) });
                DrawPoint(ypxl1 + 1, xpxl1, new byte[] { colorData[0], colorData[1], colorData[2], (byte)(GetFractionPart(yend) * xgap * 255) });
            }
            else
            {
                DrawPoint(xpxl1, ypxl1, new byte[] { colorData[0], colorData[1], colorData[2], (byte)(GetRFractionPart(yend) * xgap * 255) });
                DrawPoint(xpxl1, ypxl1 + 1, new byte[] { colorData[0], colorData[1], colorData[2], (byte)(GetFractionPart(yend) * xgap * 255) });
            }

            double intery = yend + gradient;

            xend = x2;
            yend = y2 + gradient * (xend - x2);
            xgap = GetFractionPart(x2 + 0.5);
            double xpxl2 = xend;
            double ypxl2 = Math.Truncate(yend);

            if (steep)
            {
                DrawPoint(ypxl2, xpxl2, new byte[] { colorData[0], colorData[1], colorData[2], (byte)(GetRFractionPart(yend) * xgap * 255) });
                DrawPoint(ypxl2 + 1, xpxl2, new byte[] { colorData[0], colorData[1], colorData[2], (byte)(GetFractionPart(yend) * xgap * 255) });
            }
            else
            {
                DrawPoint(xpxl2, ypxl2, new byte[] { colorData[0], colorData[1], colorData[2], (byte)(GetRFractionPart(yend) * xgap * 255) });
                DrawPoint(xpxl2, ypxl2 + 1, new byte[] { colorData[0], colorData[1], colorData[2], (byte)(GetFractionPart(yend) * xgap * 255) });
            }

            if (steep)
            {
                for (double x = xpxl1 + 1; x < xpxl2; x++)
                {
                    DrawPoint(Math.Truncate(intery), x, new byte[] { colorData[0], colorData[1], colorData[2], (byte)(GetRFractionPart(intery) * 255) });
                    DrawPoint(Math.Truncate(intery) + 1, x, new byte[] { colorData[0], colorData[1], colorData[2], (byte)(GetFractionPart(intery) * 255) });
                    intery += gradient;
                }
            }
            else
            {
                for (double x = xpxl1 + 1; x < xpxl2; x++)
                {
                    DrawPoint(x, Math.Truncate(intery), new byte[] { colorData[0], colorData[1], colorData[2], (byte)(GetRFractionPart(intery) * 255) });
                    DrawPoint(x, Math.Truncate(intery) + 1, new byte[] { colorData[0], colorData[1], colorData[2], (byte)(GetFractionPart(intery) * 255) });
                    intery += gradient;
                }
            }
        }

        public void DrawLineBresenham(Point p1, Point p2, Color color) =>
            DrawLineBresenham(p1.X, p1.Y, p2.X, p2.Y, color.ToBgra());

        public void DrawLineBresenham(int x1, int y1, int x2, int y2, byte[] colorData)
        {
            int dy = Math.Abs(y2 - y1);
            int dx = Math.Abs(x2 - x1);

            if (dy <= dx) // gradient <= 1
            {
                int di = 2 * dy - dx;
                if (y1 > y2)
                {
                    (y1, y2) = (y2, y1);
                    (x1, x2) = (x2, x1);
                }
                int step = 1;
                if (x1 > x2)
                {
                    step = -1;
                }
                int y = y1;
                for (int x = x1; x * step <= x2 * step; x += step)
                {
                    if (di < 0)
                    {
                        di += 2 * dy;
                    }
                    else
                    {
                        y++;
                        di += 2 * (dy - dx);
                    }

                    DrawPoint(x, y, colorData);
                }
            }
            else // gradient > 1
            {
                int di = 2 * dx - dy;

                if (x1 > x2)
                {
                    (y1, y2) = (y2, y1);
                    (x1, x2) = (x2, x1);
                }
                int step = 1;
                if (y1 > y2)
                {
                    step = -1;
                }
                int x = x1;
                for (int y = y1; y * step <= y2 * step; y += step)
                {
                    if (di < 0)
                    {
                        di = di + 2 * dx;
                    }
                    else
                    {
                        x++;
                        di = di + 2 * (dx - dy);
                    }

                    DrawPoint(x, y, colorData);
                }
            }
        }

        public void DrawLineBresenham(Line line, Color color) =>
            DrawLineBresenham(line.X1, line.Y1, line.X2, line.Y2, color.ToBgra());

        public void DrawPoint(int x, int y, byte[] colorData, int thickness = 1)
        {
            Int32Rect rect = new Int32Rect(x, y, 1, 1);
            _bitmap.WritePixels(rect, colorData, 4, 0);
        }

        public void DrawPoint(Point point, Color color, int thickness = 1)
        {
            int radius = thickness / 2;

            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    if (j * j + i * i <= radius * radius)
                    {
                        DrawPoint(point.X + j, point.Y + i, color.ToBgra());
                    }
                }
            }
        }


        public void DrawPoint(double x, double y, byte[] colorData)
        {
            DrawPoint((int)x, (int)y, colorData);
        }

        public void DrawPolygon(IList<Point> points, Color color)
        {
            for (int i = 0; i < points.Count; i++)
            {
                var previous = points[i];
                var current = points[(i + 1) % points.Count];
                DrawLineBresenham(previous.X, previous.Y, current.X, current.Y, color.ToBgra());
            }
        }

        public void DrawPolygon(IList<Line> lines, Color color)
        {
            foreach (var line in lines)
                DrawLineBresenham(line, color);
        }


        public void DrawPolygon(Polygon polygon, Color color) =>
            DrawPolygon(polygon.Points, color);

        public void Clear()
        {
            var whiteBytes = new byte[_width * _height * 4];
            for (var i = 0; i < whiteBytes.Length; i += 4)
            {
                whiteBytes[i] = 255;
                whiteBytes[i + 1] = 255;
                whiteBytes[i + 2] = 255;
                whiteBytes[i + 3] = 255;
            }
            _bitmap.WritePixels(new Int32Rect(0, 0, _width, _height), whiteBytes, _width * 4, 0);
        }

        public void SetPixels()
        {
            int pixelWidth = _bitmap.PixelWidth;
            int pixelHeight = _bitmap.PixelHeight;
            _bitmap.CopyPixels(new Int32Rect(0, 0, pixelWidth, pixelHeight), _pixels, _stride, 0);
        }
        public Color GetPixel(int x, int y)
        {
            int index = y * _stride + 4 * x;
            byte blue = _pixels[index];
            byte green = _pixels[index + 1];
            byte red = _pixels[index + 2];
            byte alpha = _pixels[index + 3];
            return new Color(blue, green, red, alpha);
        }

        public void SetBaseColor(int x, int y)
        {
            _base = GetPixel(x, y);
            SetPixels();
        }

        private void DrawLine(int x1, int y, int x2, byte[] colorData)
        {
            int width = x2 - x1;

            byte[] pixels = new byte[width * 4];

            for (var i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = colorData[0];
                pixels[i + 1] = colorData[1];
                pixels[i + 2] = colorData[2];
                pixels[i + 3] = colorData[3];
            }

            int stride = width * 4;
            Int32Rect rect = new Int32Rect(x1, y, width, 1);
            _bitmap.WritePixels(rect, pixels, stride, 0);
        }


        //TODO вынести в Filler, создать класс (Фасад) Graphics

        public void Fill(int x, int y, Color color)
        {
            if (x < 0 || x >= _bitmap.Width || y < 0 || y >= _bitmap.Height || GetPixel(x, y).Equals(color) || GetPixel(x, y).Equals(Colors.Black) || !GetPixel(x, y).Equals(_base))
                return;

            int left_x = x;
            int right_x = x;

            while (left_x >= 0 && GetPixel(left_x, y).Equals(_base))
                left_x -= 1;
            while (right_x < _bitmap.Width && GetPixel(right_x, y).Equals(_base))
                right_x += 1;

            DrawLine(left_x + 1, y, right_x, color.ToBgra());

            SetPixels();
            for (int i = left_x + 1; i < right_x; i++)
            {
                Fill(i, y + 1, color);
                Fill(i, y - 1, color);
            }
        }

        public void FillImage(int x, int y, Bitmap image, Point center)
        {
            List<Tuple<Point, Point>> used = new();
            _fill(x, y, used, image, center);
            used.Clear();
        }

        private void _fill(int x, int y, List<Tuple<Point, Point>> used, Bitmap image, Point center)
        {
            if (used.Exists(t => t.Item1.Y == y && t.Item1.X <= x && x <= t.Item2.X))
                return;

            if (x < 0 || x >= _bitmap.Width || y < 0 || y >= _bitmap.Height || GetPixel(x, y).Equals(Colors.Black))
                return;

            int left_x = x;
            int right_x = x;

            while (left_x >= 0 && GetPixel(left_x, y).Equals(_base))
                left_x -= 1;
            while (right_x < _bitmap.Width && GetPixel(right_x, y).Equals(_base))
                right_x += 1;

            used.Add(Tuple.Create(new Point(left_x, y), new Point(right_x, y)));

            int @width = image.Width;
            int @height = image.Height;
            for (int i = left_x + 1; i < right_x; i++)
            {
                int next_x = i - center.X + @width / 2;
                int next_y = y - center.Y + @height / 2;
                while (next_x < 0)
                {
                    next_x += width;
                }
                while (next_y < 0)
                {
                    next_y += height;
                }
                var pixel = image.GetPixel(next_x % @width, next_y % @height);
                DrawPoint(i, y, new[] { pixel.B, pixel.G, pixel.R, pixel.A });
            }

            SetPixels();
            for (int i = left_x + 1; i < right_x; i++)
            {
                _fill(i, y + 1, used, image, center);
            }

            for (int i = left_x + 1; i < right_x; i++)
            {
                _fill(i, y - 1, used, image, center);
            }

        }

        private List<Point> GetBorderPoints(int x, int y)
        {
            int left_x = x;
            while (left_x > 0 && !GetPixel(left_x, y).Equals(Colors.Black))
                left_x -= 1;

            List<List<int>> directions = new List<List<int>>
            {
                new List<int>() { 1, 0 },
                new List<int>() { 1, 1 },
                new List<int>() { 0, 1 },
                new List<int>() { -1, 1 },
                new List<int>() { -1, 0 },
                new List<int>() { -1, -1 },
                new List<int>() { 0, -1 },
                new List<int>() { 1, -1 },
            };

            Stack<Point> stack = new Stack<Point>();
            HashSet<Point> labeled = new HashSet<Point>();
            List<Point> result = new List<Point>();

            stack.Push(new Point(left_x, y));

            while (stack.Count > 0)
            {
                var point = stack.Pop();
                x = point.X;
                y = point.Y;
                if (!labeled.Contains(point) && x >= 0 && x < _bitmap.Width && y >= 0 && y < _bitmap.Height && GetPixel(x, y).IsSimilar(Colors.Black, 50))
                {
                    result.Add(point);
                    labeled.Add(point);
                    foreach (var direction in directions)
                        stack.Push(new Point(x + direction[0], y + direction[1]));
                }
            }
            return result;
        }


        public void Highlight(int x, int y, Color color)
        {
            List<Point> borderPoints = GetBorderPoints(x, y);
            foreach (Point point in borderPoints)
            {
                DrawPoint(point.X, point.Y, color.ToBgra());
            }
        }
    }
}
