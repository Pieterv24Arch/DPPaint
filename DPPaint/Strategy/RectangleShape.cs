﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using DPPaint.Shapes;

namespace DPPaint.Strategy
{
    public class RectangleShape : IShapeBase
    {
        private static RectangleShape instance = null;

        private RectangleShape()
        {

        }

        public static RectangleShape Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new RectangleShape();
                }

                return instance;
            }
        }

        public Shape GetDrawShape(PaintBase paintBase)
        {
            Shape drawShape = new Rectangle();

            double x = paintBase.Width < 0 ? paintBase.X + paintBase.Width : paintBase.X;
            double y = paintBase.Height < 0 ? paintBase.Y + paintBase.Height : paintBase.Y;

            drawShape.SetValue(Canvas.LeftProperty, x);
            drawShape.SetValue(Canvas.TopProperty, y);
            drawShape.Width = paintBase.Width < 0 ? paintBase.Width * -1 : paintBase.Width;
            drawShape.Height = paintBase.Height < 0 ? paintBase.Height * -1 : paintBase.Height;

            drawShape.Fill = new SolidColorBrush(Colors.Black);

            return drawShape;
        }

        public ShapeType GetShapeType()
        {
            return ShapeType.Circle;
        }

        public override string ToString()
        {
            return "Rectangle";
        }
    }
}
