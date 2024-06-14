using Lima.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD.Controls
{
    public class Modal : View
    {
        public View Content { get; protected set; }
        private View DropShadow;

        public static readonly Color OverlayCol = new Color(0, 0, 0, 180);
        public static readonly Color ShadowCol = new Color(0, 0, 0, 180);
        public static readonly Color BgCol = new Color(15, 30, 45);
        private readonly Vector2 ShadowOffset = new Vector2(2, 4);
        private readonly Vector2 MinMargin = new Vector2(32, 32);

        public Modal(GridStatusApp app, View content = null) : base(ViewDirection.Column, OverlayCol)
        {
            var scale = app.Theme.Scale;
            
            var surfaceSize = app.Surface.SurfaceSize;
            var textureSize = app.Surface.TextureSize;
            var topLeft = new Vector2((textureSize.X - surfaceSize.X) / 2, (textureSize.Y - surfaceSize.Y) / 2);

            Absolute = true;
            
            Position = topLeft;
            
            Flex = Vector2.Zero;
            Pixels = surfaceSize / scale;
            Padding = Vector4.Zero;
            Margin = Vector4.Zero;
            Alignment = ViewAlignment.Center;

            var contentSize = new Vector2(Math.Min(300, Pixels.X - MinMargin.X), Math.Min(500, Pixels.Y - MinMargin.Y));
            var contentPosition = topLeft + (((Pixels - contentSize) / scale) / 2);

            AddChild(DropShadow = new View(ViewDirection.Column, ShadowCol));
            DropShadow.Absolute = true;
            DropShadow.Position = contentPosition + ShadowOffset;
            DropShadow.Pixels = contentSize;
            DropShadow.Flex = Vector2.Zero;

            AddChild(Content = content == null ? new View(ViewDirection.Column, BgCol) : content);
            Content.Absolute = true;
            Content.Position = contentPosition;
            Content.Pixels = contentSize;
            Content.Flex = Vector2.Zero;
        }
    }
}
