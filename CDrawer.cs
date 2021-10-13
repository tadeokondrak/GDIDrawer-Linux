using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GDIDrawer {
    enum Button {
        Unknown,
        Left,
        Middle,
        Right,
    }

    interface IRender {
        public void Render(Graphics gr, int iScale);
    }

    public delegate void GDIDrawerMouseEvent(Point pos, CDrawer dr);
    public delegate void GDIDrawerKeyEvent(bool bIsDown, Keys keyCode, CDrawer dr);

    public class CDrawer : IDisposable {
        LinkedList<IRender> shapes;
        DrawerWnd drawerWnd;
        bool dirty;
        bool continuousUpdate;
        int scale = 1;

        public readonly int m_ciWidth;
        public readonly int m_ciHeight;

        public bool ContinuousUpdate {
            get => continuousUpdate;
            set {
                continuousUpdate = value;
                if (continuousUpdate)
                    Render();
            }
        }
        public bool RedundaMouse { get; set; }

        public Point Position {
            get => drawerWnd.Position;
            set => drawerWnd.Position = value;
        }
        public Size DrawerWindowSize => drawerWnd.Size;

        public int Scale {
            get => scale;
            set {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (value > m_ciWidth)
                    throw new ArgumentOutOfRangeException(nameof(value));
                scale = value;
                lastMousePositionScaledFresh = false;
                lastMouseLeftClickScaledFresh = false;
                lastMouseLeftReleaseScaledFresh = false;
                lastMouseRightClickScaledFresh = false;
                lastMouseRightReleaseScaledFresh = false;
            }
        }
        public int ScaledWidth => m_ciWidth / Scale;
        public int ScaledHeight => m_ciHeight / Scale;

        public Color BBColour {
            set {
                drawerWnd.FillBB(value);
            }
        }

        public event GDIDrawerMouseEvent MouseMove;
        public event GDIDrawerMouseEvent MouseMoveScaled;
        public event GDIDrawerMouseEvent MouseLeftClick;
        public event GDIDrawerMouseEvent MouseLeftClickScaled;
        public event GDIDrawerMouseEvent MouseRightClick;
        public event GDIDrawerMouseEvent MouseRightClickScaled;
        public event GDIDrawerMouseEvent MouseLeftRelease;
        public event GDIDrawerMouseEvent MouseLeftReleaseScaled;
        public event GDIDrawerMouseEvent MouseRightRelease;
        public event GDIDrawerMouseEvent MouseRightReleaseScaled;
        public event GDIDrawerKeyEvent KeyboardEvent;

        Point lastMousePosition = new Point(-1, -1);
        bool lastMousePositionFresh = false;
        Point lastMousePositionScaled = new Point(-1, -1);
        bool lastMousePositionScaledFresh = false;

        Point lastMouseLeftClick = new Point(-1, -1);
        bool lastMouseLeftClickFresh = false;
        Point lastMouseLeftClickScaled = new Point(-1, -1);
        bool lastMouseLeftClickScaledFresh = false;

        Point lastMouseLeftRelease = new Point(-1, -1);
        bool lastMouseLeftReleaseFresh = false;
        Point lastMouseLeftReleaseScaled = new Point(-1, -1);
        bool lastMouseLeftReleaseScaledFresh = false;

        Point lastMouseRightClick = new Point(-1, -1);
        bool lastMouseRightClickFresh = false;
        Point lastMouseRightClickScaled = new Point(-1, -1);
        bool lastMouseRightClickScaledFresh = false;

        Point lastMouseRightRelease = new Point(-1, -1);
        bool lastMouseRightReleaseFresh = false;
        Point lastMouseRightReleaseScaled = new Point(-1, -1);
        bool lastMouseRightReleaseScaledFresh = false;

        public CDrawer(int iWindowXSize = 800, int iWindowYSize = 600,
            bool bContinuousUpdate = true, bool bRedundaMouse = false)
        {
            if (iWindowXSize <= 0) {
                throw new ArgumentOutOfRangeException(
                    nameof(iWindowXSize), "Width must be above zero.");
            }
            if (iWindowYSize <= 0) {
                throw new ArgumentOutOfRangeException(
                    nameof(iWindowYSize), "Height must be above zero.");
            }
            m_ciWidth = iWindowXSize;
            m_ciHeight = iWindowYSize;
            Init(bContinuousUpdate, bRedundaMouse);
        }

        public CDrawer(Bitmap Background, bool bContinuousUpdate = true, bool bRedundaMouse = false)
        {
            if (Background == null)
                throw new ArgumentNullException(nameof(Background));
            if (Background.Width <= 0)
                throw new ArgumentException(nameof(Background), "Width must be above zero.");
            if (Background.Height <= 0)
                throw new ArgumentException(nameof(Background), "Height must be above zero.");
            m_ciWidth = Background.Width;
            m_ciHeight = Background.Height;
            Init(bContinuousUpdate, bRedundaMouse);
        }

        // Shared constructor initialization code.
        void Init(bool bContinuousUpdate, bool bRedundaMouse) {
            shapes = new LinkedList<IRender>();
            dirty = true;
            continuousUpdate = bContinuousUpdate;
            RedundaMouse = bRedundaMouse;
            drawerWnd = new(this);
        }

        void DamageFrame() {
            dirty = true;
            if (ContinuousUpdate)
                Render();
        }

        internal void DrawTo(Graphics gr) {
            lock (shapes) {
                foreach (IRender render in shapes)
                    render.Render(gr, Scale);
            }
        }

        internal void OnMouseMove(Point point) {
            Point scaledPoint = new(point.X / Scale, point.Y / Scale);
            MouseMove?.Invoke(point, this);
            MouseMoveScaled?.Invoke(scaledPoint, this);
            lastMousePosition = point;
            lastMousePositionFresh = false;
            lastMousePosition = scaledPoint;
            lastMousePositionScaledFresh = false;
        }

        internal void OnMouseButton(Point point, Button button, bool state) {
            Point scaledPoint = new(point.X / Scale, point.Y / Scale);
            switch (button) {
            case Button.Left:
                if (state) {
                    MouseLeftClick?.Invoke(point, this);
                    MouseLeftClickScaled?.Invoke(scaledPoint, this);
                    lastMouseLeftClick = point;
                    lastMouseLeftClickFresh = true;
                    lastMouseLeftClickScaled = scaledPoint;
                    lastMouseLeftClickScaledFresh = true;
                } else {
                    MouseLeftRelease?.Invoke(point, this);
                    MouseLeftReleaseScaled?.Invoke(scaledPoint, this);
                    lastMouseLeftRelease = point;
                    lastMouseLeftReleaseFresh = true;
                    lastMouseLeftReleaseScaled = scaledPoint;
                    lastMouseLeftReleaseScaledFresh = true;
                }
                break;
            case Button.Right:
                if (state) {
                    MouseRightClick?.Invoke(point, this);
                    MouseRightClickScaled?.Invoke(scaledPoint, this);
                    lastMouseRightClick = point;
                    lastMouseRightClickFresh = true;
                    lastMouseRightClickScaled = scaledPoint;
                    lastMouseRightClickScaledFresh = true;
                } else {
                    MouseRightRelease?.Invoke(point, this);
                    MouseRightReleaseScaled?.Invoke(scaledPoint, this);
                    lastMouseRightRelease = point;
                    lastMouseRightReleaseFresh = true;
                    lastMouseRightReleaseScaled = scaledPoint;
                    lastMouseRightReleaseScaledFresh = true;
                }
                break;
            }
        }

        internal void OnKeyboardEvent(Keys keys, bool state) {
            KeyboardEvent?.Invoke(state, keys, this);
        }

        public void Clear() {
            lock (shapes)
                shapes.Clear();
        }

        public void Close() {
            Dispose();
        }

        public void Dispose() {
            drawerWnd.Dispose();
        }

        public void Render() {
            if (dirty) {
                drawerWnd.Redraw();
                dirty = false;
            }
        }

        bool GetLastPoint(Point point, ref bool fresh, out Point pCoords) {
            pCoords = point;
            if (fresh) {
                fresh = false;
                return true;
            }
            return false;
        }

        public bool GetLastMousePosition(out Point pCoords) {
            return GetLastPoint(
                lastMousePosition, ref lastMousePositionFresh, out pCoords);
        }

        public bool GetLastMousePositionScaled(out Point pCoords) {
            return GetLastPoint(
                lastMousePositionScaled, ref lastMousePositionScaledFresh, out pCoords);
        }

        public bool GetLastMouseLeftClick(out Point pCoords) {
            return GetLastPoint(
                lastMouseLeftClick, ref lastMouseLeftClickFresh, out pCoords);
        }

        public bool GetLastMouseLeftClickScaled(out Point pCoords) {
            return GetLastPoint(
                lastMouseLeftClickScaled, ref lastMouseLeftClickScaledFresh, out pCoords);
        }

        public bool GetLastMouseLeftRelease(out Point pCoords) {
            return GetLastPoint(
                lastMouseLeftRelease, ref lastMouseLeftReleaseFresh, out pCoords);
        }

        public bool GetLastMouseLeftReleaseScaled(out Point pCoords) {
            return GetLastPoint(
                lastMouseLeftReleaseScaled, ref lastMouseLeftReleaseScaledFresh, out pCoords);
        }

        public bool GetLastMouseRightClick(out Point pCoords) {
            return GetLastPoint(
                lastMouseRightClick, ref lastMouseRightClickFresh, out pCoords);
        }

        public bool GetLastMouseRightClickScaled(out Point pCoords) {
            return GetLastPoint(
                lastMouseRightClickScaled, ref lastMouseRightClickScaledFresh, out pCoords);
        }

        public bool GetLastMouseRightRelease(out Point pCoords) {
            return GetLastPoint(
                lastMouseRightRelease, ref lastMouseRightReleaseFresh, out pCoords);
        }

        public bool GetLastMouseRightReleaseScaled(out Point pCoords) {
            return GetLastPoint(
                lastMouseRightReleaseScaled, ref lastMouseRightReleaseScaledFresh, out pCoords);
        }

        public Color GetBBPixel(int iX, int iY, Color colour) {
            if (iX < 0)
                throw new ArgumentOutOfRangeException(nameof(iX), "Coordinate must be above zero.");
            if (iY < 0)
                throw new ArgumentOutOfRangeException(nameof(iY), "Coordinate must be above zero.");
            if (iX >= m_ciWidth)
                throw new ArgumentOutOfRangeException(nameof(iX), "Coordinate must be below width.");
            if (iY >= m_ciHeight)
                throw new ArgumentOutOfRangeException(nameof(iY), "Coordinate must be below height.");
            return drawerWnd.GetBBPixel(new Point(iX, iY));
        }

        public void SetBBPixel(int iX, int iY, Color colour) {
            if (iX < 0)
                throw new ArgumentOutOfRangeException(nameof(iX), "Coordinate must be above zero.");
            if (iY < 0)
                throw new ArgumentOutOfRangeException(nameof(iY), "Coordinate must be above zero.");
            if (iX >= m_ciWidth)
                throw new ArgumentOutOfRangeException(nameof(iX), "Coordinate must be below width.");
            if (iY >= m_ciHeight)
                throw new ArgumentOutOfRangeException(nameof(iY), "Coordinate must be below height.");
            drawerWnd.SetBBPixel(new Point(iX, iY), colour);
            DamageFrame();
        }

        public void SetBBScaledPixel(int iX, int iY, Color colour) {
            if (iX < 0)
                throw new ArgumentOutOfRangeException(nameof(iX), "Coordinate must be above zero.");
            if (iY < 0)
                throw new ArgumentOutOfRangeException(nameof(iY), "Coordinate must be above zero.");
            if (iX >= m_ciWidth / Scale)
                throw new ArgumentOutOfRangeException(nameof(iX), "Coordinate must be below width.");
            if (iY >= m_ciHeight / Scale)
                throw new ArgumentOutOfRangeException(nameof(iY), "Coordinate must be below height.");
            drawerWnd.FillBBRect(
                new Rectangle(iX * Scale, iY * Scale, Scale, Scale), colour);
            DamageFrame();
        }

        public void AddRectangle(int iXStart, int iYStart, int iWidth, int iHeight,
            Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
        {
            if (iWidth < 1)
                throw new ArgumentOutOfRangeException(nameof(iWidth), "Width must be above zero.");
            if (iHeight < 1)
                throw new ArgumentOutOfRangeException(nameof(iHeight), "Height must be above zero.");
            lock (shapes) {
                shapes.AddLast(new CRectangle(
                    iXStart, iYStart, iWidth, iHeight, FillColor, iBorderThickness, BorderColor));
            }
            DamageFrame();
        }

        public void AddRectangle(Rectangle rect, Color? FillColor = null, int iBorderThickness = 0,
            Color? BorderColor = null)
        {
            AddRectangle(rect.Left, rect.Top, rect.Width, rect.Height,
                FillColor, iBorderThickness, BorderColor);
        }

        public void AddCenteredRectangle(int iXCenter, int iYCenter, int iWidth, int iHeight,
            Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
        {
            AddRectangle(iXCenter - iWidth / 2, iYCenter - iHeight / 2,
                iWidth, iHeight, FillColor, iBorderThickness, BorderColor);
        }

        public void AddCenteredRectangle(Point pos, int iWidth, int iHeight,
            Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
        {
            AddCenteredRectangle(
                pos.X, pos.Y, iWidth, iHeight, FillColor, iBorderThickness, BorderColor);
        }

        public void AddEllipse(int iXStart, int iYStart, int iWidth, int iHeight,
            Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
        {
            lock (shapes) {
                shapes.AddLast(new CEllipse(
                    iXStart, iYStart, iWidth, iHeight, FillColor, iBorderThickness, BorderColor));
            }
            DamageFrame();
        }

        public void AddCenteredEllipse(int iXCenter, int iYCenter, int iWidth, int iHeight,
            Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
        {
            AddEllipse(iXCenter - iWidth / 2, iYCenter - iHeight / 2,
                iWidth, iHeight, FillColor, iBorderThickness, BorderColor);
        }

        public void AddCenteredEllipse(Point pos, int iWidth, int iHeight, Color? FillColor = null,
            int iBorderThickness = 0, Color? BorderColor = null)
        {
            AddCenteredEllipse(pos, iWidth, iHeight, FillColor, iBorderThickness, BorderColor);
        }

        public void AddLine(int iXStart, int iYStart, int iXEnd, int iYEnd, Color? LineColor = null,
            int iThickness = 1)
        {
            lock (shapes)
                shapes.AddLast(new CLine(iXStart, iYStart, iXEnd, iYEnd, LineColor, iThickness));
            DamageFrame();
        }

        public void AddLine(Point StartPos, double dLength, double dRotation = 0,
            Color? LineColor = null, int iThickness = 1)
        {
            lock (shapes)
                shapes.AddLast(new CLine(StartPos, dLength, dRotation, LineColor, iThickness));
            DamageFrame();
        }

        public void AddBezier(int iXStart, int iYStart, int iCtrlPt1X, int iCtrlPt1Y, int iCtrlPt2X,
            int iCtrlPt2Y, int iXEnd, int iYEnd, Color? LineColor = null, int iThickness = 1)
        {
            lock (shapes) {
                shapes.AddLast(new CBezier(iXStart, iYStart, iCtrlPt1X, iCtrlPt1Y, iCtrlPt2X,
                    iCtrlPt2Y, iXEnd, iYEnd, LineColor, iThickness));
            }
            DamageFrame();
        }

        public void AddPolygon(int iXStart, int iYStart, int iVertexRadius, int iNumPoints,
            double dRotation = 0, Color? FillColor = null, int iBorderThickness = 0,
            Color? BorderColor = null)
        {
            lock (shapes) {
                shapes.AddLast(new CPolygon(iXStart, iYStart, iVertexRadius, iNumPoints, dRotation,
                    FillColor, iBorderThickness, BorderColor));
            }
            DamageFrame();
        }

        public void AddText(string sText, float fTextSize, Color? TextColor = null) {
            lock (shapes)
                shapes.AddLast(new CText(sText, fTextSize, TextColor));
            DamageFrame();
        }

        public void AddText(string sText, float fTextSize, Rectangle BoundingRect,
            Color? TextColor = null)
        {
            lock (shapes)
                shapes.AddLast(new CText(sText, fTextSize, BoundingRect, TextColor));
            DamageFrame();
        }

        public void AddText(string sText, float fTextSize, int iXStart, int iYStart, int iWidth,
            int iHeight, Color ? TextColor = null)
        {
            lock (shapes) {
                shapes.AddLast(
                    new CText(sText, fTextSize, iXStart, iYStart, iWidth, iHeight, TextColor));
            }
            DamageFrame();
        }
    }

    internal abstract class CShape : IRender {
        protected int x;
        protected int y;
        protected Color color;

        protected CShape(int iXStart, int iYStart, Color? ShapeColor = null) {
            x = iXStart;
            y = iYStart;
            color = ShapeColor != null ? (Color)ShapeColor : Color.Gray;
        }

        public abstract void Render(Graphics gr, int iScale);
    }

    internal abstract class CBoundingRectWithBorderShape : CShape {
        protected int width;
        protected int height;
        protected int borderThickness;
        protected Color borderColor;

        internal CBoundingRectWithBorderShape(int iXStart, int iYStart, int iWidth, int iHeight,
            Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
            : base(iXStart, iYStart, FillColor)
        {
            if (iWidth < 0)
                throw new ArgumentOutOfRangeException(nameof(iWidth));
            if (iHeight < 0)
                throw new ArgumentOutOfRangeException(nameof(iHeight));
            if (iBorderThickness < 0)
                throw new ArgumentOutOfRangeException(nameof(iBorderThickness));
            width = iWidth;
            height = iHeight;
            borderThickness = iBorderThickness;
            borderColor = BorderColor != null ? (Color)BorderColor : color;
        }
    }

    internal class CRectangle : CBoundingRectWithBorderShape {
        internal CRectangle(int iXStart, int iYStart, int iWidth, int iHeight,
            Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
            : base(iXStart, iYStart, iWidth, iHeight, FillColor, iBorderThickness, BorderColor)
        {
        }

        public override void Render(Graphics gr, int iScale) {
            using SolidBrush brush = new(color);
            using Pen pen = new(borderColor, borderThickness);
            gr.FillRectangle(brush, x * iScale, y * iScale, width * iScale, height * iScale);
            if (borderThickness > 0)
                gr.DrawRectangle(pen, x * iScale, y * iScale, width * iScale, height * iScale);
        }
    }

    internal class CEllipse : CBoundingRectWithBorderShape {
        internal CEllipse(int iXStart, int iYStart, int iWidth, int iHeight,
            Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
            : base(iXStart, iYStart, iWidth, iHeight, FillColor, iBorderThickness, BorderColor)
        {
        }

        public override void Render(Graphics gr, int iScale) {
            using SolidBrush brush = new(color);
            using Pen pen = new(borderColor, borderThickness);
            gr.FillEllipse(brush, x * iScale, y * iScale, width * iScale, height * iScale);
            if (borderThickness > 0)
                gr.DrawEllipse(pen, x * iScale, y * iScale, width * iScale, height * iScale);
        }
    }

    internal class CPolygon : CShape {
        protected int borderThickness;
        protected Color borderColor;
        protected int numPoints;
        protected double rotation;
        protected int vertexRadius;
        internal CPolygon(int iXStart, int iYStart, int iVertexRadius, int iNumPoints,
            double dRotation = 0, Color? FillColor = null, int iBorderThickness = 0,
            Color? BorderColor = null)
            : base(iXStart, iYStart, FillColor)
        {
            if (iNumPoints < 3)
                throw new ArgumentOutOfRangeException(nameof(iNumPoints));
            if (iBorderThickness < 0)
                throw new ArgumentOutOfRangeException(nameof(iBorderThickness));
            vertexRadius = iVertexRadius;
            numPoints = iNumPoints;
            rotation = dRotation;
            borderThickness = iBorderThickness;
            borderColor = BorderColor != null ? (Color)BorderColor : color;
        }

        public override void Render(Graphics gr, int iScale) {
            using SolidBrush brush = new(color);
            using Pen pen = new(borderColor, borderThickness);
            Point[] points = new Point[numPoints];
            int iRad = vertexRadius * iScale;
            for (int i = 0; i < numPoints; ++i) {
                points[i].X = (x * iScale) + iRad +
                    (int)(Math.Sin(2 * i * Math.PI / numPoints + rotation) * iRad);
                points[i].Y = (y * iScale) + iRad -
                    (int)(Math.Cos(2 * i * Math.PI / numPoints + rotation) * iRad);
            }
            gr.FillPolygon(brush, points);
            if (borderThickness > 0)
                gr.DrawPolygon(pen, points);
        }
    }

    internal class CLine : CShape {
        protected int thickness;
        protected double lineLength;

        protected bool polar = false;
        protected double rotation = 0;
        protected int xEnd = 0;
        protected int yEnd = 0;

        internal CLine(int iXStart, int iYStart, int iXEnd, int iYEnd, Color? LineColor = null, int iThickness = 1)
            : base(iXStart, iYStart, LineColor)
        {
            if (iThickness < 1)
                throw new ArgumentOutOfRangeException(nameof(iThickness));
            xEnd = iXEnd;
            yEnd = iYEnd;
            thickness = iThickness;
        }

        internal CLine(Point StartPos, double dLength, double dRotation = 0, Color? LineColor = null, int iThickness = 1)
            : base(StartPos.X, StartPos.Y, LineColor)
        {
            if (iThickness < 1)
                throw new ArgumentOutOfRangeException(nameof(iThickness));
            if (dLength < 0)
                throw new ArgumentOutOfRangeException(nameof(dLength));
            polar = true;
            rotation = dRotation;
            lineLength = dLength;
            thickness = iThickness;
        }

        public override void Render(Graphics gr, int iScale) {
            using Pen p = new(color, (float)thickness);
            p.StartCap = LineCap.Round;
            p.EndCap = LineCap.Round;
            if (polar) {
                int iXEnd = (x * iScale) + (int)(Math.Sin(-rotation - Math.PI) * lineLength * iScale);
                int iYEnd = (y * iScale) + (int)(Math.Cos(-rotation - Math.PI) * lineLength * iScale);
                gr.DrawLine(p, x * iScale, y * iScale, iXEnd, iYEnd);
            } else {
                gr.DrawLine(p, x * iScale, y * iScale, xEnd * iScale, yEnd * iScale);
            }
        }
    }

    internal class CBezier : CShape {
        protected int thickness;
        protected int point1X;
        protected int point1Y;
        protected int point2X;
        protected int point2Y;
        protected int point3X;
        protected int point3Y;

        internal CBezier(int iXStart, int iYStart, int iCtrlPt1X, int iCtrlPt1Y, int iCtrlPt2X,
            int iCtrlPt2Y, int iXEnd, int iYEnd, Color? LineColor = null, int iThickness = 1)
            : base (iXStart, iYStart, LineColor)
        {
            if (iThickness < 1)
                throw new ArgumentOutOfRangeException(nameof(iThickness));
            point1X = iCtrlPt1X;
            point1Y = iCtrlPt1Y;
            point2X = iCtrlPt2X;
            point2Y = iCtrlPt2Y;
            point3X = iXEnd;
            point3Y = iYEnd;
            thickness = iThickness;
        }

        public override void Render(Graphics gr, int iScale) {
            using Pen p = new(color, (float)thickness);
            p.StartCap = LineCap.Round;
            p.EndCap = LineCap.Round;
            gr.DrawBezier(p, x, y, point1X, point1Y, point2X, point2Y, point3X, point3Y);
        }
    }

    internal class CText : IRender {
        protected string text;
        protected float pointSize;
        protected bool full;
        protected Rectangle boundingRect;
        protected Color color;

        internal CText(string sText, float fTextSize, Color? TextColor = null) {
            if (fTextSize < 1)
                throw new ArgumentOutOfRangeException(nameof(fTextSize));
            full = true;
            text = sText;
            pointSize = fTextSize;
            color = TextColor != null ? (Color)TextColor : Color.Blue;
        }

        internal CText(string sText, float fTextSize, int iXStart, int iYStart, int iWidth,
            int iHeight, Color? TextColor = null)
        {
            if (fTextSize < 1)
                throw new ArgumentOutOfRangeException(nameof(fTextSize));
            if (iWidth < 0)
                throw new ArgumentOutOfRangeException(nameof(iWidth));
            if (iHeight < 0)
                throw new ArgumentOutOfRangeException(nameof(iHeight));
            full = false;
            text = sText;
            pointSize = fTextSize;
            boundingRect = new(iXStart, iYStart, iWidth, iHeight);
            color = TextColor != null ? (Color)TextColor : Color.Black;
        }

        internal CText(string sText, float fTextSize, Rectangle BoundingRect,
            Color? TextColor = null)
            : this(sText, fTextSize, BoundingRect.X, BoundingRect.Y, BoundingRect.Width,
                BoundingRect.Height, TextColor)
        {
        }

        public void Render(Graphics gr, int iScale) {
            using SolidBrush brush = new(color);
            using Font font = new("Trebuchet MS", pointSize);

            StringFormat drawFormat = new StringFormat();
            drawFormat.FormatFlags = StringFormatFlags.NoWrap;
            drawFormat.Alignment = StringAlignment.Center;
            drawFormat.LineAlignment = StringAlignment.Center;
            drawFormat.Trimming = StringTrimming.EllipsisCharacter;

            int iWidth = full ? (int)gr.VisibleClipBounds.Width : boundingRect.Width * iScale;
            int iHeight = full ? (int)gr.VisibleClipBounds.Height : boundingRect.Height * iScale;
            RectangleF layoutRect =
                new(boundingRect.X * iScale, boundingRect.Y * iScale, iWidth, iHeight);
            gr.DrawString(text, font, brush, layoutRect, drawFormat);
        }
    }

    public static class CoordinateHelper {
        public static double GetDistance(Point A, Point B) {
            return Math.Sqrt(Math.Pow(A.X - B.X, 2) + Math.Pow(A.Y - B.Y, 2));
        }

        public static PointF OffsetByAngle(PointF Position, double Angle, double Radius) {
            return new((float)(Position.X + Math.Cos(Angle) * Radius),
                (float)(Position.Y + Math.Sin(Angle) * Radius));
        }

        public static PointF OffsetByAngle2(PointF Position, double Angle, double Radius) {
            return new((float)(Position.X + Math.Sin(Angle) * Radius),
                (float)(Position.Y + Math.Cos(Angle) * Radius));
        }
    }

    public static class RandColor {
        static Random random = new();
        static KnownColor[] colors = (KnownColor[])Enum.GetValues(typeof(KnownColor));

        public static Color GetColor() {
            int a = random.Next(4) * 63;
            int b = random.Next(4) * 63;
            switch (random.Next(3)) {
            case 0:
                return Color.FromArgb(255, 255, a, b);
            case 1:
                return Color.FromArgb(255, a, 255, b);
            case 2:
                return Color.FromArgb(255, a, b, 255);
            default:
                throw new Exception("Unreachable code");
            }
        }

        public static Color GetKnownColor() {
            return Color.FromKnownColor(colors[random.Next(colors.Length)]);
        }
    }
}
