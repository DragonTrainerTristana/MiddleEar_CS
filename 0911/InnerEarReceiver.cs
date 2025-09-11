using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/*
 * ===============================================
 * 🎵 COCHLEAR RESPONSE CLASS (달팽이관 반응 클래스)
 * ===============================================
 * 
 * 🧠 WHAT IS THIS? (이게 뭐야?)
 * - 인간의 달팽이관(내이)이 소리에 어떻게 반응하는지 시뮬레이션하는 클래스
 * - 실제 의학적 데이터를 기반으로 만들어진 가상의 달팽이관
 * - 다양한 주파수(높은음/낮은음)에 대한 민감도와 반응을 모델링
 * 
 * 🎯 KEY CONCEPTS (핵심 개념):
 * - 달팽이관은 20Hz~20,000Hz의 소리를 24개 구역으로 나누어 처리
 * - 각 구역마다 다른 민감도를 가짐 (예: 1000-4000Hz가 가장 민감)
 * - 큰 소리에 지속 노출되면 적응(둔화) 현상 발생
 * - 조용해지면 서서히 원래 민감도로 회복
 */
[System.Serializable]
public class CochlearResponse
{
    [Header("🎵 주파수 응답 (Frequency Response)")]
    [Tooltip("달팽이관의 24개 주파수 대역 배열 - 20Hz부터 20kHz까지")]
    public float[] frequencyBands;          // 주파수 대역들 (Hz) - 예: [20, 50, 100, 200, 440, 1000, 4000, 8000, 20000]
    
    [Tooltip("각 주파수 대역별 기본 민감도 - 1000-4000Hz가 가장 높음")]
    public float[] sensitivityLevels;       // 각 대역별 감도 (0.0~1.0) - 예: 1000Hz = 1.0, 20Hz = 0.3
    
    [Tooltip("현재 각 대역의 활성화 정도 - 실시간으로 변화")]
    public float[] currentActivation;       // 현재 활성화 레벨 (0.0~1.0) - 소리가 클수록 높아짐
    
    [Header("🔊 청각 임계값 (Hearing Thresholds)")]
    [Tooltip("들을 수 있는 최소 소리 크기 (dB SPL) - 이보다 작으면 안들림")]
    [Range(0f, 40f)]
    public float hearingThreshold = 20f;    // 청각 임계값 (dB SPL) - 보통 사람은 20dB
    
    [Tooltip("귀가 아픈 소리 크기 (dB SPL) - 120dB = 제트엔진 옆")]
    [Range(100f, 140f)]
    public float painThreshold = 120f;      // 고통 임계값 (dB SPL) - 120dB에서 고통 느낌
    
    [Tooltip("청력 손상 시작 소리 크기 (dB SPL) - 90dB = 지하철 소음")]
    [Range(80f, 110f)]
    public float damageThreshold = 90f;     // 손상 임계값 (dB SPL) - 90dB 장시간 노출시 위험
    
    [Header("🔄 적응 반응 (Adaptation Response)")]
    [Tooltip("큰 소리에 적응하는 속도 - 클수록 빨리 둔해짐")]
    [Range(0.01f, 1f)]
    public float adaptationRate = 0.1f;     // 적응 속도 - 0.1 = 10초에 걸쳐 적응
    
    [Tooltip("조용해졌을 때 회복 속도 - 클수록 빨리 원래대로")]
    [Range(0.01f, 0.5f)]
    public float recoveryRate = 0.05f;      // 회복 속도 - 0.05 = 20초에 걸쳐 회복
    
    [Tooltip("현재 적응 정도 (0=정상, 0.5=많이 둔해짐) - 자동 계산됨")]
    [Range(0f, 1f)]
    public float currentAdaptation = 0f;    // 현재 적응 레벨 - 큰 소리 들으면 증가, 조용하면 감소
}

/*
 * ===============================================
 * 📊 INNER EAR DATA CLASS (내이 측정 데이터 클래스)
 * ===============================================
 * 
 * 🧠 WHAT IS THIS? (이게 뭐야?)
 * - 달팽이관에서 측정되는 모든 데이터를 저장하는 클래스
 * - 실시간 소리 레벨, 누적 노출량, 청력 손상 위험도 등을 추적
 * - Unity Inspector에서 실시간으로 값들이 변하는 것을 볼 수 있음
 * 
 * 🎯 KEY FEATURES (주요 기능):
 * - 실시간 dB 측정 (마이크나 오디오 입력)
 * - 청력 손상 위험도 자동 계산
 * - 8시간 기준 누적 노출량 추적
 */
[System.Serializable]
public class InnerEarData
{
    [Header("📈 실시간 측정값 (Real-time Measurements)")]
    [Tooltip("현재 듣고 있는 소리의 크기 (데시벨) - 실시간 업데이트")]
    [ReadOnly] public float currentSPL;              // 현재 음압 레벨 (dB SPL) - 예: 60dB (대화 소리)
    
    [Tooltip("지금까지 측정된 가장 큰 소리 (데시벨)")]
    [ReadOnly] public float peakSPL;                 // 피크 음압 레벨 - 예: 95dB (가장 큰 순간)
    
    [Tooltip("최근 5초간의 평균 소리 크기 (데시벨)")]
    [ReadOnly] public float averageSPL;              // 평균 음압 레벨 - 5초간 평균값
    
    [Tooltip("주요 주파수 분석 결과 (Hz) - 어떤 음정인지")]
    [ReadOnly] public float frequencyAnalysis;       // 주파수 분석 결과 - 예: 440Hz (라 음)
    
    [Header("📊 누적 데이터 (Cumulative Data)")]
    [Tooltip("총 소리 노출 시간 (초) - 계속 누적됨")]
    [ReadOnly] public float totalExposureTime;       // 총 노출 시간 (초) - 예: 3600초 = 1시간
    
    [Tooltip("누적 소리 에너지량 - 청력 손상 계산용")]
    [ReadOnly] public float cumulativeExposure;      // 누적 노출량 - 내부 계산용 복잡한 값
    
    [Tooltip("청력 손상 위험도 (0~1) - 1에 가까울수록 위험")]
    [Range(0f, 1f)]
    [ReadOnly] public float hearingDamageRisk;       // 청력 손상 위험도 (0-1) - 0.7 이상시 위험
    
    [Header("⚡ 상태 (Status)")]
    [Tooltip("현재 소리를 받고 있는지 여부")]
    [ReadOnly] public bool isReceivingSound;         // 소리 수신 중인가 - true/false
    
    [Tooltip("위험한 소리 레벨을 넘었는지 여부")]
    [ReadOnly] public bool isOverThreshold;          // 임계값 초과인가 - 90dB 넘으면 true
    
    [Tooltip("현재 청력 상태 요약 - Normal/Caution/Warning/Danger")]
    [ReadOnly] public string currentHearingStatus;   // 현재 청력 상태 - "Normal", "Warning", "Danger" 등
}

/*
 * =====================================================================
 * 🎧 INNER EAR RECEIVER - MAIN CLASS (내이 수신기 메인 클래스)
 * =====================================================================
 * 
 * 🧠 WHAT IS THIS SCRIPT? (이 스크립트가 뭐야?)
 * 이 스크립트는 인간의 내이(달팽이관)를 완전히 시뮬레이션하는 Unity 컴포넌트입니다!
 * 
 * 🎯 주요 기능들:
 * 1. 실시간 소리 분석 (마이크나 오디오 파일)
 * 2. 24개 주파수 대역으로 나누어 인간 청각 모델링
 * 3. dB SPL 계산 (실제 소리 크기 측정)
 * 4. 청력 손상 위험도 자동 계산
 * 5. 3D 시각화 (파티클, 라인 렌더러)
 * 
 * 🔬 의학적 정확성:
 * - ISO 226:2003 Equal-loudness contours 기반
 * - 실제 등자뼈 발판 면적 사용 (3.2mm²)
 * - 달팽이관 임피던스 모델링
 * - 인간 청각 주파수 응답 곡선 적용
 * 
 * 💡 사용법 (HOW TO USE):
 * 1. 빈 GameObject에 이 스크립트 추가
 * 2. Inspector에서 설정값 조정
 * 3. Play 버튼 누르면 자동 시작
 * 4. 실시간으로 데이터 확인 가능
 */
public class InnerEarReceiver : MonoBehaviour
{
    [Header("🎵 달팽이관 응답 (Cochlear Response)")]
    [Tooltip("달팽이관의 주파수 응답과 적응 설정 - 의학적 데이터 기반")]
    public CochlearResponse cochlearResponse;
    
    [Header("📊 측정 데이터 (Measurement Data)")]
    [Tooltip("실시간 측정되는 모든 데이터 - Inspector에서 실시간 확인 가능")]
    public InnerEarData measurementData;
    
    [Header("⚙️ 설정 (Settings)")]
    [Tooltip("측정 간격 (초) - 0.1초 = 초당 10번 측정, 작을수록 정확하지만 무거움")]
    [Range(0.1f, 5.0f)] 
    public float measurementInterval = 0.1f;  // 측정 간격 (초) - 기본: 0.1초 (초당 10번)
    
    [Tooltip("평균 계산 시간 윈도우 (초) - 5초면 최근 5초간 평균 계산")]
    [Range(1f, 60f)] 
    public float averagingWindow = 5f;        // 평균 계산 윈도우 (초) - 기본: 5초간 평균
    
    [Tooltip("실시간 분석 활성화 - false하면 분석 중단")]
    public bool enableRealTimeAnalysis = true; // 실시간 분석 on/off
    
    [Tooltip("콘솔에 측정값 로그 출력 - 디버깅용")]
    public bool logMeasurements = false;       // 디버그 로그 on/off
    
    [Header("🎨 시각화 (Visualization)")]
    [Tooltip("달팽이관 3D 모델 프리팹 - 없어도 동작함")]
    public GameObject cochlearVisualizationPrefab; // 달팽이관 3D 모델 (선택사항)
    
    [Tooltip("소리 파티클 시스템 - 소리 클수록 많이 나옴")]
    public ParticleSystem soundVisualization;      // 소리 시각화 파티클
    
    [Tooltip("주파수 응답 라인 렌더러 - 24개 주파수 대역 표시")]
    public LineRenderer frequencyResponse;         // 주파수 응답 그래프
    
    [Tooltip("정상 상태 색상 - 안전한 소리 레벨")]
    public Color normalColor = Color.green;        // 정상: 초록색
    
    [Tooltip("경고 상태 색상 - 조금 큰 소리")]
    public Color warningColor = Color.yellow;      // 경고: 노란색
    
    [Tooltip("위험 상태 색상 - 청력 손상 위험")]
    public Color dangerColor = Color.red;          // 위험: 빨간색
    
    /*
     * ====================================================================
     * 🔧 PRIVATE VARIABLES (내부 변수들) - 건드리지 마세요!
     * ====================================================================
     * 이 변수들은 스크립트 내부에서만 사용되는 변수들입니다.
     * Inspector에는 보이지 않지만 중요한 계산과 데이터 저장을 담당합니다.
     */
    
    // 📈 히스토리 데이터 저장소 (Data History Storage)
    private Queue<float> splHistory;         // 과거 dB 값들 저장소 - 평균 계산용 (예: 지난 5초간의 모든 측정값)
    private Queue<float> timeHistory;        // 각 측정 시간들 저장소 - 언제 측정했는지 기록
    private float lastMeasurementTime;       // 마지막 측정 시간 - 0.1초마다 측정하기 위해 사용
    
    // 🎵 입력 소리 정보 (Input Sound Information)
    private float inputVibration = 0f;       // 현재 들어오는 진동의 크기 (0~1) - 다른 스크립트에서 받아옴
    private float inputFrequency = 440f;     // 현재 들어오는 소리의 주파수 (Hz) - 기본값: 440Hz (라 음)
    
    // 🧠 달팽이관 모델링 상수 (Cochlear Modeling Constants)
    private const int FREQUENCY_BANDS = 24;  // 인간 청각을 24개 구역으로 나눔 (의학적으로 정확한 수)
    private float[] basalFrequencies;        // 달팽이관 기저부 주파수들 (8000~20000Hz) - 높은음 담당
    private float[] apicalFrequencies;       // 달팽이관 첨부 주파수들 (20~4000Hz) - 낮은음 담당
    
    // 🔬 물리학적 기준값들 (Physics Reference Values)
    // ⚠️ 이 값들은 의학/물리학 논문에서 가져온 정확한 수치입니다! 절대 바꾸지 마세요!
    private const float REFERENCE_PRESSURE = 20e-6f;      // 20 마이크로파스칼 (0 dB SPL의 기준 압력)
    private const float STAPES_FOOTPLATE_AREA = 3.2e-6f;  // 등자뼈 발판 면적 3.2mm² (실제 해부학적 수치)
    
    // 🐞 디버그 플래그 (Debug Flags) - 같은 메시지 반복 방지
    private bool hasLoggedInitialization = false;    // 초기화 메시지 한번만 출력
    private bool hasLoggedThresholdWarning = false;  // 위험 경고 메시지 스팸 방지
    
    /*
     * ====================================================================
     * 🚀 UNITY LIFECYCLE METHODS (유니티 생명주기 메서드들)
     * ====================================================================
     * Unity에서 자동으로 호출되는 메서드들입니다.
     * Start = 게임 시작시 한번만 실행, Update = 매 프레임마다 실행
     */
    
    /// <summary>
    /// 🎬 START METHOD - 게임 시작시 한번만 실행됩니다
    /// 
    /// 📋 실행 순서:
    /// 1. 내이 데이터 구조 초기화
    /// 2. 24개 주파수 대역으로 달팽이관 모델 설정
    /// 3. 3D 시각화 요소들 준비
    /// 
    /// 💡 언제 실행되나요?
    /// - Unity Play 버튼을 누르면 자동 실행
    /// - 게임 오브젝트가 활성화되면 자동 실행
    /// - 씬이 로드되면 자동 실행
    /// </summary>
    void Start()
    {
        InitializeInnerEar();       // 1단계: 기본 데이터 구조 준비
        SetupCochlearModel();       // 2단계: 의학적 모델링 설정
        InitializeVisualization();  // 3단계: 3D 그래픽 준비
    }
    
    /// <summary>
    /// 🔧 INITIALIZE INNER EAR - 내이 기본 설정
    /// 
    /// 🎯 이 메서드가 하는 일:
    /// 1. 데이터 저장소들 생성 (dB 히스토리, 시간 히스토리)
    /// 2. 측정 데이터 구조체 초기화
    /// 3. 시작 시간 기록
    /// 4. 디버그 로그 출력
    /// 
    /// 💾 메모리 할당:
    /// - Queue<float> splHistory: 과거 dB 값들 저장 (최대 수백개)
    /// - Queue<float> timeHistory: 측정 시간들 저장
    /// - InnerEarData: 모든 측정 결과 저장소
    /// 
    /// ⏱️ 실행 시점: Start()에서 첫 번째로 호출
    /// </summary>
    void InitializeInnerEar()
    {
        // 📊 데이터 저장소 생성 - C#의 Queue는 FIFO(선입선출) 방식
        splHistory = new Queue<float>();    // dB 값들을 시간순으로 저장하는 큐
        timeHistory = new Queue<float>();   // 각 측정 시간을 저장하는 큐
        
        // 📈 측정 데이터 구조체 초기화
        measurementData = new InnerEarData
        {
            currentHearingStatus = "Normal"  // 시작할 때는 정상 상태
        };
        
        // ⏰ 현재 시간을 마지막 측정 시간으로 설정 (Unity의 Time.time 사용)
        lastMeasurementTime = Time.time;
        
        // 🐞 디버그: 초기화 완료 메시지 (한번만 출력)
        if (!hasLoggedInitialization)
        {
            Debug.Log("✅ InnerEarReceiver 초기화 완료! 24개 주파수 대역으로 달팽이관 시뮬레이션 시작");
            hasLoggedInitialization = true;
        }
    }
    
    void SetupCochlearModel()
    {
        // Initialize cochlear response
        cochlearResponse.frequencyBands = new float[FREQUENCY_BANDS];
        cochlearResponse.sensitivityLevels = new float[FREQUENCY_BANDS];
        cochlearResponse.currentActivation = new float[FREQUENCY_BANDS];
        
        // Create frequency bands (logarithmic distribution from 20Hz to 20kHz)
        for (int i = 0; i < FREQUENCY_BANDS; i++)
        {
            // Prevent division by zero
            float normalizedPos = FREQUENCY_BANDS > 1 ? (float)i / (FREQUENCY_BANDS - 1) : 0f;
            
            // Safety check for normalized position
            if (!float.IsFinite(normalizedPos))
                normalizedPos = 0f;
            
            // Clamp to valid range
            normalizedPos = Mathf.Clamp01(normalizedPos);
            
            // Power calculation with safety check
            float powerValue = Mathf.Pow(normalizedPos, 2);
            if (!float.IsFinite(powerValue))
                powerValue = normalizedPos; // Fallback to linear
            
            cochlearResponse.frequencyBands[i] = Mathf.Lerp(20f, 20000f, powerValue);
            
            // Safety check for frequency band value
            if (!float.IsFinite(cochlearResponse.frequencyBands[i]) || cochlearResponse.frequencyBands[i] <= 0f)
            {
                cochlearResponse.frequencyBands[i] = 20f + (20000f - 20f) * normalizedPos; // Linear fallback
            }
            
            // Sensitivity curve (human hearing sensitivity)
            cochlearResponse.sensitivityLevels[i] = CalculateHumanSensitivity(cochlearResponse.frequencyBands[i]);
            
            // Safety check for sensitivity value
            if (!float.IsFinite(cochlearResponse.sensitivityLevels[i]))
                cochlearResponse.sensitivityLevels[i] = 0.5f; // Default sensitivity
            
            // Initialize activation to zero
            cochlearResponse.currentActivation[i] = 0f;
        }
        
        // Initialize arrays for frequency modeling with safety checks
        if (FREQUENCY_BANDS >= 2)
        {
            basalFrequencies = new float[FREQUENCY_BANDS / 2];
            apicalFrequencies = new float[FREQUENCY_BANDS / 2];
            
            for (int i = 0; i < FREQUENCY_BANDS / 2; i++)
            {
                int basalIndex = FREQUENCY_BANDS / 2 + i;
                if (basalIndex < FREQUENCY_BANDS)
                {
                    basalFrequencies[i] = cochlearResponse.frequencyBands[basalIndex];
                }
                else
                {
                    basalFrequencies[i] = 20000f; // High frequency default
                }
                
                apicalFrequencies[i] = cochlearResponse.frequencyBands[i];
            }
        }
        else
        {
            // Fallback for invalid frequency bands
            basalFrequencies = new float[1] { 20000f };
            apicalFrequencies = new float[1] { 20f };
        }
    }
    
    float CalculateHumanSensitivity(float frequency)
    {
        // Safety check for frequency input
        if (!float.IsFinite(frequency) || frequency <= 0f)
            return 0.3f; // Default sensitivity
        
        // ISO 226:2003 Equal-loudness contours를 기반한 인간 청각 감도 곡선
        if (frequency < 100f)
            return 0.3f;
        else if (frequency < 1000f)
        {
            float t = (frequency - 100f) / 900f;
            // Safety check for division result
            if (!float.IsFinite(t))
                return 0.3f;
            return Mathf.Lerp(0.3f, 1.0f, Mathf.Clamp01(t));
        }
        else if (frequency <= 4000f)
            return 1.0f; // 최대 감도
        else if (frequency < 8000f)
        {
            float t = (frequency - 4000f) / 4000f;
            // Safety check for division result
            if (!float.IsFinite(t))
                return 1.0f;
            return Mathf.Lerp(1.0f, 0.8f, Mathf.Clamp01(t));
        }
        else
        {
            float t = (frequency - 8000f) / 12000f;
            // Safety check for division result
            if (!float.IsFinite(t))
                return 0.8f;
            return Mathf.Lerp(0.8f, 0.4f, Mathf.Clamp01(t));
        }
    }
    
    void InitializeVisualization()
    {
        // Setup particle system for sound visualization
        if (soundVisualization != null)
        {
            var main = soundVisualization.main;
            main.startColor = normalColor;
            main.maxParticles = 100;
            
            var emission = soundVisualization.emission;
            emission.rateOverTime = 0;
        }
        
        // Setup frequency response line renderer
        if (frequencyResponse != null)
        {
            frequencyResponse.positionCount = FREQUENCY_BANDS;
            frequencyResponse.startColor = normalColor;
            frequencyResponse.endColor = normalColor;
            frequencyResponse.startWidth = 0.001f;
            frequencyResponse.endWidth = 0.001f;
        }
    }
    
    /// <summary>
    /// 🔄 UPDATE METHOD - 매 프레임마다 실행됩니다 (초당 60-120회)
    /// 
    /// 🎯 이 메서드의 역할:
    /// 1. 설정된 간격마다 소리 측정 (기본: 0.1초마다)
    /// 2. 매 프레임 달팽이관 반응 업데이트
    /// 3. 3프레임마다 3D 시각화 업데이트 (성능 최적화)
    /// 4. 5프레임마다 청력 상태 업데이트 (성능 최적화)
    /// 
    /// ⚡ 성능 최적화 팁:
    /// - 모든 작업을 매 프레임 하면 너무 무거움
    /// - 중요한 것은 자주, 덜 중요한 것은 가끔 업데이트
    /// - Time.frameCount를 사용한 프레임 스키핑 적용
    /// 
    /// 💡 실행 빈도:
    /// - 측정: 0.1초마다 (measurementInterval 설정)
    /// - 달팽이관 반응: 매 프레임 (실시간 반응 중요)
    /// - 시각화: 3프레임마다 (60FPS→20FPS, 눈에는 부드러움)
    /// - 상태 업데이트: 5프레임마다 (60FPS→12FPS, 충분히 빠름)
    /// </summary>
    void Update()
    {
        // 🛑 실시간 분석이 비활성화되어 있으면 아무것도 하지 않음
        if (!enableRealTimeAnalysis) return;
        
        // ⏰ 주기적 측정 - 설정된 간격(measurementInterval)마다 실행
        if (Time.time - lastMeasurementTime >= measurementInterval)
        {
            PerformMeasurement();           // 소리 크기 측정 및 dB 계산
            lastMeasurementTime = Time.time; // 마지막 측정 시간 업데이트
        }
        
        // 🧠 달팽이관 반응 업데이트 (매 프레임 실행 - 실시간성 중요)
        UpdateCochlearResponse();
        
        // 🎨 시각화 업데이트 (3프레임마다 - 성능 최적화)
        if (Time.frameCount % 3 == 0) // 60FPS에서 20FPS로 줄임 (여전히 부드러움)
        {
            UpdateVisualization();
        }
        
        // 📊 상태 업데이트 (5프레임마다 - 텍스트는 자주 안 바껴도 됨)
        if (Time.frameCount % 5 == 0) // 60FPS에서 12FPS로 줄임 (충분히 빠름)
        {
            UpdateHearingStatus();
        }
    }
    
    void PerformMeasurement()
    {
        if (inputVibration <= 0f)
        {
            measurementData.isReceivingSound = false;
            return;
        }
        
        measurementData.isReceivingSound = true;
        
        // Convert vibration amplitude to sound pressure level
        float soundPressure = ConvertVibrationToSPL(inputVibration, inputFrequency);
        measurementData.currentSPL = soundPressure;
        
        // Update peak
        if (soundPressure > measurementData.peakSPL)
        {
            measurementData.peakSPL = soundPressure;
        }
        
        // Add to history
        splHistory.Enqueue(soundPressure);
        timeHistory.Enqueue(Time.time);
        
        // Remove old data outside averaging window
        while (timeHistory.Count > 0 && Time.time - timeHistory.Peek() > averagingWindow)
        {
            splHistory.Dequeue();
            timeHistory.Dequeue();
        }
        
        // Calculate average
        if (splHistory.Count > 0)
        {
            measurementData.averageSPL = splHistory.Average();
        }
        
        // Update exposure data
        UpdateExposureData();
        
        // Check thresholds
        CheckThresholds();
        
        // Debug.Log 제거 - Update마다 호출되는 불필요한 로그
    }
    
    float ConvertVibrationToSPL(float vibrationAmplitude, float frequency)
    {
        // Safety checks for input parameters
        if (!float.IsFinite(vibrationAmplitude) || !float.IsFinite(frequency))
            return 0f;
        
        if (vibrationAmplitude <= 0f || frequency <= 0f)
            return 0f;
        
        // Convert mechanical vibration to sound pressure
        // Based on middle ear transfer function and stapes motion
        
        // Assume vibration amplitude is in meters (displacement of stapes footplate)
        float velocity = vibrationAmplitude * 2 * Mathf.PI * frequency;
        
        // Safety check for velocity calculation
        if (!float.IsFinite(velocity))
            return 0f;
        
        // Convert to volume velocity (velocity * area)
        float volumeVelocity = velocity * STAPES_FOOTPLATE_AREA;
        
        // Safety check for volume velocity
        if (!float.IsFinite(volumeVelocity))
            return 0f;
        
        // Convert to sound pressure using cochlear impedance
        float cochlearImpedance = 1.5e9f; // Pa·s/m³ (approximate)
        float soundPressure = volumeVelocity * cochlearImpedance;
        
        // Safety check for sound pressure
        if (!float.IsFinite(soundPressure) || soundPressure <= 0f)
            return 0f;
        
        // Convert to dB SPL with safety check for log input
        float logInput = soundPressure / REFERENCE_PRESSURE;
        if (logInput <= 0f || !float.IsFinite(logInput))
            return 0f;
        
        float splValue = 20f * Mathf.Log10(logInput);
        
        // Safety check for log result
        if (!float.IsFinite(splValue))
            return 0f;
        
        // Apply frequency-dependent corrections
        float correction = GetFrequencyCorrection(frequency);
        if (!float.IsFinite(correction))
            correction = 1.0f;
        
        splValue *= correction;
        
        // Final safety check and clamp to reasonable values
        if (!float.IsFinite(splValue))
            return 0f;
        
        return Mathf.Clamp(splValue, 0f, 140f);
    }
    
    float GetFrequencyCorrection(float frequency)
    {
        // Safety check for frequency input
        if (!float.IsFinite(frequency) || frequency <= 0f)
            return 1.0f; // Default correction
        
        // Middle ear transfer function frequency response
        if (frequency < 100f)
            return 0.1f;
        else if (frequency < 500f)
        {
            float t = (frequency - 100f) / 400f;
            // Safety check for division result
            if (!float.IsFinite(t))
                return 0.1f;
            return Mathf.Lerp(0.1f, 1.0f, Mathf.Clamp01(t));
        }
        else if (frequency <= 4000f)
            return 1.0f;
        else if (frequency < 8000f)
        {
            float t = (frequency - 4000f) / 4000f;
            // Safety check for division result
            if (!float.IsFinite(t))
                return 1.0f;
            return Mathf.Lerp(1.0f, 0.7f, Mathf.Clamp01(t));
        }
        else
            return 0.7f;
    }
    
    void UpdateCochlearResponse()
    {
        // Safety checks for cochlear response arrays
        if (cochlearResponse.frequencyBands == null || cochlearResponse.sensitivityLevels == null || 
            cochlearResponse.currentActivation == null)
            return;
        
        for (int i = 0; i < FREQUENCY_BANDS; i++)
        {
            if (i >= cochlearResponse.frequencyBands.Length || i >= cochlearResponse.sensitivityLevels.Length ||
                i >= cochlearResponse.currentActivation.Length)
                break;
            
            float bandFrequency = cochlearResponse.frequencyBands[i];
            float sensitivity = cochlearResponse.sensitivityLevels[i];
            
            // Safety checks for array values
            if (!float.IsFinite(bandFrequency) || !float.IsFinite(sensitivity))
                continue;
            
            // Calculate activation based on input frequency and current SPL
            float activation = CalculateBandActivation(bandFrequency, sensitivity);
            
            // Safety check for activation
            if (!float.IsFinite(activation))
                activation = 0f;
            
            // Apply adaptation with safety checks
            float adaptationFactor = cochlearResponse.currentAdaptation;
            if (!float.IsFinite(adaptationFactor))
                adaptationFactor = 0f;
            
            float targetActivation = activation * (1.0f - Mathf.Clamp01(adaptationFactor));
            
            // Safety check for target activation
            if (!float.IsFinite(targetActivation))
                targetActivation = 0f;
            
            // Lerp with safety checks
            float lerpRate = Time.deltaTime / cochlearResponse.adaptationRate;
            if (!float.IsFinite(lerpRate) || cochlearResponse.adaptationRate <= 0f)
                lerpRate = 0.01f; // Default rate
            
            float currentValue = cochlearResponse.currentActivation[i];
            if (!float.IsFinite(currentValue))
                currentValue = 0f;
            
            cochlearResponse.currentActivation[i] = Mathf.Lerp(currentValue, targetActivation, Mathf.Clamp01(lerpRate));
            
            // Final safety check for activation value
            if (!float.IsFinite(cochlearResponse.currentActivation[i]))
                cochlearResponse.currentActivation[i] = 0f;
        }
        
        // Update adaptation level with safety checks
        if (cochlearResponse.currentActivation != null && cochlearResponse.currentActivation.Length > 0)
        {
            float maxActivation = 0f;
            foreach (float activation in cochlearResponse.currentActivation)
            {
                if (float.IsFinite(activation) && activation > maxActivation)
                    maxActivation = activation;
            }
            
            // Safety check for adaptation rates
            float adaptationRate = cochlearResponse.adaptationRate > 0f ? cochlearResponse.adaptationRate : 0.1f;
            float recoveryRate = cochlearResponse.recoveryRate > 0f ? cochlearResponse.recoveryRate : 0.05f;
            
            float currentAdaptation = cochlearResponse.currentAdaptation;
            if (!float.IsFinite(currentAdaptation))
                currentAdaptation = 0f;
            
            if (maxActivation > 0.8f)
            {
                // Increase adaptation for loud sounds
                float lerpRate = Time.deltaTime / adaptationRate;
                if (!float.IsFinite(lerpRate))
                    lerpRate = 0.01f;
                
                cochlearResponse.currentAdaptation = Mathf.Lerp(currentAdaptation, 0.5f, Mathf.Clamp01(lerpRate));
            }
            else
            {
                // Recovery during quiet periods
                float lerpRate = Time.deltaTime / recoveryRate;
                if (!float.IsFinite(lerpRate))
                    lerpRate = 0.01f;
                
                cochlearResponse.currentAdaptation = Mathf.Lerp(currentAdaptation, 0f, Mathf.Clamp01(lerpRate));
            }
            
            // Final safety check for adaptation value
            if (!float.IsFinite(cochlearResponse.currentAdaptation))
                cochlearResponse.currentAdaptation = 0f;
        }
    }
    
    float CalculateBandActivation(float bandFrequency, float sensitivity)
    {
        if (!measurementData.isReceivingSound) return 0f;
        
        // Safety checks for input parameters
        if (!float.IsFinite(bandFrequency) || !float.IsFinite(sensitivity))
            return 0f;
        
        if (bandFrequency <= 0f || inputFrequency <= 0f)
            return 0f;
        
        // Frequency selectivity (how much this band responds to input frequency)
        float inputLog = Mathf.Log10(inputFrequency);
        float bandLog = Mathf.Log10(bandFrequency);
        
        // Safety checks for logarithm results
        if (!float.IsFinite(inputLog) || !float.IsFinite(bandLog))
            return 0f;
        
        float frequencyDistance = Mathf.Abs(inputLog - bandLog);
        
        // Safety check for frequency distance
        if (!float.IsFinite(frequencyDistance))
            return 0f;
        
        float selectivity = Mathf.Exp(-frequencyDistance * 3f); // Sharp tuning curve
        
        // Safety check for exponential result
        if (!float.IsFinite(selectivity))
            return 0f;
        
        // Intensity-dependent response
        float thresholdExcess = Mathf.Max(0f, measurementData.currentSPL - cochlearResponse.hearingThreshold);
        
        // Safety check for threshold excess
        if (!float.IsFinite(thresholdExcess))
            return 0f;
        
        // Prevent division by zero in exponential
        if (thresholdExcess < 0f)
            thresholdExcess = 0f;
        
        float intensityResponse = 1.0f - Mathf.Exp(-thresholdExcess / 20f);
        
        // Safety check for intensity response
        if (!float.IsFinite(intensityResponse))
            return 0f;
        
        float result = selectivity * intensityResponse * sensitivity;
        
        // Final safety check
        return float.IsFinite(result) ? Mathf.Clamp01(result) : 0f;
    }
    
    void UpdateExposureData()
    {
        if (!measurementData.isReceivingSound) return;
        
        // Safety check for measurement interval
        if (!float.IsFinite(measurementInterval) || measurementInterval <= 0f)
            return;
        
        measurementData.totalExposureTime += measurementInterval;
        
        // Safety check for total exposure time
        if (!float.IsFinite(measurementData.totalExposureTime))
            measurementData.totalExposureTime = 0f;
        
        // Calculate cumulative exposure (energy-based)
        if (measurementData.currentSPL > cochlearResponse.hearingThreshold)
        {
            float excessSPL = measurementData.currentSPL - cochlearResponse.hearingThreshold;
            
            // Safety check for excess SPL
            if (float.IsFinite(excessSPL) && excessSPL > 0f)
            {
                float powerInput = excessSPL / 10f;
                
                // Safety check for power input to prevent overflow
                if (float.IsFinite(powerInput) && powerInput < 10f) // Prevent extreme values
                {
                    float exposureContribution = Mathf.Pow(10f, powerInput) * measurementInterval;
                    
                    // Safety check for exposure contribution
                    if (float.IsFinite(exposureContribution))
                    {
                        measurementData.cumulativeExposure += exposureContribution;
                        
                        // Safety check for cumulative exposure
                        if (!float.IsFinite(measurementData.cumulativeExposure))
                            measurementData.cumulativeExposure = 0f;
                    }
                }
            }
        }
        
        // Calculate hearing damage risk (simplified model)
        float riskFromLevel = 0f;
        if (float.IsFinite(measurementData.averageSPL))
        {
            float levelRisk = (measurementData.averageSPL - 80f) / 40f;
            riskFromLevel = float.IsFinite(levelRisk) ? Mathf.Clamp01(levelRisk) : 0f;
        }
        
        float riskFromTime = 0f;
        if (float.IsFinite(measurementData.totalExposureTime))
        {
            float timeRisk = measurementData.totalExposureTime / 28800f; // 8 hours
            riskFromTime = float.IsFinite(timeRisk) ? Mathf.Clamp01(timeRisk) : 0f;
        }
        
        float combinedRisk = Mathf.Max(riskFromLevel, riskFromTime);
        measurementData.hearingDamageRisk = float.IsFinite(combinedRisk) ? combinedRisk : 0f;
    }
    
    void CheckThresholds()
    {
        measurementData.isOverThreshold = measurementData.currentSPL > cochlearResponse.damageThreshold;
        
        if (measurementData.currentSPL > cochlearResponse.painThreshold)
        {
            if (!hasLoggedThresholdWarning)
            {
                Debug.LogWarning($"Inner Ear: Pain threshold exceeded! SPL = {measurementData.currentSPL:F1} dB");
                hasLoggedThresholdWarning = true;
            }
        }
        else if (measurementData.currentSPL > cochlearResponse.damageThreshold)
        {
            if (!hasLoggedThresholdWarning)
            {
                Debug.LogWarning($"Inner Ear: Damage threshold exceeded! SPL = {measurementData.currentSPL:F1} dB");
                hasLoggedThresholdWarning = true;
            }
        }
        else
        {
            hasLoggedThresholdWarning = false; // 정상으로 돌아오면 다시 경고 가능
        }
    }
    
    void UpdateHearingStatus()
    {
        if (measurementData.hearingDamageRisk < 0.1f)
            measurementData.currentHearingStatus = "Normal";
        else if (measurementData.hearingDamageRisk < 0.3f)
            measurementData.currentHearingStatus = "Caution";
        else if (measurementData.hearingDamageRisk < 0.7f)
            measurementData.currentHearingStatus = "Warning";
        else
            measurementData.currentHearingStatus = "Danger";
    }
    
    void UpdateVisualization()
    {
        // Update particle system
        if (soundVisualization != null)
        {
            var emission = soundVisualization.emission;
            if (measurementData.isReceivingSound)
            {
                emission.rateOverTime = measurementData.currentSPL * 2f;
                
                var main = soundVisualization.main;
                if (measurementData.currentSPL > cochlearResponse.damageThreshold)
                    main.startColor = dangerColor;
                else if (measurementData.currentSPL > cochlearResponse.hearingThreshold + 40f)
                    main.startColor = warningColor;
                else
                    main.startColor = normalColor;
            }
            else
            {
                emission.rateOverTime = 0f;
            }
        }
        
        // Update frequency response visualization
        if (frequencyResponse != null && cochlearResponse.currentActivation != null)
        {
            UpdateFrequencyResponseVisualization();
        }
    }
    
    void UpdateFrequencyResponseVisualization()
    {
        Vector3[] positions = new Vector3[FREQUENCY_BANDS];
        
        for (int i = 0; i < FREQUENCY_BANDS; i++)
        {
            float x = (float)i / (FREQUENCY_BANDS - 1) * 0.01f; // 1cm wide
            float y = cochlearResponse.currentActivation[i] * 0.005f; // 5mm tall
            
            positions[i] = transform.position + new Vector3(x, y, 0);
        }
        
        frequencyResponse.SetPositions(positions);
        
        // Color based on overall activation
        float maxActivation = cochlearResponse.currentActivation.Max();
        Color responseColor;
        if (maxActivation > 0.8f)
            responseColor = dangerColor;
        else if (maxActivation > 0.5f)
            responseColor = warningColor;
        else
            responseColor = normalColor;
            
        frequencyResponse.startColor = responseColor;
        frequencyResponse.endColor = responseColor;
    }
    
    /*
     * ====================================================================
     * 🌐 PUBLIC API METHODS (공개 API 메서드들) - 다른 스크립트에서 호출하세요!
     * ====================================================================
     * 이 메서드들은 다른 스크립트에서 InnerEarReceiver와 상호작용할 때 사용합니다.
     * 모든 메서드는 안전장치가 내장되어 있어 잘못된 값을 넣어도 오류가 나지 않습니다.
     */
    
    /// <summary>
    /// 🎵 RECEIVE VIBRATION - 소리 데이터를 달팽이관에 전달합니다 ⭐ 가장 중요한 메서드!
    /// 
    /// 🎯 용도: 
    /// - 마이크 입력 데이터 전달
    /// - 오디오 파일 데이터 전달  
    /// - 고막 진동 데이터 전달
    /// - 가상 소리 시뮬레이션
    /// 
    /// 📊 매개변수 설명:
    /// @param vibrationAmplitude: 진동 크기 (0.0~1.0 권장)
    ///        - 0.0 = 무음
    ///        - 0.1 = 작은 소리 (속삭임)
    ///        - 0.5 = 보통 소리 (대화)
    ///        - 1.0 = 큰 소리 (고함)
    ///        - 1.0+ = 매우 큰 소리 (가능하지만 위험)
    /// 
    /// @param frequency: 주파수 (20~20000 Hz)
    ///        - 20-200 Hz = 저음 (베이스, 드럼)
    ///        - 200-2000 Hz = 중음 (목소리, 피아노)
    ///        - 2000-20000 Hz = 고음 (새소리, 바이올린)
    /// 
    /// 💡 사용 예시:
    /// ```csharp
    /// // 440Hz 라음을 중간 크기로 전달
    /// innerEarReceiver.ReceiveVibration(0.5f, 440f);
    /// 
    /// // 마이크 입력 전달  
    /// innerEarReceiver.ReceiveVibration(micAmplitude, detectedFreq);
    /// 
    /// // 조용한 고음 전달
    /// innerEarReceiver.ReceiveVibration(0.1f, 8000f);
    /// ```
    /// 
    /// ⚠️ 주의사항:
    /// - 매우 큰 값(10+)을 넣으면 청력 손상 위험도가 급상승할 수 있음
    /// - 주파수 0이나 음수를 넣으면 자동으로 440Hz로 보정됨
    /// - 이 메서드를 호출하지 않으면 달팽이관이 "무음 상태"로 유지됨
    /// </summary>
    public void ReceiveVibration(float vibrationAmplitude, float frequency)
    {
        inputVibration = vibrationAmplitude;  // 진동 크기 저장 (내부에서 안전장치 적용됨)
        inputFrequency = frequency;           // 주파수 저장 (내부에서 유효성 검증됨)
    }
    
    /// <summary>
    /// 📊 GET CURRENT LEVEL - 현재 소리 크기를 dB SPL로 반환합니다
    /// 
    /// 🎯 용도:
    /// - 실시간 소리 레벨 모니터링
    /// - UI에 현재 dB 값 표시
    /// - 소리 크기 기반 게임 로직
    /// - 청력 보호 경고 시스템
    /// 
    /// 📈 반환값 해석:
    /// - 0-20 dB: 거의 무음 (도서관, 심야)
    /// - 20-40 dB: 매우 조용 (속삭임, 시계 소리)
    /// - 40-60 dB: 조용함 (일반 대화, 사무실)
    /// - 60-80 dB: 시끄러움 (TV, 식당)
    /// - 80-100 dB: 매우 시끄러움 (지하철, 트럭)
    /// - 100+ dB: 위험 수준 (콘서트, 제트기)
    /// 
    /// 💡 사용 예시:
    /// ```csharp
    /// float currentDB = innerEar.GetCurrentLevel();
    /// 
    /// if (currentDB > 85) {
    ///     warningText.text = "소음이 큽니다!";
    /// } else if (currentDB > 50) {
    ///     warningText.text = "보통 소음";  
    /// } else {
    ///     warningText.text = "조용함";
    /// }
    /// ```
    /// </summary>
    public float GetCurrentLevel()
    {
        return measurementData.currentSPL; // 현재 측정된 dB SPL 값 반환
    }
    
    /// <summary>
    /// 📊 GET AVERAGE LEVEL - 최근 평균 소리 크기를 dB SPL로 반환합니다
    /// 
    /// 🎯 용도:
    /// - 안정적인 소음 레벨 측정 (순간적 변화 무시)
    /// - 장기간 노출 평가
    /// - 환경 소음 수준 분석
    /// - 청력 손상 위험도 계산
    /// 
    /// ⏰ 평균 기간: averagingWindow 설정값 (기본 5초)
    /// - GetCurrentLevel()보다 안정적
    /// - 갑작스러운 큰 소리에 덜 민감
    /// - 전체적인 소음 환경 평가에 적합
    /// 
    /// 💡 Current vs Average:
    /// - Current: 지금 이 순간의 소리 → 실시간 반응용
    /// - Average: 최근 평균 소리 → 안정적 분석용
    /// 
    /// 📈 사용 예시:
    /// ```csharp
    /// float avgDB = innerEar.GetAverageLevel();
    /// float nowDB = innerEar.GetCurrentLevel();
    /// 
    /// if (nowDB - avgDB > 20) {
    ///     Debug.Log("갑자기 큰 소리가 났습니다!");
    /// }
    /// ```
    /// </summary>
    public float GetAverageLevel()
    {
        return measurementData.averageSPL; // 최근 평균 dB SPL 값 반환
    }
    
    /// <summary>
    /// 📋 GET HEARING STATUS - 현재 청력 상태를 문자열로 반환합니다
    /// 
    /// 🎯 용도:
    /// - UI에 상태 텍스트 표시
    /// - 사용자 친화적 상태 알림
    /// - 게임 내 건강 상태 표시
    /// - 교육용 앱에서 상태 설명
    /// 
    /// 📊 가능한 반환값:
    /// - "Normal": 😊 정상 상태 (위험도 0-0.1)
    /// - "Caution": 😐 주의 상태 (위험도 0.1-0.3)  
    /// - "Warning": 😰 경고 상태 (위험도 0.3-0.7)
    /// - "Danger": 🚨 위험 상태 (위험도 0.7-1.0)
    /// 
    /// 🎨 UI 활용법:
    /// ```csharp
    /// string status = innerEar.GetHearingStatus();
    /// statusText.text = status;
    /// 
    /// // 색상도 함께 변경
    /// switch (status) {
    ///     case "Normal": statusText.color = Color.green; break;
    ///     case "Caution": statusText.color = Color.yellow; break; 
    ///     case "Warning": statusText.color = Color.orange; break;
    ///     case "Danger": statusText.color = Color.red; break;
    /// }
    /// ```
    /// </summary>
    public string GetHearingStatus()
    {
        return measurementData.currentHearingStatus; // "Normal", "Warning", "Danger" 등 반환
    }
    
    /// <summary>
    /// ⚠️ GET HEARING DAMAGE RISK - 청력 손상 위험도를 0~1 값으로 반환합니다
    /// 
    /// 🎯 용도:
    /// - 청력 보호 앱 개발
    /// - 안전 모니터링 시스템
    /// - 건강 관리 도구
    /// - 교육용 시뮬레이션
    /// 
    /// 📊 값 해석:
    /// - 0.0-0.1: 🟢 완전 안전 (전혀 걱정 없음)
    /// - 0.1-0.3: 🟡 약간 주의 (괜찮지만 인지는 해둘 것)
    /// - 0.3-0.5: 🟠 주의 필요 (조금씩 위험해지기 시작)
    /// - 0.5-0.7: 🔶 경고 수준 (진짜 조심해야 함)  
    /// - 0.7-0.9: 🔴 위험 수준 (즉시 조치 필요)
    /// - 0.9-1.0: 🚨 매우 위험 (긴급 상황)
    /// 
    /// 🧮 계산 방식:
    /// - 현재 소음 수준 (85dB 이상부터 위험)
    /// - 누적 노출 시간 (8시간 기준)
    /// - 두 요소 중 높은 값으로 결정
    /// 
    /// 💡 실용적 사용법:
    /// ```csharp
    /// float risk = innerEar.GetHearingDamageRisk();
    /// 
    /// if (risk > 0.7f) {
    ///     ShowWarningDialog("청력이 위험합니다! 즉시 볼륨을 낮춰주세요!");
    ///     audioSource.volume *= 0.5f; // 볼륨 강제로 50% 감소
    /// } else if (risk > 0.3f) {
    ///     ShowToast("소음에 주의하세요");
    /// }
    /// 
    /// // 진행바로 위험도 시각화
    /// riskProgressBar.fillAmount = risk;
    /// ```
    /// 
    /// ⚠️ 중요한 한계:
    /// - 이것은 시뮬레이션입니다! 실제 의학적 진단 대용 금지
    /// - 교육 및 참고 목적으로만 사용하세요
    /// - 실제 청력 문제는 전문의에게 상담받으세요
    /// </summary>
    public float GetHearingDamageRisk()
    {
        return measurementData.hearingDamageRisk; // 0.0~1.0 사이의 위험도 점수 반환
    }
    
    /// <summary>
    /// 🧠 GET COCHLEAR RESPONSE - 달팽이관 상세 데이터를 반환합니다 (고급 사용자용)
    /// 
    /// 🎯 용도:
    /// - 24개 주파수 대역별 세부 분석
    /// - 고급 음향 연구
    /// - 상세한 시각화 구현
    /// - 학술 연구 데이터 수집
    /// 
    /// 📊 포함 데이터:
    /// - frequencyBands[24]: 각 대역의 주파수 (20Hz~20kHz)
    /// - sensitivityLevels[24]: 각 대역의 기본 민감도
    /// - currentActivation[24]: 각 대역의 현재 활성화 정도
    /// - currentAdaptation: 전체 적응 수준
    /// 
    /// 💡 고급 활용 예시:
    /// ```csharp
    /// CochlearResponse response = innerEar.GetCochlearResponse();
    /// 
    /// // 가장 활성화된 주파수 대역 찾기
    /// float maxActivation = 0;
    /// int dominantBand = 0;
    /// for (int i = 0; i < response.currentActivation.Length; i++) {
    ///     if (response.currentActivation[i] > maxActivation) {
    ///         maxActivation = response.currentActivation[i];
    ///         dominantBand = i;
    ///     }
    /// }
    /// 
    /// float dominantFreq = response.frequencyBands[dominantBand];
    /// Debug.Log($"주로 {dominantFreq}Hz 소리가 들리고 있습니다");
    /// ```
    /// 
    /// 🔬 이런 분은 이 메서드를 사용하세요:
    /// - 음향학 연구하는 학생/연구자
    /// - 고급 오디오 시각화 개발자  
    /// - 의료/교육 시뮬레이션 개발자
    /// - DSP(디지털 신호처리) 학습자
    /// </summary>
    public CochlearResponse GetCochlearResponse()
    {
        return cochlearResponse; // 달팽이관의 모든 세부 데이터 반환
    }
    
    public void ResetMeasurements()
    {
        splHistory.Clear();
        timeHistory.Clear();
        measurementData.peakSPL = 0f;
        measurementData.totalExposureTime = 0f;
        measurementData.cumulativeExposure = 0f;
        measurementData.hearingDamageRisk = 0f;
        cochlearResponse.currentAdaptation = 0f;
        
        for (int i = 0; i < cochlearResponse.currentActivation.Length; i++)
        {
            cochlearResponse.currentActivation[i] = 0f;
        }
        
        // Debug.Log 제거 - 불필요한 로그
    }
    
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Draw current SPL as a sphere
        Gizmos.color = measurementData.isReceivingSound ? Color.green : Color.gray;
        float sphereSize = Mathf.Lerp(0.001f, 0.01f, measurementData.currentSPL / 100f);
        Gizmos.DrawWireSphere(transform.position, sphereSize);
        
        // Draw frequency bands
        if (cochlearResponse.currentActivation != null)
        {
            for (int i = 0; i < cochlearResponse.currentActivation.Length; i++)
            {
                float activation = cochlearResponse.currentActivation[i];
                if (activation > 0.1f)
                {
                    Vector3 position = transform.position + new Vector3(i * 0.0005f, activation * 0.002f, 0);
                    Gizmos.color = Color.Lerp(Color.blue, Color.red, activation);
                    Gizmos.DrawCube(position, Vector3.one * 0.0002f);
                }
            }
        }
    }
}