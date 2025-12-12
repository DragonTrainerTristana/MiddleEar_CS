using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// 고막 천공 시뮬레이터 메인 컨트롤러 (과학적 모델 기반)
///
/// 참고 논문:
/// - PMC2918411: Determinants of Hearing Loss in TM Perforations
/// - PMC6640663: Middle Ear Mechanics
/// - PMC1868690: Middle Ear Volume Effects
///
/// 물리 모델:
/// ABG = BaseLoss(size) + VolumeEffect + PositionWeight + FrequencyModulation
///
/// 핵심 파라미터:
/// - 중이강 볼륨: 0.5-2.0 cm³ (평균 0.7-1.0 cm³)
/// - 볼륨 효과: 10-20 dB 영향
/// - 천공 크기-ABG 관계: 비선형 (논문 기반)
/// </summary>
public class EarSimulator : MonoBehaviour
{
    [Header("=== 오브젝트 참조 ===")]
    [Tooltip("고막 오브젝트 (MeshFilter 필요)")]
    public GameObject tympanicMembrane;

    [Tooltip("이소골 오브젝트")]
    public GameObject ossicle;

    [Tooltip("달팽이관 오브젝트 (Observer 위치)")]
    public GameObject cochlea;

    [Tooltip("실험 실행기 (자동 검색)")]
    public ExperimentRunner experimentRunner;

    [Header("=== 천공 설정 ===")]
    [Tooltip("천공 등급: 0=없음, 1=<25%, 2=25-50%, 3=50-75%, 4=>75%")]
    [Range(0, 4)]
    public int perforationGrade = 0;

    [Tooltip("천공 위치 X (0=전방/Anterior, 1=후방/Posterior)")]
    [Range(0f, 1f)]
    public float perforationPosX = 0.5f;

    [Tooltip("천공 위치 Y (0=하방/Inferior, 1=상방/Superior)")]
    [Range(0f, 1f)]
    public float perforationPosY = 0.5f;

    [Header("=== 테스트 주파수 ===")]
    [Tooltip("현재 테스트 주파수 (Hz)")]
    public float testFrequency = 1000f;

    [Header("=== 물리 파라미터 (논문 기반) ===")]
    [Tooltip("중이강 볼륨 (cm³) - 실제: 0.5-2.0, 평균 0.7-1.0")]
    [Range(0.3f, 2.5f)]
    public float middleEarVolumeCm3 = 0.8f;

    [Tooltip("입력 음압 레벨 (dB SPL)")]
    [Range(40f, 100f)]
    public float inputSPL = 70f;

    [Tooltip("고막 면적 (mm²) - 실제: 55-90, 평균 85")]
    [Range(55f, 90f)]
    public float tympanicAreaMM2 = 85f;

    [Header("=== 시각화 ===")]
    public bool showVibrationPath = true;
    public bool showGizmos = true;
    public Color pathColor = Color.cyan;
    public LineRenderer vibrationPathLine;

    [Header("=== 결과 (읽기 전용) ===")]
    [SerializeField] private float currentABG_dB;
    [SerializeField] private float volumeEffect_dB;
    [SerializeField] private float sizeEffect_dB;
    [SerializeField] private float positionEffect_dB;
    [SerializeField] private string severityLevel;
    [SerializeField] private float transmittedAmplitude;

    // 표준 청력검사 주파수 (Hz)
    private readonly float[] standardFrequencies = { 125f, 250f, 500f, 1000f, 2000f, 3000f, 4000f, 8000f };

    // 등급별 천공 비율 (면적비)
    private readonly float[] gradeToRatio = { 0f, 0.125f, 0.375f, 0.625f, 0.875f };

    // 문헌 기반 ABG 참고값 (논문 데이터)
    // 출처: Voss et al. 2001, Mehta et al. 2006, Ahmad & Ramani 1979
    // 순수 천공 환자 기준 - 물리 모델 보정용 (50% 가중치)
    // 저주파 ABG 상향 조정 (임상 데이터 반영)
    private readonly float[,] literatureABG = new float[5, 8]
    {
        // 125Hz, 250Hz, 500Hz, 1kHz, 2kHz, 3kHz, 4kHz, 8kHz
        { 0, 0, 0, 0, 0, 0, 0, 0 },             // Grade 0: No perforation
        { 18, 16, 14, 10, 8, 8, 10, 6 },        // Grade 1: <25% - 저주파 상향
        { 35, 32, 28, 22, 18, 20, 22, 14 },     // Grade 2: 25-50% - 저주파 상향
        { 48, 44, 38, 32, 26, 28, 30, 22 },     // Grade 3: 50-75% - Peak
        { 32, 28, 15, 20, 14, 22, 30, 16 }      // Grade 4: >75% - Round Window Shielding
    };

    // 컴포넌트 참조
    private MeshFilter tympanicMeshFilter;
    private TympanicMembrane tympanicMembraneScript;
    private OssicleVibration ossicleVibrationScript;

    // Mesh 분석 데이터
    private Vector3 meshCenter;
    private float meshRadius;
    private float meshTotalArea;

    // 계산된 위치
    private Vector3 sourcePosition;
    private Vector3 perforationWorldPos;
    private float[] calculatedABG;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        calculatedABG = new float[standardFrequencies.Length];

        // 고막 컴포넌트 연결
        if (tympanicMembrane != null)
        {
            tympanicMeshFilter = tympanicMembrane.GetComponent<MeshFilter>();
            tympanicMembraneScript = tympanicMembrane.GetComponent<TympanicMembrane>();

            if (tympanicMeshFilter != null && tympanicMeshFilter.sharedMesh != null)
            {
                AnalyzeMesh();
            }
        }

        // 이소골 컴포넌트 연결
        if (ossicle != null)
        {
            ossicleVibrationScript = ossicle.GetComponent<OssicleVibration>();
        }

        SetupLineRenderer();

        // ExperimentRunner 자동 검색
        if (experimentRunner == null)
            experimentRunner = FindObjectOfType<ExperimentRunner>();

        Debug.Log($"[EarSimulator] 초기화 완료 - 중이볼륨: {middleEarVolumeCm3}cm³, 고막면적: {tympanicAreaMM2}mm²");
    }

    void AnalyzeMesh()
    {
        Mesh mesh = tympanicMeshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;

        // 중심점 계산
        meshCenter = Vector3.zero;
        foreach (var v in vertices)
            meshCenter += v;
        meshCenter /= vertices.Length;

        // 반경 계산
        meshRadius = 0f;
        foreach (var v in vertices)
        {
            float dist = Vector3.Distance(v, meshCenter);
            if (dist > meshRadius) meshRadius = dist;
        }

        // 표면적 계산
        meshTotalArea = CalculateMeshArea(mesh);

        Debug.Log($"[EarSimulator] Mesh 분석: Radius={meshRadius:F4}m, Area={meshTotalArea * 1e6f:F2}mm²");
    }

    float CalculateMeshArea(Mesh mesh)
    {
        float area = 0f;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];
            area += Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
        }
        return area;
    }

    void SetupLineRenderer()
    {
        if (vibrationPathLine == null)
        {
            vibrationPathLine = gameObject.AddComponent<LineRenderer>();
        }

        vibrationPathLine.startWidth = 0.002f;
        vibrationPathLine.endWidth = 0.002f;
        vibrationPathLine.material = new Material(Shader.Find("Sprites/Default"));
        vibrationPathLine.startColor = pathColor;
        vibrationPathLine.endColor = pathColor;
        vibrationPathLine.positionCount = 4;
    }

    void Update()
    {
        UpdatePositions();
        CalculateCurrentABG();
        UpdateVibrationComponents();
        UpdateVisualization();
    }

    void UpdatePositions()
    {
        if (tympanicMembrane == null) return;

        // Source: 고막 앞 2cm (외이도 끝)
        sourcePosition = tympanicMembrane.transform.position +
                        tympanicMembrane.transform.forward * 0.02f;

        // 천공 위치 (로컬 → 월드)
        Vector3 localPerforationPos = meshCenter + new Vector3(
            (perforationPosX - 0.5f) * meshRadius * 1.8f,
            (perforationPosY - 0.5f) * meshRadius * 1.8f,
            0f
        );
        perforationWorldPos = tympanicMembrane.transform.TransformPoint(localPerforationPos);
    }

    void CalculateCurrentABG()
    {
        currentABG_dB = CalculateAirBoneGap(testFrequency);
        severityLevel = GetSeverityLevel(currentABG_dB);
    }

    /// <summary>
    /// Air-Bone Gap 계산 (과학적 모델)
    /// 논문 기반: ABG = f(size, volume, position, frequency)
    /// </summary>
    public float CalculateAirBoneGap(float frequency)
    {
        if (perforationGrade == 0) return 0f;

        float perforationRatio = gradeToRatio[perforationGrade];

        // ========== 1. 크기 효과 (Size Effect) ==========
        // 논문: ABG는 천공 크기에 비례하되, 비선형
        // 작은 천공: ABG 증가 빠름, 큰 천공: ABG 증가 느림 (log 관계)
        sizeEffect_dB = CalculateSizeEffect(perforationRatio, frequency);

        // ========== 2. 볼륨 효과 (Middle Ear Volume Effect) ==========
        // 논문 (PMC1868690): 중이강 볼륨이 작으면 ABG 증가
        // V_normal = 0.8 cm³ 기준
        volumeEffect_dB = CalculateVolumeEffect(perforationRatio);

        // ========== 3. 위치 효과 (Position Effect) ==========
        // 논문: 후방(Posterior) 천공이 전방보다 청력손실 큼
        // 이유: 이소골 연결점(umbo)이 후상방에 위치
        positionEffect_dB = CalculatePositionEffect();

        // ========== 4. 주파수 변조 (Frequency Modulation) ==========
        float freqModulation = CalculateFrequencyModulation(frequency);

        // ========== 5. Round Window Shielding Effect ==========
        // 큰 천공(>75%)에서 ABG가 오히려 감소하는 현상
        float roundWindowShielding = CalculateRoundWindowShielding(perforationRatio, frequency);

        // ========== 6. 최종 ABG 계산 ==========
        // 물리 모델 50% + 문헌 데이터 50% (균형)
        float literatureValue = GetLiteratureABG(perforationGrade, frequency);
        float physicalValue = (sizeEffect_dB + volumeEffect_dB) * freqModulation + positionEffect_dB;

        // Round Window Shielding 적용 (ABG 감소) - 둘 다 적용
        physicalValue = Mathf.Max(0f, physicalValue - roundWindowShielding);
        literatureValue = Mathf.Max(0f, literatureValue - roundWindowShielding * 0.5f);

        // 가중 평균: 균형 (50:50)
        float finalABG = physicalValue * 0.5f + literatureValue * 0.5f;

        return Mathf.Clamp(finalABG, 0f, 60f);
    }

    /// <summary>
    /// 크기 효과 계산
    /// 논문 기반: 천공 면적과 ABG는 log 관계
    /// 저주파에서 ABG 영향 강화 (임상 데이터 반영)
    /// </summary>
    float CalculateSizeEffect(float perforationRatio, float frequency)
    {
        if (perforationRatio <= 0f) return 0f;

        // 천공 면적 (mm²)
        float holeArea = perforationRatio * tympanicAreaMM2;

        // 음향 질량과 저항 계산 (간소화)
        // 논문: 작은 구멍은 높은 음향 저항, 저주파 영향 큼
        float effectiveRadius = Mathf.Sqrt(holeArea / Mathf.PI) / 1000f; // mm → m

        // 기본 크기 손실 (비선형)
        // 10*log10(S_hole/S_total) 근사
        float sizeLoss = 20f * Mathf.Log10(1f / (1f - perforationRatio + 0.01f));

        // 저주파에서 더 큰 손실 (계수 강화)
        float freqFactor = 1f;
        if (frequency < 500f)
            freqFactor = 1.5f;      // 상향: 1.3 → 1.5
        else if (frequency < 1000f)
            freqFactor = 1.25f;     // 상향: 1.15 → 1.25
        else if (frequency > 2000f)
            freqFactor = 0.85f;

        return Mathf.Clamp(sizeLoss * freqFactor, 0f, 50f);
    }

    /// <summary>
    /// 중이강 볼륨 효과 계산
    /// 논문: 볼륨 감소 → 강성 증가 → 저주파 ABG 증가
    /// </summary>
    float CalculateVolumeEffect(float perforationRatio)
    {
        // 정상 볼륨 기준: 0.8 cm³
        float normalVolume = 0.8f;

        // 볼륨 비율
        float volumeRatio = middleEarVolumeCm3 / normalVolume;

        // 천공으로 인해 중이강이 외이도와 연결 → 유효 볼륨 증가
        float effectiveVolume = middleEarVolumeCm3 * (1f + perforationRatio * 0.5f);
        float effectiveRatio = effectiveVolume / normalVolume;

        // 볼륨이 작으면 ABG 증가 (강성 증가)
        // 볼륨이 크면 ABG 증가 (압력 손실)
        float volumeEffect;
        if (effectiveRatio < 1f)
        {
            // 작은 볼륨: 저주파 손실 증가
            volumeEffect = (1f - effectiveRatio) * 15f;
        }
        else
        {
            // 큰 볼륨 (천공으로 연결): 전반적 손실
            volumeEffect = (effectiveRatio - 1f) * 10f;
        }

        return Mathf.Clamp(volumeEffect, -5f, 20f);
    }

    /// <summary>
    /// 위치 효과 계산
    /// 논문: 후방 천공 > 전방 천공 (약 5-10dB 차이)
    /// 출처: Mehta et al. 2006, Bigelow et al. 1998
    /// </summary>
    float CalculatePositionEffect()
    {
        // perforationPosX: 0=전방(Anterior), 1=후방(Posterior)
        // perforationPosY: 0=하방(Inferior), 1=상방(Superior)

        // 기본 위치 효과 (-1 ~ +1 정규화)
        float anteriorPosterior = (perforationPosX - 0.5f) * 2f;
        float inferiorSuperior = (perforationPosY - 0.5f) * 2f;

        // 전후방 효과: 후방이 더 큰 손실
        // 논문: 후방 천공은 이소골(umbo)에 가까워 진동 전달 방해
        // 전방: -4dB (기준보다 낮음), 후방: +6dB (기준보다 높음)
        float posEffect = anteriorPosterior * 5f;

        // 상하방 효과: 상방이 더 큰 손실 (이소골 연결점)
        // 상방: +4dB, 하방: -2dB
        posEffect += inferiorSuperior * 3f;

        // 후상방 4분면: 최대 손실 (이소골 연결점 = umbo 근처)
        // 추가 +3dB
        if (anteriorPosterior > 0.2f && inferiorSuperior > 0.2f)
        {
            float quadrantEffect = (anteriorPosterior - 0.2f) * (inferiorSuperior - 0.2f) * 10f;
            posEffect += Mathf.Min(quadrantEffect, 4f);
        }

        // 전하방 4분면: 최소 손실 (이소골에서 가장 멀음)
        // 추가 -2dB
        if (anteriorPosterior < -0.2f && inferiorSuperior < -0.2f)
        {
            posEffect -= 2f;
        }

        return Mathf.Clamp(posEffect, -8f, 12f);
    }

    /// <summary>
    /// 주파수 변조 계수
    /// 논문: 저주파에서 천공 영향이 더 큼
    /// 임상 데이터 반영하여 저주파 계수 상향
    /// </summary>
    float CalculateFrequencyModulation(float frequency)
    {
        // 1000Hz를 기준(1.0)으로
        // 저주파 계수 강화 (임상 데이터 250-500Hz에서 ABG 높음)
        if (frequency < 250f)
            return 1.4f;      // 상향: 1.25 → 1.4
        else if (frequency < 500f)
            return 1.3f;      // 상향: 1.15 → 1.3
        else if (frequency < 1000f)
            return 1.15f;     // 상향: 1.05 → 1.15
        else if (frequency < 2000f)
            return 1.0f;
        else if (frequency < 4000f)
            return 0.9f;
        else
            return 0.8f;
    }

    /// <summary>
    /// Round Window Shielding Effect (정원창 차폐 효과)
    ///
    /// 큰 천공(>75%)에서 ABG가 오히려 감소하는 현상:
    /// 1. 소리가 중이강을 통해 정원창에 직접 도달
    /// 2. 난원창과 정원창이 동위상으로 진동 → 일부 상쇄
    /// 3. 직접 이소골 자극 가능 (고막 bypass)
    ///
    /// 참고: Voss et al. 2001, Merchant et al. 1997
    /// </summary>
    float CalculateRoundWindowShielding(float perforationRatio, float frequency)
    {
        // 천공이 70% 미만이면 효과 없음
        if (perforationRatio < 0.7f) return 0f;

        // 천공 비율이 클수록 shielding 효과 증가
        // 0.7~1.0 범위에서 0~1로 정규화
        float shieldingStrength = (perforationRatio - 0.7f) / 0.3f;
        shieldingStrength = Mathf.Clamp01(shieldingStrength);

        // 주파수별 shielding 효과
        // ICW6 환자 데이터 기반: 500Hz에서 ABG 5dB로 가장 낮음
        // 저주파에서 직접 이소골 전달 효과가 가장 큼
        float freqFactor;
        if (frequency < 500f)
            freqFactor = 1.2f;      // 저주파: 매우 높은 효과 (직접 이소골 전달)
        else if (frequency < 1000f)
            freqFactor = 1.0f;      // 중저주파: 높은 효과
        else if (frequency < 2000f)
            freqFactor = 0.8f;      // 중주파: 중간 효과
        else if (frequency < 4000f)
            freqFactor = 0.5f;      // 고주파: 낮은 효과
        else
            freqFactor = 0.3f;      // 초고주파: 최소 효과

        // 최대 20dB까지 ABG 감소 (실제 환자 데이터 기반)
        // ICW6: 시뮬 ~30dB → 실제 ~19dB = 약 11dB 감소 필요
        // 저주파에서는 더 큰 감소 필요 (500Hz: 시뮬 ~17dB → 실제 5dB)
        float shieldingReduction = shieldingStrength * freqFactor * 20f;

        return shieldingReduction;
    }

    /// <summary>
    /// 문헌 기반 ABG 참고값 조회
    /// 물리 모델 보정용 (30% 가중치)
    /// </summary>
    float GetLiteratureABG(int grade, float frequency)
    {
        // 주파수 인덱스
        int freqIdx = 0;
        float minDiff = float.MaxValue;
        for (int i = 0; i < standardFrequencies.Length; i++)
        {
            float diff = Mathf.Abs(frequency - standardFrequencies[i]);
            if (diff < minDiff)
            {
                minDiff = diff;
                freqIdx = i;
            }
        }

        return literatureABG[Mathf.Clamp(grade, 0, 4), freqIdx];
    }

    string GetSeverityLevel(float abg)
    {
        if (abg < 15) return "Normal";
        if (abg < 25) return "Mild";
        if (abg < 40) return "Moderate";
        if (abg < 55) return "Moderately Severe";
        if (abg < 70) return "Severe";
        return "Profound";
    }

    /// <summary>
    /// 진동 컴포넌트 업데이트
    /// </summary>
    void UpdateVibrationComponents()
    {
        // ABG에 따른 진동 감쇠 계산
        float attenuation = Mathf.Pow(10f, -currentABG_dB / 20f);
        float normalizedInput = (inputSPL - 40f) / 60f; // 40-100 dB → 0-1
        transmittedAmplitude = normalizedInput * attenuation;

        // 고막 진동 적용
        if (tympanicMembraneScript != null)
        {
            tympanicMembraneScript.ApplyVibration(transmittedAmplitude, testFrequency);
        }

        // 이소골 주파수 설정
        if (ossicleVibrationScript != null)
        {
            ossicleVibrationScript.SetFrequency(testFrequency);
        }
    }

    void UpdateVisualization()
    {
        if (!showVibrationPath || vibrationPathLine == null) return;

        Vector3[] positions = new Vector3[4];
        positions[0] = sourcePosition;
        positions[1] = tympanicMembrane != null ? tympanicMembrane.transform.position : sourcePosition;
        positions[2] = ossicle != null ? ossicle.transform.position : positions[1];
        positions[3] = cochlea != null ? cochlea.transform.position : positions[2];

        vibrationPathLine.SetPositions(positions);

        // ABG에 따른 색상 (녹색=정상, 빨간색=손실큼)
        float normalizedABG = Mathf.Clamp01(currentABG_dB / 50f);
        Color currentColor = Color.Lerp(Color.green, Color.red, normalizedABG);
        vibrationPathLine.startColor = currentColor;
        vibrationPathLine.endColor = currentColor;
    }

    // ==================== Public API ====================

    /// <summary>
    /// 모든 주파수에서 ABG 계산 및 결과 출력
    /// </summary>
    [ContextMenu("Calculate All Frequencies")]
    public void CalculateAllFrequencies()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("\n============ Air-Bone Gap Results ============");
        sb.AppendLine($"Grade: {perforationGrade} ({gradeToRatio[perforationGrade]:P0} perforation)");
        sb.AppendLine($"Position: X={perforationPosX:F2} (Ant-Post), Y={perforationPosY:F2} (Inf-Sup)");
        sb.AppendLine($"Middle Ear Volume: {middleEarVolumeCm3:F2} cm³");
        sb.AppendLine($"Tympanic Area: {tympanicAreaMM2:F1} mm²");
        sb.AppendLine("-----------------------------------------------");
        sb.AppendLine("Freq(Hz)\tABG(dB)\tSeverity");

        float sumLow = 0f, sumMid = 0f, sumHigh = 0f;

        for (int i = 0; i < standardFrequencies.Length; i++)
        {
            calculatedABG[i] = CalculateAirBoneGap(standardFrequencies[i]);
            string severity = GetSeverityLevel(calculatedABG[i]);
            sb.AppendLine($"{standardFrequencies[i]:F0}\t\t{calculatedABG[i]:F1}\t{severity}");

            if (i < 3) sumLow += calculatedABG[i];
            else if (i < 5) sumMid += calculatedABG[i];
            else sumHigh += calculatedABG[i];
        }

        float avgLow = sumLow / 3f;
        float avgMid = sumMid / 2f;
        float avgHigh = sumHigh / 3f;
        float avgTotal = 0f;
        foreach (var abg in calculatedABG) avgTotal += abg;
        avgTotal /= calculatedABG.Length;

        sb.AppendLine("-----------------------------------------------");
        sb.AppendLine($"Low Avg (125-500Hz): {avgLow:F1} dB");
        sb.AppendLine($"Mid Avg (1k-2kHz): {avgMid:F1} dB");
        sb.AppendLine($"High Avg (3k-8kHz): {avgHigh:F1} dB");
        sb.AppendLine($"Total Average: {avgTotal:F1} dB ({GetSeverityLevel(avgTotal)})");
        sb.AppendLine("==============================================");

        Debug.Log(sb.ToString());

        // 한 줄 요약
        Debug.Log($"[RESULT] Grade{perforationGrade} Pos({perforationPosX:F1},{perforationPosY:F1}) Vol={middleEarVolumeCm3:F1}cm³ | Low:{avgLow:F1}dB Mid:{avgMid:F1}dB High:{avgHigh:F1}dB | Total:{avgTotal:F1}dB ({GetSeverityLevel(avgTotal)})");
    }

    /// <summary>
    /// CSV 파일로 저장
    /// </summary>
    [ContextMenu("Export to CSV")]
    public void ExportToCSV()
    {
        for (int i = 0; i < standardFrequencies.Length; i++)
        {
            calculatedABG[i] = CalculateAirBoneGap(standardFrequencies[i]);
        }

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"ABG_Result_{timestamp}.csv";
        string filePath = Path.Combine(Application.dataPath, fileName);

        StringBuilder csv = new StringBuilder();

        // 헤더
        csv.AppendLine("Frequency_Hz,ABG_dB,Severity,Grade,PosX,PosY,MiddleEarVolume_cm3,TympanicArea_mm2");

        // 데이터
        for (int i = 0; i < standardFrequencies.Length; i++)
        {
            string severity = GetSeverityLevel(calculatedABG[i]);
            csv.AppendLine($"{standardFrequencies[i]:F0},{calculatedABG[i]:F2},{severity},{perforationGrade},{perforationPosX:F2},{perforationPosY:F2},{middleEarVolumeCm3:F2},{tympanicAreaMM2:F1}");
        }

        // 평균값
        float avgLow = (calculatedABG[0] + calculatedABG[1] + calculatedABG[2]) / 3f;
        float avgMid = (calculatedABG[3] + calculatedABG[4]) / 2f;
        float avgHigh = (calculatedABG[5] + calculatedABG[6] + calculatedABG[7]) / 3f;

        csv.AppendLine($"AVG_LOW,{avgLow:F2},{GetSeverityLevel(avgLow)},{perforationGrade},{perforationPosX:F2},{perforationPosY:F2},{middleEarVolumeCm3:F2},{tympanicAreaMM2:F1}");
        csv.AppendLine($"AVG_MID,{avgMid:F2},{GetSeverityLevel(avgMid)},{perforationGrade},{perforationPosX:F2},{perforationPosY:F2},{middleEarVolumeCm3:F2},{tympanicAreaMM2:F1}");
        csv.AppendLine($"AVG_HIGH,{avgHigh:F2},{GetSeverityLevel(avgHigh)},{perforationGrade},{perforationPosX:F2},{perforationPosY:F2},{middleEarVolumeCm3:F2},{tympanicAreaMM2:F1}");

        File.WriteAllText(filePath, csv.ToString());

        Debug.Log($"[CSV] Saved: {filePath}");
        Debug.Log($"[RESULT] Grade{perforationGrade} Pos({perforationPosX:F1},{perforationPosY:F1}) | Low:{avgLow:F1}dB Mid:{avgMid:F1}dB High:{avgHigh:F1}dB");
    }

    /// <summary>
    /// 천공 등급 설정
    /// </summary>
    public void SetPerforationGrade(int grade)
    {
        perforationGrade = Mathf.Clamp(grade, 0, 4);
    }

    /// <summary>
    /// 천공 위치 설정
    /// </summary>
    public void SetPerforationPosition(float x, float y)
    {
        perforationPosX = Mathf.Clamp01(x);
        perforationPosY = Mathf.Clamp01(y);
    }

    /// <summary>
    /// 현재 ABG 반환
    /// </summary>
    public float GetCurrentABG()
    {
        return currentABG_dB;
    }

    /// <summary>
    /// 모든 주파수 ABG 배열 반환
    /// </summary>
    public float[] GetAllABG()
    {
        for (int i = 0; i < standardFrequencies.Length; i++)
        {
            calculatedABG[i] = CalculateAirBoneGap(standardFrequencies[i]);
        }
        return (float[])calculatedABG.Clone();
    }

    /// <summary>
    /// 달팽이관에 전달되는 진동량 반환
    /// </summary>
    public float GetTransmittedToCochlea()
    {
        if (ossicleVibrationScript != null)
        {
            return ossicleVibrationScript.GetTransmittedToStapes();
        }
        return transmittedAmplitude;
    }

    // ==================== Gizmos ====================

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Source
        if (tympanicMembrane != null)
        {
            Vector3 src = tympanicMembrane.transform.position + tympanicMembrane.transform.forward * 0.02f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(src, 0.003f);
        }

        // Perforation
        if (perforationGrade > 0 && Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(perforationWorldPos, 0.002f * perforationGrade);
        }

        // Cochlea (Observer)
        if (cochlea != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(cochlea.transform.position, 0.003f);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (tympanicMembrane == null) return;

        Gizmos.color = Color.cyan;
        Vector3 src = tympanicMembrane.transform.position + tympanicMembrane.transform.forward * 0.02f;

        // 경로 표시
        Gizmos.DrawLine(src, tympanicMembrane.transform.position);

        if (ossicle != null)
            Gizmos.DrawLine(tympanicMembrane.transform.position, ossicle.transform.position);

        if (ossicle != null && cochlea != null)
            Gizmos.DrawLine(ossicle.transform.position, cochlea.transform.position);
    }

    // ==================== GUI ====================

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 350, 450));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== Ear Perforation Simulator ===");
        GUILayout.Space(5);

        GUILayout.Label($"Grade: {perforationGrade} ({gradeToRatio[perforationGrade]:P0})");
        GUILayout.Label($"Position: ({perforationPosX:F2}, {perforationPosY:F2})");
        GUILayout.Label($"Middle Ear Volume: {middleEarVolumeCm3:F2} cm³");
        GUILayout.Label($"Frequency: {testFrequency:F0} Hz");

        GUILayout.Space(10);
        GUILayout.Label($"--- Current ABG: {currentABG_dB:F1} dB ---");
        GUILayout.Label($"  Size Effect: {sizeEffect_dB:F1} dB");
        GUILayout.Label($"  Volume Effect: {volumeEffect_dB:F1} dB");
        GUILayout.Label($"  Position Effect: {positionEffect_dB:F1} dB");
        GUILayout.Label($"Severity: {severityLevel}");

        GUILayout.Space(10);

        if (GUILayout.Button("Calculate All Frequencies"))
            CalculateAllFrequencies();

        if (GUILayout.Button("Export to CSV"))
            ExportToCSV();

        // ExperimentRunner 버튼들
        GUILayout.Space(15);
        GUILayout.Label("=== Experiment Runner ===");

        if (experimentRunner != null)
        {
            // 실행 중이면 진행 상황 표시
            if (experimentRunner.isRunning)
            {
                GUILayout.Label($"Running: {experimentRunner.progressPercent:F1}%");
                GUILayout.Label($"({experimentRunner.completedExperiments}/{experimentRunner.totalExperiments})");
                GUILayout.Label($"Status: {experimentRunner.currentStatus}");

                if (GUILayout.Button("STOP"))
                    experimentRunner.StopExperiments();
            }
            else
            {
                // 모드 표시
                experimentRunner.visualizationMode = GUILayout.Toggle(experimentRunner.visualizationMode, "Visualization Mode");

                // Run 버튼 (Grade0: 3개 + Grade1-4: 4*25*3 = 303개)
                if (GUILayout.Button("Run All Experiments"))
                    experimentRunner.RunAllExperiments();

                // Patient Comparison 버튼
                if (GUILayout.Button("Run Patient Comparison"))
                    experimentRunner.RunPatientComparison();
            }
        }
        else
        {
            GUILayout.Label("ExperimentRunner not found!");
            if (GUILayout.Button("Find ExperimentRunner"))
                experimentRunner = FindObjectOfType<ExperimentRunner>();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
