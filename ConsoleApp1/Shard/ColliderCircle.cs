/*
*
*   The collider for circles.   Handles circle/circle, circle/rect, and circle/point collisions.
*   @author Michael Heron
*   @version 1.0
*
*/

using System;
using System.Drawing;
using System.Numerics;

namespace Shard
{
    class ColliderCircle : Collider
    {
        private Transform myRect;
        private float x, y, rad;
        private float xoff, yoff;
        private bool fromTrans;

        public ColliderCircle(CollisionHandler gob, Transform t) : base(gob)
        {
            this.MyRect = t;
            fromTrans = true;
            RotateAtOffset = false;
            calculateBoundingBox();
        }

        public ColliderCircle(CollisionHandler gob, Transform t, float x, float y, float rad) : base(gob)
        {
            Xoff = x;
            Yoff = y;
            X = Xoff;
            Y = Yoff;
            Rad = rad;
            RotateAtOffset = true;

            this.MyRect = t;
            fromTrans = false;

            calculateBoundingBox();
        }

        public ColliderCircle(CollisionHandler gob) : base(gob) { }

        public void calculateBoundingBox()
        {
            float x1, x2, y1, y2;
            float intWid;
            float angle = (float)(Math.PI * MyRect.Rotz / 180.0f);

            if (fromTrans)
            {
                intWid = MyRect.Wid * (float)MyRect.Scalex;
                Rad = (float)(intWid / 2);
                X = (float)MyRect.X + Xoff + Rad;
                Y = (float)MyRect.Y + Yoff + Rad;
            }
            else
            {
                X = (float)MyRect.X + Xoff;
                Y = (float)MyRect.Y + Yoff;
            }

            if (RotateAtOffset == true)
            {
                // Now we work out the X and Y based on the rotation of the body to
                // which this belongs,.
                x1 = X - MyRect.Centre.X;
                y1 = Y - MyRect.Centre.Y;

                x2 = (float)(x1 * Math.Cos(angle) - y1 * Math.Sin(angle));
                y2 = (float)(x1 * Math.Sin(angle) + y1 * Math.Cos(angle));

                X = x2 + (float)MyRect.Centre.X;
                Y = y2 + (float)MyRect.Centre.Y;
            }

            MinAndMaxX[0] = X - Rad;
            MinAndMaxX[1] = X + Rad;
            MinAndMaxY[0] = Y - Rad;
            MinAndMaxY[1] = Y + Rad;
        }

        internal Transform MyRect { get => myRect; set => myRect = value; }
        public float X { get => x; set => x = value; }
        public float Y { get => y; set => y = value; }
        public float Rad { get => rad; set => rad = value; }

        public float Left { get => MinAndMaxX[0]; set => MinAndMaxX[0] = value; }
        public float Right { get => MinAndMaxX[1]; set => MinAndMaxX[1] = value; }
        public float Top { get => MinAndMaxY[0]; set => MinAndMaxY[0] = value; }
        public float Bottom { get => MinAndMaxY[1]; set => MinAndMaxY[1] = value; }
        public float Xoff { get => xoff; set => xoff = value; }
        public float Yoff { get => yoff; set => yoff = value; }

        public override void recalculate()
        {
            calculateBoundingBox();
        }

        public override Vector2? checkCollision(ColliderRect other)
        {
            double tx = X;
            double ty = Y;
            double dx, dy, dist;
            Vector2 dir;
            double depth;

            if (X < other.Left) tx = other.Left;
            else if (X > other.Right) tx = other.Right;

            if (Y < other.Top) ty = other.Top;
            else if (Y > other.Bottom) ty = other.Bottom;

            // PYTHAGORAS YO
            dx = X - tx;
            dy = Y - ty;

            dist = Math.Sqrt(dx * dx + dy * dy);

            // if the distance is less than the radius, collision!
            if (dist < Rad)
            {
                depth = Rad - dist;

                // Circle centre is inside the rect (or exactly on edge) -> choose nearest face.
                if (dist < 1e-6)
                {
                    float dLeft = (float)Math.Abs(X - other.Left);
                    float dRight = (float)Math.Abs(other.Right - X);
                    float dTop = (float)Math.Abs(Y - other.Top);
                    float dBottom = (float)Math.Abs(other.Bottom - Y);

                    float min = Math.Min(Math.Min(dLeft, dRight), Math.Min(dTop, dBottom));

                    // Tie-break is deterministic (left -> right -> top -> bottom)
                    if (min == dLeft) dir = new Vector2(-1f, 0f);
                    else if (min == dRight) dir = new Vector2(1f, 0f);
                    else if (min == dTop) dir = new Vector2(0f, -1f);
                    else dir = new Vector2(0f, 1f);

                    dir *= (float)depth;
                    return dir;
                }

                // NEW: distinguish edge-interior hits from corner hits to avoid "edge looks like corner"
                const double eps = 1e-6;

                bool txOnLeft = Math.Abs(tx - other.Left) < eps;
                bool txOnRight = Math.Abs(tx - other.Right) < eps;
                bool tyOnTop = Math.Abs(ty - other.Top) < eps;
                bool tyOnBot = Math.Abs(ty - other.Bottom) < eps;

                bool txInside = (tx > other.Left + eps) && (tx < other.Right - eps);
                bool tyInside = (ty > other.Top + eps) && (ty < other.Bottom - eps);

                // Edge interior hit -> axis aligned normal
                if ((tyOnTop || tyOnBot) && txInside)
                {
                    // Top/bottom edge: vertical normal. dy = Y - ty tells direction.
                    dir = new Vector2(0f, (float)Math.Sign(dy));
                }
                else if ((txOnLeft || txOnRight) && tyInside)
                {
                    // Left/right edge: horizontal normal.
                    dir = new Vector2((float)Math.Sign(dx), 0f);
                }
                else
                {
                    // Corner hit: geometric normal
                    dir = new Vector2((float)dx, (float)dy);
                    dir = Vector2.Normalize(dir);
                }

                dir *= (float)depth;
                return dir;
            }

            return null;
        }

        public override void drawMe(Color col)
        {
            Display d = Bootstrap.getDisplay();
            d.drawCircle((int)X, (int)Y, (int)Rad, col);
        }

        public override Vector2? checkCollision(ColliderCircle c)
        {
            double dist, depth, radsq;
            double xpen, ypen;
            Vector2 dir;

            xpen = Math.Pow(c.X - this.X, 2);
            ypen = Math.Pow(c.Y - this.Y, 2);

            radsq = Math.Pow(c.Rad + this.Rad, 2);
            dist = xpen + ypen;

            depth = (c.Rad + Rad) - Math.Sqrt(dist);

            if (dist <= radsq)
            {
                dir = new Vector2(X - c.X, Y - c.Y);
                dir = Vector2.Normalize(dir);
                dir *= (float)depth;
                return dir;
            }

            return null;
        }

        public override float[] getMinAndMaxX() => MinAndMaxX;
        public override float[] getMinAndMaxY() => MinAndMaxY;

        public override Vector2? checkCollision(Vector2 c)
        {
            if (c.X >= Left && c.X <= Right && c.Y >= Top && c.Y <= Bottom)
                return new Vector2(0, 0);

            return null;
        }
    }
}