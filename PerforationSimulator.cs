using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 고막 천공 시뮬레이터
/// 천공 크기(0~4 단계)와 위치를 설정하고 mesh에 적용
/// </summary>
public class PerforationSimulator : MonoBehaviour
{
    [Header("Target Mesh")]
    public MeshFilter tympanicMembraneMesh;

    [Header("Perforation Settings")]
    [Tooltip("0: none, 1: <25%, 2: 25-50%, 3: 50-75%, 4: >75%")]
    [Range(0, 4)]
    public int perforationGrade = 0;

    [Tooltip("천공 중심 위치 (정규화된 좌표, 0~1)")]
    [Range(0f, 1f)]
    public float perforationPositionX = 0.5f;
    [Range(0f, 1f)]
    public float perforationPositionY = 0.5f;

    [Header("Perforation Data (Read Only)")]
    [SerializeField] private float perforationRatio;      // 실제 천공 비율 (0~1)
    [SerializeField] private float perforationAreaMM2;    // 천공 면적 (mm²)
    [SerializeField] private float remainingAreaMM2;      // 남은 고막 면적 (mm²)
    [SerializeField] private int affectedVertexCount;     // 영향받는 vertex 수

    [Header("Visualization")]
    public Color perforationColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color normalMembraneColor = new Color(1f, 0.9f, 0.8f, 1f);
    public bool showPerforationGizmo = true;

    // Grade별 천공 비율 (의학 데이터 기반)
    private readonly float[] gradeToRatio = { 0f, 0.125f, 0.375f, 0.625f, 0.85f };

    // 실제 고막 면적 (평균): 약 85mm² (성인 기준)
    private const float REAL_TYMPANIC_AREA_MM2 = 85f;

    // Private variables
    private Mesh originalMesh;
    private Mesh workingMesh;
    private Vector3[] originalVertices;
    private Vector3[] currentVertices;
    private Color[] vertexColors;
    private float totalMeshArea;
    private Vector3 meshCenter;
    private float meshRadius;
    private List<int> perforatedVertices = new List<int>();

    // 천공 위치 (world space)
    private Vector3 perforationCenter;
    private float perforationRadius;

    void Start()
    {
        InitializeMesh();
    }

    void InitializeMesh()
    {
        if (tympanicMembraneMesh == null)
            tympanicMembraneMesh = GetComponent<MeshFilter>();

        if (tympanicMembraneMesh == null || tympanicMembraneMesh.sharedMesh == null)
        {
            Debug.LogError("[PerforationSimulator] MeshFilter 또는 Mesh가 없습니다!");
            return;
        }

        // 원본 mesh 복사
        originalMesh = tympanicMembraneMesh.sharedMesh;
        workingMesh = Instantiate(originalMesh);
        workingMesh.name = originalMesh.name + "_Working";

        originalVertices = originalMesh.vertices;
        currentVertices = (Vector3[])originalVertices.Clone();
        vertexColors = new Color[originalVertices.Length];

        // Mesh 분석
        AnalyzeMesh();

        // 초기 색상 설정
        for (int i = 0; i < vertexColors.Length; i++)
            vertexColors[i] = normalMembraneColor;

        workingMesh.colors = vertexColors;
        tympanicMembraneMesh.mesh = workingMesh;

        // 초기 천공 적용
        ApplyPerforation();
    }

    void AnalyzeMesh()
    {
        // Mesh 중심 계산
        meshCenter = Vector3.zero;
        foreach (var v in originalVertices)
            meshCenter += v;
        meshCenter /= originalVertices.Length;

        // Mesh 반경 계산 (중심에서 가장 먼 vertex까지)
        meshRadius = 0f;
        foreach (var v in originalVertices)
        {
            float dist = Vector3.Distance(v, meshCenter);
            if (dist > meshRadius) meshRadius = dist;
        }

        // 총 면적 계산
        totalMeshArea = CalculateMeshArea(originalMesh);

        Debug.Log($"[PerforationSimulator] Mesh 분석 완료: Center={meshCenter}, Radius={meshRadius:F4}, Area={totalMeshArea:F4}");
    }

    float CalculateMeshArea(Mesh mesh)
    {
        float area = 0f;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];
            area += Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
        }
        return area;
    }

    /// <summary>
    /// 현재 설정에 따라 천공 적용
    /// </summary>
    [ContextMenu("Apply Perforation")]
    public void ApplyPerforation()
    {
        if (originalVertices == null) return;

        // Grade에 따른 천공 비율 결정
        perforationRatio = gradeToRatio[perforationGrade];

        // 천공 반경 계산 (면적 비율 = 반경² 비율)
        perforationRadius = meshRadius * Mathf.Sqrt(perforationRatio);

        // 천공 중심 계산 (정규화된 위치를 mesh 좌표로 변환)
        Vector3 offset = new Vector3(
            (perforationPositionX - 0.5f) * meshRadius * 1.5f,
            (perforationPositionY - 0.5f) * meshRadius * 1.5f,
            0f
        );
        perforationCenter = meshCenter + offset;

        // 영향받는 vertex 찾기
        perforatedVertices.Clear();
        for (int i = 0; i < originalVertices.Length; i++)
        {
            float dist = Vector3.Distance(originalVertices[i], perforationCenter);
            if (dist <= perforationRadius)
            {
                perforatedVertices.Add(i);
            }
        }

        // Vertex 색상 및 위치 업데이트
        for (int i = 0; i < originalVertices.Length; i++)
        {
            if (perforatedVertices.Contains(i))
            {
                // 천공된 영역: 색상 변경 + 약간 들어가게
                vertexColors[i] = perforationColor;

                float dist = Vector3.Distance(originalVertices[i], perforationCenter);
                float normalizedDist = dist / perforationRadius;
                float depression = (1f - normalizedDist) * 0.002f; // 최대 2mm 깊이

                currentVertices[i] = originalVertices[i] - transform.forward * depression;
            }
            else
            {
                vertexColors[i] = normalMembraneColor;
                currentVertices[i] = originalVertices[i];
            }
        }

        // Mesh 업데이트
        workingMesh.vertices = currentVertices;
        workingMesh.colors = vertexColors;
        workingMesh.RecalculateNormals();
        workingMesh.RecalculateBounds();

        // 통계 업데이트
        affectedVertexCount = perforatedVertices.Count;
        perforationAreaMM2 = REAL_TYMPANIC_AREA_MM2 * perforationRatio;
        remainingAreaMM2 = REAL_TYMPANIC_AREA_MM2 - perforationAreaMM2;

        Debug.Log($"[PerforationSimulator] Grade {perforationGrade}: " +
                  $"천공 {perforationRatio:P1} ({perforationAreaMM2:F1}mm²), " +
                  $"남은 면적 {remainingAreaMM2:F1}mm², " +
                  $"영향 vertex {affectedVertexCount}개");
    }

    /// <summary>
    /// Inspector에서 값이 변경될 때 자동 적용
    /// </summary>
    void OnValidate()
    {
        if (Application.isPlaying && originalVertices != null)
        {
            ApplyPerforation();
        }
    }

    // ===================== Public API =====================

    /// <summary>
    /// 천공 등급 설정 (0~4)
    /// </summary>
    public void SetPerforationGrade(int grade)
    {
        perforationGrade = Mathf.Clamp(grade, 0, 4);
        ApplyPerforation();
    }

    /// <summary>
    /// 천공 비율 반환 (0~1)
    /// </summary>
    public float GetPerforationRatio()
    {
        return perforationRatio;
    }

    /// <summary>
    /// 남은 고막 비율 반환 (0~1)
    /// </summary>
    public float GetRemainingRatio()
    {
        return 1f - perforationRatio;
    }

    /// <summary>
    /// 천공 면적 반환 (mm²)
    /// </summary>
    public float GetPerforationAreaMM2()
    {
        return perforationAreaMM2;
    }

    /// <summary>
    /// 남은 고막 면적 반환 (mm²)
    /// </summary>
    public float GetRemainingAreaMM2()
    {
        return remainingAreaMM2;
    }

    /// <summary>
    /// 천공된 vertex 인덱스 목록 반환
    /// </summary>
    public List<int> GetPerforatedVertices()
    {
        return new List<int>(perforatedVertices);
    }

    /// <summary>
    /// 특정 위치가 천공 영역인지 확인
    /// </summary>
    public bool IsPerforated(Vector3 localPosition)
    {
        return Vector3.Distance(localPosition, perforationCenter) <= perforationRadius;
    }

    // ===================== Gizmos =====================

    void OnDrawGizmosSelected()
    {
        if (!showPerforationGizmo) return;

        // Mesh 경계 표시
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;

        if (meshRadius > 0)
        {
            Gizmos.DrawWireSphere(meshCenter, meshRadius);
        }

        // 천공 영역 표시
        if (perforationGrade > 0)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawSphere(perforationCenter, perforationRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(perforationCenter, perforationRadius);
        }

        // 천공 중심점
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(perforationCenter, meshRadius * 0.02f);
    }
}
