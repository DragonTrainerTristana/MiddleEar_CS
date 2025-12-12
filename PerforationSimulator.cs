using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 고막 천공 시각화
/// EarSimulator와 연동하여 mesh에 천공을 시각적으로 표시
/// </summary>
public class PerforationSimulator : MonoBehaviour
{
    [Header("=== EarSimulator 연동 ===")]
    [Tooltip("EarSimulator 컴포넌트 (자동 검색 가능)")]
    public EarSimulator earSimulator;

    [Header("=== 시각화 설정 ===")]
    public Color perforationColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color normalMembraneColor = new Color(1f, 0.9f, 0.8f, 1f);
    public bool showPerforationGizmo = true;


    [Header("=== 결과 (읽기 전용) ===")]
    [SerializeField] private int perforationGrade;
    [SerializeField] private float perforationRatio;
    [SerializeField] private float perforationPosX;
    [SerializeField] private float perforationPosY;
    [SerializeField] private int affectedVertexCount;

    // Grade별 천공 비율 (면적 기준)
    // Grade 0: 없음, 1: <25%, 2: 25-50%, 3: 50-75%, 4: >75%
    // 각 등급의 최소값 사용 (시각적으로 적절한 크기)
    private readonly float[] gradeToRatio = { 0f, 0.05f, 0.25f, 0.50f, 0.75f };

    // Mesh 데이터
    private MeshFilter meshFilter;
    private Mesh originalMesh;
    private Mesh workingMesh;
    private Vector3[] originalVertices;
    private Vector3[] currentVertices;
    private Color[] vertexColors;

    // Mesh 분석 결과
    private Vector3 meshCenter;
    private float meshRadius;
    private Vector3 meshNormal;      // Mesh 평면의 법선
    private Vector3 meshTangentX;    // Mesh 평면의 X축 (좌우)
    private Vector3 meshTangentY;    // Mesh 평면의 Y축 (상하)

    // 천공된 vertex 목록
    private List<int> perforatedVertices = new List<int>();

    // 천공 위치 (로컬)
    private Vector3 perforationLocalPos;
    private float perforationRadius;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        // EarSimulator 자동 검색
        if (earSimulator == null)
            earSimulator = FindObjectOfType<EarSimulator>();

        // MeshFilter 가져오기
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("[PerforationSimulator] MeshFilter 또는 Mesh가 없습니다!");
            enabled = false;
            return;
        }

        // Mesh 복사
        originalMesh = meshFilter.sharedMesh;
        workingMesh = Instantiate(originalMesh);
        workingMesh.name = originalMesh.name + "_Perforation";

        originalVertices = originalMesh.vertices;
        currentVertices = (Vector3[])originalVertices.Clone();
        vertexColors = new Color[originalVertices.Length];

        // Mesh 분석
        AnalyzeMesh();

        // 초기 색상
        for (int i = 0; i < vertexColors.Length; i++)
            vertexColors[i] = normalMembraneColor;

        workingMesh.colors = vertexColors;
        meshFilter.mesh = workingMesh;

        Debug.Log("[PerforationSimulator] 초기화 완료");
    }

    void AnalyzeMesh()
    {
        // 중심점 계산
        meshCenter = Vector3.zero;
        foreach (var v in originalVertices)
            meshCenter += v;
        meshCenter /= originalVertices.Length;

        // 반경 계산
        meshRadius = 0f;
        foreach (var v in originalVertices)
        {
            float dist = Vector3.Distance(v, meshCenter);
            if (dist > meshRadius) meshRadius = dist;
        }

        // Mesh 평면의 법선 계산 (삼각형들의 평균 법선)
        int[] triangles = originalMesh.triangles;
        meshNormal = Vector3.zero;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = originalVertices[triangles[i]];
            Vector3 v1 = originalVertices[triangles[i + 1]];
            Vector3 v2 = originalVertices[triangles[i + 2]];

            Vector3 triNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            meshNormal += triNormal;
        }
        meshNormal = meshNormal.normalized;

        // Mesh 평면의 탄젠트 벡터 계산 (법선에 수직인 두 축)
        // 가장 먼 vertex 방향을 X축으로 사용
        Vector3 farthestDir = Vector3.zero;
        float maxDist = 0f;
        foreach (var v in originalVertices)
        {
            Vector3 dir = v - meshCenter;
            // 법선에 투영 제거 (평면 위의 방향만)
            dir = dir - Vector3.Dot(dir, meshNormal) * meshNormal;
            if (dir.magnitude > maxDist)
            {
                maxDist = dir.magnitude;
                farthestDir = dir.normalized;
            }
        }
        meshTangentX = farthestDir;
        meshTangentY = Vector3.Cross(meshNormal, meshTangentX).normalized;

        Debug.Log($"[PerforationSimulator] Mesh 분석 완료 - 중심: {meshCenter}, 반경: {meshRadius:F4}");
        Debug.Log($"[PerforationSimulator] 법선: {meshNormal}, X축: {meshTangentX}, Y축: {meshTangentY}");
    }

    void Update()
    {
        if (earSimulator == null) return;

        // EarSimulator에서 설정값 가져오기
        int newGrade = earSimulator.perforationGrade;
        float newPosX = earSimulator.perforationPosX;
        float newPosY = earSimulator.perforationPosY;

        // 값이 변경되었으면 천공 업데이트
        if (newGrade != perforationGrade ||
            Mathf.Abs(newPosX - perforationPosX) > 0.01f ||
            Mathf.Abs(newPosY - perforationPosY) > 0.01f)
        {
            perforationGrade = newGrade;
            perforationPosX = newPosX;
            perforationPosY = newPosY;
            ApplyPerforation();
        }
    }

    /// <summary>
    /// 천공을 mesh에 적용 (실제 구멍 생성)
    /// </summary>
    public void ApplyPerforation()
    {
        if (originalVertices == null) return;

        // 천공 비율
        perforationRatio = gradeToRatio[Mathf.Clamp(perforationGrade, 0, 4)];

        // 천공 반경 (면적 비율 = 반경² 비율)
        perforationRadius = meshRadius * Mathf.Sqrt(perforationRatio);

        Debug.Log($"[PerforationSimulator] Grade: {perforationGrade}, Ratio: {perforationRatio}, MeshRadius: {meshRadius}, PerforationRadius: {perforationRadius}");

        // Grade 0이면 구멍 없음 - 원본 복원
        if (perforationGrade == 0 || perforationRadius <= 0)
        {
            if (workingMesh != null && originalMesh != null)
            {
                workingMesh.triangles = originalMesh.triangles;
                workingMesh.RecalculateNormals();
            }
            Debug.Log("[PerforationSimulator] Grade 0 - 구멍 없음");
            return;
        }

        // 천공 위치 계산 (Mesh 평면 위에서 계산)
        float offsetX = (perforationPosX - 0.5f) * meshRadius * 1.5f;
        float offsetY = (perforationPosY - 0.5f) * meshRadius * 1.5f;

        // Mesh의 실제 평면 좌표계 사용
        perforationLocalPos = meshCenter + meshTangentX * offsetX + meshTangentY * offsetY;

        Debug.Log($"[PerforationSimulator] 천공 위치: {perforationLocalPos}, Mesh 중심: {meshCenter}, offset: ({offsetX:F4}, {offsetY:F4})");

        // 영향받는 vertex 찾기
        perforatedVertices.Clear();
        for (int i = 0; i < originalVertices.Length; i++)
        {
            float dist = Vector3.Distance(originalVertices[i], perforationLocalPos);
            if (dist <= perforationRadius)
            {
                perforatedVertices.Add(i);
            }
        }

        // Mesh 적용 (Read/Write 체크)
        if (!workingMesh.isReadable)
        {
            Debug.LogWarning("[PerforationSimulator] Mesh가 Read/Write 불가능합니다. Project에서 모델 → Read/Write 체크 필요!");
            return;
        }

        // 원본 삼각형 가져오기
        int[] originalTriangles = originalMesh.triangles;
        List<int> newTriangles = new List<int>();
        int removedCount = 0;
        float minDist = float.MaxValue;
        float maxDist = float.MinValue;

        // 천공 영역의 삼각형 제거 (실제 구멍!)
        for (int i = 0; i < originalTriangles.Length; i += 3)
        {
            int v0 = originalTriangles[i];
            int v1 = originalTriangles[i + 1];
            int v2 = originalTriangles[i + 2];

            // 삼각형 중심점 계산
            Vector3 triCenter = (originalVertices[v0] + originalVertices[v1] + originalVertices[v2]) / 3f;
            float distToHole = Vector3.Distance(triCenter, perforationLocalPos);

            // 거리 통계
            if (distToHole < minDist) minDist = distToHole;
            if (distToHole > maxDist) maxDist = distToHole;

            // 삼각형 중심이 천공 반경 안에 있으면 제거
            if (distToHole < perforationRadius * 0.9f)
            {
                removedCount++;
                continue;
            }

            // 구멍이 아닌 삼각형은 유지
            newTriangles.Add(v0);
            newTriangles.Add(v1);
            newTriangles.Add(v2);
        }

        Debug.Log($"[PerforationSimulator] 삼각형 거리 범위: {minDist:F6} ~ {maxDist:F6}, 천공반경: {perforationRadius:F6}");

        // Vertex 색상 업데이트 (구멍 가장자리 표시)
        for (int i = 0; i < originalVertices.Length; i++)
        {
            float dist = Vector3.Distance(originalVertices[i], perforationLocalPos);

            if (dist <= perforationRadius * 1.2f && dist > perforationRadius * 0.8f)
            {
                // 구멍 가장자리: 빨간색
                vertexColors[i] = new Color(0.8f, 0.2f, 0.2f, 1f);
            }
            else
            {
                vertexColors[i] = normalMembraneColor;
            }

            currentVertices[i] = originalVertices[i];
        }

        // 새 Mesh 적용 - Clear 후 재할당
        workingMesh.Clear();
        workingMesh.vertices = originalVertices;  // 원본 vertex 사용
        workingMesh.triangles = newTriangles.ToArray();
        workingMesh.colors = vertexColors;

        // UV 복사 (있으면)
        if (originalMesh.uv != null && originalMesh.uv.Length > 0)
            workingMesh.uv = originalMesh.uv;

        workingMesh.RecalculateNormals();
        workingMesh.RecalculateBounds();

        // MeshFilter에 강제 재할당
        meshFilter.mesh = workingMesh;

        affectedVertexCount = perforatedVertices.Count;

        // 검증 로그
        Debug.Log($"[PerforationSimulator] 구멍 생성 완료 - 제거된 삼각형: {removedCount}개 (총 {originalTriangles.Length / 3}개 중)");
        Debug.Log($"[PerforationSimulator] 현재 mesh 삼각형 수: {meshFilter.mesh.triangles.Length / 3}개");
    }

    // ==================== Public API ====================

    /// <summary>
    /// 천공된 vertex 목록 반환
    /// </summary>
    public List<int> GetPerforatedVertices()
    {
        return new List<int>(perforatedVertices);
    }

    /// <summary>
    /// 천공 비율 반환
    /// </summary>
    public float GetPerforationRatio()
    {
        return perforationRatio;
    }

    /// <summary>
    /// 천공 위치 (월드 좌표) 반환
    /// </summary>
    public Vector3 GetPerforationWorldPosition()
    {
        return transform.TransformPoint(perforationLocalPos);
    }

    // ==================== Gizmos ====================

    void OnDrawGizmosSelected()
    {
        if (!showPerforationGizmo) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        // Mesh 중심점만 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(meshCenter, meshRadius * 0.03f);

        // 천공 영역 - 와이어만 (구체 X)
        if (perforationGrade > 0 && perforationRadius > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(perforationLocalPos, perforationRadius);
        }
    }
}
