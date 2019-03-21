/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using System;
using System.Collections.Generic;

namespace LowPoints
{
    public class Commands
    {
        [CommandMethod("findBowlsByVertex")]
        public static void FindWaterBowlsByVertex()
        {
            CivilDocument civilDoc = CivilApplication.ActiveDocument;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord modelSpace = trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                ObjectIdCollection surfaceIds = civilDoc.GetSurfaceIds();
                foreach (ObjectId surfaceId in surfaceIds)
                {
                    TinSurface surf = trans.GetObject(surfaceId, OpenMode.ForRead) as TinSurface;
                    if (surf == null) continue;

                    foreach (TinSurfaceVertex vertex in surf.Vertices)
                    {
                        // start assuming is a low point
                        bool isLowest = true;
                        // now look at the other end of each Edge from the vertex
                        foreach ( TinSurfaceEdge edge in vertex.Edges)
                        {
                            TinSurfaceVertex otherVertice = (edge.Vertex1.Location.DistanceTo(vertex.Location) < Tolerance.Global.EqualPoint ? edge.Vertex2 : edge.Vertex1);
                            // is the other vertice lower? 
                            if (otherVertice.Location.Z < vertex.Location.Z)
                                isLowest = false; // other is lower, therefore this is not the lowest
                        }
                        
                        // if is the lowest, add a point there
                        if (isLowest)
                        {
                            DBPoint newPoint = new DBPoint();
                            newPoint.Position = vertex.Location;
                            modelSpace.AppendEntity(newPoint);
                            trans.AddNewlyCreatedDBObject(newPoint, true);
                        }
                    }
                }
                trans.Commit();
            }
        }

        [CommandMethod("findBowlsByWaterdrop")]
        public static void FindWaterBowlsByWaterdrop()
        {
            CivilDocument civilDoc = CivilApplication.ActiveDocument;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord modelSpace = trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                ObjectIdCollection surfaceIds = civilDoc.GetSurfaceIds();
                foreach (ObjectId surfaceId in surfaceIds)
                {
                    TinSurface surf = trans.GetObject(surfaceId, OpenMode.ForRead) as TinSurface;
                    if (surf == null) continue;

                    foreach (TinSurfaceVertex vertex in surf.Vertices)
                    {
                        // get the centroid is failing... need to investigate better
                        //Point2d centroid = GetCentroid(new List<Point3d>() { triangle.Vertex1.Location, triangle.Vertex2.Location, triangle.Vertex3.Location });

                        ObjectIdCollection drops = surf.Analysis.CreateWaterdrop(vertex.Location.Convert2d(new Plane()), Autodesk.Civil.WaterdropObjectType.Polyline3D);
                        if (drops.Count > 1)
                        {
                            Point3d closestPoint = FindClosestPoint(trans, drops[0], drops[1]);
                            Polyline3d curveA = trans.GetObject(drops[0], OpenMode.ForRead) as Polyline3d;
                            if (closestPoint.DistanceTo(curveA.StartPoint) > Tolerance.Global.EqualPoint)
                            {
                                DBPoint newPoint = new DBPoint();
                                newPoint.Position = closestPoint;
                                modelSpace.AppendEntity(newPoint);
                                trans.AddNewlyCreatedDBObject(newPoint, true);
                            }
                        }

                        // cleanup...
                        foreach (ObjectId drop in drops) trans.GetObject(drop, OpenMode.ForWrite).Erase();

                    }
                }
                trans.Commit();
            }
        }

        public static Point2d GetCentroid(List<Point3d> poly)
        {
            double accumulatedArea = 0.0f;
            double centerX = 0.0f;
            double centerY = 0.0f;

            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                double temp = poly[i].X * poly[j].Y - poly[j].X * poly[i].Y;
                accumulatedArea += temp;
                centerX += (poly[i].X + poly[j].X) * temp;
                centerY += (poly[i].Y + poly[j].Y) * temp;
            }

            if (Math.Abs(accumulatedArea) < 1E-7f)
                return Point2d.Origin;  // Avoid division by zero

            accumulatedArea *= 3f;
            return new Point2d(centerX / accumulatedArea, centerY / accumulatedArea);
        }

        public static Point3d FindClosestPoint(Transaction trans, ObjectId idA, ObjectId idB)
        {
            Polyline3d curveA = trans.GetObject(idA, OpenMode.ForRead) as Polyline3d;
            Polyline3d curveB = trans.GetObject(idB, OpenMode.ForRead) as Polyline3d;

            double distance = double.MaxValue;
            Point3d result = Point3d.Origin;
            for (double param = curveA.StartParam; param < curveA.EndParam; param += ((curveA.EndParam - curveA.StartParam) / 100))
            {
                Point3d pointOnA = curveA.GetPointAtParameter(param);
                Point3d closestPointOnB = curveB.GetClosestPointTo(pointOnA, false);

                double distanceBetweenPoints = pointOnA.DistanceTo(closestPointOnB);
                if (distanceBetweenPoints < distance)
                {
                    distance = distanceBetweenPoints;
                    result = pointOnA; // equals closestPointOnB
                }
            }

            return result;
        }
    }
}
