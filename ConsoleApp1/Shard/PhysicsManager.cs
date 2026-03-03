/*
*
*   The Shard Physics Manager.
*
*   As with the PhysicsBody class, upon which this class depends, I make no claims as to the
*       accuracy of the physics.  My interest in this course is showing you how an engine is
*       architected.  It's not a course on game physics.  The task of making this work in
*       a way that simulates real world physics is well beyond the scope of the course.
*
*   This class is responsible for a lot.  It handles the broad phase collision
*       detection (via Sweep and Prune).  It handles the narrow phase collisions, making use of the
*       collider objects and the Minkowski differences they generate.  It does some collision resolutions
*       that are linked to the mass of colliding bodies.  And it has the management routines that
*       let all that happen.
*
*   @author Michael Heron
*   @version 1.0
*
*   Several substantial contributions to the code made by others:
*   @author Mårten Åsberg (see Changelog for 1.0.1)
*
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace Shard
{
    /*
     * An internal class used to hold a combination of two potentially colliding objects.
     */
    class CollidingObject
    {
        PhysicsBody a, b;

        public CollidingObject() { }

        public CollidingObject(PhysicsBody x, PhysicsBody y)
        {
            A = x;
            B = y;
        }

        internal PhysicsBody A { get => a; set => a = value; }
        internal PhysicsBody B { get => b; set => b = value; }

        public override bool Equals(object other)
        {
            return other is CollidingObject co &&
                (A == co.A && B == co.B ||
                 A == co.B && B == co.A);
        }

        public override int GetHashCode()
        {
            return A.GetHashCode() ^ B.GetHashCode();
        }

        public override String ToString()
        {
            return "[" + A.Parent.ToString() + " v " + B.Parent.ToString() + "]";
        }
    }

    /*
     * SAP is Search and Prune, a broad pass collision detection algorithm and the one used
     * in the course. This is an internal class used to contain a node in what will be a
     * linked list used elsewhere in this code.
     */
    class SAPEntry
    {
        PhysicsBody owner;
        float start, end;
        SAPEntry previous, next;

        public float Start { get => start; set => start = value; }
        public float End { get => end; set => end = value; }
        internal PhysicsBody Owner { get => owner; set => owner = value; }
        internal SAPEntry Previous { get => previous; set => previous = value; }
        internal SAPEntry Next { get => next; set => next = value; }
    }

    class PhysicsManager
    {
        private static PhysicsManager me;
        private List<CollidingObject> collisionsToCheck;
        HashSet<CollidingObject> colliding;

        private long timeInterval;
        SAPEntry sapX, sapY;
        float gravityModifier;
        Vector2 gravityDir;

        List<PhysicsBody> allPhysicsObjects;
        private long lastUpdate;
        private long lastDebugDraw;

        private PhysicsManager()
        {
            string tmp = "";
            string[] tmpbits;

            allPhysicsObjects = new List<PhysicsBody>();
            colliding = new HashSet<CollidingObject>();

            lastUpdate = Bootstrap.getCurrentMillis();
            collisionsToCheck = new List<CollidingObject>();

            gravityDir = new Vector2(0, 1);

            // 50 FPS
            TimeInterval = 20;

            if (Bootstrap.checkEnvironmentalVariable("gravity_modifier"))
            {
                gravityModifier = float.Parse(
                    Bootstrap.getEnvironmentalVariable("gravity_modifier"),
                    CultureInfo.InvariantCulture
                );
            }
            else
            {
                gravityModifier = 0.1f;
            }

            if (Bootstrap.checkEnvironmentalVariable("gravity_dir"))
            {
                tmp = Bootstrap.getEnvironmentalVariable("gravity_dir");
                tmpbits = tmp.Split(",");
                gravityDir = new Vector2(int.Parse(tmpbits[0]), int.Parse(tmpbits[1]));
            }
            else
            {
                gravityDir = new Vector2(0, 1);
            }
        }

        public static PhysicsManager getInstance()
        {
            if (me == null)
            {
                me = new PhysicsManager();
            }
            return me;
        }

        public long LastUpdate { get => lastUpdate; set => lastUpdate = value; }
        public long TimeInterval { get => timeInterval; set => timeInterval = value; }
        public long LastDebugDraw { get => lastDebugDraw; set => lastDebugDraw = value; }
        public float GravityModifier { get => gravityModifier; set => gravityModifier = value; }

        public void addPhysicsObject(PhysicsBody body)
        {
            if (allPhysicsObjects.Contains(body))
            {
                return;
            }
            allPhysicsObjects.Add(body);
        }

        public void removePhysicsObject(PhysicsBody body)
        {
            allPhysicsObjects.Remove(body);
        }

        public void clearList(SAPEntry node)
        {
            if (node == null)
            {
                return;
            }

            while (node != null && node.Next != null)
            {
                node = node.Next;
                node.Previous.Next = null;
                node.Previous = null;
            }

            node.Previous = null;
        }

        public SAPEntry addToList(SAPEntry node, SAPEntry entry)
        {
            SAPEntry current = node;

            if (current == null)
            {
                return entry;
            }

            if (entry.Start < current.Start)
            {
                current.Previous = entry;
                entry.Next = current;
                return entry;
            }

            while (current.Next != null && entry.Start > current.Next.Start)
            {
                current = current.Next;
            }

            if (current.Next != null)
            {
                entry.Previous = current;
                entry.Next = current.Next;
                current.Next = entry;
            }
            else
            {
                current.Next = entry;
                entry.Previous = current;
            }

            return node;
        }

        public void outputList(SAPEntry node)
        {
            SAPEntry pointer = node;
            int counter = 0;
            string text = "";

            if (pointer == null)
            {
                Debug.getInstance().log("No List");
                return;
            }

            while (pointer != null)
            {
                text += "[" + counter + "]: " + pointer.Owner.Parent + ", ";
                pointer = pointer.Next;
                counter += 1;
            }

            Debug.getInstance().log("List:" + text);
        }

        public bool willTick()
        {
            if (Bootstrap.getCurrentMillis() - lastUpdate > TimeInterval)
            {
                return true;
            }
            return false;
        }

        public void clearSimulationState()
        {
            collisionsToCheck.Clear();
            colliding.Clear();
            lastUpdate = Bootstrap.getCurrentMillis();
        }

        public bool update()
        {
            CollisionHandler ch, ch2;
            List<CollidingObject> toRemove;

            if (willTick() == false)
            {
                return false;
            }

            lastUpdate = Bootstrap.getCurrentMillis();
            toRemove = new List<CollidingObject>();

            foreach (PhysicsBody body in allPhysicsObjects)
            {
                if (body.UsesGravity)
                {
                    body.applyGravity(gravityModifier, gravityDir);
                }

                body.physicsTick();
                body.recalculateColliders();
            }

            // Check for old collisions that should be persisted
            foreach (CollidingObject col in colliding)
            {
                ch = col.A.Colh;
                ch2 = col.B.Colh;
                Vector2? impulse;

                if (col.A.Parent.ToBeDestroyed)
                {
                    ch2.onCollisionExit(null);
                    toRemove.Add(col);
                }

                if (col.B.Parent.ToBeDestroyed)
                {
                    ch.onCollisionExit(null);
                    toRemove.Add(col);
                }

                impulse = checkCollisionBetweenObjects(col.A, col.B);

                if (impulse != null)
                {
                    ch.onCollisionStay(col.B);
                    ch2.onCollisionStay(col.A);
                }
                else
                {
                    ch.onCollisionExit(col.B);
                    ch2.onCollisionExit(col.A);
                    toRemove.Add(col);
                }
            }

            foreach (CollidingObject col in toRemove)
            {
                colliding.Remove(col);
            }

            toRemove.Clear();

            // Check for new collisions
            checkForCollisions();

            return true;
        }

        public void drawDebugColliders()
        {
            foreach (PhysicsBody body in allPhysicsObjects)
            {
                body.drawMe();
            }
        }

        // ============================
        // CHANGED: choose MTV instead of first hit
        // ============================
        private Vector2? checkCollisionBetweenObjects(PhysicsBody a, PhysicsBody b)
        {
            Vector2? best = null;
            float bestLenSq = float.PositiveInfinity;

            foreach (Collider col in a.getColliders())
            {
                foreach (Collider col2 in b.getColliders())
                {
                    Vector2? impulse = col.checkCollision(col2);

                    if (!impulse.HasValue)
                        continue;

                    Vector2 v = impulse.Value;
                    float lenSq = v.LengthSquared();

                    // Ignore degenerate impulses
                    if (lenSq < 1e-12f)
                        continue;

                    // Pick the smallest translation vector (MTV)
                    if (lenSq < bestLenSq)
                    {
                        bestLenSq = lenSq;
                        best = v;
                    }
                }
            }

            return best;
        }

        // omg this won't scale omg
        private void broadPassBruteForce()
        {
            CollidingObject tmp;

            if (allPhysicsObjects.Count < 2)
            {
                return;
            }

            for (int i = 0; i < allPhysicsObjects.Count; i++)
            {
                for (int j = 0; j < allPhysicsObjects.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    if (findColliding(allPhysicsObjects[i], allPhysicsObjects[j]))
                    {
                        continue;
                    }

                    if (findColliding(allPhysicsObjects[j], allPhysicsObjects[i]))
                    {
                        continue;
                    }

                    tmp = new CollidingObject();
                    tmp.A = allPhysicsObjects[i];
                    tmp.B = allPhysicsObjects[j];

                    collisionsToCheck.Add(tmp);
                }
            }
        }

        public bool findColliding(PhysicsBody a, PhysicsBody b)
        {
            CollidingObject col = new CollidingObject(a, b);
            return colliding.Contains(col);
        }

        private void narrowPass()
        {
            Vector2 impulse;
            Vector2? possibleImpulse;
            float massTotal;
            float massProp = 0.0f;

            // ensure each body reflects at most once per physics tick
            HashSet<PhysicsBody> reflectedThisTick = new HashSet<PhysicsBody>();

            foreach (CollidingObject ob in collisionsToCheck)
            {
                possibleImpulse = checkCollisionBetweenObjects(ob.A, ob.B);

                if (!possibleImpulse.HasValue)
                    continue;

                impulse = possibleImpulse.Value;

                Debug.Log("Col is " + ob + ", impulse " + possibleImpulse);

                // If impulse is tiny, we can skip normal-based epsilon to avoid NaNs
                Vector2 n = Vector2.Zero;
                float eps = 0.001f;
                if (impulse.LengthSquared() > 1e-8f)
                    n = Vector2.Normalize(impulse);

                if (ob.A.PassThrough != true && ob.B.PassThrough != true)
                {
                    massTotal = ob.A.Mass + ob.B.Mass;

                    // --- First move B ---
                    if (ob.A.Kinematic)
                        massProp = 1f;
                    else
                        massProp = ob.A.Mass / massTotal;

                    if (ob.A.ImpartForce)
                    {
                        ob.A.impartForces(ob.B, massProp);
                        ob.A.reduceForces(1.0f - massProp);
                    }

                    if (!ob.B.Kinematic)
                    {
                        float mx = impulse.X * massProp;
                        float my = impulse.Y * massProp;

                        mx += n.X * eps * massProp;
                        my += n.Y * eps * massProp;

                        ob.B.Parent.Transform.translate(-mx, -my);
                    }

                    // --- Then move A ---
                    if (ob.B.Kinematic)
                        massProp = 1f;
                    else
                        massProp = 1.0f - massProp;

                    if (!ob.A.Kinematic)
                    {
                        float mx = impulse.X * massProp;
                        float my = impulse.Y * massProp;

                        mx += n.X * eps * massProp;
                        my += n.Y * eps * massProp;

                        ob.A.Parent.Transform.translate(mx, my);
                    }

                    if (ob.A.StopOnCollision) ob.A.stopForces();
                    if (ob.B.StopOnCollision) ob.B.stopForces();
                }

                ((CollisionHandler)ob.A.Parent).onCollisionEnter(ob.B);
                ((CollisionHandler)ob.B.Parent).onCollisionEnter(ob.A);
                colliding.Add(ob);

                if (ob.A.ReflectOnCollision && !reflectedThisTick.Contains(ob.A))
                {
                    ob.A.reflectForces(impulse);
                    reflectedThisTick.Add(ob.A);
                }

                if (ob.B.ReflectOnCollision && !reflectedThisTick.Contains(ob.B))
                {
                    ob.B.reflectForces(-impulse);
                    reflectedThisTick.Add(ob.B);
                }
            }
        }

        public void reportCollisionsInAxis(SAPEntry start)
        {
            List<SAPEntry> activeObjects;
            List<int> toRemove;
            CollidingObject col;

            activeObjects = new List<SAPEntry>();
            toRemove = new List<int>();
            col = new CollidingObject();

            while (start != null)
            {
                activeObjects.Add(start);

                for (int i = 0; i < activeObjects.Count; i++)
                {
                    if (start == activeObjects[i])
                    {
                        continue;
                    }

                    if (start.Start >= activeObjects[i].End)
                    {
                        toRemove.Add(i);
                    }
                    else
                    {
                        col = new CollidingObject();

                        if (start.Owner.Mass > activeObjects[i].Owner.Mass)
                        {
                            col.A = start.Owner;
                            col.B = activeObjects[i].Owner;
                        }
                        else
                        {
                            col.B = start.Owner;
                            col.A = activeObjects[i].Owner;
                        }

                        if (!findColliding(col.A, col.B))
                        {
                            collisionsToCheck.Add(col);
                        }
                    }
                }

                for (int j = toRemove.Count - 1; j >= 0; j--)
                {
                    activeObjects.RemoveAt(toRemove[j]);
                }

                toRemove.Clear();
                start = start.Next;
            }
        }

        public void broadPassSearchAndSweep()
        {
            SAPEntry sx;
            float[] x;
            sapX = null;
            sapY = null;

            foreach (PhysicsBody body in allPhysicsObjects)
            {
                sx = new SAPEntry();

                x = body.MinAndMaxX;

                sx.Owner = body;
                sx.Start = x[0];
                sx.End = x[1];

                sapX = addToList(sapX, sx);
            }

            reportCollisionsInAxis(sapX);
            clearList(sapX);
        }

        public void broadPass()
        {
            broadPassSearchAndSweep();
            // broadPassBruteForce();
        }

        private void checkForCollisions()
        {
            broadPass();
            narrowPass();
            collisionsToCheck.Clear();
        }
    }
}
