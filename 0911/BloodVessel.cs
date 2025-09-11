using UnityEngine;
using System.Collections;

/*
 * ===============================================
 * 🩸 BLOOD VESSEL - 혈관 시스템 컴포넌트
 * ===============================================
 * 
 * 🧠 이 스크립트가 뭐야? (What is this?)
 * 귀 안의 혈관계를 시뮬레이션하는 컴포넌트입니다.
 * 혈관은 귀의 모든 조직에 영양을 공급하고, 염증 반응을 조절하며,
 * 감염이나 손상 시 치료 과정에 핵심적인 역할을 합니다.
 * 
 * 🩸 혈관의 주요 기능:
 * 1. 영양 공급 - 산소와 영양분을 귀 조직에 전달
 * 2. 노폐물 제거 - 대사 산물과 독소를 제거
 * 3. 면역 반응 - 감염 시 백혈구와 항체 운반
 * 4. 염증 조절 - 염증 반응의 시작과 종료 조절
 * 5. 온도 조절 - 귀 내부 온도 유지
 * 
 * 🔬 의학적 정확성:
 * - 실제 귀의 혈관 해부학 기반
 * - 혈류 변화가 청력에 미치는 영향 반영
 * - 염증성 질환(중이염)과 혈관계 상호작용 모델링
 * 
 * 💡 초보자를 위한 팁:
 * - 이 스크립트를 혈관 3D 모델에 붙이세요
 * - 색상 변화로 혈류 상태를 시각적으로 확인할 수 있습니다
 * - 염증이 생기면 혈관이 붓고 색이 짙어집니다
 */

[System.Serializable]
public class BloodVesselProperties
{
    [Header("🩸 기본 혈관 특성 (Basic Vessel Properties)")]
    [Tooltip("혈관의 기본 직경 (mm) - 실제 귀 혈관은 0.1-2mm")]
    [Range(0.1f, 2.0f)]
    public float baseDiameter = 0.5f;
    
    [Tooltip("혈관 벽의 탄성 - 클수록 더 유연함")]
    [Range(0.1f, 2.0f)]
    public float elasticity = 1.0f;
    
    [Tooltip("혈관의 저항 - 클수록 혈류가 어려움")]
    [Range(0.5f, 3.0f)]
    public float resistance = 1.0f;
    
    [Header("💓 혈류 동역학 (Hemodynamics)")]
    [Tooltip("정상 혈압 (mmHg) - 귀의 평균 혈압")]
    [Range(60f, 120f)]
    public float normalBloodPressure = 80f;
    
    [Tooltip("혈액 점도 - 클수록 끈적끈적함")]
    [Range(0.8f, 1.5f)]
    public float bloodViscosity = 1.0f;
    
    [Tooltip("맥박 강도 - 심장박동에 따른 혈류 변화")]
    [Range(0.1f, 2.0f)]
    public float pulseStrength = 1.0f;
    
    [Header("🦠 면역 반응 (Immune Response)")]
    [Tooltip("백혈구 농도 - 감염 시 증가")]
    [Range(0.5f, 3.0f)]
    public float whiteBloodCellCount = 1.0f;
    
    [Tooltip("항체 수준 - 면역력 지표")]
    [Range(0.5f, 2.0f)]
    public float antibodyLevel = 1.0f;
    
    [Tooltip("혈관 투과성 - 클수록 물질 교환이 활발")]
    [Range(0.5f, 2.0f)]
    public float permeability = 1.0f;
}

[System.Serializable]
public class BloodVesselStatus
{
    [Header("📊 실시간 혈관 상태 (Real-time Vessel Status)")]
    [Tooltip("현재 혈류량 (%) - 100%가 정상")]
    [ReadOnly] public float currentBloodFlow = 100f;
    
    [Tooltip("현재 염증 수준 (%) - 0%가 정상")]
    [ReadOnly] public float currentInflammation = 0f;
    
    [Tooltip("혈관 확장 정도 (%) - 100%가 정상, 클수록 부어있음")]
    [ReadOnly] public float vasodilation = 100f;
    
    [Tooltip("산소 공급률 (%) - 100%가 최적")]
    [ReadOnly] public float oxygenSupply = 100f;
    
    [Tooltip("독소 제거율 (%) - 100%가 최적")]
    [ReadOnly] public float toxinClearance = 100f;
    
    [Header("🩺 건강 지표 (Health Indicators)")]
    [Tooltip("혈관 건강 상태")]
    [ReadOnly] public string vesselHealth = "Healthy";
    
    [Tooltip("현재 맥박수 (BPM)")]
    [ReadOnly] public float heartRate = 70f;
    
    [Tooltip("혈압 (mmHg)")]
    [ReadOnly] public float currentBloodPressure = 80f;
    
    [Tooltip("혈관 온도 (°C)")]
    [ReadOnly] public float vesselTemperature = 37.0f;
}

public class BloodVessel : MonoBehaviour
{
    [Header("🩸 혈관 특성 (Vessel Properties)")]
    [Tooltip("이 혈관의 기본 특성 설정")]
    public BloodVesselProperties properties;
    
    [Header("📊 혈관 상태 (Vessel Status)")]
    [Tooltip("현재 혈관 상태 - 실시간으로 업데이트됨")]
    public BloodVesselStatus status;
    
    [Header("🎮 실시간 제어 (Runtime Controls)")]
    [Tooltip("혈류량 (0~2) - 1이 정상, 2는 과도한 혈류")]
    [Range(0f, 2f)]
    public float bloodFlow = 1.0f;
    
    [Tooltip("염증 수준 (0~1) - 0이 정상, 1은 심각한 염증")]
    [Range(0f, 1f)]
    public float inflammation = 0f;
    
    [Tooltip("스트레스 수준 (0~1) - 혈압과 혈류에 영향")]
    [Range(0f, 1f)]
    public float stressLevel = 0f;
    
    [Header("🎨 시각화 (Visualization)")]
    [Tooltip("혈관 메쉬 렌더러 - 색상 변화용")]
    public MeshRenderer vesselRenderer;
    
    [Tooltip("혈류 파티클 시스템 - 혈액 흐름 시각화")]
    public ParticleSystem bloodFlowParticles;
    
    [Tooltip("염증 이펙트 - 붓기와 발적 표현")]
    public GameObject inflammationEffect;
    
    [Header("🎨 색상 설정 (Color Settings)")]
    [Tooltip("정상 혈관 색상 - 건강한 상태")]
    public Color normalColor = new Color(0.8f, 0.2f, 0.2f, 1f); // 밝은 빨강
    
    [Tooltip("염증 혈관 색상 - 염증 상태")]
    public Color inflammedColor = new Color(0.5f, 0.1f, 0.1f, 1f); // 어두운 빨강
    
    [Tooltip("저혈류 색상 - 혈류 부족 상태")]
    public Color lowFlowColor = new Color(0.3f, 0.1f, 0.3f, 1f); // 보라색
    
    [Tooltip("과혈류 색상 - 과도한 혈류 상태")]
    public Color highFlowColor = new Color(1f, 0.4f, 0.4f, 1f); // 밝은 분홍
    
    [Header("🔊 오디오 (Audio Effects)")]
    [Tooltip("맥박 소리 - 심장박동 효과")]
    public AudioSource heartbeatAudio;
    
    [Tooltip("혈류 소리 클립")]
    public AudioClip bloodFlowSoundClip;
    
    [Header("🐞 디버그 (Debug)")]
    [Tooltip("디버그 정보 콘솔 출력")]
    public bool enableDebugLogs = false;
    
    [Tooltip("혈관 상태 변화 감지 민감도")]
    [Range(0.01f, 0.1f)]
    public float changeDetectionThreshold = 0.05f;
    
    // ============================================================================
    // 🔧 내부 변수들 (Private Variables)
    // ============================================================================
    
    private Material vesselMaterial;                  // 혈관 재질 (색상 변경용)
    private float baseHeartRate = 70f;                // 기본 심박수
    private float heartbeatTimer = 0f;                // 맥박 타이머
    private float lastInflammationLevel = 0f;         // 이전 염증 수준 (변화 감지용)
    private float lastBloodFlowLevel = 1f;            // 이전 혈류 수준 (변화 감지용)
    private float oxygenConsumption = 1f;             // 산소 소비량
    private float metabolicRate = 1f;                 // 대사율
    private bool systemInitialized = false;          // 시스템 초기화 완료 여부
    
    // 성능 최적화용 캐시
    private float cachedVasodilation = 100f;
    private float cachedOxygenSupply = 100f;
    private float lastCacheUpdateTime = 0f;
    private float cacheUpdateInterval = 0.1f;

    /*
     * ====================================================================
     * 🚀 UNITY 생명주기 메서드들 (Unity Lifecycle Methods)
     * ====================================================================
     */

    /// <summary>
    /// 🎬 START - 혈관 시스템 초기화
    /// 
    /// 초기화 과정:
    /// 1. 기본 설정값 검증
    /// 2. 시각화 컴포넌트 설정
    /// 3. 오디오 시스템 준비
    /// 4. 초기 상태 설정
    /// </summary>
    void Start()
    {
        LogDebug("🩸 혈관 시스템 초기화 시작...");
        
        InitializeBloodVesselSystem();
        SetupVisualization();
        SetupAudioSystem();
        InitializeStatus();
        
        systemInitialized = true;
        LogDebug("✅ 혈관 시스템 초기화 완료");
    }

    /// <summary>
    /// 🔄 UPDATE - 실시간 혈관 상태 업데이트
    /// 
    /// 매 프레임 실행 내용:
    /// 1. 심장박동 시뮬레이션
    /// 2. 혈류 동역학 계산
    /// 3. 염증 반응 처리
    /// 4. 시각화 업데이트
    /// </summary>
    void Update()
    {
        if (!systemInitialized) return;
        
        // 심장박동 시뮬레이션 (매 프레임)
        SimulateHeartbeat();
        
        // 혈류 동역학 계산 (매 프레임)
        CalculateHemodynamics();
        
        // 염증 반응 처리 (매 프레임)
        ProcessInflammatoryResponse();
        
        // 캐시된 값들 업데이트 (최적화)
        if (Time.time - lastCacheUpdateTime >= cacheUpdateInterval)
        {
            UpdateCachedValues();
            lastCacheUpdateTime = Time.time;
        }
        
        // 시각화 업데이트 (변화가 있을 때만)
        if (HasSignificantChanges())
        {
            UpdateVisualization();
            UpdateAudioEffects();
        }
        
        // 상태 정보 업데이트
        UpdateStatusDisplay();
    }

    /*
     * ====================================================================
     * 🛠️ 초기화 메서드들 (Initialization Methods)
     * ====================================================================
     */

    /// <summary>
    /// 🔧 혈관 시스템 기본 초기화
    /// </summary>
    void InitializeBloodVesselSystem()
    {
        // 설정값 검증 및 초기화
        if (properties == null)
        {
            properties = new BloodVesselProperties();
            LogDebug("⚠️ 혈관 특성이 설정되지 않아 기본값으로 초기화");
        }
        
        if (status == null)
        {
            status = new BloodVesselStatus();
        }
        
        // 기본값 설정
        baseHeartRate = 70f + Random.Range(-10f, 10f); // 개인차 반영
        oxygenConsumption = 1f;
        metabolicRate = 1f;
        
        ValidateProperties();
    }

    /// <summary>
    /// 🔍 혈관 특성 유효성 검사
    /// </summary>
    void ValidateProperties()
    {
        // 직경 검사
        properties.baseDiameter = Mathf.Clamp(properties.baseDiameter, 0.1f, 2.0f);
        
        // 혈압 검사 
        properties.normalBloodPressure = Mathf.Clamp(properties.normalBloodPressure, 60f, 120f);
        
        // 점도 검사
        properties.bloodViscosity = Mathf.Clamp(properties.bloodViscosity, 0.8f, 1.5f);
        
        LogDebug($"혈관 특성 검증 완료: 직경 {properties.baseDiameter}mm, 혈압 {properties.normalBloodPressure}mmHg");
    }

    /// <summary>
    /// 🎨 시각화 시스템 설정
    /// </summary>
    void SetupVisualization()
    {
        // 메쉬 렌더러 설정
        if (vesselRenderer == null)
        {
            vesselRenderer = GetComponent<MeshRenderer>();
        }
        
        if (vesselRenderer != null)
        {
            vesselMaterial = vesselRenderer.material;
            if (vesselMaterial != null)
            {
                vesselMaterial.color = normalColor;
            }
        }
        
        // 파티클 시스템 설정
        if (bloodFlowParticles != null)
        {
            var main = bloodFlowParticles.main;
            main.startColor = new Color(0.9f, 0.1f, 0.1f, 0.8f); // 혈액색
            main.maxParticles = 50;
            
            var emission = bloodFlowParticles.emission;
            emission.rateOverTime = 10f;
        }
        
        // 염증 이펙트 초기 비활성화
        if (inflammationEffect != null)
        {
            inflammationEffect.SetActive(false);
        }
        
        LogDebug("🎨 시각화 시스템 설정 완료");
    }

    /// <summary>
    /// 🔊 오디오 시스템 설정
    /// </summary>
    void SetupAudioSystem()
    {
        if (heartbeatAudio == null)
        {
            heartbeatAudio = GetComponent<AudioSource>();
        }
        
        if (heartbeatAudio != null)
        {
            heartbeatAudio.clip = bloodFlowSoundClip;
            heartbeatAudio.loop = false;
            heartbeatAudio.volume = 0.3f;
            heartbeatAudio.pitch = 1.0f;
        }
        
        LogDebug("🔊 오디오 시스템 설정 완료");
    }

    /// <summary>
    /// 📊 상태 정보 초기화
    /// </summary>
    void InitializeStatus()
    {
        status.currentBloodFlow = 100f;
        status.currentInflammation = 0f;
        status.vasodilation = 100f;
        status.oxygenSupply = 100f;
        status.toxinClearance = 100f;
        status.vesselHealth = "Healthy";
        status.heartRate = baseHeartRate;
        status.currentBloodPressure = properties.normalBloodPressure;
        status.vesselTemperature = 37.0f;
        
        // 캐시 초기화
        cachedVasodilation = 100f;
        cachedOxygenSupply = 100f;
        
        LogDebug("📊 상태 정보 초기화 완료");
    }

    /*
     * ====================================================================
     * 💓 생리학적 시뮬레이션 메서드들 (Physiological Simulation)
     * ====================================================================
     */

    /// <summary>
    /// 💓 심장박동 시뮬레이션
    /// 
    /// 시뮬레이션 내용:
    /// - 심박수에 따른 주기적 혈류 변화
    /// - 스트레스에 따른 심박수 증가
    /// - 염증에 따른 심박수 변화
    /// </summary>
    void SimulateHeartbeat()
    {
        // 현재 심박수 계산 (스트레스와 염증 반영)
        float currentHeartRate = baseHeartRate;
        currentHeartRate += stressLevel * 30f;        // 스트레스로 인한 증가
        currentHeartRate += inflammation * 20f;       // 염증으로 인한 증가
        
        status.heartRate = currentHeartRate;
        
        // 심장박동 주기 계산
        float heartbeatInterval = 60f / currentHeartRate; // 초 단위
        heartbeatTimer += Time.deltaTime;
        
        // 심장박동 시점에서 혈류 펄스 생성
        if (heartbeatTimer >= heartbeatInterval)
        {
            TriggerHeartbeatPulse();
            heartbeatTimer = 0f;
        }
        
        // 심장박동에 따른 혈류 변화 (사인파 기반)
        float heartbeatPhase = (heartbeatTimer / heartbeatInterval) * 2f * Mathf.PI;
        float heartbeatEffect = 1f + 0.2f * Mathf.Sin(heartbeatPhase) * properties.pulseStrength;
        
        // 혈류에 맥박 효과 적용
        bloodFlow *= heartbeatEffect;
    }

    /// <summary>
    /// 💥 심장박동 펄스 트리거
    /// 
    /// 심장박동 순간에 발생하는 효과들:
    /// - 오디오 재생
    /// - 파티클 버스트
    /// - 혈압 변화
    /// </summary>
    void TriggerHeartbeatPulse()
    {
        // 맥박 소리 재생
        if (heartbeatAudio != null && bloodFlowSoundClip != null)
        {
            float volumeMultiplier = Mathf.Lerp(0.1f, 1.0f, bloodFlow);
            heartbeatAudio.volume = 0.3f * volumeMultiplier;
            heartbeatAudio.pitch = Mathf.Lerp(0.8f, 1.2f, status.heartRate / 100f);
            heartbeatAudio.PlayOneShot(bloodFlowSoundClip);
        }
        
        // 파티클 버스트
        if (bloodFlowParticles != null)
        {
            bloodFlowParticles.Emit(Mathf.RoundToInt(10f * bloodFlow));
        }
        
        // 혈압 일시적 증가
        float systolicPressure = properties.normalBloodPressure + 40f * properties.pulseStrength;
        status.currentBloodPressure = systolicPressure;
        
        LogDebug($"💓 심장박동: HR={status.heartRate:F0}bpm, BP={status.currentBloodPressure:F0}mmHg");
    }

    /// <summary>
    /// 🌊 혈류 동역학 계산
    /// 
    /// 계산 항목:
    /// - 혈관 저항에 따른 혈류 변화
    /// - 혈액 점도의 영향
    /// - 혈관 직경 변화 (혈관 확장/수축)
    /// - 산소 운반 능력
    /// </summary>
    void CalculateHemodynamics()
    {
        // 혈관 저항 계산 (염증과 스트레스 반영)
        float totalResistance = properties.resistance;
        totalResistance *= (1f + inflammation * 0.5f);    // 염증시 저항 증가
        totalResistance *= (1f + stressLevel * 0.3f);     // 스트레스시 저항 증가
        
        // 혈류량 계산 (오름의 법칙: 혈류 = 압력차 / 저항)
        float pressureDifference = status.currentBloodPressure - 5f; // 정맥압 고려
        float calculatedFlow = pressureDifference / totalResistance;
        
        // 혈액 점도 영향
        calculatedFlow /= properties.bloodViscosity;
        
        // 혈관 탄성 영향
        calculatedFlow *= properties.elasticity;
        
        // 혈류량 정규화 및 제한
        bloodFlow = Mathf.Clamp(calculatedFlow / properties.normalBloodPressure, 0.1f, 2.0f);
        
        // 상태값 업데이트
        status.currentBloodFlow = bloodFlow * 100f;
        
        // 혈압 점진적 회복 (이완기압으로)
        float diastolicTarget = properties.normalBloodPressure;
        status.currentBloodPressure = Mathf.Lerp(status.currentBloodPressure, diastolicTarget, Time.deltaTime * 3f);
    }

    /// <summary>
    /// 🦠 염증 반응 처리
    /// 
    /// 염증의 생리학적 효과:
    /// - 혈관 확장 (vasodilation)
    /// - 혈관 투과성 증가
    /// - 백혈구 증가
    /// - 온도 상승
    /// - 혈류 증가
    /// </summary>
    void ProcessInflammatoryResponse()
    {
        if (inflammation > 0.01f)
        {
            // 혈관 확장 (염증의 특징적 반응)
            float targetDilation = 100f + (inflammation * 50f); // 최대 150%까지 확장
            cachedVasodilation = Mathf.Lerp(cachedVasodilation, targetDilation, Time.deltaTime * 2f);
            
            // 혈관 투과성 증가
            properties.permeability = 1f + inflammation * 0.5f;
            
            // 백혈구 수 증가 (면역 반응)
            properties.whiteBloodCellCount = 1f + inflammation * 1.5f;
            
            // 체온 상승
            float targetTemperature = 37f + inflammation * 2f; // 최대 39도까지
            status.vesselTemperature = Mathf.Lerp(status.vesselTemperature, targetTemperature, Time.deltaTime);
            
            // 산소 소비량 증가 (염증 조직의 대사 증가)
            oxygenConsumption = 1f + inflammation * 0.8f;
            
            LogDebug($"🦠 염증 반응: 확장 {cachedVasodilation:F0}%, 온도 {status.vesselTemperature:F1}°C");
        }
        else
        {
            // 정상 상태로 회복
            cachedVasodilation = Mathf.Lerp(cachedVasodilation, 100f, Time.deltaTime);
            properties.permeability = Mathf.Lerp(properties.permeability, 1f, Time.deltaTime * 0.5f);
            properties.whiteBloodCellCount = Mathf.Lerp(properties.whiteBloodCellCount, 1f, Time.deltaTime * 0.3f);
            status.vesselTemperature = Mathf.Lerp(status.vesselTemperature, 37f, Time.deltaTime * 0.5f);
            oxygenConsumption = Mathf.Lerp(oxygenConsumption, 1f, Time.deltaTime * 0.5f);
        }
        
        status.vasodilation = cachedVasodilation;
        status.currentInflammation = inflammation * 100f;
    }

    /// <summary>
    /// 📦 캐시된 값들 업데이트 (성능 최적화)
    /// </summary>
    void UpdateCachedValues()
    {
        // 산소 공급률 계산
        float baseOxygenSupply = bloodFlow * 100f;
        float temperatureFactor = Mathf.Lerp(1f, 0.8f, (status.vesselTemperature - 37f) / 3f);
        cachedOxygenSupply = baseOxygenSupply * temperatureFactor / oxygenConsumption;
        cachedOxygenSupply = Mathf.Clamp(cachedOxygenSupply, 0f, 150f);
        
        // 독소 제거율 계산
        status.toxinClearance = bloodFlow * properties.permeability * 100f;
        status.toxinClearance = Mathf.Clamp(status.toxinClearance, 0f, 150f);
        
        status.oxygenSupply = cachedOxygenSupply;
    }

    /*
     * ====================================================================
     * 🎨 시각화 업데이트 메서드들 (Visualization Updates)
     * ====================================================================
     */

    /// <summary>
    /// 🔍 의미있는 변화 감지
    /// 
    /// 성능 최적화를 위해 변화가 클 때만 시각화를 업데이트합니다.
    /// </summary>
    bool HasSignificantChanges()
    {
        bool hasChanges = false;
        
        if (Mathf.Abs(inflammation - lastInflammationLevel) > changeDetectionThreshold)
        {
            hasChanges = true;
            lastInflammationLevel = inflammation;
        }
        
        if (Mathf.Abs(bloodFlow - lastBloodFlowLevel) > changeDetectionThreshold)
        {
            hasChanges = true;
            lastBloodFlowLevel = bloodFlow;
        }
        
        return hasChanges;
    }

    /// <summary>
    /// 🎨 시각화 업데이트
    /// 
    /// 업데이트 내용:
    /// - 혈관 색상 변경
    /// - 파티클 효과 조절
    /// - 염증 이펙트 제어
    /// </summary>
    void UpdateVisualization()
    {
        // 혈관 색상 업데이트
        UpdateVesselColor();
        
        // 파티클 시스템 업데이트
        UpdateParticleEffects();
        
        // 염증 시각 효과
        UpdateInflammationEffects();
        
        LogDebug($"🎨 시각화 업데이트: 혈류 {bloodFlow:F2}, 염증 {inflammation:F2}");
    }

    /// <summary>
    /// 🌈 혈관 색상 업데이트
    /// 
    /// 색상 결정 요인:
    /// - 혈류량 (많음=밝음, 적음=어두움)
    /// - 염증 수준 (높음=어두운 빨강)
    /// - 산소 포화도 (낮음=푸르스름)
    /// </summary>
    void UpdateVesselColor()
    {
        if (vesselMaterial == null) return;
        
        Color targetColor = normalColor;
        
        // 염증 상태 색상
        if (inflammation > 0.3f)
        {
            targetColor = Color.Lerp(normalColor, inflammedColor, inflammation);
        }
        // 혈류 상태에 따른 색상
        else if (bloodFlow < 0.5f)
        {
            targetColor = Color.Lerp(normalColor, lowFlowColor, (1f - bloodFlow * 2f));
        }
        else if (bloodFlow > 1.5f)
        {
            targetColor = Color.Lerp(normalColor, highFlowColor, (bloodFlow - 1f) * 2f);
        }
        
        // 산소 포화도 반영
        if (cachedOxygenSupply < 70f)
        {
            float blueTint = (70f - cachedOxygenSupply) / 70f;
            targetColor = Color.Lerp(targetColor, Color.blue, blueTint * 0.3f);
        }
        
        vesselMaterial.color = Color.Lerp(vesselMaterial.color, targetColor, Time.deltaTime * 2f);
    }

    /// <summary>
    /// ✨ 파티클 효과 업데이트
    /// 
    /// 파티클로 표현하는 것:
    /// - 혈류 속도 (발생률)
    /// - 혈액 색상
    /// - 혈류 방향
    /// </summary>
    void UpdateParticleEffects()
    {
        if (bloodFlowParticles == null) return;
        
        var emission = bloodFlowParticles.emission;
        emission.rateOverTime = 10f * bloodFlow * bloodFlow; // 제곱으로 더 극적인 효과
        
        var main = bloodFlowParticles.main;
        
        // 혈류에 따른 파티클 색상
        if (inflammation > 0.2f)
        {
            main.startColor = Color.Lerp(new Color(0.9f, 0.1f, 0.1f, 0.8f), 
                                       new Color(0.5f, 0.05f, 0.05f, 0.9f), inflammation);
        }
        else
        {
            main.startColor = new Color(0.9f, 0.1f, 0.1f, 0.8f);
        }
        
        // 혈류 속도에 따른 파티클 속도
        var velocityOverLifetime = bloodFlowParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(bloodFlow * 0.01f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);
    }

    /// <summary>
    /// 🔥 염증 효과 업데이트
    /// 
    /// 염증 시각 효과:
    /// - 붓기 효과 (스케일 증가)
    /// - 발적 효과 (빨간 광채)
    /// - 열감 효과 (파티클)
    /// </summary>
    void UpdateInflammationEffects()
    {
        if (inflammationEffect == null) return;
        
        // 염증 수준에 따른 효과 활성화/비활성화
        bool shouldShowInflammation = inflammation > 0.2f;
        
        if (inflammationEffect.activeInHierarchy != shouldShowInflammation)
        {
            inflammationEffect.SetActive(shouldShowInflammation);
        }
        
        if (shouldShowInflammation)
        {
            // 염증 강도에 따른 스케일 조정
            float inflammationScale = 1f + inflammation * 0.3f;
            inflammationEffect.transform.localScale = Vector3.one * inflammationScale;
            
            // 염증 이펙트 파티클 조정
            ParticleSystem inflammationParticles = inflammationEffect.GetComponent<ParticleSystem>();
            if (inflammationParticles != null)
            {
                var emission = inflammationParticles.emission;
                emission.rateOverTime = inflammation * 20f;
                
                var main = inflammationParticles.main;
                main.startColor = Color.Lerp(Color.yellow, Color.red, inflammation);
            }
        }
    }

    /// <summary>
    /// 🔊 오디오 효과 업데이트
    /// 
    /// 오디오 효과:
    /// - 심박수에 따른 맥박 속도
    /// - 혈류량에 따른 볼륨
    /// - 염증에 따른 음색 변화
    /// </summary>
    void UpdateAudioEffects()
    {
        if (heartbeatAudio == null) return;
        
        // 심박수가 높으면 더 자주 소리 재생
        // (실제 재생은 TriggerHeartbeatPulse에서 처리)
        
        // 염증이 있으면 음색 변화
        if (inflammation > 0.3f)
        {
            heartbeatAudio.pitch = Mathf.Lerp(1.0f, 0.8f, inflammation); // 낮은 톤
        }
        else
        {
            heartbeatAudio.pitch = 1.0f;
        }
    }

    /// <summary>
    /// 📊 상태 디스플레이 업데이트
    /// 
    /// UI 표시용 상태 정보 갱신
    /// </summary>
    void UpdateStatusDisplay()
    {
        // 전체 건강 상태 평가
        EvaluateVesselHealth();
    }

    /// <summary>
    /// 🏥 혈관 건강 상태 평가
    /// 
    /// 건강 상태 기준:
    /// - Excellent: 모든 지표 95% 이상
    /// - Healthy: 모든 지표 80% 이상  
    /// - Moderate: 일부 지표 저하
    /// - Poor: 여러 지표 문제
    /// - Critical: 심각한 문제
    /// </summary>
    void EvaluateVesselHealth()
    {
        float avgScore = (status.currentBloodFlow + cachedOxygenSupply + status.toxinClearance) / 3f;
        
        if (inflammation > 0.7f || avgScore < 40f)
        {
            status.vesselHealth = "Critical";
        }
        else if (inflammation > 0.4f || avgScore < 60f)
        {
            status.vesselHealth = "Poor";
        }
        else if (inflammation > 0.2f || avgScore < 80f)
        {
            status.vesselHealth = "Moderate";
        }
        else if (avgScore >= 95f && inflammation < 0.1f)
        {
            status.vesselHealth = "Excellent";
        }
        else
        {
            status.vesselHealth = "Healthy";
        }
    }

    /*
     * ====================================================================
     * 🌐 공개 API 메서드들 (Public API Methods)
     * ====================================================================
     */

    /// <summary>
    /// 🩸 혈류량 설정 (Public API)
    /// 
    /// @param flow: 혈류량 (0.0~2.0, 1.0이 정상)
    /// </summary>
    public void SetBloodFlow(float flow)
    {
        bloodFlow = Mathf.Clamp(flow, 0f, 2f);
        LogDebug($"🩸 혈류량 설정: {bloodFlow:F2}");
    }

    /// <summary>
    /// 🔥 염증 수준 설정 (Public API)
    /// 
    /// @param inflammationLevel: 염증 수준 (0.0~1.0, 0이 정상)
    /// </summary>
    public void SetInflammation(float inflammationLevel)
    {
        inflammation = Mathf.Clamp01(inflammationLevel);
        LogDebug($"🔥 염증 수준 설정: {inflammation:F2}");
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
    /// 📊 현재 혈관 상태 반환 (Public API)
    /// </summary>
    public BloodVesselStatus GetVesselStatus()
    {
        return status;
    }

    /// <summary>
    /// 💓 현재 심박수 반환 (Public API)
    /// </summary>
    public float GetHeartRate()
    {
        return status.heartRate;
    }

    /// <summary>
    /// 🌡️ 현재 혈관 온도 반환 (Public API)
    /// </summary>
    public float GetVesselTemperature()
    {
        return status.vesselTemperature;
    }

    /// <summary>
    /// 🔄 혈관 시스템 재설정 (Public API)
    /// </summary>
    public void ResetVesselSystem()
    {
        bloodFlow = 1.0f;
        inflammation = 0f;
        stressLevel = 0f;
        
        InitializeStatus();
        
        LogDebug("🔄 혈관 시스템 재설정 완료");
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
            Debug.Log($"[BloodVessel] {message}");
        }
    }

    /// <summary>
    /// 🎨 Scene View 기즈모 그리기
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !systemInitialized) return;
        
        // 혈관 건강 상태에 따른 색상
        switch (status.vesselHealth)
        {
            case "Excellent": Gizmos.color = Color.cyan; break;
            case "Healthy": Gizmos.color = Color.green; break;
            case "Moderate": Gizmos.color = Color.yellow; break;
            case "Poor": Gizmos.color = new Color(1f, 0.5f, 0f); break;
            case "Critical": Gizmos.color = Color.red; break;
        }
        
        // 혈관 기본 모양
        Gizmos.DrawWireSphere(transform.position, 0.01f);
        
        // 혈류 방향 표시
        if (bloodFlow > 0.1f)
        {
            Gizmos.color = Color.red;
            Vector3 flowDirection = transform.forward * bloodFlow * 0.02f;
            Gizmos.DrawRay(transform.position, flowDirection);
        }
        
        // 염증 표시
        if (inflammation > 0.2f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.015f + inflammation * 0.005f);
        }
    }
}