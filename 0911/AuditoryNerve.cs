using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/*
 * ===============================================
 * 🧠 AUDITORY NERVE - 청신경 시스템 컴포넌트
 * ===============================================
 * 
 * 🧠 이 스크립트가 뭐야? (What is this?)
 * 청신경(제8뇌신경, Cranial Nerve VIII)을 시뮬레이션하는 컴포넌트입니다.
 * 내이에서 생성된 전기 신호를 뇌간으로 전달하는 중요한 역할을 담당합니다.
 * 실제 의학적 기능과 신경 전달 메커니즘을 기반으로 구현되었습니다.
 * 
 * 🧠 청신경의 주요 기능:
 * 1. 신호 전달 - 내이의 전기 신호를 뇌로 전송
 * 2. 신호 증폭 - 약한 신호를 강화하여 인식 가능하게 만듦
 * 3. 신호 필터링 - 노이즈 제거 및 중요한 정보만 선별
 * 4. 주파수 분석 - 음높이 정보 처리
 * 5. 시간 동기화 - 양쪽 귀의 신호 타이밍 조절
 * 6. 적응 조절 - 지속적 소음에 대한 적응 반응
 * 
 * 🔬 의학적 정확성:
 * - 실제 청신경의 해부학적 구조 반영
 * - 신경 섬유의 전도 속도 고려 (초당 50-120m)
 * - 시냅스 전달 지연 시간 포함
 * - 신경 피로와 회복 메커니즘 구현
 * - 노화와 손상에 따른 기능 저하 모델링
 * 
 * 💡 초보자를 위한 팁:
 * - 이 스크립트를 청신경 3D 모델에 붙이세요
 * - 파티클 시스템으로 신경 신호의 흐름을 시각화할 수 있습니다
 * - 신호 강도가 낮으면 청력 손실을 의미합니다
 * - 지연 시간이 길면 신경 전달 속도가 느린 것입니다
 */

[System.Serializable]
public class NerveProperties
{
    [Header("🧠 기본 신경 특성 (Basic Nerve Properties)")]
    [Tooltip("신경 섬유 개수 - 실제 청신경은 약 30,000개")]
    [Range(10000, 50000)]
    public int fiberCount = 30000;
    
    [Tooltip("신경 전도 속도 (m/s) - 실제는 50-120m/s")]
    [Range(30f, 150f)]
    public float conductionVelocity = 80f;
    
    [Tooltip("신경 섬유 직경 (μm) - 클수록 빠른 전도")]
    [Range(1f, 20f)]
    public float fiberDiameter = 5f;
    
    [Tooltip("수초 두께 - 절연체 역할, 클수록 빠른 전도")]
    [Range(0.5f, 3.0f)]
    public float myelinThickness = 1.5f;
    
    [Header("⚡ 전기적 특성 (Electrical Properties)")]
    [Tooltip("정지 전위 (mV) - 신경의 기본 전압")]
    [Range(-90f, -50f)]
    public float restingPotential = -70f;
    
    [Tooltip("활동 전위 임계값 (mV) - 이 전압을 넘어야 신호 발생")]
    [Range(-60f, -40f)]
    public float actionPotentialThreshold = -55f;
    
    [Tooltip("불응기 시간 (ms) - 신호 후 회복 시간")]
    [Range(1f, 5f)]
    public float refractoryPeriod = 2f;
    
    [Tooltip("시냅스 지연 시간 (ms) - 연결부에서의 지연")]
    [Range(0.5f, 3f)]
    public float synapticDelay = 1f;
    
    [Header("🔄 적응 특성 (Adaptation Properties)")]
    [Tooltip("신호 적응 속도 - 지속적 자극에 대한 둔화")]
    [Range(0.01f, 1f)]
    public float adaptationRate = 0.1f;
    
    [Tooltip("회복 속도 - 조용해진 후 민감도 회복")]
    [Range(0.01f, 0.5f)]
    public float recoveryRate = 0.05f;
    
    [Tooltip("최대 적응 수준 - 얼마나 둔해질 수 있는지")]
    [Range(0.1f, 0.8f)]
    public float maxAdaptation = 0.6f;
}

[System.Serializable]
public class NerveSignal
{
    [Header("📡 신호 정보 (Signal Information)")]
    [Tooltip("신호 강도 (0~1) - 1이 최대")]
    [ReadOnly] public float intensity = 0f;
    
    [Tooltip("신호 주파수 (Hz) - 음높이 정보")]
    [ReadOnly] public float frequency = 440f;
    
    [Tooltip("신호 지속 시간 (ms)")]
    [ReadOnly] public float duration = 0f;
    
    [Tooltip("신호 발생 시간")]
    [ReadOnly] public float timestamp = 0f;
    
    [Tooltip("신호 품질 (0~1) - 노이즈 대비 신호 비율")]
    [ReadOnly] public float quality = 1f;
    
    [Tooltip("신호가 활성화되어 있는지")]
    [ReadOnly] public bool isActive = false;
    
    [Header("🎯 처리 상태 (Processing Status)")]
    [Tooltip("신호 처리 단계")]
    [ReadOnly] public string processingStage = "Idle";
    
    [Tooltip("전달 지연 시간 (ms)")]
    [ReadOnly] public float transmissionDelay = 0f;
    
    [Tooltip("신호 손실률 (%)")]
    [ReadOnly] public float signalLoss = 0f;
}

[System.Serializable]
public class NerveHealth
{
    [Header("🏥 신경 건강 상태 (Nerve Health Status)")]
    [Tooltip("전체 신경 건강도 (0~1) - 1이 완전 건강")]
    [Range(0f, 1f)]
    [ReadOnly] public float overallHealth = 1f;
    
    [Tooltip("손상된 신경 섬유 비율 (%)")]
    [ReadOnly] public float damagedFibers = 0f;
    
    [Tooltip("염증 수준 (%) - 신경염 정도")]
    [ReadOnly] public float inflammation = 0f;
    
    [Tooltip("혈류 공급 상태 (%) - 100%가 정상")]
    [ReadOnly] public float bloodSupply = 100f;
    
    [Tooltip("신경 피로 수준 (%) - 과도한 사용으로 인한 피로")]
    [ReadOnly] public float fatigue = 0f;
    
    [Tooltip("회복 속도 (%) - 얼마나 빨리 회복되는지")]
    [ReadOnly] public float healingRate = 100f;
    
    [Header("📊 기능 평가 (Functional Assessment)")]
    [Tooltip("신호 전달 효율 (%) - 100%가 완벽")]
    [ReadOnly] public float transmissionEfficiency = 100f;
    
    [Tooltip("주파수 분석 능력 (%) - 음높이 구분 능력")]
    [ReadOnly] public float frequencyResolution = 100f;
    
    [Tooltip("시간 분해능 (%) - 빠른 소리 변화 감지 능력")]
    [ReadOnly] public float temporalResolution = 100f;
    
    [Tooltip("동적 범위 (%) - 작은소리~큰소리 처리 범위")]
    [ReadOnly] public float dynamicRange = 100f;
}

public class AuditoryNerve : MonoBehaviour
{
    [Header("🧠 신경 특성 (Nerve Properties)")]
    [Tooltip("청신경의 생리학적 특성 설정")]
    public NerveProperties properties;
    
    [Header("📡 신호 상태 (Signal Status)")]
    [Tooltip("현재 처리 중인 신호 정보")]
    public NerveSignal currentSignal;
    
    [Header("🏥 건강 상태 (Health Status)")]
    [Tooltip("신경의 전반적인 건강 상태")]
    public NerveHealth health;
    
    [Header("🎮 실시간 제어 (Runtime Controls)")]
    [Tooltip("신호 강도 (0~1) - 1이 최대 강도")]
    [Range(0f, 1f)]
    public float signalStrength = 1.0f;
    
    [Tooltip("신경 손상 수준 (0~1) - 0이 완전 건강, 1이 완전 손상")]
    [Range(0f, 1f)]
    public float damageLevel = 0f;
    
    [Tooltip("노화 정도 (0~1) - 나이에 따른 기능 저하")]
    [Range(0f, 1f)]
    public float agingLevel = 0f;
    
    [Tooltip("스트레스 수준 (0~1) - 신경계에 미치는 스트레스")]
    [Range(0f, 1f)]
    public float stressLevel = 0f;
    
    [Header("🎨 시각화 (Visualization)")]
    [Tooltip("신경 신호 파티클 시스템 - 전기 신호 흐름 표현")]
    public ParticleSystem signalParticles;
    
    [Tooltip("신경 섬유 라인 렌더러 - 신경 경로 표시")]
    public LineRenderer[] nerveFibers;
    
    [Tooltip("시냅스 연결점들 - 신경 연결부 표시")]
    public GameObject[] synapsePoints;
    
    [Tooltip("신경 메쉬 렌더러 - 신경 모양 표시")]
    public MeshRenderer nerveRenderer;
    
    [Header("🎨 색상 설정 (Color Settings)")]
    [Tooltip("건강한 신경 색상")]
    public Color healthyNerveColor = new Color(1f, 1f, 0.8f, 1f); // 밝은 노랑
    
    [Tooltip("손상된 신경 색상")]
    public Color damagedNerveColor = new Color(0.5f, 0.3f, 0.2f, 1f); // 어두운 갈색
    
    [Tooltip("활성화된 신호 색상")]
    public Color activeSignalColor = new Color(0f, 1f, 1f, 1f); // 청록색
    
    [Tooltip("비활성 신호 색상")]
    public Color inactiveSignalColor = new Color(0.3f, 0.3f, 0.3f, 1f); // 회색
    
    [Header("🔊 오디오 효과 (Audio Effects)")]
    [Tooltip("신경 활동 소리 - 전기 신호 음향화")]
    public AudioSource nerveActivityAudio;
    
    [Tooltip("신경 활동 사운드 클립")]
    public AudioClip nerveActivityClip;
    
    [Tooltip("신호 전달 완료 소리")]
    public AudioClip signalCompleteClip;
    
    [Header("🐞 디버그 (Debug)")]
    [Tooltip("디버그 정보 출력")]
    public bool enableDebugLogs = false;
    
    [Tooltip("신경 활동 모니터링 - 실시간 신호 추적")]
    public bool monitorNerveActivity = false;
    
    [Tooltip("성능 통계 표시")]
    public bool showPerformanceStats = false;
    
    // ============================================================================
    // 🔧 내부 변수들 (Private Variables)
    // ============================================================================
    
    private Queue<NerveSignal> signalQueue;           // 신호 대기열
    private List<float> recentSignalHistory;          // 최근 신호 이력
    private float currentAdaptationLevel = 0f;        // 현재 적응 수준
    private float nerveFatigueAccumulation = 0f;      // 신경 피로 누적
    private float lastSignalTime = 0f;                // 마지막 신호 시간
    private bool isProcessingSignal = false;          // 신호 처리 중 여부
    private Material nerveMaterial;                   // 신경 재질
    private float baseTransmissionDelay = 0f;         // 기본 전달 지연
    private int activeNeuronCount = 0;                // 활성 뉴런 수
    private bool systemInitialized = false;          // 시스템 초기화 완료
    
    // 성능 최적화용 캐시
    private float cachedEfficiency = 100f;
    private float cachedQuality = 100f;
    private float lastCacheUpdateTime = 0f;
    private float cacheUpdateInterval = 0.1f;
    
    // 신호 처리 통계
    private int totalSignalsProcessed = 0;
    private float averageProcessingTime = 0f;
    private float peakSignalStrength = 0f;

    /*
     * ====================================================================
     * 🚀 UNITY 생명주기 메서드들 (Unity Lifecycle Methods)
     * ====================================================================
     */

    /// <summary>
    /// 🎬 START - 청신경 시스템 초기화
    /// 
    /// 초기화 과정:
    /// 1. 신경 특성 검증 및 설정
    /// 2. 신호 처리 시스템 준비
    /// 3. 시각화 컴포넌트 설정
    /// 4. 오디오 시스템 초기화
    /// 5. 건강 상태 초기화
    /// </summary>
    void Start()
    {
        LogDebug("🧠 청신경 시스템 초기화 시작...");
        
        InitializeNerveSystem();
        SetupSignalProcessing();
        SetupVisualization();
        SetupAudioSystem();
        InitializeHealthStatus();
        
        systemInitialized = true;
        LogDebug("✅ 청신경 시스템 초기화 완료");
    }

    /// <summary>
    /// 🔄 UPDATE - 실시간 신경 활동 처리
    /// 
    /// 매 프레임 실행 내용:
    /// 1. 신호 대기열 처리
    /// 2. 신경 적응 및 피로 계산
    /// 3. 건강 상태 업데이트
    /// 4. 시각화 및 오디오 효과
    /// </summary>
    void Update()
    {
        if (!systemInitialized) return;
        
        // 신호 대기열 처리 (매 프레임)
        ProcessSignalQueue();
        
        // 신경 적응 및 피로 (매 프레임)
        UpdateNerveAdaptation();
        UpdateNerveFatigue();
        
        // 건강 상태 업데이트 (캐시 최적화)
        if (Time.time - lastCacheUpdateTime >= cacheUpdateInterval)
        {
            UpdateHealthStatus();
            lastCacheUpdateTime = Time.time;
        }
        
        // 시각화 업데이트 (조건부)
        if (currentSignal.isActive || HasRecentActivity())
        {
            UpdateVisualization();
            UpdateAudioEffects();
        }
        
        // 성능 통계 업데이트
        if (showPerformanceStats)
        {
            UpdatePerformanceStats();
        }
    }

    /*
     * ====================================================================
     * 🛠️ 초기화 메서드들 (Initialization Methods)
     * ====================================================================
     */

    /// <summary>
    /// 🔧 신경 시스템 기본 초기화
    /// </summary>
    void InitializeNerveSystem()
    {
        // 기본 설정 검증
        if (properties == null)
        {
            properties = new NerveProperties();
            LogDebug("⚠️ 신경 특성이 설정되지 않아 기본값으로 초기화");
        }
        
        if (currentSignal == null)
        {
            currentSignal = new NerveSignal();
        }
        
        if (health == null)
        {
            health = new NerveHealth();
        }
        
        // 큐와 리스트 초기화
        signalQueue = new Queue<NerveSignal>();
        recentSignalHistory = new List<float>();
        
        // 기본 전달 지연 계산 (거리 / 속도)
        float nerveLength = 0.025f; // 내이에서 뇌간까지 약 2.5cm
        baseTransmissionDelay = (nerveLength / properties.conductionVelocity) * 1000f; // ms 단위
        
        ValidateNerveProperties();
        LogDebug("🔧 신경 시스템 기본 초기화 완료");
    }

    /// <summary>
    /// 🔍 신경 특성 유효성 검사
    /// </summary>
    void ValidateNerveProperties()
    {
        properties.fiberCount = Mathf.Clamp(properties.fiberCount, 10000, 50000);
        properties.conductionVelocity = Mathf.Clamp(properties.conductionVelocity, 30f, 150f);
        properties.fiberDiameter = Mathf.Clamp(properties.fiberDiameter, 1f, 20f);
        properties.refractoryPeriod = Mathf.Clamp(properties.refractoryPeriod, 1f, 5f);
        
        LogDebug($"신경 특성 검증: 섬유 {properties.fiberCount}개, 속도 {properties.conductionVelocity}m/s");
    }

    /// <summary>
    /// 📡 신호 처리 시스템 설정
    /// </summary>
    void SetupSignalProcessing()
    {
        currentSignal.processingStage = "Idle";
        currentSignal.isActive = false;
        
        // 활성 뉴런 수 계산 (건강 상태에 따라)
        activeNeuronCount = Mathf.RoundToInt(properties.fiberCount * (1f - damageLevel));
        
        LogDebug($"📡 신호 처리 시스템 설정: 활성 뉴런 {activeNeuronCount}개");
    }

    /// <summary>
    /// 🎨 시각화 시스템 설정
    /// </summary>
    void SetupVisualization()
    {
        // 신경 메쉬 재질 설정
        if (nerveRenderer != null)
        {
            nerveMaterial = nerveRenderer.material;
            if (nerveMaterial != null)
            {
                nerveMaterial.color = healthyNerveColor;
            }
        }
        
        // 파티클 시스템 설정
        if (signalParticles != null)
        {
            var main = signalParticles.main;
            main.startColor = inactiveSignalColor;
            main.maxParticles = 100;
            
            var emission = signalParticles.emission;
            emission.rateOverTime = 0f;
        }
        
        // 신경 섬유 라인 렌더러 설정
        SetupNerveFibers();
        
        // 시냅스 연결점 설정
        SetupSynapsePoints();
        
        LogDebug("🎨 시각화 시스템 설정 완료");
    }

    /// <summary>
    /// 🕸️ 신경 섬유 라인 렌더러 설정
    /// </summary>
    void SetupNerveFibers()
    {
        if (nerveFibers != null && nerveFibers.Length > 0)
        {
            foreach (LineRenderer fiber in nerveFibers)
            {
                if (fiber != null)
                {
                    fiber.startColor = healthyNerveColor;
                    fiber.endColor = healthyNerveColor;
                    fiber.startWidth = 0.001f;
                    fiber.endWidth = 0.0005f;
                    fiber.positionCount = 2;
                    fiber.useWorldSpace = true;
                }
            }
        }
    }

    /// <summary>
    /// 🔗 시냅스 연결점 설정
    /// </summary>
    void SetupSynapsePoints()
    {
        if (synapsePoints != null && synapsePoints.Length > 0)
        {
            foreach (GameObject synapse in synapsePoints)
            {
                if (synapse != null)
                {
                    // 시냅스 기본 상태 설정
                    synapse.SetActive(true);
                    
                    // 시냅스 색상 설정
                    Renderer synapseRenderer = synapse.GetComponent<Renderer>();
                    if (synapseRenderer != null)
                    {
                        synapseRenderer.material.color = inactiveSignalColor;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 🔊 오디오 시스템 설정
    /// </summary>
    void SetupAudioSystem()
    {
        if (nerveActivityAudio == null)
        {
            nerveActivityAudio = GetComponent<AudioSource>();
        }
        
        if (nerveActivityAudio != null)
        {
            nerveActivityAudio.clip = nerveActivityClip;
            nerveActivityAudio.loop = false;
            nerveActivityAudio.volume = 0.2f;
            nerveActivityAudio.pitch = 1.0f;
            nerveActivityAudio.spatialBlend = 0.7f; // 3D 사운드
        }
        
        LogDebug("🔊 오디오 시스템 설정 완료");
    }

    /// <summary>
    /// 🏥 건강 상태 초기화
    /// </summary>
    void InitializeHealthStatus()
    {
        health.overallHealth = 1f - damageLevel;
        health.damagedFibers = damageLevel * 100f;
        health.inflammation = 0f;
        health.bloodSupply = 100f;
        health.fatigue = 0f;
        health.healingRate = 100f * (1f - agingLevel * 0.5f);
        
        health.transmissionEfficiency = 100f * health.overallHealth;
        health.frequencyResolution = 100f * health.overallHealth;
        health.temporalResolution = 100f * health.overallHealth;
        health.dynamicRange = 100f * health.overallHealth;
        
        LogDebug($"🏥 건강 상태 초기화: 전체 건강 {health.overallHealth:F2}");
    }

    /*
     * ====================================================================
     * 📡 신호 처리 메서드들 (Signal Processing Methods)
     * ====================================================================
     */

    /// <summary>
    /// 📡 외부 신호 수신 (Public API)
    /// 
    /// 내이에서 전달받은 전기 신호를 처리 대기열에 추가
    /// 
    /// @param intensity: 신호 강도 (0~1)
    /// @param frequency: 신호 주파수 (Hz)
    /// @param duration: 신호 지속 시간 (ms)
    /// </summary>
    public void TransmitSignal(float intensity)
    {
        // 신호 강도 유효성 검사
        if (intensity < 0.001f)
        {
            // 너무 약한 신호는 무시
            SetSignalInactive();
            return;
        }
        
        // 새 신호 생성
        NerveSignal newSignal = new NerveSignal
        {
            intensity = Mathf.Clamp01(intensity),
            frequency = 440f, // 기본값, 추후 확장 가능
            duration = 100f,  // 기본 100ms
            timestamp = Time.time,
            quality = CalculateSignalQuality(intensity),
            isActive = true,
            processingStage = "Received"
        };
        
        // 신호 대기열에 추가
        signalQueue.Enqueue(newSignal);
        
        // 현재 신호 업데이트
        currentSignal = newSignal;
        
        // 통계 업데이트
        totalSignalsProcessed++;
        if (intensity > peakSignalStrength)
        {
            peakSignalStrength = intensity;
        }
        
        LogDebug($"📡 신호 수신: 강도 {intensity:F3}, 품질 {newSignal.quality:F2}");
    }

    /// <summary>
    /// 🔄 신호 대기열 처리
    /// 
    /// 대기 중인 신호들을 순서대로 처리
    /// </summary>
    void ProcessSignalQueue()
    {
        // 현재 신호가 처리 중이면 대기
        if (isProcessingSignal) return;
        
        // 대기열에 신호가 있으면 처리 시작
        if (signalQueue.Count > 0)
        {
            StartCoroutine(ProcessSingleSignal(signalQueue.Dequeue()));
        }
    }

    /// <summary>
    /// ⚡ 단일 신호 처리 (코루틴)
    /// 
    /// 신경 전달의 실제 과정을 시뮬레이션:
    /// 1. 신호 수신 및 검증
    /// 2. 신경 섬유를 통한 전도
    /// 3. 시냅스 전달
    /// 4. 뇌간으로 신호 전송
    /// </summary>
    System.Collections.IEnumerator ProcessSingleSignal(NerveSignal signal)
    {
        isProcessingSignal = true;
        float startTime = Time.time;
        
        // 1단계: 신호 접수 및 전처리
        signal.processingStage = "Preprocessing";
        yield return StartCoroutine(PreprocessSignal(signal));
        
        // 2단계: 신경 전도
        signal.processingStage = "Conducting";
        yield return StartCoroutine(ConductSignal(signal));
        
        // 3단계: 시냅스 전달
        signal.processingStage = "Synaptic";
        yield return StartCoroutine(SynapticTransmission(signal));
        
        // 4단계: 뇌간 전달
        signal.processingStage = "Brainstem";
        yield return StartCoroutine(TransmitToBrainstem(signal));
        
        // 처리 완료
        signal.processingStage = "Completed";
        signal.isActive = false;
        
        // 처리 시간 계산
        float processingTime = (Time.time - startTime) * 1000f; // ms 단위
        signal.transmissionDelay = processingTime;
        
        // 평균 처리 시간 업데이트
        averageProcessingTime = (averageProcessingTime + processingTime) / 2f;
        
        // 최근 신호 이력에 추가
        recentSignalHistory.Add(signal.intensity);
        if (recentSignalHistory.Count > 50) // 최근 50개만 유지
        {
            recentSignalHistory.RemoveAt(0);
        }
        
        // 신경 피로 누적
        nerveFatigueAccumulation += signal.intensity * 0.1f;
        lastSignalTime = Time.time;
        
        isProcessingSignal = false;
        
        LogDebug($"⚡ 신호 처리 완료: {processingTime:F1}ms, 손실 {signal.signalLoss:F1}%");
    }

    /// <summary>
    /// 🔧 신호 전처리
    /// 
    /// 수신된 신호의 품질 검사 및 초기 처리
    /// </summary>
    System.Collections.IEnumerator PreprocessSignal(NerveSignal signal)
    {
        // 신호 강도에 손상 수준 적용
        signal.intensity *= (1f - damageLevel);
        
        // 노화에 따른 신호 약화
        signal.intensity *= (1f - agingLevel * 0.3f);
        
        // 피로에 따른 신호 약화
        float fatigueEffect = 1f - (nerveFatigueAccumulation / properties.fiberCount);
        signal.intensity *= Mathf.Clamp01(fatigueEffect);
        
        // 신호 품질 재계산
        signal.quality = CalculateSignalQuality(signal.intensity);
        
        // 처리 지연 시뮬레이션
        yield return new WaitForSeconds(0.001f); // 1ms
    }

    /// <summary>
    /// 🏃 신경 전도
    /// 
    /// 신경 섬유를 통한 활동 전위 전파
    /// </summary>
    System.Collections.IEnumerator ConductSignal(NerveSignal signal)
    {
        // 전도 속도 계산 (손상과 노화 반영)
        float effectiveVelocity = properties.conductionVelocity;
        effectiveVelocity *= (1f - damageLevel * 0.5f);
        effectiveVelocity *= (1f - agingLevel * 0.3f);
        
        // 전도 시간 계산
        float conductionDelay = baseTransmissionDelay / effectiveVelocity;
        
        // 신호 손실 계산
        float conductionLoss = damageLevel * 10f + agingLevel * 5f;
        signal.signalLoss += conductionLoss;
        signal.intensity *= (1f - conductionLoss / 100f);
        
        // 전도 시뮬레이션
        yield return new WaitForSeconds(conductionDelay / 1000f);
        
        LogDebug($"🏃 신경 전도: 속도 {effectiveVelocity:F0}m/s, 지연 {conductionDelay:F2}ms");
    }

    /// <summary>
    /// 🔗 시냅스 전달
    /// 
    /// 신경 연결부에서의 화학적 신호 전달
    /// </summary>
    System.Collections.IEnumerator SynapticTransmission(NerveSignal signal)
    {
        // 시냅스 지연
        float synapticDelay = properties.synapticDelay;
        
        // 스트레스에 따른 시냅스 효율 변화
        float stressEffect = 1f - stressLevel * 0.2f;
        signal.intensity *= stressEffect;
        
        // 염증에 따른 시냅스 지연 증가
        if (health.inflammation > 10f)
        {
            synapticDelay *= (1f + health.inflammation / 100f);
        }
        
        // 시냅스 전달 손실
        float synapticLoss = damageLevel * 5f;
        signal.signalLoss += synapticLoss;
        signal.intensity *= (1f - synapticLoss / 100f);
        
        yield return new WaitForSeconds(synapticDelay / 1000f);
        
        LogDebug($"🔗 시냅스 전달: 지연 {synapticDelay:F2}ms, 효율 {stressEffect:F2}");
    }

    /// <summary>
    /// 🧠 뇌간으로 신호 전송
    /// 
    /// 최종적으로 뇌간의 청각 핵으로 신호 전달
    /// </summary>
    System.Collections.IEnumerator TransmitToBrainstem(NerveSignal signal)
    {
        // 최종 신호 강도 적용
        signalStrength = signal.intensity;
        
        // 신호 완료 사운드 재생
        if (nerveActivityAudio != null && signalCompleteClip != null)
        {
            nerveActivityAudio.PlayOneShot(signalCompleteClip, signal.intensity * 0.3f);
        }
        
        // 뇌간 전달 시뮬레이션
        yield return new WaitForSeconds(0.002f); // 2ms
        
        LogDebug($"🧠 뇌간 전달 완료: 최종 강도 {signalStrength:F3}");
    }

    /// <summary>
    /// 📊 신호 품질 계산
    /// 
    /// 신호의 전반적인 품질을 평가
    /// </summary>
    float CalculateSignalQuality(float intensity)
    {
        float quality = intensity;
        
        // 손상에 따른 품질 저하
        quality *= (1f - damageLevel);
        
        // 노화에 따른 품질 저하
        quality *= (1f - agingLevel * 0.4f);
        
        // 피로에 따른 품질 저하
        float fatigueRatio = nerveFatigueAccumulation / properties.fiberCount;
        quality *= (1f - fatigueRatio * 0.5f);
        
        // 스트레스에 따른 품질 저하
        quality *= (1f - stressLevel * 0.3f);
        
        return Mathf.Clamp01(quality);
    }

    /// <summary>
    /// 🔇 신호 비활성화
    /// </summary>
    void SetSignalInactive()
    {
        currentSignal.isActive = false;
        currentSignal.intensity = 0f;
        currentSignal.processingStage = "Idle";
        signalStrength = 0f;
    }

    /*
     * ====================================================================
     * 🧠 신경 생리학 메서드들 (Neural Physiology Methods)
     * ====================================================================
     */

    /// <summary>
    /// 🔄 신경 적응 업데이트
    /// 
    /// 지속적인 자극에 대한 신경의 적응 반응
    /// </summary>
    void UpdateNerveAdaptation()
    {
        if (HasRecentActivity())
        {
            // 적응 증가 (지속적 자극시)
            float targetAdaptation = properties.maxAdaptation;
            currentAdaptationLevel = Mathf.Lerp(currentAdaptationLevel, targetAdaptation, 
                                              properties.adaptationRate * Time.deltaTime);
        }
        else
        {
            // 적응 회복 (조용한 상태시)
            currentAdaptationLevel = Mathf.Lerp(currentAdaptationLevel, 0f, 
                                              properties.recoveryRate * Time.deltaTime);
        }
        
        // 적응 수준을 신호 강도에 반영
        float adaptationEffect = 1f - currentAdaptationLevel;
        signalStrength *= adaptationEffect;
    }

    /// <summary>
    /// 😴 신경 피로 업데이트
    /// 
    /// 과도한 사용으로 인한 신경 피로 관리
    /// </summary>
    void UpdateNerveFatigue()
    {
        // 시간에 따른 피로 회복
        if (Time.time - lastSignalTime > 1f) // 1초 이상 조용하면
        {
            nerveFatigueAccumulation = Mathf.Lerp(nerveFatigueAccumulation, 0f, Time.deltaTime * 0.5f);
        }
        
        // 피로 수준 계산
        health.fatigue = Mathf.Clamp01(nerveFatigueAccumulation / properties.fiberCount) * 100f;
        
        // 과도한 피로시 보호 메커니즘
        if (health.fatigue > 80f)
        {
            signalStrength *= 0.5f; // 신호 강도 50% 감소
            LogDebug("😴 신경 과피로 - 보호 모드 활성화");
        }
    }

    /// <summary>
    /// 🕒 최근 활동 여부 확인
    /// </summary>
    bool HasRecentActivity()
    {
        return Time.time - lastSignalTime < 0.5f; // 0.5초 이내
    }

    /// <summary>
    /// 🏥 건강 상태 업데이트
    /// </summary>
    void UpdateHealthStatus()
    {
        // 전체 건강도 계산
        health.overallHealth = (1f - damageLevel) * (1f - agingLevel * 0.5f);
        health.damagedFibers = damageLevel * 100f;
        
        // 혈류 공급 상태 (스트레스 반영)
        health.bloodSupply = 100f * (1f - stressLevel * 0.3f);
        
        // 염증 수준 (스트레스와 피로 반영)
        health.inflammation = (stressLevel + health.fatigue / 100f) * 50f;
        
        // 치유율 (나이와 건강 상태 반영)
        health.healingRate = 100f * health.overallHealth * (1f - agingLevel * 0.5f);
        
        // 기능 평가
        UpdateFunctionalAssessment();
        
        cachedEfficiency = health.transmissionEfficiency;
        cachedQuality = health.overallHealth * 100f;
    }

    /// <summary>
    /// 📊 기능 평가 업데이트
    /// </summary>
    void UpdateFunctionalAssessment()
    {
        float baseEfficiency = health.overallHealth * 100f;
        
        // 전달 효율 (피로와 적응 반영)
        health.transmissionEfficiency = baseEfficiency * (1f - health.fatigue / 100f) * (1f - currentAdaptationLevel);
        
        // 주파수 분해능 (노화에 특히 민감)
        health.frequencyResolution = baseEfficiency * (1f - agingLevel * 0.6f);
        
        // 시간 분해능 (피로에 민감)
        health.temporalResolution = baseEfficiency * (1f - health.fatigue / 100f);
        
        // 동적 범위 (전체적인 건강 상태 반영)
        health.dynamicRange = baseEfficiency * (1f - stressLevel * 0.4f);
        
        // 모든 값을 0-100 범위로 제한
        health.transmissionEfficiency = Mathf.Clamp(health.transmissionEfficiency, 0f, 100f);
        health.frequencyResolution = Mathf.Clamp(health.frequencyResolution, 0f, 100f);
        health.temporalResolution = Mathf.Clamp(health.temporalResolution, 0f, 100f);
        health.dynamicRange = Mathf.Clamp(health.dynamicRange, 0f, 100f);
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
        UpdateNerveColorAndAppearance();
        UpdateSignalParticles();
        UpdateNerveFiberVisualization();
        UpdateSynapseVisualization();
    }

    /// <summary>
    /// 🌈 신경 색상 및 외관 업데이트
    /// </summary>
    void UpdateNerveColorAndAppearance()
    {
        if (nerveMaterial == null) return;
        
        // 건강 상태에 따른 색상
        Color targetColor = Color.Lerp(damagedNerveColor, healthyNerveColor, health.overallHealth);
        
        // 활성 신호가 있으면 활성화 색상
        if (currentSignal.isActive)
        {
            targetColor = Color.Lerp(targetColor, activeSignalColor, currentSignal.intensity);
        }
        
        // 염증이 있으면 붉은기 추가
        if (health.inflammation > 20f)
        {
            float inflammationRatio = health.inflammation / 100f;
            targetColor = Color.Lerp(targetColor, Color.red, inflammationRatio * 0.3f);
        }
        
        nerveMaterial.color = Color.Lerp(nerveMaterial.color, targetColor, Time.deltaTime * 3f);
        
        // 투명도 조절 (건강도에 따라)
        Color currentColor = nerveMaterial.color;
        currentColor.a = 0.7f + health.overallHealth * 0.3f;
        nerveMaterial.color = currentColor;
    }

    /// <summary>
    /// ✨ 신호 파티클 업데이트
    /// </summary>
    void UpdateSignalParticles()
    {
        if (signalParticles == null) return;
        
        var emission = signalParticles.emission;
        var main = signalParticles.main;
        
        if (currentSignal.isActive)
        {
            // 활성 신호시 파티클 생성
            emission.rateOverTime = currentSignal.intensity * 50f;
            main.startColor = Color.Lerp(inactiveSignalColor, activeSignalColor, currentSignal.intensity);
            
            // 신호 강도에 따른 파티클 속도
            var velocityOverLifetime = signalParticles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(currentSignal.intensity * 0.1f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);
        }
        else
        {
            // 비활성시 파티클 중단
            emission.rateOverTime = 0f;
            main.startColor = inactiveSignalColor;
        }
        
        // 신경 건강도에 따른 파티클 수명
        main.startLifetime = 2f * health.overallHealth;
    }

    /// <summary>
    /// 🕸️ 신경 섬유 시각화 업데이트
    /// </summary>
    void UpdateNerveFiberVisualization()
    {
        if (nerveFibers == null || nerveFibers.Length == 0) return;
        
        for (int i = 0; i < nerveFibers.Length; i++)
        {
            LineRenderer fiber = nerveFibers[i];
            if (fiber == null) continue;
            
            // 건강도에 따른 색상
            Color fiberColor = Color.Lerp(damagedNerveColor, healthyNerveColor, health.overallHealth);
            
            // 활성 신호가 있으면 일부 섬유만 활성화 표시
            if (currentSignal.isActive && i < nerveFibers.Length * currentSignal.intensity)
            {
                fiberColor = Color.Lerp(fiberColor, activeSignalColor, 0.8f);
            }
            
            fiber.startColor = fiberColor;
            fiber.endColor = fiberColor;
            
            // 건강도에 따른 두께
            float healthThickness = 0.001f * health.overallHealth;
            fiber.startWidth = healthThickness;
            fiber.endWidth = healthThickness * 0.5f;
        }
    }

    /// <summary>
    /// 🔗 시냅스 시각화 업데이트
    /// </summary>
    void UpdateSynapseVisualization()
    {
        if (synapsePoints == null || synapsePoints.Length == 0) return;
        
        foreach (GameObject synapse in synapsePoints)
        {
            if (synapse == null) continue;
            
            Renderer synapseRenderer = synapse.GetComponent<Renderer>();
            if (synapseRenderer == null) continue;
            
            // 신호 처리 단계에 따른 시냅스 활성화
            Color synapseColor = inactiveSignalColor;
            
            if (currentSignal.isActive && currentSignal.processingStage == "Synaptic")
            {
                synapseColor = Color.Lerp(inactiveSignalColor, activeSignalColor, currentSignal.intensity);
            }
            
            synapseRenderer.material.color = synapseColor;
            
            // 건강도에 따른 크기
            float healthScale = 0.8f + health.overallHealth * 0.4f;
            synapse.transform.localScale = Vector3.one * healthScale;
        }
    }

    /// <summary>
    /// 🔊 오디오 효과 업데이트
    /// </summary>
    void UpdateAudioEffects()
    {
        if (nerveActivityAudio == null) return;
        
        if (currentSignal.isActive && nerveActivityClip != null)
        {
            // 신호 강도에 따른 볼륨과 피치
            nerveActivityAudio.volume = 0.2f * currentSignal.intensity;
            nerveActivityAudio.pitch = 0.8f + currentSignal.intensity * 0.4f;
            
            // 간헐적 재생 (너무 자주 재생하지 않도록)
            if (!nerveActivityAudio.isPlaying)
            {
                nerveActivityAudio.Play();
            }
        }
        else
        {
            // 비활성시 오디오 중단
            if (nerveActivityAudio.isPlaying)
            {
                nerveActivityAudio.Stop();
            }
        }
    }

    /// <summary>
    /// 📊 성능 통계 업데이트
    /// </summary>
    void UpdatePerformanceStats()
    {
        // 통계 정보는 OnGUI에서 표시됨
    }

    /*
     * ====================================================================
     * 🌐 공개 API 메서드들 (Public API Methods)
     * ====================================================================
     */

    /// <summary>
    /// 💊 신경 손상 설정 (Public API)
    /// 
    /// @param damage: 손상 수준 (0.0~1.0)
    /// </summary>
    public void SetDamageLevel(float damage)
    {
        damageLevel = Mathf.Clamp01(damage);
        activeNeuronCount = Mathf.RoundToInt(properties.fiberCount * (1f - damageLevel));
        LogDebug($"💊 신경 손상 설정: {damageLevel:F2} (활성 뉴런: {activeNeuronCount})");
    }

    /// <summary>
    /// 👴 노화 수준 설정 (Public API)
    /// 
    /// @param aging: 노화 수준 (0.0~1.0)
    /// </summary>
    public void SetAgingLevel(float aging)
    {
        agingLevel = Mathf.Clamp01(aging);
        LogDebug($"👴 노화 수준 설정: {agingLevel:F2}");
    }

    /// <summary>
    /// 😰 스트레스 수준 설정 (Public API)
    /// 
    /// @param stress: 스트레스 수준 (0.0~1.0)
    /// </summary>
    public void SetStressLevel(float stress)
    {
        stressLevel = Mathf.Clamp01(stress);
        LogDebug($"😰 스트레스 수준 설정: {stressLevel:F2}");
    }

    /// <summary>
    /// 📊 현재 신경 건강 상태 반환 (Public API)
    /// </summary>
    public NerveHealth GetNerveHealth()
    {
        return health;
    }

    /// <summary>
    /// 📡 현재 신호 정보 반환 (Public API)
    /// </summary>
    public NerveSignal GetCurrentSignal()
    {
        return currentSignal;
    }

    /// <summary>
    /// ⚡ 신호 전달 효율 반환 (Public API)
    /// </summary>
    public float GetTransmissionEfficiency()
    {
        return health.transmissionEfficiency;
    }

    /// <summary>
    /// 🔄 신경 시스템 재설정 (Public API)
    /// </summary>
    public void ResetNerveSystem()
    {
        signalQueue.Clear();
        recentSignalHistory.Clear();
        currentAdaptationLevel = 0f;
        nerveFatigueAccumulation = 0f;
        isProcessingSignal = false;
        
        SetSignalInactive();
        InitializeHealthStatus();
        
        LogDebug("🔄 신경 시스템 재설정 완료");
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
            Debug.Log($"[AuditoryNerve] {message}");
        }
    }

    /// <summary>
    /// 🎨 Scene View 기즈모 그리기
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !systemInitialized) return;
        
        // 신경 건강도에 따른 색상
        if (health.overallHealth > 0.8f)
            Gizmos.color = Color.green;
        else if (health.overallHealth > 0.5f)
            Gizmos.color = Color.yellow;
        else
            Gizmos.color = Color.red;
        
        // 기본 신경 모양
        Gizmos.DrawWireSphere(transform.position, 0.008f);
        
        // 활성 신호 표시
        if (currentSignal.isActive)
        {
            Gizmos.color = activeSignalColor;
            Gizmos.DrawSphere(transform.position, 0.003f * currentSignal.intensity);
        }
        
        // 신경 경로 표시
        Gizmos.color = Color.white;
        Vector3 brainDirection = Vector3.up * 0.02f;
        Gizmos.DrawRay(transform.position, brainDirection);
        
        // 손상 정도 표시
        if (damageLevel > 0.1f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.right * 0.01f, Vector3.one * damageLevel * 0.005f);
        }
    }

    /// <summary>
    /// 📋 GUI 정보 표시 (디버그용)
    /// </summary>
    void OnGUI()
    {
        if (!enableDebugLogs || !showPerformanceStats || !systemInitialized) return;
        
        string statsText = "🧠 청신경 상태:\n";
        statsText += $"건강도: {health.overallHealth:F2}\n";
        statsText += $"신호 강도: {signalStrength:F3}\n";
        statsText += $"처리 단계: {currentSignal.processingStage}\n";
        statsText += $"전달 효율: {health.transmissionEfficiency:F1}%\n";
        statsText += $"피로도: {health.fatigue:F1}%\n";
        statsText += $"적응도: {currentAdaptationLevel:F2}\n";
        statsText += $"활성 뉴런: {activeNeuronCount}\n";
        statsText += $"처리된 신호: {totalSignalsProcessed}\n";
        statsText += $"평균 지연: {averageProcessingTime:F1}ms\n";
        
        GUI.Label(new Rect(320, 10, 250, 250), statsText);
    }
}