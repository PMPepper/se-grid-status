using Lima.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    public static class TouchScreenUtils
    {
        //left, top, right, bottom
        //X, Y, Z, W
        public static float OuterWidth(this ElementBase element)
        {
            return element.Margin.X + element.Margin.Z + element.Pixels.X;
        }

        public static float OuterHeight(this ElementBase element)
        {
            return element.Margin.Y + element.Margin.W + element.Pixels.Y;
        }

        public static void RemoveAllChildren(this ContainerBase container)
        {
            while(container.Children.Count > 0)
            {
                container.RemoveChild(0);
            }
        }

        //Gets size of contents, ignoring any flex
        public static Vector2 GetContentSize(this View container)
        {
            float width = 0;
            float height = 0;
            int visibleChildren = 0;

            bool isRow = container.Direction == ViewDirection.Row || container.Direction == ViewDirection.RowReverse;
            bool isColumn = container.Direction == ViewDirection.Column || container.Direction == ViewDirection.ColumnReverse;
            
            foreach (var child in container.Children)
            {
                var element = new View(child);

                if(element.Enabled == false)
                {
                    continue;
                }

                visibleChildren++;

                var itemWidth = element.OuterWidth();
                var itemHeight = element.OuterHeight();

                if(isRow)
                {
                    width += itemWidth;
                    height = Math.Max(height, itemHeight);
                } else if(isColumn)
                {
                    width = Math.Max(width, itemWidth);
                    height += itemHeight;
                }
            }

            var totalGap = visibleChildren > 0 ? container.Gap * (visibleChildren - 1) : 0;
            
            return new Vector2(width + (isRow ? totalGap : 0), height + (isColumn ? totalGap : 0));
        }

        public static void SizeToContent(this View view)
        {
            var contentSize = view.GetContentSize();
            
            view.Pixels = new Vector2(
                contentSize.X + view.Padding.X + view.Padding.Z + view.Border.X + view.Border.Z,
                contentSize.Y + view.Padding.Y + view.Padding.W + view.Border.Y + view.Border.W
            );
        }
    }
}
