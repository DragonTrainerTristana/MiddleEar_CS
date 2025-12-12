using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 고막 진동 시뮬레이션 (과학적 모델 기반)
///
/// 참고 논문:
/// - PMC3628134: Vibrations in the human middle ear
/// - PMC2918411: Determinants of Hearing Loss in TM Perforations
///
/// 물리적 파라미터:
/// - 고막 면적: ~55-90 mm² (평균 85mm²)
/// - 고막 두께: ~0.1 mm
/// - 최대 변위: 1-10 μm (정상 대화 수준, 60-70 dB SPL)
/// - 공명 주파수: 800-1600 Hz (평균 ~1000 Hz)
/// </summary>
public class TympanicMembrane : MonoBehaviour
{
    [Header("=== 물리적 파라미터 (논문 기반) ===")]
    [Tooltip("고막 면적 (mm²) - 실제: 55-90, 평균 85")]
    [Range(55f, 90f)]
    public float membraneAreaMM2 = 85f;

    [Tooltip("고막 두께 (mm) - 실제: ~0.1")]
    [Range(0.05f, 0.15f)]
    public float membraneThicknessMM = 0.1f;

    [Tooltip("최대 변위 (μm) - 실제: 1-10μm at 60-70dB SPL")]
    [Range(1f, 20f)]
    public float maxDisplacementMicron = 10f;

    [Tooltip("공명 주파수 (Hz) - 실제: 800-1600Hz")]
    [Range(800f, 1600f)]
    public float resonanceFrequency = 1000f;

    [Tooltip("Q 팩터 (공명 날카로움) - 실제: 1-3")]
    [Range(1f, 3f)]
    public float qFactor = 2f;

    [Header("=== 재질 속성 ===")]
    [Tooltip("막의 장력 계수")]
    [Range(0.1f, 2.0f)]
    public float tension = 1.0f;

    [Tooltip("댐핑 계수 (감쇠)")]
    [Range(0.1f, 1.0f)]
    public float damping = 0.3f;

    [Tooltip("밀도 (kg/m³) - 실제: ~1100")]
    public float density = 1100f;

    [Header("=== 진동 모드 가중치 (PMC3628134 기반) ===")]
    [Tooltip("피스톤 모드 - 저주파 우세")]
    [Range(0f, 1f)]
    public float pistonModeWeight = 1.0f;

    [Tooltip("첫 번째 고유모드 - 중주파")]
    [Range(0f, 1f)]
    public float firstModeWeight = 0.6f;

    [Tooltip("두 번째 고유모드 - 고주파")]
    [Range(0f, 1f)]
    public float secondModeWeight = 0.3f;

    [Header("=== 상태 (읽기 전용) ===")]
    [SerializeField] private float currentDisplacementMicron;
    [SerializeField] private float inputAmplitude;
    [SerializeField] private float inputFrequency;
    [SerializeField] private float frequencyResponse;
    [SerializeField] private string currentMode;

    // Mesh 데이터
    private MeshFilter meshFilter;
    private Mesh originalMesh;
    private Vector3[] originalVertices;
    private Vector3[] currentVertices;
    private Vector3[] velocities;

    // 분석 데이터
    private Vector3 centerPoint;
    private float maxDistanceFromCenter;
    private float maxDisplacementMeters; // 미터 단위로 변환

    // 천공 데이터
    private List<int> perforatedVertices = new List<int>();
    private PerforationSimulator perforationSimulator;

    // 시간 기반 진동
    private float phase = 0f;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("[TympanicMembrane] MeshFilter 또는 Mesh가 없습니다!");
            enabled = false;
            return;
        }

        // Mesh 복사 (런타임 변형용)
        originalMesh = meshFilter.sharedMesh;
        meshFilter.mesh = Instantiate(originalMesh);

        originalVertices = originalMesh.vertices;
        currentVertices = (Vector3[])originalVertices.Clone();
        velocities = new Vector3[originalVertices.Length];

        // 중심점 계산
        CalculateMeshCenter();

        // 최대 변위를 미터로 변환 (μm → m)
        maxDisplacementMeters = maxDisplacementMicron * 1e-6f;

        // PerforationSimulator 연동
        perforationSimulator = GetComponent<PerforationSimulator>();

        Debug.Log($"[TympanicMembrane] 초기화 완료 - 공명주파수: {resonanceFrequency}Hz, 최대변위: {maxDisplacementMicron}μm");
    }

    void CalculateMeshCenter()
    {
        centerPoint = Vector3.zero;
        foreach (var v in originalVertices)
            centerPoint += v;
        centerPoint /= originalVertices.Length;

        maxDistanceFromCenter = 0f;
        foreach (var v in originalVertices)
        {
            float dist = Vector3.Distance(v, centerPoint);
            if (dist > maxDistanceFromCenter)
                maxDistanceFromCenter = dist;
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        // 최대 변위 업데이트
        maxDisplacementMeters = maxDisplacementMicron * 1e-6f;

        // 천공된 vertex 업데이트
        if (perforationSimulator != null)
        {
            perforatedVertices = perforationSimulator.GetPerforatedVertices();
        }

        UpdateVibration();
        ApplyMeshDeformation();
    }

    void UpdateVibration()
    {
        if (inputAmplitude <= 0f)
        {
            // 진동 없으면 원래 위치로 서서히 복귀
            for (int i = 0; i < currentVertices.Length; i++)
            {
                currentVertices[i] = Vector3.Lerp(currentVertices[i], originalVertices[i], Time.deltaTime * 10f);
                velocities[i] *= 0.9f;
            }
            currentDisplacementMicron = 0f;
            currentMode = "Resting";
            return;
        }

        // 주파수 응답 계산 (2차 시스템 - 공명 특성)
        frequencyResponse = CalculateFrequencyResponse(inputFrequency);

        // 현재 진동 모드 결정
        currentMode = DetermineVibrationMode(inputFrequency);

        // 위상 업데이트
        phase += inputFrequency * Time.deltaTime * 2f * Mathf.PI;
        if (phase > 2f * Mathf.PI) phase -= 2f * Mathf.PI;

        float deltaTime = Time.deltaTime;

        for (int i = 0; i < currentVertices.Length; i++)
        {
            // 천공된 vertex는 진동하지 않음 (에너지 누출)
            if (perforatedVertices.Contains(i))
            {
                currentVertices[i] = originalVertices[i];
                velocities[i] = Vector3.zero;
                continue;
            }

            Vector3 originalPos = originalVertices[i];

            // 중심에서의 거리 (정규화)
            float distFromCenter = Vector3.Distance(originalPos, centerPoint);
            float normalizedDist = distFromCenter / maxDistanceFromCenter;

            // 진동량 계산 (주파수별 모드 적용)
            float vibrationAmount = CalculateVibrationAtVertex(normalizedDist, inputFrequency);

            // 실제 변위량 (논문 기반: 최대 10μm)
            float actualDisplacement = inputAmplitude * frequencyResponse * vibrationAmount * maxDisplacementMeters;

            // 목표 위치
            Vector3 targetPos = originalPos + transform.forward * actualDisplacement * Mathf.Sin(phase);

            // 스프링-댐퍼 시스템
            Vector3 springForce = tension * 1000f * (targetPos - currentVertices[i]);
            Vector3 dampingForce = -damping * 100f * velocities[i];
            Vector3 totalForce = springForce + dampingForce;

            // 질량 기반 가속도 (면적 분포)
            float vertexMass = (density * membraneAreaMM2 * 1e-6f * membraneThicknessMM * 1e-3f) / originalVertices.Length;
            Vector3 acceleration = totalForce / Mathf.Max(vertexMass, 1e-10f);

            // 속도 및 위치 업데이트
            velocities[i] += acceleration * deltaTime;
            currentVertices[i] += velocities[i] * deltaTime;

            // NaN 체크 - 문제 발생시 원래 위치로 복구
            if (float.IsNaN(currentVertices[i].x) || float.IsNaN(currentVertices[i].y) || float.IsNaN(currentVertices[i].z) ||
                float.IsInfinity(currentVertices[i].x) || float.IsInfinity(currentVertices[i].y) || float.IsInfinity(currentVertices[i].z))
            {
                currentVertices[i] = originalPos;
                velocities[i] = Vector3.zero;
                continue;
            }

            // 최대 변위 제한
            Vector3 displacement = currentVertices[i] - originalPos;
            if (displacement.magnitude > maxDisplacementMeters * 2f)
            {
                displacement = displacement.normalized * maxDisplacementMeters * 2f;
                currentVertices[i] = originalPos + displacement;
                velocities[i] *= 0.5f; // 에너지 손실
            }
        }

        // 현재 평균 변위 계산 (μm 단위로 표시)
        currentDisplacementMicron = CalculateAverageDisplacement() * 1e6f;
    }

    /// <summary>
    /// 주파수 응답 계산 (2차 공명 시스템)
    /// 논문 기반: 700-1200Hz에서 공명
    /// </summary>
    float CalculateFrequencyResponse(float frequency)
    {
        float omega = frequency / resonanceFrequency;

        // 2차 시스템 전달함수: H(s) = 1 / (1 - ω² + j*ω/Q)
        float denominator = Mathf.Sqrt(
            Mathf.Pow(1f - omega * omega, 2) +
            Mathf.Pow(omega / qFactor, 2)
        );

        float response = 1f / Mathf.Max(denominator, 0.1f);

        // 응답 정규화 (최대 1.5배까지)
        return Mathf.Clamp(response, 0.1f, 1.5f);
    }

    /// <summary>
    /// 진동 모드 결정 (주파수 기반)
    /// </summary>
    string DetermineVibrationMode(float frequency)
    {
        if (frequency < 500f) return "Piston Mode";
        else if (frequency < 2000f) return "Mixed Mode";
        else return "Complex Mode";
    }

    /// <summary>
    /// 정점별 진동량 계산 (모드 기반)
    /// PMC3628134 참조: 저주파=피스톤, 고주파=복잡모드
    /// </summary>
    float CalculateVibrationAtVertex(float normalizedDistance, float frequency)
    {
        // 모드별 진동 패턴
        float pistonMode = 1.0f; // 전체 균일
        float firstMode = 1.0f - normalizedDistance * 0.7f; // 중심이 크게
        float secondMode = Mathf.Cos(normalizedDistance * Mathf.PI) * (1.0f - normalizedDistance * 0.5f);

        // 주파수에 따른 모드 혼합
        float freqRatio = frequency / resonanceFrequency;
        float combinedMode;

        if (freqRatio < 0.5f)
        {
            // 저주파: 피스톤 모드 우세 (전체가 같이 움직임)
            combinedMode = pistonMode * pistonModeWeight;
        }
        else if (freqRatio < 2.0f)
        {
            // 공명 근처: 첫 번째 고유모드 혼합
            float blend = (freqRatio - 0.5f) / 1.5f;
            combinedMode = Mathf.Lerp(
                pistonMode * pistonModeWeight,
                firstMode * firstModeWeight,
                blend
            );
        }
        else
        {
            // 고주파: 복잡한 모드 혼합
            float blend = Mathf.Clamp01((freqRatio - 2.0f) / 2.0f);
            combinedMode = Mathf.Lerp(
                firstMode * firstModeWeight,
                secondMode * secondModeWeight,
                blend
            );
        }

        // 가장자리는 고정 (annulus)
        float edgeFactor = 1.0f - Mathf.Pow(normalizedDistance, 3);

        return combinedMode * edgeFactor;
    }

    float CalculateAverageDisplacement()
    {
        float totalDisplacement = 0f;
        int count = 0;

        for (int i = 0; i < currentVertices.Length; i++)
        {
            if (!perforatedVertices.Contains(i))
            {
                totalDisplacement += (currentVertices[i] - originalVertices[i]).magnitude;
                count++;
            }
        }

        return count > 0 ? totalDisplacement / count : 0f;
    }

    void ApplyMeshDeformation()
    {
        if (meshFilter == null || meshFilter.mesh == null) return;

        // Mesh가 읽기 가능한지 확인
        if (!meshFilter.mesh.isReadable)
        {
            Debug.LogWarning("[TympanicMembrane] Mesh가 Read/Write 불가능합니다. Project에서 모델 선택 → Import Settings → Read/Write Enabled 체크 필요");
            return;
        }

        meshFilter.mesh.vertices = currentVertices;
        meshFilter.mesh.RecalculateNormals();
        meshFilter.mesh.RecalculateBounds();
    }

    // ==================== Public API ====================

    /// <summary>
    /// 진동 적용 (EarSimulator에서 호출)
    /// amplitude: 0-1 정규화된 입력 강도
    /// frequency: Hz
    /// </summary>
    public void ApplyVibration(float amplitude, float frequency)
    {
        inputAmplitude = Mathf.Clamp01(amplitude);
        inputFrequency = Mathf.Clamp(frequency, 20f, 20000f);
    }

    /// <summary>
    /// 현재 변위 반환 (μm)
    /// </summary>
    public float GetCurrentDisplacementMicron()
    {
        return currentDisplacementMicron;
    }

    /// <summary>
    /// 현재 주파수 응답 반환
    /// </summary>
    public float GetFrequencyResponse()
    {
        return frequencyResponse;
    }

    /// <summary>
    /// 등자뼈로 전달되는 진동량 반환 (이소골 연동용)
    /// </summary>
    public float GetTransmittedVibration()
    {
        // 천공으로 인한 손실 고려
        float perforationLoss = 1f;
        if (perforatedVertices.Count > 0 && originalVertices.Length > 0)
        {
            perforationLoss = 1f - ((float)perforatedVertices.Count / originalVertices.Length);
        }

        return inputAmplitude * frequencyResponse * perforationLoss;
    }

    /// <summary>
    /// 천공 vertex 설정
    /// </summary>
    public void SetPerforation(List<int> perforatedVertexIndices)
    {
        perforatedVertices = perforatedVertexIndices ?? new List<int>();
    }

    // ==================== Gizmos ====================

    void OnDrawGizmosSelected()
    {
        if (originalVertices == null || !Application.isPlaying) return;

        // 중심점
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.TransformPoint(centerPoint), 0.0005f);

        // 진동 시각화 (확대해서 표시)
        Gizmos.color = Color.green;
        float visualScale = 10000f; // 변위가 μm 단위라 크게 확대

        for (int i = 0; i < currentVertices.Length; i += Mathf.Max(1, currentVertices.Length / 50))
        {
            Vector3 worldPos = transform.TransformPoint(currentVertices[i]);
            Vector3 displacement = currentVertices[i] - originalVertices[i];

            if (displacement.magnitude > 1e-9f)
            {
                Gizmos.DrawLine(worldPos, worldPos + displacement * visualScale);
            }
        }
    }
}
