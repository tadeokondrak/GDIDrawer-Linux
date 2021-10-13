using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;

namespace GDIDrawer {
    static class Application {
        static object appLock = new();
        static int appRefCount;
        static Thread appThread;

        public static void Start() {
            if (Interlocked.Increment(ref appRefCount) == 1) {
                Gtk.Application.Init();
                lock (appLock) {
                    appThread = new(Gtk.Application.Run);
                    appThread.IsBackground = true;
                    appThread.Start();
                }
            }
        }

        public static void Stop() {
            if (Interlocked.Decrement(ref appRefCount) == 0) {
                Gtk.Application.Quit();
                lock (appLock) {
                    appThread.Join();
                    appThread = null;
                }
            }
        }

        public static void RunBlocking(Action action) {
            using (ManualResetEventSlim mres = new(false)) {
                Gtk.Application.Invoke((o, a) => {
                    action();
                    mres.Set();
                });
                mres.Wait();
            }
        }
    }

    internal class DrawerWnd : IDisposable {
        CDrawer parent;
        Gtk.Window window;
        Gtk.DrawingArea drawingArea;
        Bitmap underlay;

        public DrawerWnd(CDrawer parent) {
            Application.Start();
            this.parent = parent;
            underlay = new Bitmap(parent.m_ciWidth, parent.m_ciHeight, PixelFormat.Format32bppArgb);
            FillBB(Color.Black);
            Application.RunBlocking(() => {
                window = new(Gtk.WindowType.Toplevel);
                window.SetSizeRequest(parent.m_ciWidth, parent.m_ciHeight);
                window.Resizable = false;
                drawingArea = new();
                drawingArea.Drawn += DrawingAreaOnDrawn;
                drawingArea.ButtonPressEvent += DrawingAreaOnButtonPressEvent;
                drawingArea.ButtonReleaseEvent += DrawingAreaOnButtonReleaseEvent;
                drawingArea.MotionNotifyEvent += DrawingAreaOnMotionNotifyEvent;
                drawingArea.KeyPressEvent += DrawingAreaOnKeyPressEvent;
                drawingArea.KeyReleaseEvent += DrawingAreaOnKeyReleaseEvent;
                drawingArea.Events |= Gdk.EventMask.PointerMotionMask;
                drawingArea.Events |= Gdk.EventMask.ButtonPressMask;
                drawingArea.Events |= Gdk.EventMask.ButtonReleaseMask;
                window.Add(drawingArea);
                window.ShowAll();
            });
        }

        public void Dispose() {
            underlay.Dispose();
            Application.RunBlocking(() => {
                drawingArea.Dispose();
                window.Dispose();
            });
            Application.Stop();
        }

        public void Redraw() {
            Application.RunBlocking(() => {
                drawingArea.QueueDraw();
            });
        }

        public Size Size {
            get {
                int width = 0;
                int height = 0;
                Application.RunBlocking(() => {
                    width = window.Allocation.Width;
                    height = window.Allocation.Height;
                });
                return new(width, height);
            }
        }

        public Point Position {
            get {
                int x = 0;
                int y = 0;
                Application.RunBlocking(() => {
                    window.GetPosition(out x, out y);
                });
                return new(x, y);
            }
            set {
                Application.RunBlocking(() => {
                    window.Move(value.X, value.Y);
                });
            }
        }

        public void SetBBPixel(Point point, Color color) {
            Application.RunBlocking(() => {
                underlay.SetPixel(point.X, point.Y, color);
            });
        }

        public Color GetBBPixel(Point point) {
            Color color = Color.Black;
            Application.RunBlocking(() => {
                underlay.GetPixel(point.X, point.Y);
            });
            return color;
        }

        public void FillBB(Color color) {
            FillBBRect(new Rectangle(0, 0, underlay.Width, underlay.Height), color);
        }

        public void FillBBRect(Rectangle rect, Color color) {
            Application.RunBlocking(() => {
                using (Graphics gr = Graphics.FromImage(underlay)) {
                    using SolidBrush brush = new(color);
                    gr.FillRectangle(brush, rect);
                }
            });
        }

        public void SetBBImage(Bitmap bitmap) {
            Application.RunBlocking(() => {
                if (bitmap.Width != underlay.Width || bitmap.Height != underlay.Height)
                    throw new ArgumentException(nameof(bitmap));
                underlay = new Bitmap(bitmap);
            });
        }

        Button ConvertButton(uint gdkButton) {
            switch (gdkButton) {
            case 1:
                return Button.Left;
            case 2:
                return Button.Middle;
            case 3:
                return Button.Right;
            default:
                return Button.Unknown;
            }
        }

        Keys ConvertKeys(Gdk.Key gdkKey) {
            switch (gdkKey) {
            case Gdk.Key.Key_0: return Keys.D0;
            case Gdk.Key.Key_1: return Keys.D1;
            case Gdk.Key.Key_2: return Keys.D2;
            case Gdk.Key.Key_3: return Keys.D3;
            case Gdk.Key.Key_4: return Keys.D4;
            case Gdk.Key.Key_5: return Keys.D5;
            case Gdk.Key.Key_6: return Keys.D6;
            case Gdk.Key.Key_7: return Keys.D7;
            case Gdk.Key.Key_8: return Keys.D8;
            case Gdk.Key.Key_9: return Keys.D9;
            case Gdk.Key.a: case Gdk.Key.A: return Keys.A;
            case Gdk.Key.b: case Gdk.Key.B: return Keys.B;
            case Gdk.Key.c: case Gdk.Key.C: return Keys.C;
            case Gdk.Key.d: case Gdk.Key.D: return Keys.D;
            case Gdk.Key.e: case Gdk.Key.E: return Keys.E;
            case Gdk.Key.f: case Gdk.Key.F: return Keys.F;
            case Gdk.Key.g: case Gdk.Key.G: return Keys.G;
            case Gdk.Key.h: case Gdk.Key.H: return Keys.H;
            case Gdk.Key.i: case Gdk.Key.I: return Keys.I;
            case Gdk.Key.j: case Gdk.Key.J: return Keys.J;
            case Gdk.Key.k: case Gdk.Key.K: return Keys.K;
            case Gdk.Key.l: case Gdk.Key.L: return Keys.L;
            case Gdk.Key.m: case Gdk.Key.M: return Keys.M;
            case Gdk.Key.n: case Gdk.Key.N: return Keys.N;
            case Gdk.Key.o: case Gdk.Key.O: return Keys.O;
            case Gdk.Key.p: case Gdk.Key.P: return Keys.P;
            case Gdk.Key.q: case Gdk.Key.Q: return Keys.Q;
            case Gdk.Key.r: case Gdk.Key.R: return Keys.R;
            case Gdk.Key.s: case Gdk.Key.S: return Keys.S;
            case Gdk.Key.t: case Gdk.Key.T: return Keys.T;
            case Gdk.Key.u: case Gdk.Key.U: return Keys.U;
            case Gdk.Key.v: case Gdk.Key.V: return Keys.V;
            case Gdk.Key.w: case Gdk.Key.W: return Keys.W;
            case Gdk.Key.x: case Gdk.Key.X: return Keys.X;
            case Gdk.Key.y: case Gdk.Key.Y: return Keys.Y;
            case Gdk.Key.z: case Gdk.Key.Z: return Keys.Z;
            case Gdk.Key.F1: return Keys.F1;
            case Gdk.Key.F2: return Keys.F2;
            case Gdk.Key.F3: return Keys.F3;
            case Gdk.Key.F4: return Keys.F4;
            case Gdk.Key.F5: return Keys.F5;
            case Gdk.Key.F6: return Keys.F6;
            case Gdk.Key.F7: return Keys.F7;
            case Gdk.Key.F8: return Keys.F8;
            case Gdk.Key.F9: return Keys.F9;
            case Gdk.Key.F10: return Keys.F10;
            case Gdk.Key.F11: return Keys.F11;
            case Gdk.Key.F12: return Keys.F12;
            case Gdk.Key.F13: return Keys.F13;
            case Gdk.Key.F14: return Keys.F14;
            case Gdk.Key.F15: return Keys.F15;
            case Gdk.Key.F16: return Keys.F16;
            case Gdk.Key.F17: return Keys.F17;
            case Gdk.Key.F18: return Keys.F18;
            case Gdk.Key.F19: return Keys.F19;
            case Gdk.Key.F20: return Keys.F20;
            case Gdk.Key.F21: return Keys.F21;
            case Gdk.Key.F22: return Keys.F22;
            case Gdk.Key.F23: return Keys.F23;
            case Gdk.Key.F24: return Keys.F24;
            default: return Keys.None;
            }
        }

        Keys ConvertKeys(Gdk.ModifierType gdkModifier) {
            Keys keys = Keys.None;
            if (gdkModifier.HasFlag(Gdk.ModifierType.ShiftMask))
                keys |= Keys.Shift;
            if (gdkModifier.HasFlag(Gdk.ModifierType.ControlMask))
                keys |= Keys.Control;
            if (gdkModifier.HasFlag(Gdk.ModifierType.Mod1Mask))
                keys |= Keys.Alt;
            return keys;
        }

        void DrawingAreaOnButtonPressEvent(object o, Gtk.ButtonPressEventArgs args) {
            parent.OnMouseButton(new Point((int)args.Event.X, (int)args.Event.Y),
                ConvertButton(args.Event.Button), true);
        }

        void DrawingAreaOnButtonReleaseEvent(object o, Gtk.ButtonReleaseEventArgs args) {
            parent.OnMouseButton(new Point((int)args.Event.X, (int)args.Event.Y),
                ConvertButton(args.Event.Button), false);
        }

        void DrawingAreaOnMotionNotifyEvent(object o, Gtk.MotionNotifyEventArgs args) {
            parent.OnMouseMove(new Point((int)args.Event.X, (int)args.Event.Y));
        }

        void DrawingAreaOnKeyPressEvent(object o, Gtk.KeyPressEventArgs args) {
            Keys keys = ConvertKeys(args.Event.Key) | ConvertKeys(args.Event.State);
            parent.OnKeyboardEvent(keys, true);
        }

        void DrawingAreaOnKeyReleaseEvent(object o, Gtk.KeyReleaseEventArgs args) {
            Keys keys = ConvertKeys(args.Event.Key) | ConvertKeys(args.Event.State);
            parent.OnKeyboardEvent(keys, false);
        }

        void DrawingAreaOnDrawn(object o, Gtk.DrawnArgs args) {
            using Bitmap bitmap = new(drawingArea.Allocation.Width, drawingArea.Allocation.Height,
                PixelFormat.Format32bppArgb);
            using (Graphics gr = Graphics.FromImage(bitmap)) {
                gr.DrawImage(underlay, 0, 0);
                parent.DrawTo(gr);
            }
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);
            using Cairo.ImageSurface imageSurface = new(bitmapData.Scan0, Cairo.Format.ARGB32,
                bitmapData.Width, bitmapData.Height, bitmapData.Stride);
            imageSurface.Show(args.Cr, 0, 0);
            bitmap.UnlockBits(bitmapData);
        }
    }
}
