using System.Collections.Generic;
using UnityEngine;

public class MeshDestroy
{
    private static bool edgeSet = false;
    private static Vector3 edgeVertex = Vector3.zero;
    private static Vector2 edgeUV = Vector2.zero;
    private static Plane edgePlane = new Plane();

    public static float minRigidbodyMassKinetic = 2f;
    public static float minMassDestroy= 0.7f;
    public static float minRigidbodyQuickDestroy = 0.2f;
    public static int subBreakCount = 2;

    public static List<PartMesh> BreakGameObject(GameObject hitGameObject, Vector3 hitPoint, Vector3 direction, bool subBreak = true)
    {
        var spawnedParts = new List<PartMesh>();
        var hitPointParts = GenerateHitPointCutMesh(hitGameObject, hitPoint, direction);
        if (hitPointParts.Count > 0)
        {
            SpawnParts(hitGameObject, hitPointParts);
            spawnedParts.AddRange(hitPointParts);
            if (subBreak)
            {
                var closestPart = GetClosestPartToHitPoint(hitPoint, hitPointParts);
                if (closestPart != null)
                {
                    var randomParts = GenerateRandomCutMesh(closestPart.gameObject, subBreakCount);
                    if (randomParts.Count > 0)
                    {
                        SpawnParts(closestPart.gameObject, randomParts);
                        spawnedParts.Remove(closestPart);
                        spawnedParts.AddRange(randomParts);
                    }
                }
            }
        }
        else
        {
            var randomParts = GenerateRandomCutMesh(hitGameObject.gameObject, subBreakCount + 1);
            if (randomParts.Count > 0)
            {
                SpawnParts(hitGameObject.gameObject, randomParts);
                spawnedParts.AddRange(randomParts);
            }
        }
        
        foreach (var part in spawnedParts)
        {
            var rigidBody = part.gameObject.GetComponent<Rigidbody>();
            if (rigidBody.mass > minRigidbodyMassKinetic && rigidBody.velocity.sqrMagnitude < 0.5f) rigidBody.isKinematic = true;
            if (rigidBody.mass < minMassDestroy) part.gameObject.AddComponent<ClearGameObject>().SetWaiTime(20f);
            if (rigidBody.mass < minRigidbodyQuickDestroy) part.gameObject.AddComponent<ClearGameObject>().SetWaiTime(5f);
        }

        return spawnedParts;
    }

    private static List<PartMesh> GenerateHitPointCutMesh(GameObject cutGameObject, Vector3 hitPoint, Vector3 direction)
    {
        var parts = CreatePartMeshData(cutGameObject);
        var planePoint = hitPoint + direction;
        
        #if UNITY_EDITOR
        var planeWorldNormal = hitPoint - cutGameObject.transform.position;
        DrawPlane(planeWorldNormal, planePoint, Color.green);
        #endif
        
        planePoint = cutGameObject.transform.InverseTransformPoint(planePoint);
        var planeNormal = cutGameObject.transform.InverseTransformPoint(hitPoint);
        
        parts = CutPartMeshData(planePoint, planeNormal, parts);
        return parts;
    }
    
    private static List<PartMesh> GenerateRandomCutMesh(GameObject cutGameObject, int cutCount)
    {
        var parts = CreatePartMeshData(cutGameObject);
        var centerOfMesh = cutGameObject.transform.TransformPoint(parts[0].MeshLocalCenter);
//        var startNormal = Random.insideUnitSphere;
        for (int i = 0; i < cutCount; i++)
        {
            var planePoint = cutGameObject.transform.InverseTransformPoint(centerOfMesh);
            var normal = Random.insideUnitSphere;
            parts = CutPartMeshData(planePoint, normal, parts);
            
            #if UNITY_EDITOR
            DrawPlane(normal - cutGameObject.transform.position, centerOfMesh, Color.green);
            #endif
        }
        return parts;
    }
    
    private static List<PartMesh> CreatePartMeshData(GameObject hitGameObject)
    {
        var originalMesh = hitGameObject.GetComponent<MeshFilter>().mesh;
        originalMesh.RecalculateBounds();
        var parts = new List<PartMesh>();

        var mainPart = new PartMesh
        {
            UV = originalMesh.uv,
            Vertices = originalMesh.vertices,
            Normals = originalMesh.normals,
            Triangles = new int[originalMesh.subMeshCount][]
        };
        
        for (int i = 0; i < originalMesh.subMeshCount; i++)
            mainPart.Triangles[i] = originalMesh.GetTriangles(i);

        mainPart.UpdateMeshLocalCenter();
        parts.Add(mainPart);
        return parts;
    }

    private static List<PartMesh> CutPartMeshData(Vector3 planePoint, Vector3 planeNormal, List<PartMesh> parts)
    {
        var subParts = new List<PartMesh>();
        for (var i = 0; i < parts.Count; i++)
        {   
            var plane = new Plane(planeNormal, planePoint);
            var meshLeft = GenerateMesh(parts[i], plane, true);
            var meshRight = GenerateMesh(parts[i], plane, false);

            if (meshLeft != null && meshRight != null)
            {
                subParts.Add(meshLeft);
                subParts.Add(meshRight);
            }
        }
        return subParts;
    }
    
    private static void SpawnParts(GameObject hitGameObject, List<PartMesh> parts)
    {
        foreach (var part in parts)
        {
            part.MakeGameobject(hitGameObject);
        }

        GameObject.Destroy(hitGameObject);
    }
    
    private static PartMesh GetClosestPartToHitPoint(Vector3 hitPoint, List<PartMesh> parts)
    {
        PartMesh closestPart = null;
        var minDistance = float.MaxValue;

        foreach (var part in parts)
        {
            var sqrDistance = (part.MeshCenterToWord - hitPoint).sqrMagnitude;
            if (closestPart == null || sqrDistance < minDistance)
            {
                closestPart = part;
                minDistance = sqrDistance;
            }
        }

        return closestPart;
    }
    
    private static PartMesh GenerateMesh(PartMesh original, Plane plane, bool left)
    {
        var partMesh = new PartMesh();
        var ray1 = new Ray();
        var ray2 = new Ray();

        for (var i = 0; i < original.Triangles.Length; i++)
        {
            var triangles = original.Triangles[i];
            edgeSet = false;

            for (var j = 0; j < triangles.Length; j = j + 3)
            {
                var sideA = plane.GetSide(original.Vertices[triangles[j]]) == left;
                var sideB = plane.GetSide(original.Vertices[triangles[j + 1]]) == left;
                var sideC = plane.GetSide(original.Vertices[triangles[j + 2]]) == left;

                var sideCount = (sideA ? 1 : 0) + (sideB ? 1 : 0) + (sideC ? 1 : 0);
                if (sideCount == 0)
                {
                    continue;
                }
                if (sideCount == 3)
                {
                    partMesh.AddTriangle(i,
                                         original.Vertices[triangles[j]], original.Vertices[triangles[j + 1]], original.Vertices[triangles[j + 2]],
                                         original.Normals[triangles[j]], original.Normals[triangles[j + 1]], original.Normals[triangles[j + 2]],
                                         original.UV[triangles[j]], original.UV[triangles[j + 1]], original.UV[triangles[j + 2]]);
                    continue;
                }

                //cut points
                var singleIndex = sideB == sideC ? 0 : sideA == sideC ? 1 : 2;

                ray1.origin = original.Vertices[triangles[j + singleIndex]];
                var dir1 = original.Vertices[triangles[j + ((singleIndex + 1) % 3)]] - original.Vertices[triangles[j + singleIndex]];
                ray1.direction = dir1;
                plane.Raycast(ray1, out var enter1);
                var lerp1 = enter1 / dir1.magnitude;

                ray2.origin = original.Vertices[triangles[j + singleIndex]];
                var dir2 = original.Vertices[triangles[j + ((singleIndex + 2) % 3)]] - original.Vertices[triangles[j + singleIndex]];
                ray2.direction = dir2;
                plane.Raycast(ray2, out var enter2);
                var lerp2 = enter2 / dir2.magnitude;

                AddEdge(i,
                        partMesh,
                        left ? plane.normal * -1f : plane.normal,
                        ray1.origin + ray1.direction.normalized * enter1,
                        ray2.origin + ray2.direction.normalized * enter2,
                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], lerp2));
                
                if (sideCount == 1)
                {
                    partMesh.AddTriangle(i,
                                        original.Vertices[triangles[j + singleIndex]],
                                        ray1.origin + ray1.direction.normalized * enter1,
                                        ray2.origin + ray2.direction.normalized * enter2,
                                        original.Normals[triangles[j + singleIndex]],
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 2) % 3)]], lerp2),
                                        original.UV[triangles[j + singleIndex]],
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], lerp2));
                    
                    continue;
                }

                if (sideCount == 2)
                {
                    partMesh.AddTriangle(i,
                                        ray1.origin + ray1.direction.normalized * enter1,
                                        original.Vertices[triangles[j + ((singleIndex + 1) % 3)]],
                                        original.Vertices[triangles[j + ((singleIndex + 2) % 3)]],
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        original.Normals[triangles[j + ((singleIndex + 1) % 3)]],
                                        original.Normals[triangles[j + ((singleIndex + 2) % 3)]],
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        original.UV[triangles[j + ((singleIndex + 1) % 3)]],
                                        original.UV[triangles[j + ((singleIndex + 2) % 3)]]);
                    partMesh.AddTriangle(i,
                                        ray1.origin + ray1.direction.normalized * enter1,
                                        original.Vertices[triangles[j + ((singleIndex + 2) % 3)]],
                                        ray2.origin + ray2.direction.normalized * enter2,
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        original.Normals[triangles[j + ((singleIndex + 2) % 3)]],
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 2) % 3)]], lerp2),
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        original.UV[triangles[j + ((singleIndex + 2) % 3)]],
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], lerp2));
                    continue;
                }
            }
        }

        if (partMesh.IsValidMesh())
        {
            partMesh.FillArrays();
            return partMesh;
        }
        
        return null;
    }

    private static void AddEdge(int subMesh, PartMesh partMesh, Vector3 normal, Vector3 vertex1, Vector3 vertex2, Vector2 uv1, Vector2 uv2)
    {
        if (!edgeSet)
        {
            edgeSet = true;
            edgeVertex = vertex1;
            edgeUV = uv1;
        }
        else
        {
            edgePlane.Set3Points(edgeVertex, vertex1, vertex2);
            
            // don't give texture to internal meshes
            partMesh.AddTriangle(subMesh,
            edgeVertex,
            edgePlane.GetSide(edgeVertex + normal) ? vertex1 : vertex2,
            edgePlane.GetSide(edgeVertex + normal) ? vertex2 : vertex1,
            normal,
            normal,
            normal,
            Vector2.zero,
            Vector2.zero,
            Vector2.zero);
        }
    }
    
    private static  void DrawPlane(Vector3 normal, Vector3 position, Color color)
    {
        Vector3 v3;
        if (normal.normalized != Vector3.forward) v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude;
        else v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude;
        var corner0 = position + v3;
        var corner2 = position - v3;
        var q = Quaternion.AngleAxis(90f, normal);
        v3 = q * v3;
        var corner1 = position + v3;
        var corner3 = position - v3;
        Debug.DrawLine(corner0, corner2, color);
        Debug.DrawLine(corner1, corner3, color);
        Debug.DrawLine(corner0, corner1, color);
        Debug.DrawLine(corner1, corner2, color);
        Debug.DrawLine(corner2, corner3, color);
        Debug.DrawLine(corner3, corner0, color);
        Debug.DrawLine(Camera.main.transform.position, position, color);
        Debug.DrawRay(position, normal, Color.red);
    }

    public class PartMesh
    {
        private List<Vector3> _Verticies = new List<Vector3>();
        private List<Vector3> _Normals = new List<Vector3>();
        private List<List<int>> _Triangles = new List<List<int>>();
        private List<Vector2> _UVs = new List<Vector2>();
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public int[][] Triangles;
        public Vector2[] UV;
        public GameObject gameObject;
        private Vector2 xBoundaries = new Vector2(1000, -1000);
        private Vector2 yBoundaries = new Vector2(1000, -1000);
        private Vector2 zBoundaries = new Vector2(1000, -1000);

        public Vector3 MeshLocalCenter;
        public Vector3 MeshCenterToWord => gameObject.transform.TransformPoint(MeshLocalCenter / _UVs.Count);

        public void UpdateMeshLocalCenter()
        {
            foreach (var verticy in Vertices)
            {
                MeshLocalCenter += verticy;
            }

            MeshLocalCenter /= Vertices.Length;
        }

        public void AddTriangle(int submesh, Vector3 vert1, Vector3 vert2, Vector3 vert3, Vector3 normal1, Vector3 normal2, Vector3 normal3, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            if (_Triangles.Count - 1 < submesh)
                _Triangles.Add(new List<int>());

            _Triangles[submesh].Add(_Verticies.Count);
            _Verticies.Add(vert1);
            _Triangles[submesh].Add(_Verticies.Count);
            _Verticies.Add(vert2);
            _Triangles[submesh].Add(_Verticies.Count);
            _Verticies.Add(vert3);
            _Normals.Add(normal1);
            _Normals.Add(normal2);
            _Normals.Add(normal3);
            _UVs.Add(uv1);
            _UVs.Add(uv2);
            _UVs.Add(uv3);

            MeshLocalCenter += vert1 + vert2 + vert3;

            xBoundaries.x = Mathf.Min(xBoundaries.x, vert1.x);
            xBoundaries.y = Mathf.Max(xBoundaries.y, vert1.x);
            
            xBoundaries.x = Mathf.Min(xBoundaries.x, vert2.x);
            xBoundaries.y = Mathf.Max(xBoundaries.y, vert2.x);
            
            xBoundaries.x = Mathf.Min(xBoundaries.x, vert3.x);
            xBoundaries.y = Mathf.Max(xBoundaries.y, vert3.x);
            
            yBoundaries.x = Mathf.Min(yBoundaries.x, vert1.y);
            yBoundaries.y = Mathf.Max(yBoundaries.y, vert1.y);
            
            yBoundaries.x = Mathf.Min(yBoundaries.x, vert2.y);
            yBoundaries.y = Mathf.Max(yBoundaries.y, vert2.y);
            
            yBoundaries.x = Mathf.Min(yBoundaries.x, vert3.y);
            yBoundaries.y = Mathf.Max(yBoundaries.y, vert3.y);
            
            zBoundaries.x = Mathf.Min(zBoundaries.x, vert1.z);
            zBoundaries.y = Mathf.Max(zBoundaries.y, vert1.z);
            
            zBoundaries.x = Mathf.Min(zBoundaries.x, vert2.z);
            zBoundaries.y = Mathf.Max(zBoundaries.y, vert2.z);
            
            zBoundaries.x = Mathf.Min(zBoundaries.x, vert3.z);
            zBoundaries.y = Mathf.Max(zBoundaries.y, vert3.z);
        }

        public bool IsValidMesh()
        {
            return _Triangles.Count > 0;
        }

        public void FillArrays()
        {
            Vertices = _Verticies.ToArray();
            Normals = _Normals.ToArray();
            UV = _UVs.ToArray();
            Triangles = new int[_Triangles.Count][];
            for (var i = 0; i < _Triangles.Count; i++)
                Triangles[i] = _Triangles[i].ToArray();
        }

        public void MakeGameobject(GameObject original)
        {
            gameObject = new GameObject(original.name);
            gameObject.transform.position = original.transform.position;
            gameObject.transform.rotation = original.transform.rotation;
            gameObject.transform.localScale = original.transform.localScale;

            var mesh = new Mesh();
            mesh.name = original.GetComponent<MeshFilter>().mesh.name;

            mesh.vertices = Vertices;
            mesh.normals = Normals;
            mesh.uv = UV;
            for(var i = 0; i < Triangles.Length; i++)
                mesh.SetTriangles(Triangles[i], i, true);
            
//            mesh.bounds = new Bounds(MeshLocalCenter, boundBox);
            
            var renderer = gameObject.AddComponent<MeshRenderer>();
            var originalRender = original.GetComponent<MeshRenderer>();
            renderer.materials = originalRender.materials;
            renderer.receiveShadows = originalRender.receiveShadows;
            renderer.lightProbeUsage = originalRender.lightProbeUsage;
            renderer.reflectionProbeUsage = originalRender.reflectionProbeUsage;
            renderer.castShadows = originalRender.castShadows;

            var filter = gameObject.AddComponent<MeshFilter>();
            filter.mesh = mesh;

            var collider = gameObject.AddComponent<MeshCollider>();
            collider.convex = true;

            var rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.velocity = original.GetComponent<Rigidbody>().velocity;
            rigidbody.mass = (xBoundaries.y - xBoundaries.x) * (yBoundaries.y - yBoundaries.x) * (zBoundaries.y - zBoundaries.x);
            var clearComponent = original.GetComponent<ClearGameObject>();
            if (clearComponent != null)
            {
                var clear = gameObject.AddComponent<ClearGameObject>();
                clear.fadeTime = clearComponent.fadeTime;
                clear.waitTime = clearComponent.waitTime;
            }
        }
    }
}
