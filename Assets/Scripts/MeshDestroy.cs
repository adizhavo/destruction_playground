using UnityEngine;
using System.Collections.Generic;

public class MeshDestroy
{
    // Configs
    public static float minRigidbodyMassKinetic = 2f;
    public static float minMassDestroy= 0.7f;
    public static float minRigidbodyQuickDestroy = 0.2f;
    public static int subBreakCount = 2;
    public static float boundaryMassMult = 1f;
    
    private static bool edgeSet = false;
    private static Vector3 edgeVertex = Vector3.zero;
    private static Plane edgePlane = new Plane();

    public static List<PartMeshData> BreakGameObject(GameObject gameObject, Vector3 breakPoint, Vector3 breakVector, bool subBreak = true)
    {
        var spawnedPartMeshDatas = new List<PartMeshData>();
        if (gameObject.GetComponent<Rigidbody>().mass > minMassDestroy)
        {
            // breakVector is the normal vector from which a plan will be created to cut the game object
            // the length of the vector defines how deep will the mesh be cut
            var partMesheDatas = GeneratePartMeshes(gameObject, breakPoint, breakVector);
            if (partMesheDatas.Count > 0)
            {
                SpawnParts(gameObject, partMesheDatas);
                spawnedPartMeshDatas.AddRange(partMesheDatas);
                Object.Destroy(gameObject);
                
                if (subBreak)
                {
                    // subbreak the closes 3d model into more fine pieces
                    var closestPartMesh = GetClosestPartMeshDataToPos(breakPoint, partMesheDatas);
                    if (closestPartMesh != null)
                    {
                        var randomPartMeshDatas = GenerateRandomPartMeshes(closestPartMesh.gameObject, subBreakCount);
                        if (randomPartMeshDatas.Count > 0)
                        {
                            SpawnParts(closestPartMesh.gameObject, randomPartMeshDatas);
                            spawnedPartMeshDatas.Remove(closestPartMesh);
                            spawnedPartMeshDatas.AddRange(randomPartMeshDatas);
                            Object.Destroy(closestPartMesh.gameObject);
                        }
                    }
                }
            }
            else
            {
                // If the plan doesn't cut any 3D verts then simply make random cuts in the model
                var randomPartMeshes = GenerateRandomPartMeshes(gameObject, subBreakCount + 1);
                if (randomPartMeshes.Count > 0)
                {
                    SpawnParts(gameObject, randomPartMeshes);
                    spawnedPartMeshDatas.AddRange(randomPartMeshes);
                    Object.Destroy(gameObject);
                }
            }

            TryCleanupPartMeshDatas(spawnedPartMeshDatas);
        }

        return spawnedPartMeshDatas;
    }
    
    // Create the mesh data structure from a game object
    public static PartMeshData CreatePartMeshData(GameObject gameObject)
    {
        var mesh = gameObject.GetComponent<MeshFilter>().mesh;
        mesh.RecalculateBounds();

        var partMeshData = new PartMeshData
        {
            UV = mesh.uv,
            Vertices = mesh.vertices,
            Normals = mesh.normals,
            Triangles = new int[mesh.subMeshCount][],
            gameObject = gameObject
        };
        
        for (int i = 0; i < mesh.subMeshCount; i++)
            partMeshData.Triangles[i] = mesh.GetTriangles(i);

        return partMeshData;
    }

    private static void TryCleanupPartMeshDatas(List<PartMeshData> spawnedPartMeshDatas)
    {
        // Set the property of the resut rigid bodies
        // Fade out and clear the smaller pieces for performance
        foreach (var part in spawnedPartMeshDatas)
        {
            var rigidBody = part.gameObject.GetComponent<Rigidbody>();
            if (rigidBody.mass > minRigidbodyMassKinetic * boundaryMassMult && rigidBody.velocity.sqrMagnitude < Mathf.Epsilon)
            {
                rigidBody.isKinematic = true;
            }

            if (rigidBody.mass < minMassDestroy * boundaryMassMult)
            {
                part.gameObject.AddComponent<ClearGameObject>().SetWaiTime(20f);
            }

            if (rigidBody.mass < minRigidbodyQuickDestroy * boundaryMassMult)
            {
                part.gameObject.AddComponent<ClearGameObject>().SetWaiTime(5f);
            }
        }
    }

    private static List<PartMeshData> GeneratePartMeshes(GameObject gameObject, Vector3 breakPoint, Vector3 breakVector)
    {
        // Crate the mesh data structure out of the main cut object
        var partMeshData = CreatePartMeshData(gameObject);
        var planePoint = breakPoint + breakVector;
        
        #if UNITY_EDITOR
        var planeWorldNormal = breakPoint - gameObject.transform.position;
        DrawPlane(planeWorldNormal, planePoint, Color.green);
        #endif
        
        planePoint = gameObject.transform.InverseTransformPoint(planePoint);
        var planeNormal = gameObject.transform.InverseTransformPoint(breakPoint);
        
        // cut the parts based on the plant point and normal
        return CutPartMeshData(planePoint, planeNormal, partMeshData);
    }
    
    private static List<PartMeshData> GenerateRandomPartMeshes(GameObject cutGameObject, int cutCount)
    {
        // Crate the mesh data structure out of the main cut object
        var meshData = CreatePartMeshData(cutGameObject);
        var partMeshDatas = new List<PartMeshData> {meshData};
        for (int i = 0; i < cutCount; i++)
        {
            var planePoint = cutGameObject.transform.InverseTransformPoint(meshData.MeshWorldCenter);
            var normal = Random.insideUnitSphere;
            partMeshDatas = CutPartMeshData(planePoint, normal, partMeshDatas);
            
            #if UNITY_EDITOR
            DrawPlane(normal - cutGameObject.transform.position, meshData.MeshWorldCenter, Color.green);
            #endif
        }
        return partMeshDatas;
    }

    private static List<PartMeshData> CutPartMeshData(Vector3 planePoint, Vector3 planeNormal, PartMeshData part)
    {
        return CutPartMeshData(planePoint, planeNormal, new List<PartMeshData>{part});
    }

    private static List<PartMeshData> CutPartMeshData(Vector3 planePoint, Vector3 planeNormal, List<PartMeshData> parts)
    {
        var subParts = new List<PartMeshData>();
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
    
    private static void SpawnParts(GameObject gameObject, List<PartMeshData> partMeshDatas)
    {
        foreach (var part in partMeshDatas)
        {
            part.CreateGameObject(gameObject);
        }
    }
    
    private static PartMeshData GetClosestPartMeshDataToPos(Vector3 worldPosition, List<PartMeshData> partMeshDatas)
    {
        PartMeshData closestMeshData = null;
        var minDistance = float.MaxValue;

        foreach (var part in partMeshDatas)
        {
            var sqrDistance = (part.MeshWorldCenter - worldPosition).sqrMagnitude;
            if (closestMeshData == null || sqrDistance < minDistance)
            {
                closestMeshData = part;
                minDistance = sqrDistance;
            }
        }

        return closestMeshData;
    }
    
    private static PartMeshData GenerateMesh(PartMeshData original, Plane plane, bool left)
    {
        var partMeshData = new PartMeshData();
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
                    partMeshData.AddTriangle(i,
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
                        partMeshData,
                        left ? plane.normal * -1f : plane.normal,
                        ray1.origin + ray1.direction.normalized * enter1,
                        ray2.origin + ray2.direction.normalized * enter2,
                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], lerp2));
                
                if (sideCount == 1)
                {
                    partMeshData.AddTriangle(i,
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
                    partMeshData.AddTriangle(i,
                                        ray1.origin + ray1.direction.normalized * enter1,
                                        original.Vertices[triangles[j + ((singleIndex + 1) % 3)]],
                                        original.Vertices[triangles[j + ((singleIndex + 2) % 3)]],
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        original.Normals[triangles[j + ((singleIndex + 1) % 3)]],
                                        original.Normals[triangles[j + ((singleIndex + 2) % 3)]],
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        original.UV[triangles[j + ((singleIndex + 1) % 3)]],
                                        original.UV[triangles[j + ((singleIndex + 2) % 3)]]);
                    partMeshData.AddTriangle(i,
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

        if (partMeshData.IsValid())
        {
            partMeshData.FillArrays();
            return partMeshData;
        }
        
        return null;
    }

    private static void AddEdge(int subMesh, PartMeshData partMeshData, Vector3 normal, Vector3 vertex1, Vector3 vertex2, Vector2 uv1, Vector2 uv2)
    {
        if (!edgeSet)
        {
            edgeSet = true;
            edgeVertex = vertex1;
        }
        else
        {
            edgePlane.Set3Points(edgeVertex, vertex1, vertex2);
            
            // don't give texture to internal meshes
            partMeshData.AddTriangle(subMesh,
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

    public class PartMeshData
    {
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public int[][] Triangles;
        public Vector2[] UV;
        public GameObject gameObject;
        public Vector3 MeshWorldCenter => gameObject.GetComponent<Renderer>().bounds.center;
        
        private List<Vector3> _Verticies = new List<Vector3>();
        private List<Vector3> _Normals = new List<Vector3>();
        private List<List<int>> _Triangles = new List<List<int>>();
        private List<Vector2> _UVs = new List<Vector2>();
        private Vector2 xBoundaries = new Vector2(1000, -1000);
        private Vector2 yBoundaries = new Vector2(1000, -1000);
        private Vector2 zBoundaries = new Vector2(1000, -1000);

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

        public bool IsValid()
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

        public void CreateGameObject(GameObject original)
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
            
            var renderer = gameObject.AddComponent<MeshRenderer>();
            var originalRender = original.GetComponent<MeshRenderer>();
            renderer.materials = originalRender.materials;
            renderer.receiveShadows = originalRender.receiveShadows;
            renderer.lightProbeUsage = originalRender.lightProbeUsage;
            renderer.reflectionProbeUsage = originalRender.reflectionProbeUsage;
            renderer.castShadows = originalRender.castShadows;
            renderer.allowOcclusionWhenDynamic = originalRender.allowOcclusionWhenDynamic;

            var filter = gameObject.AddComponent<MeshFilter>();
            filter.mesh = mesh;

            var collider = gameObject.AddComponent<MeshCollider>();
            collider.convex = true;

            var rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.velocity = original.GetComponent<Rigidbody>().velocity;
            rigidbody.mass = (xBoundaries.y - xBoundaries.x) * (yBoundaries.y - yBoundaries.x) * (zBoundaries.y - zBoundaries.x) * boundaryMassMult;
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
