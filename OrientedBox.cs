using Rhino.Geometry;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSystem2020
{
    class OrientedBox
    {
        public Brep BoundingBox;
        public BrepFace SideZmin;
        public BrepFace SideZmax;
        public BrepFace SideXmin;
        public BrepFace SideYmin;
        public BrepFace SideXmax;
        public BrepFace SideYmax;
        public double minX = double.MaxValue;
        public double maxX = double.MinValue;
        public double minY = double.MaxValue;
        public double maxY = double.MinValue;
        public double minZ = double.MaxValue;
        public double maxZ = double.MinValue;
        public Plane ReferencePlane;

        public Plane SideZminPlane;
        public Plane SideZmaxPlane;
        public Plane SideXminPlane;
        public Plane SideXmaxPlane;
        public Line xAxis;
        public Line yAxis;
        public Line zAxis;

        public double xDim;
        public double yDim;
        public double zDim;
        public OrientedBox(Brep brep, Plane plane)
        {
            BoundingBox = brep;
            ReferencePlane = plane;
            xAxis = new Line(ReferencePlane.Origin, ReferencePlane.XAxis);
            yAxis = new Line(ReferencePlane.Origin, ReferencePlane.YAxis);
            zAxis = new Line(ReferencePlane.Origin, ReferencePlane.ZAxis);
            setSides();
            SetOrigin();
            SetDimensions();
            SetPlanes();
        }
        private void SetDimensions()
        {
            xDim = Math.Abs(FaceCentroid(SideXmax).DistanceTo(FaceCentroid(SideXmin)));
            yDim = Math.Abs(FaceCentroid(SideYmax).DistanceTo(FaceCentroid(SideYmin)));
            zDim = Math.Abs(FaceCentroid(SideZmax).DistanceTo(FaceCentroid(SideZmin)));
        }
        private void SetPlanes()
        {
            SideZminPlane = ReferencePlane;

            SideZmaxPlane = ReferencePlane;
            SideZmaxPlane.Origin = ReferencePlane.Origin + ReferencePlane.XAxis * xDim + ReferencePlane.ZAxis * zDim;
            SideZmaxPlane.XAxis = ReferencePlane.XAxis * -1;
            SideZmaxPlane.ZAxis = ReferencePlane.ZAxis * -1;

            SideXminPlane = ReferencePlane;
            SideXminPlane.Origin = ReferencePlane.Origin + ReferencePlane.XAxis * xDim;
            SideXminPlane.XAxis = ReferencePlane.ZAxis;
            SideXminPlane.ZAxis = ReferencePlane.XAxis * -1;

            SideXmaxPlane = ReferencePlane;
            SideXmaxPlane.Origin = ReferencePlane.Origin + ReferencePlane.ZAxis * zDim;
            SideXmaxPlane.XAxis = ReferencePlane.ZAxis * -1;
            SideXmaxPlane.ZAxis = ReferencePlane.XAxis;

            //CheckPlane(SideZminPlane);
            //CheckPlane(SideZmaxPlane);
            //CheckPlane(SideXminPlane);
            //CheckPlane(SideXmaxPlane);
        }
        public Plane PlaneSelection(Orientation orientation)
        {
            switch (orientation)
            {
                case Orientation.Ceiling:
                    return SideZmaxPlane;
                case Orientation.Floor:
                    return SideZminPlane;
                case Orientation.SideFar:
                    return SideXmaxPlane;
                case Orientation.SideNear:
                    return SideXminPlane;
                default:
                    return ReferencePlane;
            }
        }
        public static void CheckPlane(Plane plane)
        {
            RhinoDoc.ActiveDoc.Objects.AddPoint(plane.Origin);
            RhinoDoc.ActiveDoc.Objects.AddLine(new Line(plane.Origin,plane.XAxis*1000));
            RhinoDoc.ActiveDoc.Objects.AddLine(new Line(plane.Origin, plane.YAxis * 2000));
            RhinoDoc.ActiveDoc.Objects.AddLine(new Line(plane.Origin, plane.ZAxis * 3000));
        }
        private void setSides()
        {
            foreach (BrepFace face in BoundingBox.Faces)
            {
                Point3d centroid = FaceCentroid(face);
                //RhinoDoc.ActiveDoc.Objects.AddPoint(centroid);
                double deltaX = xAxis.ClosestPoint(centroid, false).DistanceTo(ReferencePlane.Origin);
                double deltaY = yAxis.ClosestPoint(centroid, false).DistanceTo(ReferencePlane.Origin);
                double deltaZ = zAxis.ClosestPoint(centroid, false).DistanceTo(ReferencePlane.Origin);
                if (deltaX < minX)
                {
                    minX = deltaX;
                    SideXmin = face;
                }
                    
                if (deltaX > maxX)
                {
                    maxX = deltaX;
                    SideXmax = face;
                }
                if (deltaY < minY)
                {
                    minY = deltaY;
                    SideYmin = face;
                }
                    
                if (deltaY > maxY)
                {
                    maxY = deltaY;
                    SideYmax = face;
                }
                    

                if (deltaZ < minZ)
                {
                    minZ = deltaZ;
                    SideZmin = face;
                }
                    
                if (deltaZ > maxZ)
                {
                    maxZ = deltaZ;
                    SideZmax = face;
                }
                    
            }
        }
        private void SetOrigin()
        {
            Point3d origin = new Point3d();
            double minDist = double.MaxValue;
            foreach(var p in BoundingBox.Vertices)
            {
                if(p.Location.DistanceTo(ReferencePlane.Origin) < minDist)
                {
                    minDist = p.Location.DistanceTo(ReferencePlane.Origin);
                    origin = p.Location;
                }
            }
            ReferencePlane.Origin = origin;
        }
        public static Plane BrepFacePlane(BrepFace brepFace, bool pointInside)
        {
            Plane plane = new Plane(FaceCentroid(brepFace), NormalCentroid(brepFace));
            if (pointInside) plane.Flip();
            return plane;
        }
        public static Point3d FaceCentroid(BrepFace brepFace)
        {
            return brepFace.PointAt(brepFace.Domain(0).Mid, brepFace.Domain(1).Mid);
        }
        public static Vector3d NormalCentroid(BrepFace brepFace)
        {
            return brepFace.NormalAt(brepFace.Domain(0).Mid, brepFace.Domain(1).Mid);
        }

        public static Plane FaceOffsetPlane(BrepFace brepFace, double trimOffset)
        {
            
            Point3d origin = FaceCentroid(brepFace);
            Vector3d normal = NormalCentroid(brepFace);

            normal.Unitize();

            origin = origin + (normal * -trimOffset);

            Plane plane = new Plane(origin, normal);
            return plane;
        }
       
    }
}
