﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Drawing;

using XColour = Microsoft.Xna.Framework.Color;
using DColour = System.Drawing.Color;
using DPoint = System.Drawing.Point;
using DRectangle = System.Drawing.Rectangle;
using System.IO;

namespace HumanGraphicsPipelineXna
{
    class TriangleClippingScene : TriangleScene
    {
        enum Side
        { 
            Up,
            Left,
            Down,
            Right,
            TL,
            TR,
            BL,
            BR
        }

        class ClippingPoint
        {
            public Vector2 triPoint;
            public List<Vector2> intersectionPointsTo = new List<Vector2>();
            public List<Vector2> intersectionPointsFrom = new List<Vector2>();
            public bool isOutside = false;

            public Side side;

            void setIsOutside(bool t) { isOutside = t; }

            public ClippingPoint(Vector2 triPointIn, Vector2 normalisedPointIn, List<Vector2> intersectionPointsToIn, List<Vector2> intersectionPointsFromIn) 
            {
                intersectionPointsTo = intersectionPointsToIn;
                intersectionPointsFrom = intersectionPointsFromIn;
                triPoint = triPointIn;

                if (normalisedPointIn.X < -1 && normalisedPointIn.Y > 1)
                    side = Side.TL;
                else if (normalisedPointIn.X > 1 && normalisedPointIn.Y > 1)
                    side = Side.TR;
                else if (normalisedPointIn.X < -1 && normalisedPointIn.Y < -1)
                    side = Side.BL;
                else if (normalisedPointIn.X > 1 && normalisedPointIn.Y < -1)
                    side = Side.BR;

                else if (normalisedPointIn.X < -1)
                    side = Side.Left;
                else if (normalisedPointIn.X > 1)
                    side = Side.Right;
                else if (normalisedPointIn.Y > 1)
                    side = Side.Up;
                else if (normalisedPointIn.Y < -1)
                    side = Side.Down;

                int p = 3;
                p = 5;
            }

            
        }


        static Vector2 pointTopLeft = new Vector2(Globals.viewportWidth / 6, Globals.viewportHeight / 6);
        static Vector2 pointTopRight = new Vector2(Globals.viewportWidth - (Globals.viewportWidth / 6), Globals.viewportHeight / 6);
        static Vector2 pointBottomLeft = new Vector2(Globals.viewportWidth / 6, Globals.viewportHeight - (Globals.viewportHeight / 6));
        static Vector2 pointBottomRight = new Vector2(Globals.viewportWidth - (Globals.viewportWidth / 6), Globals.viewportHeight - (Globals.viewportHeight / 6));

        Vector2 intersectionPointAB1;
        Vector2 intersectionPointBC1;
        Vector2 intersectionPointAC1;
        Vector2 intersectionPointAB2;
        Vector2 intersectionPointBC2;
        Vector2 intersectionPointAC2;

        Line lineTop = new Line(pointTopLeft, pointTopRight, XColour.Black, 1f);
        Line lineLeft = new Line(pointTopLeft, pointBottomLeft, XColour.Black, 1f);
        Line lineBottom = new Line(pointBottomLeft, pointBottomRight, XColour.Black, 1f);
        Line lineRight = new Line(pointTopRight, pointBottomRight, XColour.Black, 1f);

        List<Vector2> l = new List<Vector2>() {
            pointTopLeft, pointTopRight, // Top
            pointTopLeft, pointBottomLeft, // Left
            pointBottomLeft, pointBottomRight, // Bottom
            pointTopRight, pointBottomRight}; // Right

        ClippingPoint clippingA;
        ClippingPoint clippingB;
        ClippingPoint clippingC;


        protected override void DerivedInit()
        {
            base.DerivedInit();

            trianglePoints = new Vector2[3];
            normalisedTrianglePoints = new Vector2[3];
            triangleSquares = new Square[3];
            triangleLines = new Line[3]; //AB, BC, CA

            intersectionAB1 = false;
            intersectionBC1 = false;
            intersectionAC1 = false;
            intersectionAB2 = false;
            intersectionBC2 = false;
            intersectionAC2 = false;

            intersectionPointAB1 = new Vector2(float.NegativeInfinity);
            intersectionPointBC1 = new Vector2(float.NegativeInfinity);
            intersectionPointAC1 = new Vector2(float.NegativeInfinity);
            intersectionPointAB2 = new Vector2(float.NegativeInfinity);
            intersectionPointBC2 = new Vector2(float.NegativeInfinity);
            intersectionPointAC2 = new Vector2(float.NegativeInfinity);

            outsideCount = 0;
            isOutsideA = false;
            isOutsideB = false;
            isOutsideC = false;

            clippingA = null;
            clippingB = null;
            clippingC = null;

            insideTriPoints = null;
            outsideTriPoints = null;

        }

        protected override void StateChanges(GameTime gameTime)
        {
            base.StateChanges(gameTime);
            if (Inputs.MouseState.LeftButton == ButtonState.Released && Inputs.MouseStatePrevious.LeftButton == ButtonState.Pressed)
            {
                if (state <= 3)
                {
                    CorrectNormalisedTriangle(state);
                }
            }
        }

        private void CorrectNormalisedTriangle(int state)
        {
            float aX = normalisedTrianglePoints[(int)state - 1].X;
            float bX = normalisedTrianglePoints[(int)state - 1].X / 2;
            float pX = aX + bX;

            float aY = normalisedTrianglePoints[(int)state - 1].Y;
            float bY = normalisedTrianglePoints[(int)state - 1].Y / 2;
            float pY = aY + bY;
            normalisedTrianglePoints[(int)state - 1] = new Vector2(pX, pY);
        }
        

        private bool CheckLineIntersection(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2, out Vector2 intersectionPoint)
        {
            intersectionPoint = new Vector2(float.NegativeInfinity);
		    double xD1 = p2.X-p1.X;
		    double xD2 = q2.X-q1.X;
		    double yD1 = p2.Y-p1.Y;
		    double yD2 = q2.Y-q1.Y;
		    double xD3 = p1.X-q1.X;
		    double yD3 = p1.Y-q1.Y;

		    double len1 = Math.Sqrt(xD1*xD1+yD1*yD1);
		    double len2 = Math.Sqrt(xD2*xD2+yD2*yD2);
            
		    double dot = xD1*xD2+yD1*yD2;
		    double deg = dot/(len1*len2);

		    double div = yD2*xD1-xD2*yD1;
		    double ua = (xD2*yD3-yD2*xD3)/div;
		    double ub = (xD1*yD3-yD1*xD3)/div;

            Vector2 pt = new Vector2((float)(p1.X + ua * xD1), (float)(p1.Y + ua * yD1));

		    xD1=pt.X-p1.X;  
		    xD2=pt.X-p2.X;  
		    yD1=pt.Y-p1.Y;  
		    yD2=pt.Y-p2.Y;  

		    double segmentLength1 = Math.Sqrt(xD1*xD1+yD1*yD1)+Math.Sqrt(xD2*xD2+yD2*yD2);  
            
		    xD1=pt.X-q1.X;  
		    xD2=pt.X-q2.X;  
		    yD1=pt.Y-q1.Y;  
		    yD2=pt.Y-q2.Y; 

		    double segmentLength2 = Math.Sqrt(xD1*xD1+yD1*yD1)+Math.Sqrt(xD2*xD2+yD2*yD2); 

		    if(Math.Abs(len1-segmentLength1)>0.01 || Math.Abs(len2-segmentLength2)>0.01)
			    return false;

		    if ((Math.Round(pt.X) == Math.Round(p1.X) && Math.Round(pt.Y) == Math.Round(p1.Y)) || (Math.Round(pt.X) == Math.Round(p2.X) && Math.Round(pt.Y) == Math.Round(p2.Y)))
			    return false;

		    if (pt.X/2 != pt.X/2 || pt.Y/2 != pt.Y/2)
			    return false;

            intersectionPoint = pt;

		    return true;
        }

        bool intersectionAB1;
        bool intersectionBC1;
        bool intersectionAC1;
        bool intersectionAB2;
        bool intersectionBC2;
        bool intersectionAC2;

        Square squareIntersectionAB1;
        Square squareIntersectionBC1;
        Square squareIntersectionAC1;
        Square squareIntersectionAB2;
        Square squareIntersectionBC2;
        Square squareIntersectionAC2;


        bool isOutsideA = false;
        bool isOutsideB = false;
        bool isOutsideC = false;
        int outsideCount = 0;

        List<ClippingPoint> insideTriPoints;
        List<ClippingPoint> outsideTriPoints;
        List<Vector2> intersectionsC;
        List<Vector2> intersectionsA;
        List<Vector2> intersectionsB;


        protected override void LastPointPlaced(GameTime gameTime)
        {

            intersectionsC = new List<Vector2>();
            intersectionsA = new List<Vector2>();
            intersectionsB = new List<Vector2>();
            CorrectNormalisedTriangle(3);

            isOutsideA = (Math.Abs(normalisedTrianglePoints[0].X) >= 1 || Math.Abs(normalisedTrianglePoints[0].Y) >= 1) ? true : false;
            isOutsideB = (Math.Abs(normalisedTrianglePoints[1].X) >= 1 || Math.Abs(normalisedTrianglePoints[1].Y) >= 1) ? true : false;
            isOutsideC = (Math.Abs(normalisedTrianglePoints[2].X) >= 1 || Math.Abs(normalisedTrianglePoints[2].Y) >= 1) ? true : false;

            Console.WriteLine("A: " + isOutsideA);
            Console.WriteLine("B: " + isOutsideB);
            Console.WriteLine("C: " + isOutsideC + "\n");

            outsideCount = 0;

            
            /*
            if (isOutsideA)
                outsideTriPoints.Add(trianglePoints[0]);
            if (isOutsideB)
                outsideTriPoints.Add(trianglePoints[1]);
            if (isOutsideC)
                outsideTriPoints.Add(trianglePoints[2]);
            */
            /*
            for (int i = 0; i < 8; i+=2)
            {
                if (!intersectionAB1)
                    intersectionAB1 = CheckLineIntersection(trianglePoints[0], trianglePoints[1], l[i], l[i+1], out intersectionPointAB1);
                else if (!intersectionAB2)
                    intersectionAB2 = CheckLineIntersection(trianglePoints[0], trianglePoints[1], l[i], l[i + 1], out intersectionPointAB2);

                if (!intersectionBC1)
                    intersectionBC1 = CheckLineIntersection(trianglePoints[1], trianglePoints[2], l[i], l[i + 1], out intersectionPointBC1);
                else if (!intersectionBC2)
                    intersectionBC2 = CheckLineIntersection(trianglePoints[1], trianglePoints[2], l[i], l[i + 1], out intersectionPointBC2);

                if (!intersectionAC1)
                    intersectionAC1 = CheckLineIntersection(trianglePoints[2], trianglePoints[0], l[i], l[i + 1], out intersectionPointAC1);
                else if (!intersectionAC2)
                    intersectionAC2 = CheckLineIntersection(trianglePoints[2], trianglePoints[0], l[i], l[i + 1], out intersectionPointAC2);

            }*/

            for (int i = 0; i < 8; i+=2)
            {
                bool b;
                Vector2 v;

                b = CheckLineIntersection(trianglePoints[0], trianglePoints[1], l[i], l[i + 1], out v);
                intersectionsA.Add(v);

                b = CheckLineIntersection(trianglePoints[1], trianglePoints[2], l[i], l[i + 1], out v);
                intersectionsB.Add(v);

                b = CheckLineIntersection(trianglePoints[2], trianglePoints[0], l[i], l[i + 1], out v);
                intersectionsC.Add(v);
            }

            intersectionsA = intersectionsA.Distinct().ToList();
            intersectionsA.Remove(new Vector2(float.NegativeInfinity));

            intersectionsB = intersectionsB.Distinct().ToList();
            intersectionsB.Remove(new Vector2(float.NegativeInfinity));

            intersectionsC = intersectionsC.Distinct().ToList();
            intersectionsC.Remove(new Vector2(float.NegativeInfinity));

            /*
            if (intersectionAB1)
                intersectionsA.Add(intersectionPointAB1);
            if (intersectionAC1)
                intersectionsA.Add(intersectionPointAC1);

            if (intersectionBC1)
                intersectionsB.Add(intersectionPointBC1);
            if (intersectionAB2)
                intersectionsB.Add(intersectionPointAB2);

            if (intersectionAC1)
                intersectionsC.Add(intersectionPointAC1);
            if (intersectionAB2)
                intersectionsC.Add(intersectionPointAB2);


            */
            if (intersectionsA.Count > 1)
            { 
                float d1;
                float d2;
                Vector2 v1 = intersectionsA[0];
                Vector2 v2 = intersectionsA[1];
                Vector2 t1 = trianglePoints[0];
                Vector2.Distance(ref v1, ref v2, out d1);
                Vector2.Distance(ref v1, ref t1, out d2);
                if (d1 < d2)
                {
                    Vector2 temp = intersectionsA[0];
                    intersectionsA[0] = intersectionsA[1];
                    intersectionsA[1] = temp;
                }
            }


            if (intersectionsB.Count > 1)
            {
                float d1;
                float d2;
                Vector2 v1 = intersectionsB[0];
                Vector2 v2 = intersectionsB[1];
                Vector2 t1 = trianglePoints[0];
                Vector2.Distance(ref v1, ref v2, out d1);
                Vector2.Distance(ref v1, ref t1, out d2);
                if (d1 < d2)
                {
                    Vector2 temp = intersectionsB[0];
                    intersectionsB[0] = intersectionsB[1];
                    intersectionsB[1] = temp;
                }
            }


            if (intersectionsC.Count > 1)
            {
                float d1;
                float d2;
                Vector2 v1 = intersectionsC[0];
                Vector2 v2 = intersectionsC[1];
                Vector2 t1 = trianglePoints[0];
                Vector2.Distance(ref v1, ref v2, out d1);
                Vector2.Distance(ref v1, ref t1, out d2);
                if (d1 < d2)
                {
                    Vector2 temp = intersectionsC[0];
                    intersectionsC[0] = intersectionsC[1];
                    intersectionsC[1] = temp;
                }
            }

            insideTriPoints = new List<ClippingPoint>();
            outsideTriPoints = new List<ClippingPoint>();





            List<Vector2> tempIntersectionsTo = new List<Vector2>();
            List<Vector2> tempIntersectionsFrom = new List<Vector2>();
            //if (intersectionAB1)
                tempIntersectionsFrom.AddRange(intersectionsA);
            //if (intersectionAC1)
                tempIntersectionsTo.AddRange(intersectionsC);

            clippingA = new ClippingPoint(trianglePoints[0], normalisedTrianglePoints[0], tempIntersectionsTo, tempIntersectionsFrom);
            clippingA.isOutside = isOutsideA;

            if (isOutsideA)
                outsideTriPoints.Add(clippingA);
            else
                insideTriPoints.Add(clippingA);




            tempIntersectionsTo = new List<Vector2>();
            tempIntersectionsFrom = new List<Vector2>();
            //if (intersectionBC1)
                tempIntersectionsFrom.AddRange(intersectionsB);
            //if (intersectionAB1)
                tempIntersectionsTo.AddRange(intersectionsA);
            

            
            clippingB = new ClippingPoint(trianglePoints[1], normalisedTrianglePoints[1], tempIntersectionsTo, tempIntersectionsFrom);
            clippingB.isOutside = isOutsideB;

            if (isOutsideB)
                outsideTriPoints.Add(clippingB);
            else
                insideTriPoints.Add(clippingB);





            tempIntersectionsTo = new List<Vector2>();
            tempIntersectionsFrom = new List<Vector2>();
            //if (intersectionBC1)
                tempIntersectionsTo.AddRange(intersectionsB);
            //if (intersectionAC1)
                tempIntersectionsFrom.AddRange(intersectionsC);

            clippingC = new ClippingPoint(trianglePoints[2], normalisedTrianglePoints[2], tempIntersectionsTo, tempIntersectionsFrom);
            clippingC.isOutside = isOutsideC;

            if (isOutsideC)
                outsideTriPoints.Add(clippingC);
            else
                insideTriPoints.Add(clippingC);






            squareIntersectionAB1 = new Square(new Vector2(intersectionPointAB1.X - 2, intersectionPointAB1.Y - 2), new Vector2(4, 4), XColour.Red);
            squareIntersectionBC1 = new Square(new Vector2(intersectionPointBC1.X - 2, intersectionPointBC1.Y - 2), new Vector2(4, 4), XColour.Green);
            squareIntersectionAC1 = new Square(new Vector2(intersectionPointAC1.X - 2, intersectionPointAC1.Y - 2), new Vector2(4, 4), XColour.LightBlue);
            squareIntersectionAB2 = new Square(new Vector2(intersectionPointAB2.X - 2, intersectionPointAB2.Y - 2), new Vector2(4, 4), XColour.Yellow);
            squareIntersectionBC2 = new Square(new Vector2(intersectionPointBC2.X - 2, intersectionPointBC2.Y - 2), new Vector2(4, 4), XColour.Pink);
            squareIntersectionAC2 = new Square(new Vector2(intersectionPointAC2.X - 2, intersectionPointAC2.Y - 2), new Vector2(4, 4), XColour.Brown);
        }

        protected override void DrawOnAnimate(SpriteBatch spriteBatch)
        {
           // throw new NotImplementedException();
        }
        
        protected override void ActionOnTrianglePlaced(SpriteBatch spriteBatch)
        {
            
        }


        protected bool CheckListEquality<T>(List<T> a, List<T> b)
        {
            if (a.Count == b.Count)
            {
                for (int i = 0; i < a.Count; i++)
                { 
                    if (!a[i].Equals(b[i]))
                        return false;
                }
            }
            else
                return false;

            return true;
        }
        

        Polygon t;
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            lineTop.Draw(spriteBatch);
            lineLeft.Draw(spriteBatch);
            lineBottom.Draw(spriteBatch);
            lineRight.Draw(spriteBatch);


            if (squareIntersectionAB1 != null)
                squareIntersectionAB1.Draw(spriteBatch);
            if (squareIntersectionBC1 != null)
                squareIntersectionBC1.Draw(spriteBatch);
            if (squareIntersectionAC1 != null)
                squareIntersectionAC1.Draw(spriteBatch);
            if (squareIntersectionAB2 != null)
                squareIntersectionAB2.Draw(spriteBatch);
            if (squareIntersectionBC2 != null)
                squareIntersectionBC2.Draw(spriteBatch);
            if (squareIntersectionAC2 != null)
                squareIntersectionAC2.Draw(spriteBatch);

            if (outsideTriPoints != null)
            {
                if (outsideTriPoints.Count == 1)
                {
                    int index = 1;


                        if (insideTriPoints[1].intersectionPointsTo.Count != 0 
                            && outsideTriPoints[0].intersectionPointsFrom[0] == insideTriPoints[1].intersectionPointsTo[0])
                            index = 0;


                    Line ll = new Line(outsideTriPoints[0].intersectionPointsFrom[0], insideTriPoints[index].triPoint, XColour.Red, 3f);

                    ll.Draw(spriteBatch);
                }
                else if (outsideTriPoints.Count == 2)
                {
                    if ((outsideTriPoints[0].intersectionPointsFrom.Count == 0 && outsideTriPoints[1].intersectionPointsTo.Count == 0) ||
                        (outsideTriPoints[1].intersectionPointsFrom.Count == 0 && outsideTriPoints[0].intersectionPointsTo.Count == 0))
                    {
                        Line l1;
                        if (insideTriPoints[0].intersectionPointsFrom[0].X != insideTriPoints[0].intersectionPointsTo[0].X &&
                            insideTriPoints[0].intersectionPointsFrom[0].Y != insideTriPoints[0].intersectionPointsTo[0].Y)
                        {
                            l1 = new Line(insideTriPoints[0].intersectionPointsTo[0], insideTriPoints[0].intersectionPointsFrom[0], XColour.Green, 3f);
                            l1.Draw(spriteBatch);
                        }
                        else
                        {
                            List<DPoint> d = new List<DPoint>{new DPoint((int)insideTriPoints[0].intersectionPointsFrom[0].X, (int)insideTriPoints[0].intersectionPointsFrom[0].Y),
                                             new DPoint((int)insideTriPoints[0].intersectionPointsTo[0].X, (int)insideTriPoints[0].intersectionPointsTo[0].Y),
                                             new DPoint((int)insideTriPoints[0].triPoint.X, (int)insideTriPoints[0].triPoint.Y)};
                            Polygon p = new Polygon(d, DColour.Red);
                            p.Draw(spriteBatch);
                        }



                    }
                    else
                    {
                        int index = 0;
                        int notIndex;
                        if (outsideTriPoints[0].intersectionPointsFrom.Count < outsideTriPoints[1].intersectionPointsFrom.Count)
                            index = 1;

                        notIndex = (index == 0) ? 1 : 0;

                        Line l1 = new Line(outsideTriPoints[index].intersectionPointsFrom[0], insideTriPoints[0].triPoint, XColour.Red, 2f);
                        Line l3;
                        Vector2 v;
                        if ((insideTriPoints[0].intersectionPointsFrom[0].X == outsideTriPoints[index].intersectionPointsFrom[0].X) ||
                            insideTriPoints[0].intersectionPointsFrom[0].Y == outsideTriPoints[index].intersectionPointsFrom[0].Y)
                        {
                            v = insideTriPoints[0].intersectionPointsTo[0];//outsideTriPoints[index].intersectionPointsFrom[0];
                        }
                        else
                            v = insideTriPoints[0].intersectionPointsFrom[0];

                        l3 = new Line(outsideTriPoints[index].intersectionPointsFrom[0], v, XColour.Green, 2f);
                        l1.Draw(spriteBatch);
                        if (l3 != null)
                            l3.Draw(spriteBatch);
                    }
                }
                else if (outsideTriPoints.Count == 3)
                {
                    int index1 = 0;
                    int index2 = 0;
                    if (outsideTriPoints[0].intersectionPointsFrom.Count == 0)
                        index1 = 1;

                    for (int i = 0; i < 3; i++)
                    {
                        if (i == index1)
                            continue;
                        if (outsideTriPoints[i].intersectionPointsFrom.Count > 0)
                            index2 = i;
                    }

                    if (outsideTriPoints[index1].intersectionPointsTo.Count == 0 ||
                        outsideTriPoints[index2].intersectionPointsTo.Count == 0)
                    {
                        Line l1;
                        if (!(outsideTriPoints[index1].intersectionPointsFrom[0].X != outsideTriPoints[index2].intersectionPointsTo[0].Y &&
                            outsideTriPoints[index1].intersectionPointsFrom[0].Y != outsideTriPoints[index2].intersectionPointsTo[0].Y))
                            l1 = new Line(outsideTriPoints[index1].intersectionPointsFrom[0], outsideTriPoints[index2].intersectionPointsFrom.Last(), XColour.Green, 2f);
                        else
                            l1 = new Line(outsideTriPoints[index1].intersectionPointsFrom[0], outsideTriPoints[index2].intersectionPointsFrom[0], XColour.Green, 2f);
                        l1.Draw(spriteBatch);
                    }

                    if (outsideTriPoints[index1].intersectionPointsTo.Count > 0 &&
                        outsideTriPoints[index2].intersectionPointsTo.Count > 0)
                    {
                        Line l2 = new Line(outsideTriPoints[index1].intersectionPointsFrom[0], outsideTriPoints[index1].intersectionPointsTo.Last(), XColour.Red, 2f);
                        Line l3 = new Line(outsideTriPoints[index2].intersectionPointsTo.Last(), outsideTriPoints[index1].intersectionPointsFrom[0], XColour.Blue, 2f);
                        Line l4 = new Line(outsideTriPoints[index2].intersectionPointsTo[0], outsideTriPoints[index1].intersectionPointsFrom[0], XColour.Yellow, 2f);
                        l2.Draw(spriteBatch);
                        l3.Draw(spriteBatch);
                        l4.Draw(spriteBatch);
                    }

                    
                    for (int i = 0; i < 8; i += 2)
                    {
                        XColour col;
                        if (i == 0)
                            col = XColour.Pink;
                        else if (i == 2)
                            col = XColour.White;
                        else if (i == 4)
                            col = XColour.Salmon;
                        else
                            col = XColour.RoyalBlue;
                        if (FindPointInPolygon(trianglePoints.ToList<Vector2>(), l[i]))
                        {
                            Vector2 v;

                            if (outsideTriPoints[index2].intersectionPointsFrom.Last().X == l[i].X ||
                                outsideTriPoints[index2].intersectionPointsFrom.Last().Y == l[i].Y)
                                v = outsideTriPoints[index2].intersectionPointsFrom[0];
                            else
                                v = outsideTriPoints[index2].intersectionPointsFrom.Last();
                            Line lX = new Line(l[i], v, col, 2f);
                            lX.Draw(spriteBatch);
                        }
                        else
                            Console.WriteLine("False! + " + i);
                    }
                    
                }
            }


        }

        public bool FindPointInPolygon(List<Vector2> points, Vector2 p)
        {
            List<float> v = new List<float>();
            for (int i = 1; i < points.Count; i++)
            {
                v.Add(orient2d(points[i - 1], points[i], p));
                if (v.Last() < 0)
                    return false;
            }
            v.Add(orient2d(points[points.Count-1], points[0], p));
            if (v.Last() < 0)
                return false;


            return true;
        }

        private float orient2d(Vector2 a, Vector2 b, Vector2 p) // a = input 1, b = input 2, p = point to check
        {
            return (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
        }
    }
}
