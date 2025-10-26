using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalFormwork.Components
{
    internal class Utils
    {
        public static bool CheckBrep(Brep brep)
        {
            // method returns "false" if at least one of the following is false:
            // isValid, IsSolid, IsManifold, IsValidTopology(), IsValidGeometry()
            // else method returns "true"
            if (null == brep) return false;

            bool result = true;

            if (!brep.IsValid) result = false;
            if (!brep.IsSolid) result = false;
            if (!brep.IsManifold) result = false;
            if (!brep.IsValidTopology(out string logTop)) result = false;
            if (!brep.IsValidGeometry(out string logGeo)) result = false;

            return result;
        }

        public static bool CheckBrep(Brep brep, GH_Component component)
        {
            // method returns "false" if at least one of the following is false:
            // isValid, IsSolid, IsManifold, IsValidTopology(), IsValidGeometry()
            // else method returns "true"
            if (null == brep)
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Brep is null.");
                return false;
            }

            bool result = true;

            if (!brep.IsValid)
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Brep is not valid.");
                result = false;
            }
            if (!brep.IsSolid)
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Brep is not closed.");
                result = false;
            }
            if (!brep.IsManifold)
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Brep is non-manifold.");
                result = false;
            }
            if (!brep.IsValidTopology(out string logTop))
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{logTop}");
                result = false;
            }
            if (!brep.IsValidGeometry(out string logGeo))
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{logGeo}");
                result = false;
            }

            return result;
        }

        public static bool BrepPlanarOnly(Brep brep, double tolerance, GH_Component component)
        {
            // true: all faces are planar
            // false: at least one non-planar surface

            foreach (BrepFace face in brep.Faces)
            {
                if (null == face || !face.IsValid)
                {
                    component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input Brep has at least one invalid face.");
                    return false;
                }
                if (!face.IsPlanar(tolerance))
                {
                    component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input Brep has at least one non-planar face.");
                    return false;
                }
            }
            return true;
        }

        public static Brep PrepBrep(IGH_GeometricGoo geometry, int limit, double tolerance)
        {
            Brep brep = null;
            if (!geometry.CastTo(out brep))
            {
                switch (geometry)
                {
                    case GH_Mesh mesh:
                        if (mesh.Value.Faces.Count > limit) return null;
                        brep = Brep.CreateFromMesh(mesh.Value, true);
                        break;
                    case GH_SubD subd:
                        if (subd.Value.Faces.Count > limit) return null;
                        brep = subd.Value.ToBrep();
                        break;
                    default:
                        GH_Convert.ToBrep(geometry, ref brep, GH_Conversion.Both);
                        break;
                }
            }

            brep.Repair(tolerance);
            brep.MergeCoplanarFaces(tolerance);
            brep.Compact();

            return brep;
        }

        public static Brep RebuildBrep(Brep brep, double tolerance)
        {
            if (null == brep) return null;
            List<BrepFace> faces = brep.Faces.ToList();
            List<Brep> trimmedFaces = new List<Brep>();

            foreach (BrepFace face in faces)
            {
                if(null == face) continue;
                Brep faceBrep = face.DuplicateFace(true); // true = duplicate trims
                if (faceBrep != null)
                    trimmedFaces.Add(faceBrep);
            }

            // Join into a closed Brep
            Brep[] rebuilt = Brep.JoinBreps(trimmedFaces, tolerance);
            if (rebuilt.Length > 0 && rebuilt[0].IsSolid)
            {
                brep = rebuilt[0];
            }
            return brep;
        }

        public static bool CheckMeshSolid(Mesh mesh)
        {
            // method returns "false" if at least one of the following is false:
            // isValid, IsSolid
            // else method returns "true"
            if (null == mesh) return false;

            bool result = true;

            if (!mesh.IsValid) result = false;
            if (!mesh.IsClosed) result = false;
            if (!mesh.IsSolid) result = false;

            return result;
        }

        public static bool CheckMeshSolid(Mesh mesh, GH_Component component)
        {
            // method returns "false" if at least one of the following is false:
            // isValid, IsSolid
            // else method returns "true"
            if (null == mesh)
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Mesh is null.");
                return false;
            }

            bool result = true;

            if (!mesh.IsValid)
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Mesh is not valid.");
                result = false;
            }
            if (!mesh.IsClosed)
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Mesh is not closed.");
                result = false;
            }
            if (!mesh.IsSolid)
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Mesh is not solid.");
                result = false;
            }

            return result;
        }

        public static Mesh CleanMesh(Mesh mesh)
        {
            var clean = mesh.DuplicateMesh();
            clean.Vertices.CombineIdentical(true, true);
            clean.Normals.ComputeNormals();
            clean.FaceNormals.ComputeFaceNormals();
            clean.UnifyNormals();
            clean.Compact();
            return clean;
        }

        public static Vector3d GetNV(BrepFace face, int uPercent, int vPercent, out Point3d basePoint)
        {
            // takes face and looks for Point3d and normal vector at given U and V
            // if unsuccessful returns Point3d and normal vector as close as possible to centroid
            face.ShrinkFace(0);
            var pt = Point3d.Unset;                         // point
            var nv = Vector3d.Unset;                        // normal vector
            if (uPercent >= 0 && vPercent >= 0)            // if specific U and V are given try to get point
            {
                var uDom = face.Domain(0);                      // get domain U
                var vDom = face.Domain(1);                      // get domain V
                var uNorm = uPercent / 100.0;
                var vNorm = vPercent / 100.0;
                var u = uDom.ParameterAt(uNorm);
                var v = vDom.ParameterAt(vNorm);

                if (face.IsPointOnFace(u, v) != PointFaceRelation.Exterior) 
                {
                    pt = face.PointAt(u, v);
                    nv = face.NormalAt(u, v);
                }
                else
                {
                    // if point is not inside of face, use point closest to centroid
                    var amp = AreaMassProperties.Compute(face);
                    face.ClosestPoint(amp.Centroid, out u, out v);
                    pt = face.PointAt(u, v);
                    nv = face.NormalAt(u, v);
                    nv.Unitize();
                }
            }
            else
            {
                // if no U and V are given (U,V <0), use point closest to centroid
                var amp = AreaMassProperties.Compute(face);
                face.ClosestPoint(amp.Centroid, out double u, out double v);
                pt = face.PointAt(u, v);
                nv = face.NormalAt(u, v);
                nv.Unitize();
            }

            basePoint = pt;
            return nv;
        }

        public static LineCurve GetFWRL(Vector3d fwrv, BrepFace face, GH_Component component)
        {
            // turns a vector into a LineCurve based on the centroid of face
            // it is assumed fwrv is valid and non-zero
            // it is assumed face is valid and planar

            // compute centroid
            var centerPt = AreaMassProperties.Compute(face);
            if (null == centerPt)
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to compute area centroid.");
                return null;
            }
            var centroid = centerPt.Centroid;
            var u = RhinoMath.UnsetValue;
            var v = RhinoMath.UnsetValue;
            if (!face.ClosestPoint(centroid, out u, out v))
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to find UV parameters of centroid.");
                return null;
            }

            // construct fwrl
            var endPt = centroid + fwrv;
            var fwrl = new LineCurve(centroid, endPt);

            if (null == fwrl || !fwrl.IsValid)
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to create fwrl.");
                return null;
            }
            return fwrl;
        }

        public static bool NoCollisionVolume(Brep castBody, BrepFace testFace, Vector3d rv, double tolerance, GH_Component component, out Brep[] collisionVolume)
        {
            // returns true if no collision occurs
            // returns false and collisionVolume in case of collision
            // it is assumed fwrp > 0
            // it is assumed brep is valid
            // it is assumed fwrv is valid and non-zero
            // it is assumed all faces are valid and planar

            // extrude and cap
            var fwrl = GetFWRL(rv, testFace, component);
            var sweptVolume = testFace.CreateExtrusion(fwrl, true);
            if (BrepSolidOrientation.Inward == sweptVolume.SolidOrientation) sweptVolume.Flip();
            if (null == sweptVolume || !sweptVolume.IsValid)
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Extrusion failed.");
                collisionVolume = null;
                return false;
            }

            // find overlap volume
            var overlap = Brep.CreateBooleanIntersection(castBody, sweptVolume, tolerance);
            if (null == overlap || overlap.Length == 0)
            {
                collisionVolume = null;
                return true;
            }
            else
            {
                foreach (var overlapBody in overlap)
                {
                    if(null == overlapBody) continue;
                    var vmp = VolumeMassProperties.Compute(overlapBody, true, false, false, false);
                    if ((vmp?.Volume ?? 0.0) > tolerance)
                    {
                        collisionVolume = overlap;
                        return false;
                    }
                }
                collisionVolume = null;
                return true;
            }
        }

        public static bool NoCollisionSurface(Brep castBody, BrepFace testFace, Vector3d fwrv, int surfaceResolution, double tolerance, GH_Component component, out Point3d[] pointsChecked)  //, out Curve[] criticalCrvs)
        {
            // returns true if no collision occurs
            // returns false and collisionVolume in case of collision
            // it is assumed that fwrp ~ 0
            // it is assumed brep is valid and has been compacted
            // it is assumed fwrv is valid and non-zero
            // it is assumed all faces are valid and planar

            // for testing
            var outputPts = new List<Point3d>();

            // retrieve plane based on test face
            if (!testFace.TryGetPlane(out var facePlane, tolerance))
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed retrieving test face plane.");
                pointsChecked = null;
                return false;
            }

            // get all face edges as singular curves
            var edgeCurves = new List<Curve>();

            foreach (var trim in testFace.OuterLoop.Trims)
            {
                edgeCurves.Add(trim.Edge.EdgeCurve.DuplicateCurve());
            }

            // move edge curves
            var move = Transform.Translation(fwrv);
            var movedEdges = edgeCurves.Select(ec => ec.DuplicateCurve()).ToList();
            for (int mi = 0; mi < movedEdges.Count; mi++)
            {
                movedEdges[mi].Transform(move);
            }

            // generate ruled surfaces
            var ruledSurfaces = new List<NurbsSurface>();
            for (int c = 0; c < edgeCurves.Count; c++)
            {
                var ruledSrf = NurbsSurface.CreateRuledSurface(edgeCurves[c], movedEdges[c]);
                ruledSurfaces.Add(ruledSrf);
            }

            // get the outer loops as closed curves
            var allRuledSrfLoops = new List<Curve>();
            for (int si = 0; si < ruledSurfaces.Count; si++)
            {
                var brepSrf = ruledSurfaces[si].ToBrep();
                allRuledSrfLoops.Add(brepSrf.Faces[0].OuterLoop.To3dCurve());
            }

            // region union of all ruled surface edges
            var ruledRegion = Curve.CreateBooleanRegions(allRuledSrfLoops, facePlane, true, tolerance);

            // Intersection of castBody and facePlane
            Intersection.BrepPlane(castBody, facePlane, tolerance, out Curve[] intersectCrvs, out _);
            var intersectBrep = Brep.CreatePlanarBreps(intersectCrvs, tolerance);
            if (null == intersectBrep || intersectBrep.Length == 0)
            {
                pointsChecked = null;
                return true;
            }

            // get intersection loops then take away the face
            var allIntersectSrfLoops = new List<Curve>();
            for (int ii = 0; ii < intersectBrep.Length; ii++)
            {
                allIntersectSrfLoops.Add(intersectBrep[ii].Faces[0].OuterLoop.To3dCurve());
            }
            var outerLoopCurve = testFace.OuterLoop.To3dCurve();
            var reducedIntRegionCrvs = new List<Curve>();
            foreach (var intLoopCrv in allIntersectSrfLoops)
            {
                if(null == intLoopCrv) continue;
                var diff = Curve.CreateBooleanDifference(intLoopCrv, outerLoopCurve, tolerance);
                if (null != diff && diff.Length > 0) reducedIntRegionCrvs.AddRange(diff);
            }

            // find intersection of reducedIntRegion and ruledRegion
            var criticalRegionCrvs = new List<Curve>();
            foreach (var redIntLoopCrv in reducedIntRegionCrvs)
            {
                if (null == redIntLoopCrv) continue;
                foreach (var ruledLoopCrv in ruledRegion.RegionCurves(0))
                {
                    if(null == ruledLoopCrv) continue;
                    var intersect = Curve.CreateBooleanIntersection(redIntLoopCrv, ruledLoopCrv, tolerance);
                    if (null != intersect && intersect.Length > 0) criticalRegionCrvs.AddRange(intersect);
                }
            }

            // run collision check
            if (null == criticalRegionCrvs || criticalRegionCrvs.Count == 0)
            {
                // no criticalRegion
                pointsChecked = null;
                return true;
            }
            else
            {
                // convert to faces
                var criticalBreps = Brep.CreatePlanarBreps(criticalRegionCrvs, tolerance);
                var critical = new bool[criticalBreps.Length];
                // loop over criticalRegions
                for (int bi = 0; bi < criticalBreps.Length; bi++)
                {
                    // testing for collisions
                    var resolution = surfaceResolution;
                    // reset point counter for each region
                    var pointCounter = 0;
                    do
                    {
                        // reset point counter for each iteration
                        pointCounter = 0;
                        // yields points to check
                        var checkPoints = YieldPtsOnFace(criticalBreps[bi].Faces[0], resolution);
                        foreach (var pt in checkPoints)
                        {
                            pointCounter++;

                            outputPts.Add(pt);
                            // inside castBody?
                            if (castBody.IsPointInside(pt, tolerance, true))
                            {
                                // collision found, escape from do while and quit checking
                                critical[bi] = false;
                                pointCounter = surfaceResolution;
                                break;
                            }
                            else
                            {
                                // no collision for this point
                                critical[bi] = true;
                            }
                        }
                        // only quit checking if a minimum number of points has been checked
                        if (pointCounter < surfaceResolution) resolution++;
                        // keep checking until a minimum numer of points has been checked
                    } while (pointCounter < surfaceResolution);
                }

                pointsChecked = outputPts.ToArray();
                // noCollision is only true if all critical checks are true
                return critical.All(b => b);
            }

        }

        public static IEnumerable<Point3d> YieldPtsOnFace(BrepFace checkFace, int nrOfPts)
        {
            // yield returns points on checkFace for collision testing

            // shrink face to get accurate domains, then get surface and its domains
            checkFace.ShrinkFace(0);
            var checkFaceSrf = checkFace.UnderlyingSurface();
            var uDom = checkFaceSrf.Domain(0);
            var vDom = checkFaceSrf.Domain(1);
            // "Introduce a little anarchy. You know the thing about chaos? It's fair"
            var random = new Random();
            // divide the surface into subdomains e.g. nrOfPts = 3 means 3*3 = 9 subdomains
            for (int ui = 0; ui < nrOfPts; ui++)
            {
                var u = uDom.ParameterAt((random.NextDouble() + ui) / nrOfPts);
                for (int vi = 0; vi < nrOfPts; vi++)
                {
                    var v = vDom.ParameterAt((random.NextDouble() + vi) / nrOfPts);
                    var pt = checkFaceSrf.PointAt(u, v);
                    if (checkFace.IsPointOnFace(u, v) == PointFaceRelation.Interior) yield return pt;
                }
            }
        }

        public static Point3d MeshFaceCentroid(Mesh mesh, MeshFace meshFace)
        {
            // returns the average of all vertices of a MeshFace

            if (meshFace.IsTriangle)
            {
                Point3d ptA = mesh.Vertices[meshFace.A];
                Point3d ptB = mesh.Vertices[meshFace.B];
                Point3d ptC = mesh.Vertices[meshFace.C];

                Point3d centroid = new Point3d(
                            (ptA.X + ptB.X + ptC.X) / 3.0,
                            (ptA.Y + ptB.Y + ptC.Y) / 3.0,
                            (ptA.Z + ptB.Z + ptC.Z) / 3.0);

                return centroid;
            }

            if (meshFace.IsQuad)
            {
                Point3d ptA = mesh.Vertices[meshFace.A];
                Point3d ptB = mesh.Vertices[meshFace.B];
                Point3d ptC = mesh.Vertices[meshFace.C];
                Point3d ptD = mesh.Vertices[meshFace.D];

                Point3d centroid = new Point3d(
                            (ptA.X + ptB.X + ptC.X + ptD.X) / 4.0,
                            (ptA.Y + ptB.Y + ptC.Y + ptD.Y) / 4.0,
                            (ptA.Z + ptB.Z + ptC.Z + ptD.Z) / 4.0);

                return centroid;
            }

            return Point3d.Unset;
        }

        public static Mesh MeshFaceToMesh(Mesh mesh, int faceIndex)
        {
            var face = mesh.Faces[faceIndex];
            var faceMesh = new Mesh();

            if (face.IsTriangle)
            {
                faceMesh.Vertices.Add(mesh.Vertices[face.A]);
                faceMesh.Vertices.Add(mesh.Vertices[face.B]);
                faceMesh.Vertices.Add(mesh.Vertices[face.C]);

                faceMesh.Faces.AddFace(0, 1, 2);
            }
            if (face.IsQuad)
            {
                faceMesh.Vertices.Add(mesh.Vertices[face.A]);
                faceMesh.Vertices.Add(mesh.Vertices[face.B]);
                faceMesh.Vertices.Add(mesh.Vertices[face.C]);
                faceMesh.Vertices.Add(mesh.Vertices[face.D]);

                faceMesh.Faces.AddFace(0, 1, 2, 3);
            }

            faceMesh.Normals.ComputeNormals();
            faceMesh.FaceNormals.ComputeFaceNormals();
            faceMesh.Compact();
            
            return faceMesh;
        }

        public static bool MeshFaceNoCollision(Mesh mesh, int faceIndex, int ptDensity, double edgeDistance, float normalDistance, Vector3d removalVector)
        {
            // get MeshFace at given index
            var face = mesh.Faces[faceIndex];
            // move all points by given distance in direction of face normal to avoid instant collision between ray and face
            var moveNormal = Vector3f.Multiply(mesh.FaceNormals[faceIndex], normalDistance);

            if (face.IsTriangle)
            {
                // get vertices and move up
                Point3d ptA = mesh.Vertices[face.A] + moveNormal;
                Point3d ptB = mesh.Vertices[face.B] + moveNormal;
                Point3d ptC = mesh.Vertices[face.C] + moveNormal;

                var checkPts = YieldPointsOnTriangle(ptA, ptB, ptC, ptDensity, edgeDistance);
                foreach(var pt in checkPts)
                {
                    var noCollision = MeshRayNoCollision(mesh, pt, removalVector);
                    if (!noCollision) return false;

                }
                return true;

            } else {
                // if face is Quad, divide into subtriangles ABC and BCD
                Point3d ptA = mesh.Vertices[face.A] + moveNormal;
                Point3d ptB = mesh.Vertices[face.B] + moveNormal;
                Point3d ptC = mesh.Vertices[face.C] + moveNormal;
                Point3d ptD = mesh.Vertices[face.D] + moveNormal;

                var checkPtsFirst = YieldPointsOnTriangle(ptA, ptB, ptC, ptDensity, edgeDistance);
                foreach (var pt in checkPtsFirst)
                {
                    var noCollision = MeshRayNoCollision(mesh, pt, removalVector);
                    if (!noCollision) return false;
                }

                var checkPtsSecond = YieldPointsOnTriangle(ptA, ptC, ptD, ptDensity, edgeDistance);
                foreach (var pt in checkPtsSecond)
                {
                    var noCollision = MeshRayNoCollision(mesh, pt, removalVector);
                    if (!noCollision) return false;
                }
                return true;
            }
        }

        public static bool MeshMeshNoCollision(Mesh sourceMesh, int faceIndex, Mesh targetMesh, int ptDensity, double edgeDistance, float normalDistance, Vector3d removalVector, out int[] collFaceIndices)
        {
            // get MeshFace at given index
            var face = sourceMesh.Faces[faceIndex];
            // move all points by given distance in direction of face normal to avoid instant collision between ray and face
            var moveNormal = Vector3f.Multiply(sourceMesh.FaceNormals[faceIndex], normalDistance);
            // save all faces of target mesh that collid as index hash set
            var collFaceIndSet = new HashSet<int>();

            if (face.IsTriangle)
            {
                // get vertices and move up
                Point3d ptA = sourceMesh.Vertices[face.A] + moveNormal;
                Point3d ptB = sourceMesh.Vertices[face.B] + moveNormal;
                Point3d ptC = sourceMesh.Vertices[face.C] + moveNormal;

                var noCollision = true;
                // check points
                var checkPts = YieldPointsOnTriangle(ptA, ptB, ptC, ptDensity, edgeDistance);
                foreach (var pt in checkPts)
                {
                    var noCollPt = MeshRayNoCollision(targetMesh, pt, removalVector, out int[] collIndArr);
                    if (!noCollPt)           // noCollision for any point = false
                    {
                        noCollision = false;
                        collFaceIndSet.UnionWith(collIndArr);
                    }
                        

                }
                
                if (noCollision)
                {
                    collFaceIndices = Array.Empty<int>();
                    return true;
                }
                else
                {
                    collFaceIndices = collFaceIndSet.ToArray();
                    return false;
                }

                // noCollision = true for this face
                //collFaceIndices = Array.Empty<int>();
                //return true;
            }
            else
            {
                // if face is Quad, divide into subtriangles ABC and ACD
                Point3d ptA = sourceMesh.Vertices[face.A] + moveNormal;
                Point3d ptB = sourceMesh.Vertices[face.B] + moveNormal;
                Point3d ptC = sourceMesh.Vertices[face.C] + moveNormal;
                Point3d ptD = sourceMesh.Vertices[face.D] + moveNormal;

                var noCollision = true;
                // check points
                var checkPtsFirst = YieldPointsOnTriangle(ptA, ptB, ptC, ptDensity, edgeDistance);
                foreach (var pt in checkPtsFirst)
                {
                    var noCollPt = MeshRayNoCollision(targetMesh, pt, removalVector, out int[] collIndArr);
                    if (!noCollPt)           // noCollision for any point = false
                    {
                        noCollision = false;
                        collFaceIndSet.UnionWith(collIndArr);
                    }
                }

                var checkPtsSecond = YieldPointsOnTriangle(ptA, ptC, ptD, ptDensity, edgeDistance);
                foreach (var pt in checkPtsSecond)
                {
                    var noCollPt = MeshRayNoCollision(targetMesh, pt, removalVector, out int[] collIndArr);
                    if (!noCollPt)           // noCollision = false
                    {
                        noCollision = false;
                        collFaceIndSet.UnionWith(collIndArr);
                    }
                }

                if (noCollision)
                {
                    collFaceIndices = Array.Empty<int>();
                    return true;
                }
                else
                {
                    collFaceIndices = collFaceIndSet.ToArray();
                    return false;
                }
            }
        }

        public static bool MeshRayNoCollision(Mesh mesh, Point3d facePt, Vector3d removalVector)
        {
            var ray = new Ray3d(facePt, removalVector);
            var intersect = Intersection.MeshRay(mesh, ray);
            if (intersect < 0) return true;
            else return false;
        }

        public static bool MeshRayNoCollision(Mesh mesh, Point3d facePt, Vector3d removalVector, out int[] collFaceIndices)
        {
            var ray = new Ray3d(facePt, removalVector);
            var intersect = Intersection.MeshRay(mesh, ray, out int[] mfInd);
            if (intersect < 0)
            {
                // no intersection found -> noCollision = true
                collFaceIndices = Array.Empty<int>();
                return false;
            } else
            {
                // intersection found -> noCollision = false
                collFaceIndices = mfInd;
                return false;
            }
        }

        public static IEnumerable<Point3d> YieldPointsOnTriangle(Point3d ptA, Point3d ptB, Point3d ptC, int ptDensity, double edgeDistance)
        {
            Point3d triCentroid = new Point3d((ptA + ptB + ptC) / 3.0);
            yield return triCentroid;
            if(ptDensity == 0) yield break;         // only return centroid for ptDensity = 0

            // move vertices inward
            var movA = triCentroid - ptA;
            var movB = triCentroid - ptB;
            var movC = triCentroid - ptC;
            movA.Unitize();
            movB.Unitize();
            movC.Unitize();
            movA *= edgeDistance;
            movB *= edgeDistance;
            movC *= edgeDistance;
            // new vertices
            var movedA = ptA + movA;
            var movedB = ptB + movB;
            var movedC = ptC + movC;
            // generate points
            for (int i = 0; i <= ptDensity; i++)
            {
                for (int j = 0; j <= ptDensity - i; j++)
                {
                    int k = ptDensity - i - j;

                    // weights for vertices
                    double u = (double)i / ptDensity;
                    double v = (double)j / ptDensity;
                    double w = (double)k / ptDensity;

                    Point3d pt = movedA * u + movedB * v + movedC * w;
                    yield return pt;
                }
            }
        }

        public static double BoxDiagonalLength(Box box)
        {
            // returns length of diagonal of any box

            var lx = box.X.Length;
            var ly = box.Y.Length;
            var lz = box.Z.Length;
            return Math.Sqrt(lx * lx + ly * ly + lz * lz);
        }

        public static IEnumerable<Box> YieldOrientedBoxes(Mesh mesh, double tolerance, double angleTolerance, GH_Component component)
        {
            // yield returns bounding boxes for a mesh oriented towards all three edges of each mesh face
            // it is assumed mesh is valid

            // get convex hull 3D
            var cbVertices = mesh.Vertices.Select(v => (Point3d)v).ToList();
            var convex3DHull = Mesh.CreateConvexHull3D(cbVertices, out _, tolerance, angleTolerance);
            if (null != convex3DHull && convex3DHull.IsValid) convex3DHull.Compact();
            else
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed generating convex 3D hull.");
                yield return Box.Unset;
                yield break;
            }
            // find oriented face planes
            var hullFaceList = convex3DHull.Faces;
            var planes = new List<Plane>();
            foreach (var face in hullFaceList)
            {
                // get planes oriented to edges
                var aPlane = new Plane(convex3DHull.Vertices[face.A], convex3DHull.Vertices[face.B], convex3DHull.Vertices[face.C]);
                var bPlane = new Plane(convex3DHull.Vertices[face.B], convex3DHull.Vertices[face.C], convex3DHull.Vertices[face.A]);
                var cPlane = new Plane(convex3DHull.Vertices[face.C], convex3DHull.Vertices[face.A], convex3DHull.Vertices[face.B]);

                if (aPlane.IsValid) planes.Add(aPlane);
                if (bPlane.IsValid) planes.Add(bPlane);
                if (cPlane.IsValid) planes.Add(cPlane);
            }

            var orientedBoxes = new List<Box>();
            var hullList = new List<Mesh>();
            // yield bounding box for each plane
            foreach (var plane in planes)
            {
                var bbox = new Box();
                convex3DHull.GetBoundingBox(plane, out bbox);

                yield return bbox;
            }
        }

        public static Brep[] CutBrepByPlane(Brep solid, Plane plane, double tolerance, GH_Component component)
        {
            // cuts a solid Brep by a plane

            // create cutting surface
            var box = solid?.GetBoundingBox(false) ?? BoundingBox.Unset;
            Intersection.PlaneBoundingBox(plane, box, out Polyline pLine);
            if (null == pLine) return new Brep[] { solid };                                 // if no intersection, return input
            var cutterSrf = Brep.CreatePlanarBreps(pLine.ToPolylineCurve(), tolerance);
            if (null == cutterSrf) return new Brep[] { solid };

            var parts = new List<Brep>();
            foreach(var cutter in cutterSrf)
            {
                if(!cutter.IsValid) continue;
                var pieces = Brep.CreateBooleanSplit(solid, cutter, tolerance);
                
                parts.AddRange(pieces);
            }
            return parts.ToArray();
        }

        public static Mesh[] CutMeshByPlane(Mesh solid, Plane plane, bool returnSolid, GH_Component component)
        {
            // cut a solid Mesh by a plane

            var parts = solid.Split(plane);
            if (null == parts || parts.Length == 0) return new Mesh[] {solid};

            if (!returnSolid) return parts;
            for(int pi = 0; pi < parts.Length; pi++)
            {
                if (parts[pi].IsSolid) continue;

                parts[pi].FillHoles();
                parts[pi] = CleanMesh(parts[pi]);
            }
            return parts;
        }

        public static Mesh[] CutMeshByMesh(Mesh solid, Mesh cutter, bool returnSolid, GH_Component component)
        {
            // cut a solid Mesh by another mesh (into solid meshes)

            var partsA = solid.Split(cutter);
            if (null == partsA || partsA.Length == 0) return new Mesh[] { solid };
            if (!returnSolid) return partsA;

            // split cutter
            var partsB = cutter.Split(solid);
            // combine and close
            var allClosed = new List<Mesh>();
            for (int ia = 0; ia < partsA.Length; ia++)
            {
                for (int ib = 0; ib < partsB.Length; ib++)
                {
                    var currentComb = new Mesh();
                    currentComb.Append(partsA[ia]);
                    currentComb.Append(partsB[ib]);
                    currentComb = CleanMesh(currentComb);
                    if (currentComb.IsClosed) allClosed.Add(currentComb);
                }
            }

            return allClosed.ToArray();
        }
    }
}

/// DigitalFormwork
/// SPDX-License-Identifier: GPL-3.0
/// Copyright(c) 2025 Simon M. Hausknecht
