using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/*
 * ===============================================
 * 🦠 OTITIS MEDIA - 중이염 시뮬레이션 컴포넌트
 * ===============================================
 * 
 * 🧠 이 스크립트가 뭐야? (What is this?)
 * 중이염(Otitis Media)을 의학적으로 정확하게 시뮬레이션하는 컴포넌트입니다.
 * 중이(고막과 내이 사이 공간)에 발생하는 염증과 감염을 모델링하여
 * 실제 질병의 진행 과정, 증상, 치료 반응을 재현합니다.
 * 
 * 🦠 중이염의 주요 특징:
 * 1. 염증 반응 - 중이 점막의 부종과 발적
 * 2. 액체 축적 - 고름이나 삼출액이 중이강에 고임
 * 3. 압력 증가 - 액체로 인한 중이 내부 압력 상승
 * 4. 청력 저하 - 소리 전달 경로 차단
 * 5. 통증 - 압력과 염증으로 인한 이통
 * 6. 발열 - 감염에 대한 전신 반응
 * 
 * 🏥 의학적 분류:
 * - 급성 중이염 (Acute Otitis Media): 갑작스럽고 심한 증상
 * - 만성 중이염 (Chronic Otitis Media): 지속적이고 반복적인 염증
 * - 삼출성 중이염 (Otitis Media with Effusion): 감염 없이 액체만 축적
 * - 화농성 중이염 (Suppurative Otitis Media): 고름 형성
 * 
 * 🔬 의학적 정확성:
 * - 실제 중이염의 병리생리학적 과정 반영
 * - 연령별 발병률과 특성 고려
 * - 항생제 치료 반응 모델링
 * - 합병증 발생 가능성 포함
 * 
 * 💡 초보자를 위한 팁:
 * - 이 스크립트를 중이 3D 모델에 붙이세요
 * - 염증 효과로 빨간 색상과 부기 표현됩니다
 * - 액체 수준이 높으면 청력이 감소합니다
 * - 자동 치료 시스템이 포함되어 점차 회복됩니다
 */

[System.Serializable]
public class OtitisType
{
    [Header("🦠 중이염 유형 (Otitis Type)")]
    [Tooltip("중이염 종류 선택")]
    public OtitisCategory category = OtitisCategory.Acute;
    
    [Tooltip("감염 여부 - 세균이나 바이러스 감염")]
    public bool isInfective = true;
    
    [Tooltip("화농성 여부 - 고름 형성")]
    public bool isPurulent = false;
    
    [Tooltip("만성화 경향 - 오래 지속되는 정도")]
    [Range(0f, 1f)]
    public float chronicityTendency = 0.2f;
    
    [Header("🔬 병원체 정보 (Pathogen Information)")]
    [Tooltip("주요 병원체 유형")]
    public PathogenType primaryPathogen = PathogenType.Bacteria;
    
    [Tooltip("병원체 독성 - 얼마나 해로운지")]
    [Range(0.1f, 1f)]
    public float pathogenVirulence = 0.5f;
    
    [Tooltip("항생제 저항성 - 치료 어려움 정도")]
    [Range(0f, 0.8f)]
    public float antibioticResistance = 0.1f;
    
    [Tooltip("감염 확산 속도")]
    [Range(0.1f, 2f)]
    public float spreadRate = 1f;
}

public enum OtitisCategory
{
    Acute,      // 급성 중이염
    Chronic,    // 만성 중이염
    Effusion,   // 삼출성 중이염
    Suppurative // 화농성 중이염
}

public enum PathogenType
{
    Bacteria,   // 세균 (가장 흔함)
    Virus,      // 바이러스
    Fungal,     // 진균 (드물지만 심각)
    Mixed       // 복합 감염
}

[System.Serializable]
public class OtitisSymptoms
{
    [Header("🩺 주요 증상 (Primary Symptoms)")]
    [Tooltip("이통 (귀 아픔) 강도 (0~10)")]
    [Range(0f, 10f)]
    [ReadOnly] public float earPain = 0f;
    
    [Tooltip("청력 손실 정도 (%) - 일시적")]
    [ReadOnly] public float hearingLoss = 0f;
    
    [Tooltip("귀 막힘감 강도 (0~10)")]
    [Range(0f, 10f)]
    [ReadOnly] public float earFullness = 0f;
    
    [Tooltip("이명 (귀울림) 강도 (0~10)")]
    [Range(0f, 10f)]
    [ReadOnly] public float tinnitus = 0f;
    
    [Header("🌡️ 전신 증상 (Systemic Symptoms)")]
    [Tooltip("발열 온도 (°C) - 정상은 37°C")]
    [ReadOnly] public float bodyTemperature = 37f;
    
    [Tooltip("전신 불쾌감 (0~10)")]
    [Range(0f, 10f)]
    [ReadOnly] public float malaise = 0f;
    
    [Tooltip("식욕 부진 (0~10)")]
    [Range(0f, 10f)]
    [ReadOnly] public float appetiteLoss = 0f;
    
    [Tooltip("수면 장애 (0~10)")]
    [Range(0f, 10f)]
    [ReadOnly] public float sleepDisturbance = 0f;
    
    [Header("👁️ 관찰 가능한 징후 (Observable Signs)")]
    [Tooltip("고막 발적 (빨갛게 됨) 정도 (%)")]
    [ReadOnly] public float tympanicMembraneRedness = 0f;
    
    [Tooltip("고막 팽창 (부풀어 오름) 정도 (%)")]
    [ReadOnly] public float tympanicMembraneBulging = 0f;
    
    [Tooltip("귀 분비물 유무")]
    [ReadOnly] public bool hasDischarge = false;
    
    [Tooltip("분비물 양 (ml)")]
    [ReadOnly] public float dischargeAmount = 0f;
    
    [Tooltip("분비물 색상 유형")]
    [ReadOnly] public DischargeColor dischargeColor = DischargeColor.Clear;
}

public enum DischargeColor
{
    Clear,      // 투명 (삼출액)
    Yellow,     // 노란색 (고름)
    Green,      // 녹색 (세균 감염)
    Bloody,     // 혈성 (심한 염증)
    Brown       // 갈색 (만성)
}

[System.Serializable]
public class OtitisProgression
{
    [Header("📈 질병 진행 (Disease Progression)")]
    [Tooltip("현재 질병 단계")]
    [ReadOnly] public DiseaseStage currentStage = DiseaseStage.Incubation;
    
    [Tooltip("질병 진행률 (%) - 다음 단계까지")]
    [ReadOnly] public float progressionPercentage = 0f;
    
    [Tooltip("총 질병 지속 시간 (일)")]
    [ReadOnly] public float totalDuration = 0f;
    
    [Tooltip("현재 단계 지속 시간 (일)")]
    [ReadOnly] public float currentStageDuration = 0f;
    
    [Header("🏥 치료 반응 (Treatment Response)")]
    [Tooltip("치료 효과성 (%) - 치료가 얼마나 효과적인지")]
    [ReadOnly] public float treatmentEffectiveness = 0f;
    
    [Tooltip("항생제 치료 중인지")]
    [ReadOnly] public bool isOnAntibiotics = false;
    
    [Tooltip("진통제 사용 중인지")]
    [ReadOnly] public bool isOnPainkillers = false;
    
    [Tooltip("자연 치유 중인지")]
    [ReadOnly] public bool isSelfHealing = true;
    
    [Header("⚠️ 합병증 위험 (Complication Risk)")]
    [Tooltip("고막 천공 위험 (%)")]
    [ReadOnly] public float tympanicPerforationRisk = 0f;
    
    [Tooltip("유양돌기염 위험 (%)")]
    [ReadOnly] public float mastoiditisRisk = 0f;
    
    [Tooltip("뇌수막염 위험 (%) - 매우 드물지만 심각")]
    [ReadOnly] public float meningitisRisk = 0f;
}

public enum DiseaseStage
{
    Incubation,     // 잠복기
    EarlyOnset,     // 초기 발병
    Acute,          // 급성기
    Peak,           // 최고조
    Resolution,     // 회복기
    Recovery,       // 회복
    Chronic         // 만성화
}

public class Otitis : MonoBehaviour
{
    [Header("🦠 중이염 유형 (Otitis Type)")]
    [Tooltip("중이염의 종류와 특성 설정")]
    public OtitisType otitisType;
    
    [Header("🩺 증상 상태 (Symptom Status)")]
    [Tooltip("현재 나타나는 증상들")]
    public OtitisSymptoms symptoms;
    
    [Header("📈 질병 진행 (Disease Progression)")]
    [Tooltip("질병의 진행 과정과 치료 반응")]
    public OtitisProgression progression;
    
    [Header("🎮 실시간 제어 (Runtime Controls)")]
    [Tooltip("염증 심각도 (0~1) - 0이 정상, 1이 최고조")]
    [Range(0f, 1f)]
    public float severity = 0f;
    
    [Tooltip("액체/고름 수준 (0~1) - 중이강 내 액체 양")]
    [Range(0f, 1f)]
    public float fluidLevel = 0f;
    
    [Tooltip("면역 반응 강도 (0~2) - 1이 정상, 2는 과도한 반응")]
    [Range(0f, 2f)]
    public float immuneResponse = 1f;
    
    [Tooltip("환자 나이 (개월) - 아이들이 더 취약")]
    [Range(6, 1200)] // 6개월 ~ 100세
    public int patientAgeMonths = 72; // 기본 6세
    
    [Header("🎨 시각화 (Visualization)")]
    [Tooltip("중이 메쉬 렌더러 - 염증 색상 표현")]
    public MeshRenderer middleEarRenderer;
    
    [Tooltip("염증 파티클 시스템 - 염증 효과")]
    public ParticleSystem inflammationParticles;
    
    [Tooltip("액체 시각화 오브젝트 - 중이강 내 액체")]
    public GameObject fluidVisualization;
    
    [Tooltip("고름 시각화 오브젝트 - 화농성인 경우")]
    public GameObject pusVisualization;
    
    [Tooltip("고막 팽창 시각화")]
    public Transform tympanicMembraneTransform;
    
    [Header("🎨 색상 설정 (Color Settings)")]
    [Tooltip("정상 중이 색상")]
    public Color normalColor = new Color(1f, 0.9f, 0.8f, 1f); // 연한 살색
    
    [Tooltip("염증 색상 - 급성기")]
    public Color acuteInflammationColor = new Color(1f, 0.3f, 0.2f, 1f); // 빨간색
    
    [Tooltip("만성 염증 색상")]
    public Color chronicInflammationColor = new Color(0.7f, 0.5f, 0.3f, 1f); // 갈색
    
    [Tooltip("고름 색상")]
    public Color pusColor = new Color(1f, 1f, 0.6f, 0.8f); // 노란색
    
    [Tooltip("삼출액 색상")]
    public Color effusionColor = new Color(0.8f, 0.9f, 1f, 0.6f); // 연한 파란색
    
    [Header("🔊 오디오 효과 (Audio Effects)")]
    [Tooltip("염증 소리 - 욱신거리는 소리")]
    public AudioSource inflammationAudio;
    
    [Tooltip("액체 소리 - 액체가 움직이는 소리")]
    public AudioSource fluidAudio;
    
    [Tooltip("통증 신음 소리")]
    public AudioClip painSoundClip;
    
    [Tooltip("액체 움직임 소리")]
    public AudioClip fluidMovementClip;
    
    [Header("💊 치료 시스템 (Treatment System)")]
    [Tooltip("자동 치료 활성화 - 시간이 지나면서 자연 회복")]
    public bool enableAutoHealing = true;
    
    [Tooltip("치료 속도 배율 - 클수록 빨리 나음")]
    [Range(0.1f, 5f)]
    public float healingRateMultiplier = 1f;
    
    [Tooltip("항생제 효과성 - 세균 감염에만 효과")]
    [Range(0.1f, 1f)]
    public float antibioticEffectiveness = 0.8f;
    
    [Header("🐞 디버그 (Debug)")]
    [Tooltip("디버그 정보 출력")]
    public bool enableDebugLogs = false;
    
    [Tooltip("질병 진행 가속화 (테스트용)")]
    public bool accelerateProgression = false;
    
    [Tooltip("증상 모니터링 활성화")]
    public bool monitorSymptoms = false;
    
    // ============================================================================
    // 🔧 내부 변수들 (Private Variables)
    // ============================================================================
    
    private Material middleEarMaterial;               // 중이 재질
    private Vector3 originalTympanicScale;            // 고막 원래 크기
    private float diseaseStartTime;                   // 질병 시작 시간
    private float lastStageChangeTime;                // 마지막 단계 변경 시간
    private bool systemInitialized = false;          // 시스템 초기화 완료
    private Dictionary<DiseaseStage, float> stageDurations; // 각 단계별 지속 시간
    
    // 치료 관련
    private float antibioticStartTime = -1f;          // 항생제 시작 시간
    private float painkillersStartTime = -1f;         // 진통제 시작 시간
    private float currentTreatmentEffectiveness = 0f; // 현재 치료 효과
    
    // 합병증 추적
    private float complicationCheckTimer = 0f;        // 합병증 검사 타이머
    private bool hasPerforated = false;               // 고막 천공 여부
    
    // 성능 최적화
    private float lastSymptomUpdateTime = 0f;
    private float symptomUpdateInterval = 0.5f;       // 0.5초마다 증상 업데이트
    
    // 질병 진행 상수
    private const float DAYS_TO_SECONDS = 86400f;    // 하루를 초로 변환
    private const float SIMULATION_TIME_SCALE = 3600f; // 1시간 = 1일 (시뮬레이션 가속)

    /*
     * ====================================================================
     * 🚀 UNITY 생명주기 메서드들 (Unity Lifecycle Methods)
     * ====================================================================
     */

    /// <summary>
    /// 🎬 START - 중이염 시스템 초기화
    /// 
    /// 초기화 과정:
    /// 1. 질병 유형 및 특성 설정
    /// 2. 증상 시스템 초기화
    /// 3. 시각화 컴포넌트 설정
    /// 4. 치료 시스템 준비
    /// 5. 질병 진행 단계 설정
    /// </summary>
    void Start()
    {
        LogDebug("🦠 중이염 시스템 초기화 시작...");
        
        InitializeOtitisSystem();
        SetupDiseaseProgression();
        SetupVisualization();
        SetupAudioSystem();
        InitializeSymptoms();
        
        systemInitialized = true;
        LogDebug("✅ 중이염 시스템 초기화 완료");
    }

    /// <summary>
    /// 🔄 UPDATE - 실시간 중이염 진행 처리
    /// 
    /// 매 프레임 실행 내용:
    /// 1. 질병 진행 단계 업데이트
    /// 2. 증상 발현 및 변화
    /// 3. 치료 반응 처리
    /// 4. 합병증 위험 평가
    /// 5. 시각화 업데이트
    /// </summary>
    void Update()
    {
        if (!systemInitialized) return;
        
        // 질병 진행 (매 프레임)
        UpdateDiseaseProgression();
        
        // 치료 효과 처리 (매 프레임)
        ProcessTreatmentEffects();
        
        // 증상 업데이트 (최적화)
        if (Time.time - lastSymptomUpdateTime >= symptomUpdateInterval)
        {
            UpdateSymptoms();
            lastSymptomUpdateTime = Time.time;
        }
        
        // 합병증 검사 (1초마다)
        complicationCheckTimer += Time.deltaTime;
        if (complicationCheckTimer >= 1f)
        {
            CheckForComplications();
            complicationCheckTimer = 0f;
        }
        
        // 시각화 업데이트 (조건부)
        if (severity > 0.01f || fluidLevel > 0.01f)
        {
            UpdateVisualization();
            UpdateAudioEffects();
        }
    }

    /*
     * ====================================================================
     * 🛠️ 초기화 메서드들 (Initialization Methods)
     * ====================================================================
     */

    /// <summary>
    /// 🔧 중이염 시스템 기본 초기화
    /// </summary>
    void InitializeOtitisSystem()
    {
        // 기본 설정 검증
        if (otitisType == null)
        {
            otitisType = new OtitisType();
            LogDebug("⚠️ 중이염 유형이 설정되지 않아 기본값으로 초기화");
        }
        
        if (symptoms == null)
        {
            symptoms = new OtitisSymptoms();
        }
        
        if (progression == null)
        {
            progression = new OtitisProgression();
        }
        
        // 질병 시작 시간 기록
        diseaseStartTime = Time.time;
        lastStageChangeTime = Time.time;
        
        // 단계별 지속 시간 설정
        SetupStageDurations();
        
        ValidateOtitisSettings();
        LogDebug("🔧 중이염 시스템 기본 초기화 완료");
    }

    /// <summary>
    /// ⏰ 질병 단계별 지속 시간 설정
    /// 
    /// 실제 중이염의 자연 경과를 반영한 단계별 시간
    /// </summary>
    void SetupStageDurations()
    {
        stageDurations = new Dictionary<DiseaseStage, float>();
        
        // 시뮬레이션 가속화를 위해 실제 시간을 단축
        float timeScale = accelerateProgression ? 60f : SIMULATION_TIME_SCALE;
        
        switch (otitisType.category)
        {
            case OtitisCategory.Acute:
                stageDurations[DiseaseStage.Incubation] = 1f * DAYS_TO_SECONDS / timeScale;    // 1일
                stageDurations[DiseaseStage.EarlyOnset] = 1f * DAYS_TO_SECONDS / timeScale;    // 1일
                stageDurations[DiseaseStage.Acute] = 2f * DAYS_TO_SECONDS / timeScale;         // 2일
                stageDurations[DiseaseStage.Peak] = 1f * DAYS_TO_SECONDS / timeScale;          // 1일
                stageDurations[DiseaseStage.Resolution] = 3f * DAYS_TO_SECONDS / timeScale;    // 3일
                stageDurations[DiseaseStage.Recovery] = 7f * DAYS_TO_SECONDS / timeScale;      // 7일
                break;
                
            case OtitisCategory.Chronic:
                // 만성은 더 오래 지속
                stageDurations[DiseaseStage.Incubation] = 3f * DAYS_TO_SECONDS / timeScale;
                stageDurations[DiseaseStage.EarlyOnset] = 7f * DAYS_TO_SECONDS / timeScale;
                stageDurations[DiseaseStage.Acute] = 14f * DAYS_TO_SECONDS / timeScale;
                stageDurations[DiseaseStage.Peak] = 7f * DAYS_TO_SECONDS / timeScale;
                stageDurations[DiseaseStage.Chronic] = 60f * DAYS_TO_SECONDS / timeScale;     // 2개월
                break;
                
            case OtitisCategory.Effusion:
                // 삼출성은 증상이 약하지만 오래 지속
                stageDurations[DiseaseStage.Incubation] = 2f * DAYS_TO_SECONDS / timeScale;
                stageDurations[DiseaseStage.EarlyOnset] = 5f * DAYS_TO_SECONDS / timeScale;
                stageDurations[DiseaseStage.Acute] = 14f * DAYS_TO_SECONDS / timeScale;
                stageDurations[DiseaseStage.Resolution] = 21f * DAYS_TO_SECONDS / timeScale;
                stageDurations[DiseaseStage.Recovery] = 30f * DAYS_TO_SECONDS / timeScale;
                break;
                
            case OtitisCategory.Suppurative:
                // 화농성은 빠르게 진행되고 심각
                stageDurations[DiseaseStage.Incubation] = 0.5f * DAYS_TO_SECONDS / timeScale;
                stageDurations[DiseaseStage.EarlyOnset] = 0.5f * DAYS_TO_SECONDS / timeScale;
                stageDurations[DiseaseStage.Acute] = 1f * DAYS_TO_SECONDS / timeScale;
                stageDurations[DiseaseStage.Peak] = 2f * DAYS_TO_SECONDS / timeScale;
                stageDurations[DiseaseStage.Resolution] = 5f * DAYS_TO_SECONDS / timeScale;
                stageDurations[DiseaseStage.Recovery] = 14f * DAYS_TO_SECONDS / timeScale;
                break;
        }
        
        LogDebug($"단계별 지속 시간 설정 완료: {otitisType.category} 유형");
    }

    /// <summary>
    /// 🔍 중이염 설정 유효성 검사
    /// </summary>
    void ValidateOtitisSettings()
    {
        severity = Mathf.Clamp01(severity);
        fluidLevel = Mathf.Clamp01(fluidLevel);
        immuneResponse = Mathf.Clamp(immuneResponse, 0f, 2f);
        patientAgeMonths = Mathf.Clamp(patientAgeMonths, 6, 1200);
        
        otitisType.pathogenVirulence = Mathf.Clamp01(otitisType.pathogenVirulence);
        otitisType.antibioticResistance = Mathf.Clamp(otitisType.antibioticResistance, 0f, 0.8f);
    }

    /// <summary>
    /// 📈 질병 진행 시스템 설정
    /// </summary>
    void SetupDiseaseProgression()
    {
        progression.currentStage = DiseaseStage.Incubation;
        progression.progressionPercentage = 0f;
        progression.totalDuration = 0f;
        progression.currentStageDuration = 0f;
        progression.isSelfHealing = enableAutoHealing;
        
        LogDebug("📈 질병 진행 시스템 설정 완료");
    }

    /// <summary>
    /// 🎨 시각화 시스템 설정
    /// </summary>
    void SetupVisualization()
    {
        // 중이 메쉬 재질 설정
        if (middleEarRenderer != null)
        {
            middleEarMaterial = middleEarRenderer.material;
            if (middleEarMaterial != null)
            {
                middleEarMaterial.color = normalColor;
            }
        }
        
        // 고막 원래 크기 저장
        if (tympanicMembraneTransform != null)
        {
            originalTympanicScale = tympanicMembraneTransform.localScale;
        }
        
        // 파티클 시스템 설정
        if (inflammationParticles != null)
        {
            var main = inflammationParticles.main;
            main.startColor = acuteInflammationColor;
            main.maxParticles = 50;
            
            var emission = inflammationParticles.emission;
            emission.rateOverTime = 0f;
        }
        
        // 액체 및 고름 시각화 초기 비활성화
        if (fluidVisualization != null)
        {
            fluidVisualization.SetActive(false);
        }
        
        if (pusVisualization != null)
        {
            pusVisualization.SetActive(false);
        }
        
        LogDebug("🎨 시각화 시스템 설정 완료");
    }

    /// <summary>
    /// 🔊 오디오 시스템 설정
    /// </summary>
    void SetupAudioSystem()
    {
        if (inflammationAudio == null)
        {
            inflammationAudio = GetComponent<AudioSource>();
        }
        
        if (inflammationAudio != null)
        {
            inflammationAudio.clip = painSoundClip;
            inflammationAudio.loop = true;
            inflammationAudio.volume = 0f;
            inflammationAudio.pitch = 1.0f;
        }
        
        if (fluidAudio == null)
        {
            // 두 번째 AudioSource 컴포넌트 찾기
            AudioSource[] audioSources = GetComponents<AudioSource>();
            if (audioSources.Length > 1)
            {
                fluidAudio = audioSources[1];
            }
        }
        
        if (fluidAudio != null)
        {
            fluidAudio.clip = fluidMovementClip;
            fluidAudio.loop = false;
            fluidAudio.volume = 0.3f;
        }
        
        LogDebug("🔊 오디오 시스템 설정 완료");
    }

    /// <summary>
    /// 🩺 증상 초기화
    /// </summary>
    void InitializeSymptoms()
    {
        // 모든 증상을 0으로 초기화
        symptoms.earPain = 0f;
        symptoms.hearingLoss = 0f;
        symptoms.earFullness = 0f;
        symptoms.tinnitus = 0f;
        symptoms.bodyTemperature = 37f;
        symptoms.malaise = 0f;
        symptoms.appetiteLoss = 0f;
        symptoms.sleepDisturbance = 0f;
        symptoms.tympanicMembraneRedness = 0f;
        symptoms.tympanicMembraneBulging = 0f;
        symptoms.hasDischarge = false;
        symptoms.dischargeAmount = 0f;
        symptoms.dischargeColor = DischargeColor.Clear;
        
        LogDebug("🩺 증상 초기화 완료");
    }

    /*
     * ====================================================================
     * 🦠 질병 진행 메서드들 (Disease Progression Methods)
     * ====================================================================
     */

    /// <summary>
    /// 📈 질병 진행 업데이트
    /// 
    /// 중이염의 자연 경과를 시뮬레이션
    /// </summary>
    void UpdateDiseaseProgression()
    {
        // 총 질병 지속 시간 업데이트
        progression.totalDuration = (Time.time - diseaseStartTime) / DAYS_TO_SECONDS * SIMULATION_TIME_SCALE;
        
        // 현재 단계 지속 시간 업데이트
        progression.currentStageDuration = (Time.time - lastStageChangeTime) / DAYS_TO_SECONDS * SIMULATION_TIME_SCALE;
        
        // 현재 단계의 예상 지속 시간
        float expectedStageDuration = GetExpectedStageDuration(progression.currentStage);
        
        // 진행률 계산
        if (expectedStageDuration > 0)
        {
            progression.progressionPercentage = (progression.currentStageDuration / expectedStageDuration) * 100f;
            progression.progressionPercentage = Mathf.Clamp(progression.progressionPercentage, 0f, 100f);
        }
        
        // 다음 단계로 진행 체크
        if (progression.currentStageDuration >= expectedStageDuration)
        {
            AdvanceToNextStage();
        }
        
        // 심각도와 액체 수준을 질병 단계에 따라 조절
        UpdateSeverityBasedOnStage();
    }

    /// <summary>
    /// 📊 현재 단계의 예상 지속 시간 반환
    /// </summary>
    float GetExpectedStageDuration(DiseaseStage stage)
    {
        if (stageDurations.ContainsKey(stage))
        {
            float baseDuration = stageDurations[stage];
            
            // 환자 나이 고려 (어린이는 더 오래 지속)
            float ageFactor = 1f;
            if (patientAgeMonths < 24) // 2세 미만
            {
                ageFactor = 1.5f;
            }
            else if (patientAgeMonths < 72) // 6세 미만
            {
                ageFactor = 1.2f;
            }
            
            // 면역 반응 고려
            float immuneFactor = 2f / immuneResponse; // 면역 반응이 약하면 더 오래 지속
            
            // 치료 효과 고려
            float treatmentFactor = 1f - (progression.treatmentEffectiveness / 100f) * 0.5f;
            
            return baseDuration * ageFactor * immuneFactor * treatmentFactor;
        }
        
        return 1f; // 기본값
    }

    /// <summary>
    /// ⏭️ 다음 단계로 진행
    /// </summary>
    void AdvanceToNextStage()
    {
        DiseaseStage nextStage = GetNextStage(progression.currentStage);
        
        if (nextStage != progression.currentStage)
        {
            LogDebug($"질병 단계 변경: {progression.currentStage} → {nextStage}");
            
            progression.currentStage = nextStage;
            lastStageChangeTime = Time.time;
            progression.currentStageDuration = 0f;
            progression.progressionPercentage = 0f;
            
            // 단계 변경에 따른 특수 효과
            OnStageChanged(nextStage);
        }
    }

    /// <summary>
    /// 🔄 다음 단계 결정
    /// </summary>
    DiseaseStage GetNextStage(DiseaseStage currentStage)
    {
        switch (currentStage)
        {
            case DiseaseStage.Incubation:
                return DiseaseStage.EarlyOnset;
                
            case DiseaseStage.EarlyOnset:
                return DiseaseStage.Acute;
                
            case DiseaseStage.Acute:
                return DiseaseStage.Peak;
                
            case DiseaseStage.Peak:
                if (otitisType.category == OtitisCategory.Chronic && 
                    otitisType.chronicityTendency > 0.5f && 
                    progression.treatmentEffectiveness < 50f)
                {
                    return DiseaseStage.Chronic;
                }
                return DiseaseStage.Resolution;
                
            case DiseaseStage.Resolution:
                return DiseaseStage.Recovery;
                
            case DiseaseStage.Recovery:
                // 완전 회복 - 질병 종료
                return DiseaseStage.Recovery;
                
            case DiseaseStage.Chronic:
                // 만성은 치료 효과가 좋으면 회복기로
                if (progression.treatmentEffectiveness > 70f)
                {
                    return DiseaseStage.Resolution;
                }
                return DiseaseStage.Chronic;
                
            default:
                return currentStage;
        }
    }

    /// <summary>
    /// 🎯 단계 변경 시 특수 효과
    /// </summary>
    void OnStageChanged(DiseaseStage newStage)
    {
        switch (newStage)
        {
            case DiseaseStage.EarlyOnset:
                LogDebug("초기 증상 발현 시작");
                break;
                
            case DiseaseStage.Acute:
                LogDebug("급성기 진입 - 증상 악화");
                break;
                
            case DiseaseStage.Peak:
                LogDebug("최고조 단계 - 가장 심한 증상");
                CheckForTympanicPerforation();
                break;
                
            case DiseaseStage.Resolution:
                LogDebug("회복기 시작 - 증상 완화");
                break;
                
            case DiseaseStage.Recovery:
                LogDebug("회복 완료");
                severity = 0f;
                fluidLevel = 0f;
                break;
                
            case DiseaseStage.Chronic:
                LogDebug("만성화 - 지속적인 증상");
                break;
        }
    }

    /// <summary>
    /// 📊 단계에 따른 심각도 업데이트
    /// </summary>
    void UpdateSeverityBasedOnStage()
    {
        float targetSeverity = 0f;
        float targetFluidLevel = 0f;
        
        switch (progression.currentStage)
        {
            case DiseaseStage.Incubation:
                targetSeverity = 0.1f;
                targetFluidLevel = 0f;
                break;
                
            case DiseaseStage.EarlyOnset:
                targetSeverity = 0.3f;
                targetFluidLevel = 0.2f;
                break;
                
            case DiseaseStage.Acute:
                targetSeverity = 0.7f;
                targetFluidLevel = 0.5f;
                break;
                
            case DiseaseStage.Peak:
                targetSeverity = 1f;
                targetFluidLevel = 0.8f;
                if (otitisType.isPurulent)
                {
                    targetFluidLevel = 1f; // 화농성은 액체 가득
                }
                break;
                
            case DiseaseStage.Resolution:
                targetSeverity = 0.4f;
                targetFluidLevel = 0.3f;
                break;
                
            case DiseaseStage.Recovery:
                targetSeverity = 0f;
                targetFluidLevel = 0f;
                break;
                
            case DiseaseStage.Chronic:
                targetSeverity = 0.5f;
                targetFluidLevel = 0.6f;
                break;
        }
        
        // 병원체 독성 반영
        targetSeverity *= otitisType.pathogenVirulence;
        
        // 점진적 변화
        float changeRate = Time.deltaTime * 0.5f;
        severity = Mathf.Lerp(severity, targetSeverity, changeRate);
        fluidLevel = Mathf.Lerp(fluidLevel, targetFluidLevel, changeRate);
    }

    /*
     * ====================================================================
     * 💊 치료 시스템 메서드들 (Treatment System Methods)
     * ====================================================================
     */

    /// <summary>
    /// 💊 치료 효과 처리
    /// </summary>
    void ProcessTreatmentEffects()
    {
        float totalEffectiveness = 0f;
        
        // 항생제 효과 (세균 감염에만)
        if (progression.isOnAntibiotics && otitisType.primaryPathogen == PathogenType.Bacteria)
        {
            float antibioticEffect = antibioticEffectiveness * (1f - otitisType.antibioticResistance);
            float timeSinceStart = Time.time - antibioticStartTime;
            
            // 항생제는 24-48시간 후부터 효과 시작
            if (timeSinceStart > 86400f / SIMULATION_TIME_SCALE) // 1일 후
            {
                totalEffectiveness += antibioticEffect * 70f; // 최대 70% 효과
            }
        }
        
        // 진통제 효과 (증상 완화만)
        if (progression.isOnPainkillers)
        {
            float painkillersEffect = 0.3f; // 30% 증상 완화
            totalEffectiveness += painkillersEffect * 30f;
        }
        
        // 자연 치유
        if (progression.isSelfHealing)
        {
            float naturalHealing = immuneResponse * healingRateMultiplier;
            
            // 나이에 따른 자연 치유력 차이
            if (patientAgeMonths < 24) // 2세 미만
            {
                naturalHealing *= 0.8f; // 20% 감소
            }
            else if (patientAgeMonths > 720) // 60세 이상
            {
                naturalHealing *= 0.7f; // 30% 감소
            }
            
            totalEffectiveness += naturalHealing * 40f; // 최대 40% 자연 치유
        }
        
        progression.treatmentEffectiveness = Mathf.Clamp(totalEffectiveness, 0f, 100f);
        
        // 치료 효과를 질병 진행에 반영 (회복기에만)
        if (progression.currentStage == DiseaseStage.Resolution || 
            progression.currentStage == DiseaseStage.Recovery)
        {
            float healingRate = progression.treatmentEffectiveness / 100f * Time.deltaTime * 0.3f;
            severity = Mathf.Max(0f, severity - healingRate);
            fluidLevel = Mathf.Max(0f, fluidLevel - healingRate * 0.8f);
        }
    }

    /*
     * ====================================================================
     * 🩺 증상 관리 메서드들 (Symptom Management Methods)
     * ====================================================================
     */

    /// <summary>
    /// 🩺 증상 업데이트
    /// 
    /// 질병 진행 단계와 심각도에 따른 증상 발현
    /// </summary>
    void UpdateSymptoms()
    {
        UpdatePainSymptoms();
        UpdateHearingSymptoms();
        UpdateSystemicSymptoms();
        UpdatePhysicalSigns();
        
        LogDebug($"증상 업데이트: 통증 {symptoms.earPain:F1}, 청력손실 {symptoms.hearingLoss:F1}%");
    }

    /// <summary>
    /// 😰 통증 관련 증상 업데이트
    /// </summary>
    void UpdatePainSymptoms()
    {
        // 기본 통증 계산
        float basePain = severity * 8f; // 최대 8/10
        
        // 액체 압력에 의한 통증 추가
        float pressurePain = fluidLevel * 3f;
        
        // 연령에 따른 통증 표현 차이
        float ageMultiplier = 1f;
        if (patientAgeMonths < 24) // 2세 미만은 더 심하게 느낌
        {
            ageMultiplier = 1.3f;
        }
        
        symptoms.earPain = Mathf.Clamp((basePain + pressurePain) * ageMultiplier, 0f, 10f);
        
        // 귀 막힘감
        symptoms.earFullness = Mathf.Clamp(fluidLevel * 8f + severity * 4f, 0f, 10f);
        
        // 이명 (염증과 압력으로 인한)
        symptoms.tinnitus = Mathf.Clamp(severity * 6f + fluidLevel * 2f, 0f, 10f);
        
        // 진통제 효과 적용
        if (progression.isOnPainkillers)
        {
            symptoms.earPain *= 0.4f; // 60% 감소
            symptoms.earFullness *= 0.7f; // 30% 감소
        }
    }

    /// <summary>
    /// 👂 청력 관련 증상 업데이트
    /// </summary>
    void UpdateHearingSymptoms()
    {
        // 전음성 난청 (액체로 인한 소리 전달 차단)
        float conductiveHearingLoss = fluidLevel * 40f; // 최대 40dB 손실
        
        // 염증으로 인한 추가 손실
        float inflammatoryLoss = severity * 15f; // 최대 15dB 손실
        
        symptoms.hearingLoss = Mathf.Clamp(conductiveHearingLoss + inflammatoryLoss, 0f, 60f);
        
        LogDebug($"청력 손실: {symptoms.hearingLoss:F1}dB (액체: {conductiveHearingLoss:F1}, 염증: {inflammatoryLoss:F1})");
    }

    /// <summary>
    /// 🌡️ 전신 증상 업데이트
    /// </summary>
    void UpdateSystemicSymptoms()
    {
        // 발열 (감염성인 경우에만)
        if (otitisType.isInfective)
        {
            float feverIncrease = severity * 3f; // 최대 3도 상승
            
            // 어린이는 더 쉽게 열이 남
            if (patientAgeMonths < 72) // 6세 미만
            {
                feverIncrease *= 1.2f;
            }
            
            symptoms.bodyTemperature = 37f + feverIncrease;
        }
        else
        {
            symptoms.bodyTemperature = 37f; // 정상 체온
        }
        
        // 전신 불쾌감
        symptoms.malaise = Mathf.Clamp(severity * 7f + symptoms.earPain * 0.5f, 0f, 10f);
        
        // 식욕 부진
        symptoms.appetiteLoss = Mathf.Clamp(severity * 6f + (symptoms.bodyTemperature - 37f) * 2f, 0f, 10f);
        
        // 수면 장애 (통증으로 인한)
        symptoms.sleepDisturbance = Mathf.Clamp(symptoms.earPain * 0.8f + symptoms.earFullness * 0.3f, 0f, 10f);
    }

    /// <summary>
    /// 👁️ 신체 징후 업데이트
    /// </summary>
    void UpdatePhysicalSigns()
    {
        // 고막 발적
        symptoms.tympanicMembraneRedness = severity * 100f;
        
        // 고막 팽창 (액체 압력으로 인한)
        symptoms.tympanicMembraneBulging = fluidLevel * 80f;
        
        // 분비물 (고막 천공시 또는 외이도로 유출)
        if (hasPerforated || progression.currentStage == DiseaseStage.Peak)
        {
            symptoms.hasDischarge = fluidLevel > 0.3f;
            symptoms.dischargeAmount = fluidLevel * 2f; // 최대 2ml
            
            // 분비물 색상 결정
            if (otitisType.isPurulent)
            {
                symptoms.dischargeColor = DischargeColor.Yellow;
                if (otitisType.primaryPathogen == PathogenType.Bacteria)
                {
                    symptoms.dischargeColor = DischargeColor.Green;
                }
            }
            else if (symptoms.tympanicMembraneRedness > 70f)
            {
                symptoms.dischargeColor = DischargeColor.Bloody;
            }
            else
            {
                symptoms.dischargeColor = DischargeColor.Clear;
            }
        }
        else
        {
            symptoms.hasDischarge = false;
            symptoms.dischargeAmount = 0f;
        }
    }

    /*
     * ====================================================================
     * ⚠️ 합병증 관리 메서드들 (Complication Management Methods)
     * ====================================================================
     */

    /// <summary>
    /// ⚠️ 합병증 위험 검사
    /// </summary>
    void CheckForComplications()
    {
        CalculateComplicationRisks();
        CheckSpecificComplications();
    }

    /// <summary>
    /// 📊 합병증 위험도 계산
    /// </summary>
    void CalculateComplicationRisks()
    {
        float baseRisk = severity * 100f;
        
        // 고막 천공 위험
        progression.tympanicPerforationRisk = baseRisk * fluidLevel;
        if (otitisType.isPurulent)
        {
            progression.tympanicPerforationRisk *= 1.5f; // 화농성은 위험 증가
        }
        progression.tympanicPerforationRisk = Mathf.Clamp(progression.tympanicPerforationRisk, 0f, 80f);
        
        // 유양돌기염 위험 (치료받지 않은 급성 중이염의 합병증)
        if (progression.currentStage == DiseaseStage.Peak && 
            progression.treatmentEffectiveness < 30f)
        {
            progression.mastoiditisRisk = baseRisk * 0.1f; // 10분의 1
        }
        else
        {
            progression.mastoiditisRisk = 0f;
        }
        
        // 뇌수막염 위험 (매우 드물지만 치명적)
        if (progression.mastoiditisRisk > 30f && 
            progression.treatmentEffectiveness < 20f)
        {
            progression.meningitisRisk = baseRisk * 0.01f; // 100분의 1
        }
        else
        {
            progression.meningitisRisk = 0f;
        }
    }

    /// <summary>
    /// 🔍 특정 합병증 발생 검사
    /// </summary>
    void CheckSpecificComplications()
    {
        // 고막 천공 검사
        if (!hasPerforated && progression.tympanicPerforationRisk > 60f)
        {
            if (Random.Range(0f, 100f) < 2f) // 2% 확률로 천공
            {
                TriggerTympanicPerforation();
            }
        }
        
        // 기타 합병증은 실제 의료진의 판단이 필요한 부분이므로
        // 시뮬레이션에서는 위험도만 표시
    }

    /// <summary>
    /// 💥 고막 천공 발생
    /// </summary>
    void TriggerTympanicPerforation()
    {
        hasPerforated = true;
        
        // 천공으로 인한 즉각적인 변화
        symptoms.earPain *= 0.3f; // 압력 해제로 통증 감소
        symptoms.earFullness *= 0.5f; // 막힘감 감소
        symptoms.hasDischarge = true; // 분비물 유출 시작
        
        // 청력 손실 변화 (전음성 난청 증가)
        symptoms.hearingLoss += 20f; // 추가 20dB 손실
        
        LogDebug("💥 고막 천공 발생!");
        
        // 천공 후 자연 치유 과정 시작
        StartCoroutine(PerforationHealingProcess());
    }

    /// <summary>
    /// 🩹 고막 천공 치유 과정
    /// </summary>
    System.Collections.IEnumerator PerforationHealingProcess()
    {
        // 천공은 보통 2-8주에 걸쳐 자연 치유
        float healingDuration = 14f * DAYS_TO_SECONDS / SIMULATION_TIME_SCALE; // 2주
        float startTime = Time.time;
        
        while (Time.time - startTime < healingDuration)
        {
            float healingProgress = (Time.time - startTime) / healingDuration;
            
            // 점진적 청력 회복 (완전히는 회복되지 않을 수 있음)
            float targetHearingLoss = symptoms.hearingLoss * (1f - healingProgress * 0.7f);
            symptoms.hearingLoss = Mathf.Lerp(symptoms.hearingLoss, targetHearingLoss, Time.deltaTime);
            
            yield return null;
        }
        
        // 치유 완료
        hasPerforated = false;
        symptoms.hasDischarge = false;
        symptoms.dischargeAmount = 0f;
        
        LogDebug("🩹 고막 천공 치유 완료");
    }

    /// <summary>
    /// 🔍 고막 천공 가능성 검사 (Peak 단계에서)
    /// </summary>
    void CheckForTympanicPerforation()
    {
        if (otitisType.isPurulent && fluidLevel > 0.8f)
        {
            // 화농성 중이염에서 액체가 많으면 천공 위험 증가
            progression.tympanicPerforationRisk += 30f;
        }
    }

    /*
     * ====================================================================
     * 🎨 시각화 업데이트 메서드들 (Visualization Updates)
     * ====================================================================
     */

    /// <summary>
    /// 🎨 시각화 업데이트
    /// </summary>
    void UpdateVisualization()
    {
        UpdateMiddleEarAppearance();
        UpdateInflammationEffects();
        UpdateFluidVisualization();
        UpdateTympanicMembraneVisualization();
    }

    /// <summary>
    /// 🌈 중이 외관 업데이트
    /// </summary>
    void UpdateMiddleEarAppearance()
    {
        if (middleEarMaterial == null) return;
        
        Color targetColor = normalColor;
        
        // 염증 단계에 따른 색상
        switch (progression.currentStage)
        {
            case DiseaseStage.Acute:
            case DiseaseStage.Peak:
                targetColor = Color.Lerp(normalColor, acuteInflammationColor, severity);
                break;
                
            case DiseaseStage.Chronic:
                targetColor = Color.Lerp(normalColor, chronicInflammationColor, severity * 0.8f);
                break;
                
            default:
                targetColor = Color.Lerp(normalColor, acuteInflammationColor, severity * 0.5f);
                break;
        }
        
        middleEarMaterial.color = Color.Lerp(middleEarMaterial.color, targetColor, Time.deltaTime * 2f);
    }

    /// <summary>
    /// 🔥 염증 효과 업데이트
    /// </summary>
    void UpdateInflammationEffects()
    {
        if (inflammationParticles == null) return;
        
        var emission = inflammationParticles.emission;
        var main = inflammationParticles.main;
        
        if (severity > 0.2f)
        {
            emission.rateOverTime = severity * 20f;
            
            // 염증 유형에 따른 색상
            if (otitisType.category == OtitisCategory.Chronic)
            {
                main.startColor = chronicInflammationColor;
            }
            else
            {
                main.startColor = acuteInflammationColor;
            }
        }
        else
        {
            emission.rateOverTime = 0f;
        }
    }

    /// <summary>
    /// 💧 액체 시각화 업데이트
    /// </summary>
    void UpdateFluidVisualization()
    {
        bool shouldShowFluid = fluidLevel > 0.1f;
        
        // 액체 시각화 활성화/비활성화
        if (fluidVisualization != null)
        {
            if (fluidVisualization.activeInHierarchy != shouldShowFluid)
            {
                fluidVisualization.SetActive(shouldShowFluid);
            }
            
            if (shouldShowFluid)
            {
                // 액체 수준에 따른 스케일 조정
                Vector3 fluidScale = Vector3.one * fluidLevel;
                fluidVisualization.transform.localScale = fluidScale;
                
                // 액체 색상 설정
                Renderer fluidRenderer = fluidVisualization.GetComponent<Renderer>();
                if (fluidRenderer != null)
                {
                    if (otitisType.isPurulent)
                    {
                        fluidRenderer.material.color = pusColor;
                    }
                    else
                    {
                        fluidRenderer.material.color = effusionColor;
                    }
                }
            }
        }
        
        // 고름 시각화 (화농성인 경우)
        bool shouldShowPus = otitisType.isPurulent && fluidLevel > 0.5f;
        if (pusVisualization != null)
        {
            if (pusVisualization.activeInHierarchy != shouldShowPus)
            {
                pusVisualization.SetActive(shouldShowPus);
            }
            
            if (shouldShowPus)
            {
                // 고름 양에 따른 크기 조정
                Vector3 pusScale = Vector3.one * (fluidLevel * 0.8f);
                pusVisualization.transform.localScale = pusScale;
            }
        }
    }

    /// <summary>
    /// 🥁 고막 시각화 업데이트
    /// </summary>
    void UpdateTympanicMembraneVisualization()
    {
        if (tympanicMembraneTransform == null) return;
        
        // 고막 팽창 (압력으로 인한)
        float bulgingFactor = 1f + (fluidLevel * 0.3f); // 최대 30% 팽창
        Vector3 targetScale = originalTympanicScale * bulgingFactor;
        
        tympanicMembraneTransform.localScale = Vector3.Lerp(
            tympanicMembraneTransform.localScale, 
            targetScale, 
            Time.deltaTime * 2f
        );
        
        // 고막 색상 변화 (발적)
        Renderer tympanicRenderer = tympanicMembraneTransform.GetComponent<Renderer>();
        if (tympanicRenderer != null)
        {
            Color normalTympanicColor = new Color(0.9f, 0.8f, 0.7f, 1f); // 연한 살색
            Color reddenedColor = new Color(1f, 0.4f, 0.3f, 1f); // 빨간색
            
            float rednessLevel = symptoms.tympanicMembraneRedness / 100f;
            Color targetColor = Color.Lerp(normalTympanicColor, reddenedColor, rednessLevel);
            
            tympanicRenderer.material.color = Color.Lerp(
                tympanicRenderer.material.color, 
                targetColor, 
                Time.deltaTime * 3f
            );
        }
    }

    /// <summary>
    /// 🔊 오디오 효과 업데이트
    /// </summary>
    void UpdateAudioEffects()
    {
        // 염증 소리 (욱신거림)
        if (inflammationAudio != null)
        {
            if (symptoms.earPain > 3f)
            {
                inflammationAudio.volume = (symptoms.earPain / 10f) * 0.3f;
                inflammationAudio.pitch = 0.8f + (severity * 0.4f);
                
                if (!inflammationAudio.isPlaying)
                {
                    inflammationAudio.Play();
                }
            }
            else
            {
                if (inflammationAudio.isPlaying)
                {
                    inflammationAudio.Stop();
                }
            }
        }
        
        // 액체 움직임 소리
        if (fluidAudio != null && fluidLevel > 0.3f)
        {
            // 간헐적으로 액체 소리 재생
            if (Random.Range(0f, 100f) < 2f) // 2% 확률
            {
                fluidAudio.volume = fluidLevel * 0.4f;
                fluidAudio.PlayOneShot(fluidMovementClip);
            }
        }
    }

    /*
     * ====================================================================
     * 🌐 공개 API 메서드들 (Public API Methods)
     * ====================================================================
     */

    /// <summary>
    /// 🦠 중이염 유발 (Public API)
    /// 
    /// @param otitisCategory: 중이염 유형
    /// @param initialSeverity: 초기 심각도 (0~1)
    /// </summary>
    public void TriggerOtitis(OtitisCategory category, float initialSeverity = 0.3f)
    {
        otitisType.category = category;
        severity = Mathf.Clamp01(initialSeverity);
        
        // 질병 재시작
        diseaseStartTime = Time.time;
        progression.currentStage = DiseaseStage.Incubation;
        
        SetupStageDurations();
        
        LogDebug($"🦠 중이염 유발: {category}, 심각도 {initialSeverity:F2}");
    }

    /// <summary>
    /// 💊 항생제 치료 시작 (Public API)
    /// 
    /// @param effectiveness: 항생제 효과 (0~1)
    /// </summary>
    public void StartAntibioticTreatment(float effectiveness = 0.8f)
    {
        progression.isOnAntibiotics = true;
        antibioticStartTime = Time.time;
        antibioticEffectiveness = Mathf.Clamp01(effectiveness);
        
        LogDebug($"💊 항생제 치료 시작: 효과 {effectiveness:F2}");
    }

    /// <summary>
    /// 💊 진통제 투여 (Public API)
    /// </summary>
    public void StartPainkillerTreatment()
    {
        progression.isOnPainkillers = true;
        painkillersStartTime = Time.time;
        
        LogDebug("💊 진통제 투여 시작");
    }

    /// <summary>
    /// 🛑 모든 치료 중단 (Public API)
    /// </summary>
    public void StopAllTreatments()
    {
        progression.isOnAntibiotics = false;
        progression.isOnPainkillers = false;
        antibioticStartTime = -1f;
        painkillersStartTime = -1f;
        
        LogDebug("🛑 모든 치료 중단");
    }

    /// <summary>
    /// 📊 현재 증상 상태 반환 (Public API)
    /// </summary>
    public OtitisSymptoms GetCurrentSymptoms()
    {
        return symptoms;
    }

    /// <summary>
    /// 📈 질병 진행 상태 반환 (Public API)
    /// </summary>
    public OtitisProgression GetDiseaseProgression()
    {
        return progression;
    }

    /// <summary>
    /// 🎮 심각도 설정 (Public API)
    /// </summary>
    public void SetSeverity(float newSeverity)
    {
        severity = Mathf.Clamp01(newSeverity);
        LogDebug($"🎮 심각도 설정: {severity:F2}");
    }

    /// <summary>
    /// 💧 액체 수준 설정 (Public API)
    /// </summary>
    public void SetFluidLevel(float newFluidLevel)
    {
        fluidLevel = Mathf.Clamp01(newFluidLevel);
        LogDebug($"💧 액체 수준 설정: {fluidLevel:F2}");
    }

    /// <summary>
    /// 🔄 중이염 시스템 재설정 (Public API)
    /// </summary>
    public void ResetOtitisSystem()
    {
        severity = 0f;
        fluidLevel = 0f;
        hasPerforated = false;
        
        progression.currentStage = DiseaseStage.Incubation;
        diseaseStartTime = Time.time;
        
        StopAllTreatments();
        InitializeSymptoms();
        
        LogDebug("🔄 중이염 시스템 재설정 완료");
    }

    /*
     * ====================================================================
     * 🐞 디버그 및 유틸리티 (Debug & Utilities)
     * ====================================================================
     */

    /// <summary>
    /// 🐞 조건부 디버그 로그
    /// </summary>
    void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[Otitis] {message}");
        }
    }

    /// <summary>
    /// 🎨 Scene View 기즈모 그리기
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !systemInitialized) return;
        
        // 심각도에 따른 색상
        if (severity > 0.7f)
            Gizmos.color = Color.red;
        else if (severity > 0.4f)
            Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
        else if (severity > 0.1f)
            Gizmos.color = Color.yellow;
        else
            Gizmos.color = Color.green;
        
        // 기본 중이 모양
        Gizmos.DrawWireSphere(transform.position, 0.01f);
        
        // 액체 수준 표시
        if (fluidLevel > 0.1f)
        {
            Gizmos.color = otitisType.isPurulent ? Color.yellow : Color.blue;
            Gizmos.DrawSphere(transform.position + Vector3.down * 0.005f, 0.003f * fluidLevel);
        }
        
        // 염증 표시
        if (severity > 0.3f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.015f);
        }
        
        // 고막 천공 표시
        if (hasPerforated)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawCube(transform.position + Vector3.forward * 0.01f, Vector3.one * 0.002f);
        }
    }

    /// <summary>
    /// 📋 GUI 정보 표시 (디버그용)
    /// </summary>
    void OnGUI()
    {
        if (!enableDebugLogs || !monitorSymptoms || !systemInitialized) return;
        
        string symptomText = "🦠 중이염 상태:\n";
        symptomText += $"단계: {progression.currentStage}\n";
        symptomText += $"심각도: {severity:F2}\n";
        symptomText += $"액체: {fluidLevel:F2}\n";
        symptomText += $"통증: {symptoms.earPain:F1}/10\n";
        symptomText += $"청력손실: {symptoms.hearingLoss:F1}dB\n";
        symptomText += $"체온: {symptoms.bodyTemperature:F1}°C\n";
        symptomText += $"치료효과: {progression.treatmentEffectiveness:F1}%\n";
        symptomText += $"천공위험: {progression.tympanicPerforationRisk:F1}%\n";
        
        if (hasPerforated)
        {
            symptomText += "⚠️ 고막 천공!\n";
        }
        
        GUI.Label(new Rect(580, 10, 200, 250), symptomText);
    }
}