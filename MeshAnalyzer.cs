using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 고막 Mesh 구조 분석 도구
/// Inspector에서 [Analyze Mesh] 버튼을 누르면 mesh 정보가 콘솔에 출력됩니다.
/// </summary>
public class MeshAnalyzer : MonoBehaviour
{
    [Header("Target Mesh")]
    [Tooltip("분석할 MeshFilter를 드래그하세요. 비어있으면 이 GameObject의 MeshFilter를 사용합니다.")]
    public MeshFilter targetMeshFilter;

    [Header("Click to Analyze")]
    public bool clickToAnalyze = false;

    [Header("Analysis Results (Read Only)")]
    [SerializeField] private int vertexCount;
    [SerializeField] private int triangleCount;
    [SerializeField] private int submeshCount;
    [SerializeField] private int connectedComponentCount;
    [SerializeField] private Vector3 boundsSize;
    [SerializeField] private Vector3 boundsCenter;
    [SerializeField] private float surfaceArea;
    [SerializeField] private bool hasUVs;
    [SerializeField] private bool hasNormals;
    [SerializeField] private bool hasColors;

    [Header("Detailed Info")]
    [SerializeField] private List<SubmeshInfo> submeshInfoList = new List<SubmeshInfo>();
    [SerializeField] private List<ComponentInfo> componentInfoList = new List<ComponentInfo>();

    [System.Serializable]
    public class SubmeshInfo
    {
        public int submeshIndex;
        public int triangleCount;
        public int vertexCount;
    }

    [System.Serializable]
    public class ComponentInfo
    {
        public int componentIndex;
        public int vertexCount;
        public int triangleCount;
        public Vector3 centroid;
        public float approximateArea;
    }

    /// <summary>
    /// Inspector 버튼 또는 코드에서 호출하여 mesh 분석 실행
    /// </summary>
    [ContextMenu("Analyze Mesh")]
    public void AnalyzeMesh()
    {
        // MeshFilter 확인
        if (targetMeshFilter == null)
            targetMeshFilter = GetComponent<MeshFilter>();

        if (targetMeshFilter == null)
        {
            Debug.LogError("[MeshAnalyzer] MeshFilter를 찾을 수 없습니다!");
            return;
        }

        Mesh mesh = targetMeshFilter.sharedMesh;
        if (mesh == null)
        {
            Debug.LogError("[MeshAnalyzer] Mesh가 없습니다!");
            return;
        }

        // 기본 정보 수집
        vertexCount = mesh.vertexCount;
        triangleCount = mesh.triangles.Length / 3;
        submeshCount = mesh.subMeshCount;
        boundsSize = mesh.bounds.size;
        boundsCenter = mesh.bounds.center;
        hasUVs = mesh.uv != null && mesh.uv.Length > 0;
        hasNormals = mesh.normals != null && mesh.normals.Length > 0;
        hasColors = mesh.colors != null && mesh.colors.Length > 0;

        // 표면적 계산
        surfaceArea = CalculateSurfaceArea(mesh);

        // Submesh 정보 수집
        AnalyzeSubmeshes(mesh);

        // 연결된 컴포넌트 분석 (파트 수)
        AnalyzeConnectedComponents(mesh);

        // 결과 출력
        PrintAnalysisResults(mesh);
    }

    private float CalculateSurfaceArea(Mesh mesh)
    {
        float area = 0f;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            // 삼각형 면적 = 0.5 * |AB x AC|
            Vector3 cross = Vector3.Cross(v1 - v0, v2 - v0);
            area += cross.magnitude * 0.5f;
        }

        return area;
    }

    private void AnalyzeSubmeshes(Mesh mesh)
    {
        submeshInfoList.Clear();

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            var submesh = mesh.GetSubMesh(i);

            // 해당 submesh에서 사용하는 고유 vertex 수 계산
            int[] indices = mesh.GetTriangles(i);
            HashSet<int> uniqueVertices = new HashSet<int>();
            foreach (int idx in indices)
            {
                uniqueVertices.Add(idx);
            }

            submeshInfoList.Add(new SubmeshInfo
            {
                submeshIndex = i,
                triangleCount = indices.Length / 3,
                vertexCount = uniqueVertices.Count
            });
        }
    }

    private void AnalyzeConnectedComponents(Mesh mesh)
    {
        componentInfoList.Clear();

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Union-Find로 연결된 컴포넌트 찾기
        int[] parent = new int[vertices.Length];
        for (int i = 0; i < parent.Length; i++)
            parent[i] = i;

        // 같은 삼각형에 속한 vertex들을 연결
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Union(parent, triangles[i], triangles[i + 1]);
            Union(parent, triangles[i + 1], triangles[i + 2]);
        }

        // 컴포넌트별 vertex 그룹화
        Dictionary<int, List<int>> components = new Dictionary<int, List<int>>();
        for (int i = 0; i < vertices.Length; i++)
        {
            int root = Find(parent, i);
            if (!components.ContainsKey(root))
                components[root] = new List<int>();
            components[root].Add(i);
        }

        connectedComponentCount = components.Count;

        // 각 컴포넌트 정보 수집
        int compIndex = 0;
        foreach (var kvp in components)
        {
            List<int> vertexIndices = kvp.Value;

            // Centroid 계산
            Vector3 centroid = Vector3.zero;
            foreach (int vi in vertexIndices)
            {
                centroid += vertices[vi];
            }
            centroid /= vertexIndices.Count;

            // 해당 컴포넌트의 삼각형 수 및 면적 계산
            HashSet<int> vertexSet = new HashSet<int>(vertexIndices);
            int triCount = 0;
            float compArea = 0f;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (vertexSet.Contains(triangles[i]))
                {
                    triCount++;
                    Vector3 v0 = vertices[triangles[i]];
                    Vector3 v1 = vertices[triangles[i + 1]];
                    Vector3 v2 = vertices[triangles[i + 2]];
                    compArea += Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
                }
            }

            componentInfoList.Add(new ComponentInfo
            {
                componentIndex = compIndex++,
                vertexCount = vertexIndices.Count,
                triangleCount = triCount,
                centroid = centroid,
                approximateArea = compArea
            });
        }

        // 크기순 정렬
        componentInfoList.Sort((a, b) => b.vertexCount.CompareTo(a.vertexCount));
        for (int i = 0; i < componentInfoList.Count; i++)
        {
            componentInfoList[i].componentIndex = i;
        }
    }

    private int Find(int[] parent, int i)
    {
        if (parent[i] != i)
            parent[i] = Find(parent, parent[i]);
        return parent[i];
    }

    private void Union(int[] parent, int a, int b)
    {
        int rootA = Find(parent, a);
        int rootB = Find(parent, b);
        if (rootA != rootB)
            parent[rootA] = rootB;
    }

    private void PrintAnalysisResults(Mesh mesh)
    {
        string report = $@"
================================================================================
                         MESH ANALYSIS REPORT
================================================================================
Mesh Name: {mesh.name}
GameObject: {gameObject.name}

--------------------------------------------------------------------------------
                           BASIC INFORMATION
--------------------------------------------------------------------------------
Vertex Count:        {vertexCount:N0}
Triangle Count:      {triangleCount:N0}
Submesh Count:       {submeshCount}
Connected Parts:     {connectedComponentCount}

--------------------------------------------------------------------------------
                           GEOMETRY
--------------------------------------------------------------------------------
Bounds Size:         {boundsSize.x:F6} x {boundsSize.y:F6} x {boundsSize.z:F6} (Unity units)
Bounds Center:       ({boundsCenter.x:F6}, {boundsCenter.y:F6}, {boundsCenter.z:F6})
Total Surface Area:  {surfaceArea:F6} (Unity units²)

--------------------------------------------------------------------------------
                           DATA CHANNELS
--------------------------------------------------------------------------------
Has UVs:             {(hasUVs ? "Yes" : "No")}
Has Normals:         {(hasNormals ? "Yes" : "No")}
Has Vertex Colors:   {(hasColors ? "Yes" : "No")}

--------------------------------------------------------------------------------
                           SUBMESH DETAILS
--------------------------------------------------------------------------------";

        for (int i = 0; i < submeshInfoList.Count; i++)
        {
            var info = submeshInfoList[i];
            report += $"\n  Submesh {info.submeshIndex}: {info.triangleCount} triangles, {info.vertexCount} vertices";
        }

        report += @"

--------------------------------------------------------------------------------
                       CONNECTED COMPONENTS (PARTS)
--------------------------------------------------------------------------------";

        for (int i = 0; i < componentInfoList.Count; i++)
        {
            var info = componentInfoList[i];
            report += $@"
  Part {info.componentIndex}:
    - Vertices:  {info.vertexCount}
    - Triangles: {info.triangleCount}
    - Area:      {info.approximateArea:F6}
    - Centroid:  ({info.centroid.x:F4}, {info.centroid.y:F4}, {info.centroid.z:F4})";
        }

        report += @"

================================================================================
                         RECOMMENDATIONS
================================================================================";

        // 추천사항 생성
        if (connectedComponentCount > 1)
        {
            report += $@"
[!] Mesh has {connectedComponentCount} separate parts.
    - If this is the tympanic membrane only, consider separating parts.
    - Largest part (Part 0) likely represents the main membrane.";
        }

        if (vertexCount < 100)
        {
            report += @"
[!] Low vertex count. Perforation simulation may lack detail.
    Consider using a higher-resolution mesh.";
        }
        else if (vertexCount > 10000)
        {
            report += @"
[i] High vertex count. Good for detailed perforation simulation.
    May need optimization for real-time performance.";
        }

        report += @"
================================================================================
";

        Debug.Log(report);
    }

    /// <summary>
    /// 런타임에서 mesh 정보 가져오기
    /// </summary>
    public MeshAnalysisData GetAnalysisData()
    {
        AnalyzeMesh();

        return new MeshAnalysisData
        {
            vertexCount = this.vertexCount,
            triangleCount = this.triangleCount,
            submeshCount = this.submeshCount,
            connectedComponentCount = this.connectedComponentCount,
            boundsSize = this.boundsSize,
            boundsCenter = this.boundsCenter,
            surfaceArea = this.surfaceArea
        };
    }

    [System.Serializable]
    public class MeshAnalysisData
    {
        public int vertexCount;
        public int triangleCount;
        public int submeshCount;
        public int connectedComponentCount;
        public Vector3 boundsSize;
        public Vector3 boundsCenter;
        public float surfaceArea;
    }

    // Inspector에서 체크박스 클릭 시 분석 실행
    private void OnValidate()
    {
        if (clickToAnalyze)
        {
            clickToAnalyze = false;
            AnalyzeMesh();
        }
    }

    // Gizmos로 mesh 구조 시각화
    private void OnDrawGizmosSelected()
    {
        if (targetMeshFilter == null)
            targetMeshFilter = GetComponent<MeshFilter>();

        if (targetMeshFilter == null || targetMeshFilter.sharedMesh == null)
            return;

        Mesh mesh = targetMeshFilter.sharedMesh;

        // Bounds 시각화
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(mesh.bounds.center, mesh.bounds.size);

        // Center point
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mesh.bounds.center, mesh.bounds.size.magnitude * 0.02f);

        // 컴포넌트 centroids
        if (componentInfoList != null)
        {
            Color[] colors = { Color.green, Color.blue, Color.magenta, Color.cyan };
            for (int i = 0; i < componentInfoList.Count; i++)
            {
                Gizmos.color = colors[i % colors.Length];
                Gizmos.DrawWireSphere(componentInfoList[i].centroid, mesh.bounds.size.magnitude * 0.01f);
            }
        }
    }
}
