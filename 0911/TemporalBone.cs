using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

// Unity 컴파일 순서 문제 해결을 위한 전방 선언
// Forward declarations to resolve compilation order issues

/*
 * ===============================================
 * 🏗️ TEMPORAL BONE - 측두골 마스터 컨트롤러
 * ===============================================
 * 
 * 🧠 이 스크립트가 뭐야? (What is this?)
 * 측두골(Temporal Bone)은 귀의 모든 부분을 담고 있는 뼈입니다.
 * 이 스크립트는 귀의 모든 구성 요소들을 하나로 연결하고 관리하는 
 * "총 지휘관" 역할을 합니다!
 * 
 * 🏗️ 구성 요소들 (Components):
 * 1. 고막 (Tympanic Membrane) - 소리를 받아서 진동
 * 2. 이소골 (Ossicle Chain) - 고막의 진동을 내이로 전달
 * 3. 내이 (Inner Ear) - 진동을 전기 신호로 변환
 * 4. 혈관 (Blood Vessels) - 영양 공급 및 염증 반응
 * 5. 청신경 (Auditory Nerve) - 뇌로 신호 전송
 * 6. 중이염 (Otitis Media) - 질병 상태 시뮬레이션
 * 
 * 🔄 소리의 여행 경로:
 * 소리 입력 → 고막 진동 → 이소골 전달 → 내이 변환 → 신경 전송 → 뇌 인식
 * 
 * 💡 초보자를 위한 팁:
 * - 이 스크립트를 빈 GameObject에 붙이세요
 * - Inspector에서 각 구성 요소들을 연결하세요
 * - Play 버튼을 누르면 자동으로 시뮬레이션이 시작됩니다
 */

[System.Serializable]
public class TemporalBoneSettings
{
    [Header("🎛️ 전체 시스템 설정 (Overall System Settings)")]
    [Tooltip("전체적인 청각 민감도 - 클수록 소리에 더 민감하게 반응")]
    [Range(0.1f, 3.0f)]
    public float overallSensitivity = 1.0f;
    
    [Tooltip("시뮬레이션 품질 - 높을수록 정확하지만 무거움")]
    [Range(0.1f, 1.0f)]
    public float simulationQuality = 0.8f;
    
    [Tooltip("실시간 시뮬레이션 활성화 - false하면 시뮬레이션 중단")]
    public bool enableRealTimeSimulation = true;
    
    [Tooltip("자동 건강 모니터링 - 중이염, 청력 손상 등 자동 감지")]
    public bool enableHealthMonitoring = true;
    
    [Header("🩺 건강 상태 임계값 (Health Thresholds)")]
    [Tooltip("정상 상태 기준 dB 수준 - 이하면 안전")]
    [Range(60f, 80f)]
    public float safeDecibelLevel = 70f;
    
    [Tooltip("위험 상태 기준 dB 수준 - 이상면 청력 손상 위험")]
    [Range(85f, 110f)]
    public float dangerDecibelLevel = 90f;
    
    [Tooltip("염증 자동 치료 속도 - 클수록 빨리 회복")]
    [Range(0.01f, 0.5f)]
    public float autoHealingRate = 0.1f;
}

[System.Serializable]
public class TemporalBoneStatus
{
    [Header("📊 실시간 상태 정보 (Real-time Status)")]
    [Tooltip("현재 전체적인 건강 상태")]
    [ReadOnly] public string overallHealth = "Healthy";
    
    [Tooltip("현재 듣고 있는 소리 크기 (dB)")]
    [ReadOnly] public float currentSoundLevel = 0f;
    
    [Tooltip("소리 전달 효율 (%) - 100%가 정상")]
    [ReadOnly] public float transmissionEfficiency = 100f;
    
    [Tooltip("염증 수준 (%) - 0%가 정상")]
    [ReadOnly] public float inflammationLevel = 0f;
    
    [Tooltip("신경 신호 강도 (%) - 100%가 정상")]
    [ReadOnly] public float nerveSignalStrength = 100f;
    
    [Tooltip("혈류 상태 (%) - 100%가 정상")]
    [ReadOnly] public float bloodCirculation = 100f;
    
    [Header("⚠️ 경고 및 알림 (Warnings & Alerts)")]
    [Tooltip("현재 활성화된 경고들")]
    [ReadOnly] public List<string> activeWarnings = new List<string>();
    
    [Tooltip("마지막 건강 검진 시간")]
    [ReadOnly] public string lastHealthCheck = "시작 전";
}

public class TemporalBone : MonoBehaviour
{
    [Header("🏗️ 해부학적 구성 요소들 (Anatomical Components)")]
    [Tooltip("고막 컴포넌트 - 소리를 받아서 진동하는 얇은 막")]
    public TympanicMembrane tympanicMembrane;
    
    [Tooltip("이소골 체인 컴포넌트 - 고막의 진동을 내이로 전달하는 3개의 작은 뼈")]
    public OssicleChain ossicleChain;
    
    [Tooltip("내이 수신기 컴포넌트 - 진동을 전기 신호로 변환하는 달팽이관")]
    public InnerEarReceiver innerEarReceiver;
    
    [Tooltip("혈관 시스템 GameObject - 영양 공급 및 염증 반응 담당")]
    public GameObject bloodVesselGameObject;
    
    [Tooltip("청신경 GameObject - 전기 신호를 뇌로 전송")]
    public GameObject auditoryNerveGameObject;
    
    [Tooltip("중이염 GameObject - 귀 질병 상태 시뮬레이션")]
    public GameObject otitisGameObject;
    
    [Header("⚙️ 시스템 설정 (System Settings)")]
    [Tooltip("측두골 전체 설정값들")]
    public TemporalBoneSettings settings;
    
    [Header("📊 상태 모니터링 (Status Monitoring)")]
    [Tooltip("현재 시스템 상태 - 실시간으로 업데이트됨")]
    public TemporalBoneStatus status;
    
    [Header("🎮 테스트 및 디버그 (Testing & Debug)")]
    [Tooltip("테스트 모드 활성화 - 가상의 소리로 테스트")]
    public bool enableTestMode = false;
    
    [Tooltip("테스트용 소리 크기 (0~1)")]
    [Range(0f, 1f)]
    public float testSoundAmplitude = 0.3f;
    
    [Tooltip("테스트용 소리 주파수 (Hz)")]
    [Range(20f, 20000f)]
    public float testSoundFrequency = 440f;
    
    [Tooltip("콘솔에 상세 로그 출력 - 개발자용")]
    public bool enableDebugLogs = false;
    
    [Header("📊 데이터 출력 (Data Export)")]
    [Tooltip("실시간 데이터 기록 활성화")]
    public bool enableDataRecording = false;
    
    [Tooltip("CSV 파일 저장 주기 (초)")]
    [Range(0.1f, 10f)]
    public float csvSaveInterval = 1.0f;
    
    [Tooltip("CSV 파일 이름 접두사")]
    public string csvFilePrefix = "EarSimulation";
    
    [Tooltip("저장 경로 (빈칸이면 기본 경로)")]
    public string customSavePath = "";
    
    // ============================================================================
    // 🔧 내부 변수들 (Private Variables) - Inspector에 보이지 않는 작업용 변수들
    // ============================================================================
    
    private float lastHealthCheckTime = 0f;        // 마지막 건강 검진 시간
    private float healthCheckInterval = 1.0f;      // 건강 검진 간격 (초)
    private float currentAudioInput = 0f;          // 현재 오디오 입력값
    private float currentFrequencyInput = 440f;    // 현재 주파수 입력값
    private bool systemInitialized = false;       // 시스템 초기화 완료 여부
    private List<string> previousWarnings = new List<string>(); // 이전 경고 목록 (중복 방지용)
    
    // 성능 최적화를 위한 캐시 변수들
    private float cachedTransmissionEfficiency = 100f;
    private float cachedInflammationLevel = 0f;
    private float lastCacheUpdateTime = 0f;
    private float cacheUpdateInterval = 0.1f; // 캐시 업데이트 간격
    
    // 런타임에 가져올 컴포넌트 참조들 (컴파일 순서 문제 해결)
    private MonoBehaviour bloodVessel;
    private MonoBehaviour auditoryNerve;
    private MonoBehaviour otitisMedia;
    
    // CSV 데이터 기록 관련 변수들
    private System.Text.StringBuilder csvData;
    private float lastCsvSaveTime = 0f;
    private string currentCsvPath = "";
    private bool csvHeaderWritten = false;
    private List<float> recordedSoundLevels;
    private List<float> recordedTransmissionEfficiency;
    private List<string> recordedTimestamps;

    /*
     * ====================================================================
     * 🚀 UNITY 생명주기 메서드들 (Unity Lifecycle Methods)
     * ====================================================================
     * Unity에서 자동으로 호출되는 메서드들입니다.
     * 게임 시작부터 종료까지의 각 단계에서 실행됩니다.
     */

    /// <summary>
    /// 🎬 START - 게임 시작 시 한 번만 실행
    /// 
    /// 실행 순서:
    /// 1. 시스템 초기화 및 안전성 검사
    /// 2. 각 구성 요소들 연결 확인
    /// 3. 기본 설정값 적용
    /// 4. 건강 모니터링 시작
    /// 
    /// 💡 이 메서드는 Unity가 자동으로 호출합니다.
    /// 사용자가 직접 호출할 필요 없습니다.
    /// </summary>
    void Start()
    {
        LogDebug("🎬 측두골 시스템 시작 중...");
        
        // 1단계: 기본 설정 초기화
        InitializeSystem();
        
        // 2단계: 구성 요소들 검증 및 연결
        ValidateAndConnectComponents();
        
        // 3단계: 초기 상태 설정
        SetupInitialState();
        
        // 4단계: CSV 데이터 기록 시스템 초기화
        InitializeCsvRecording();
        
        // 5단계: 시스템 준비 완료
        systemInitialized = true;
        
        LogDebug("✅ 측두골 시스템 초기화 완료!");
    }

    /// <summary>
    /// 🔄 UPDATE - 매 프레임마다 실행 (초당 60-120회)
    /// 
    /// 실행 내용:
    /// 1. 소리 입력 처리 (마이크, 오디오 파일 등)
    /// 2. 구성 요소들 간 데이터 전달
    /// 3. 건강 상태 모니터링
    /// 4. 실시간 상태 업데이트
    /// 
    /// ⚡ 성능 최적화:
    /// - 중요한 작업: 매 프레임 실행
    /// - 덜 중요한 작업: 몇 프레임마다 실행
    /// </summary>
    void Update()
    {
        // 🛑 시스템이 초기화되지 않았거나 비활성화된 경우 중단
        if (!systemInitialized || !settings.enableRealTimeSimulation)
            return;

        // 🎵 1단계: 소리 입력 처리 (매 프레임)
        ProcessAudioInput();
        
        // 🔄 2단계: 소리 전달 체인 처리 (매 프레임)
        ProcessSoundTransmissionChain();
        
        // 📊 3단계: 상태 업데이트 (캐시 사용으로 최적화)
        if (Time.time - lastCacheUpdateTime >= cacheUpdateInterval)
        {
            UpdateSystemStatus();
            lastCacheUpdateTime = Time.time;
        }
        
        // 🩺 4단계: 건강 검진 (1초마다)
        if (settings.enableHealthMonitoring && Time.time - lastHealthCheckTime >= healthCheckInterval)
        {
            PerformHealthCheck();
            lastHealthCheckTime = Time.time;
        }
        
        // 📊 5단계: CSV 데이터 기록 (설정된 간격마다)
        if (enableDataRecording && Time.time - lastCsvSaveTime >= csvSaveInterval)
        {
            RecordDataToCsv();
            lastCsvSaveTime = Time.time;
        }
    }

    /*
     * ====================================================================
     * 🔧 시스템 초기화 메서드들 (System Initialization Methods)
     * ====================================================================
     * 게임 시작 시 시스템을 안전하게 초기화하는 메서드들입니다.
     */

    /// <summary>
    /// 🛠️ 시스템 기본 초기화
    /// 
    /// 하는 일:
    /// 1. 설정값 검증 및 보정
    /// 2. 상태 구조체 초기화
    /// 3. 캐시 변수들 초기화
    /// </summary>
    void InitializeSystem()
    {
        LogDebug("🛠️ 시스템 초기화 중...");
        
        // 설정값이 null인 경우 기본값으로 초기화
        if (settings == null)
        {
            settings = new TemporalBoneSettings();
            LogDebug("⚠️ 설정값이 없어서 기본값으로 초기화했습니다.");
        }
        
        // 상태 구조체 초기화
        if (status == null)
        {
            status = new TemporalBoneStatus();
        }
        
        // 경고 목록 초기화
        status.activeWarnings.Clear();
        previousWarnings.Clear();
        
        // 설정값 유효성 검사 및 보정
        ValidateSettings();
        
        LogDebug("✅ 기본 초기화 완료");
    }

    /// <summary>
    /// 🔍 설정값 유효성 검사 및 자동 보정
    /// 
    /// 검사 항목:
    /// - 민감도가 너무 높거나 낮지 않은지
    /// - dB 임계값들이 논리적으로 맞는지
    /// - 치료 속도가 현실적인지
    /// </summary>
    void ValidateSettings()
    {
        // 민감도 범위 확인
        if (settings.overallSensitivity < 0.1f || settings.overallSensitivity > 3.0f)
        {
            LogDebug("⚠️ 민감도 값이 비정상적입니다. 1.0으로 보정합니다.");
            settings.overallSensitivity = 1.0f;
        }
        
        // dB 임계값 논리 확인
        if (settings.dangerDecibelLevel <= settings.safeDecibelLevel)
        {
            LogDebug("⚠️ 위험 dB가 안전 dB보다 낮습니다. 자동 보정합니다.");
            settings.safeDecibelLevel = 70f;
            settings.dangerDecibelLevel = 90f;
        }
        
        // 치료 속도 확인
        if (settings.autoHealingRate < 0.01f || settings.autoHealingRate > 0.5f)
        {
            LogDebug("⚠️ 치료 속도가 비현실적입니다. 0.1로 보정합니다.");
            settings.autoHealingRate = 0.1f;
        }
    }

    /// <summary>
    /// 🔗 구성 요소들 검증 및 자동 연결
    /// 
    /// 하는 일:
    /// 1. 필수 컴포넌트들이 연결되어 있는지 확인
    /// 2. 없는 컴포넌트는 자동으로 찾아서 연결
    /// 3. 여전히 없으면 경고 메시지 출력
    /// </summary>
    void ValidateAndConnectComponents()
    {
        LogDebug("🔗 구성 요소 연결 확인 중...");
        
        // 고막 연결 확인
        if (tympanicMembrane == null)
        {
            tympanicMembrane = FindObjectOfType<TympanicMembrane>();
            if (tympanicMembrane == null)
                LogDebug("⚠️ 고막(TympanicMembrane) 컴포넌트를 찾을 수 없습니다.");
            else
                LogDebug("🔍 고막 컴포넌트를 자동으로 찾아 연결했습니다.");
        }
        
        // 이소골 연결 확인
        if (ossicleChain == null)
        {
            ossicleChain = FindObjectOfType<OssicleChain>();
            if (ossicleChain == null)
                LogDebug("⚠️ 이소골(OssicleChain) 컴포넌트를 찾을 수 없습니다.");
            else
                LogDebug("🔍 이소골 컴포넌트를 자동으로 찾아 연결했습니다.");
        }
        
        // 내이 연결 확인
        if (innerEarReceiver == null)
        {
            innerEarReceiver = FindObjectOfType<InnerEarReceiver>();
            if (innerEarReceiver == null)
                LogDebug("⚠️ 내이(InnerEarReceiver) 컴포넌트를 찾을 수 없습니다.");
            else
                LogDebug("🔍 내이 컴포넌트를 자동으로 찾아 연결했습니다.");
        }
        
        // 혈관 연결 확인 (GameObject 방식으로 변경)
        if (bloodVessel == null)
        {
            if (bloodVesselGameObject != null)
            {
                bloodVessel = bloodVesselGameObject.GetComponent<MonoBehaviour>();
                LogDebug("🔗 혈관 GameObject에서 컴포넌트를 가져왔습니다.");
            }
            else
            {
                // 자동으로 BloodVessel 컴포넌트를 찾아보기 (이름으로)
                var foundObjects = FindObjectsOfType<MonoBehaviour>();
                foreach (var obj in foundObjects)
                {
                    if (obj.GetType().Name == "BloodVessel")
                    {
                        bloodVessel = obj;
                        LogDebug("🔍 혈관 컴포넌트를 자동으로 찾아 연결했습니다.");
                        break;
                    }
                }
                if (bloodVessel == null)
                    LogDebug("ℹ️ 혈관(BloodVessel) 컴포넌트가 없습니다. (선택사항)");
            }
        }
        
        // 청신경 연결 확인 (GameObject 방식으로 변경)
        if (auditoryNerve == null)
        {
            if (auditoryNerveGameObject != null)
            {
                auditoryNerve = auditoryNerveGameObject.GetComponent<MonoBehaviour>();
                LogDebug("🔗 청신경 GameObject에서 컴포넌트를 가져왔습니다.");
            }
            else
            {
                // 자동으로 AuditoryNerve 컴포넌트를 찾아보기 (이름으로)
                var foundObjects = FindObjectsOfType<MonoBehaviour>();
                foreach (var obj in foundObjects)
                {
                    if (obj.GetType().Name == "AuditoryNerve")
                    {
                        auditoryNerve = obj;
                        LogDebug("🔍 청신경 컴포넌트를 자동으로 찾아 연결했습니다.");
                        break;
                    }
                }
                if (auditoryNerve == null)
                    LogDebug("ℹ️ 청신경(AuditoryNerve) 컴포넌트가 없습니다. (선택사항)");
            }
        }
        
        // 중이염 연결 확인 (GameObject 방식으로 변경)
        if (otitisMedia == null)
        {
            if (otitisGameObject != null)
            {
                otitisMedia = otitisGameObject.GetComponent<MonoBehaviour>();
                LogDebug("🔗 중이염 GameObject에서 컴포넌트를 가져왔습니다.");
            }
            else
            {
                // 자동으로 Otitis 컴포넌트를 찾아보기 (이름으로)
                var foundObjects = FindObjectsOfType<MonoBehaviour>();
                foreach (var obj in foundObjects)
                {
                    if (obj.GetType().Name == "Otitis")
                    {
                        otitisMedia = obj;
                        LogDebug("🔍 중이염 컴포넌트를 자동으로 찾아 연결했습니다.");
                        break;
                    }
                }
                if (otitisMedia == null)
                    LogDebug("ℹ️ 중이염(Otitis) 컴포넌트가 없습니다. (선택사항)");
            }
        }
        
        LogDebug("✅ 구성 요소 연결 확인 완료");
    }

    /// <summary>
    /// 🎯 초기 상태 설정
    /// 
    /// 하는 일:
    /// 1. 모든 상태값을 정상값으로 초기화
    /// 2. 시간 관련 변수들 설정
    /// 3. 각 컴포넌트들의 초기 설정 적용
    /// </summary>
    void SetupInitialState()
    {
        LogDebug("🎯 초기 상태 설정 중...");
        
        // 상태값들 초기화
        status.overallHealth = "Healthy";
        status.currentSoundLevel = 0f;
        status.transmissionEfficiency = 100f;
        status.inflammationLevel = 0f;
        status.nerveSignalStrength = 100f;
        status.bloodCirculation = 100f;
        status.lastHealthCheck = System.DateTime.Now.ToString("HH:mm:ss");
        
        // 캐시 변수들 초기화
        cachedTransmissionEfficiency = 100f;
        cachedInflammationLevel = 0f;
        
        // 시간 변수들 초기화
        lastHealthCheckTime = Time.time;
        lastCacheUpdateTime = Time.time;
        
        LogDebug("✅ 초기 상태 설정 완료");
    }

    /*
     * ====================================================================
     * 🎵 오디오 처리 메서드들 (Audio Processing Methods)
     * ====================================================================
     * 소리 입력을 받아서 귀의 각 부분으로 전달하는 메서드들입니다.
     */

    /// <summary>
    /// 🎤 오디오 입력 처리
    /// 
    /// 입력 소스:
    /// 1. 테스트 모드: 가상의 사인파 소리
    /// 2. 마이크 입력 (추후 구현 가능)
    /// 3. 오디오 파일 (추후 구현 가능)
    /// 4. 다른 스크립트에서 전달된 소리
    /// </summary>
    void ProcessAudioInput()
    {
        if (enableTestMode)
        {
            // 테스트 모드: 가상의 사인파 생성
            GenerateTestSound();
        }
        else
        {
            // 실제 입력 처리 (현재는 무음)
            currentAudioInput = 0f;
            currentFrequencyInput = 440f;
        }
        
        // 전체 민감도 적용
        currentAudioInput *= settings.overallSensitivity;
        
        // 시뮬레이션 품질에 따른 정밀도 조정
        if (settings.simulationQuality < 1.0f)
        {
            // 품질이 낮으면 입력을 단순화
            currentAudioInput = Mathf.Round(currentAudioInput * 10f) / 10f;
        }
    }

    /// <summary>
    /// 🎵 테스트용 사인파 소리 생성
    /// 
    /// 생성 방식:
    /// - 시간에 따라 변화하는 사인파
    /// - 사용자가 설정한 주파수와 크기 사용
    /// - 자연스러운 음성 효과를 위해 약간의 변화 추가
    /// </summary>
    void GenerateTestSound()
    {
        // 기본 사인파 생성
        float time = Time.time;
        float sineWave = Mathf.Sin(2 * Mathf.PI * testSoundFrequency * time);
        
        // 자연스러운 변화를 위한 저주파 변조 추가
        float modulation = 1.0f + 0.1f * Mathf.Sin(2 * Mathf.PI * 0.5f * time);
        
        // 최종 오디오 신호 계산
        currentAudioInput = testSoundAmplitude * sineWave * modulation;
        currentFrequencyInput = testSoundFrequency;
        
        // 음수값을 절댓값으로 변환 (진폭만 중요)
        currentAudioInput = Mathf.Abs(currentAudioInput);
    }

    /// <summary>
    /// 🔄 소리 전달 체인 처리
    /// 
    /// 전달 경로:
    /// 소리 입력 → 고막 → 이소골 → 내이 → 신경 → 뇌
    /// 
    /// 각 단계에서 손실과 변형이 발생할 수 있습니다:
    /// - 중이염으로 인한 전달 손실
    /// - 혈류 장애로 인한 기능 저하
    /// - 신경 손상으로 인한 신호 약화
    /// </summary>
    void ProcessSoundTransmissionChain()
    {
        // 0단계: 입력 소리가 없으면 조기 종료
        if (currentAudioInput <= 0.001f)
        {
            // 모든 컴포넌트에 무음 전달
            SendSilenceToAllComponents();
            return;
        }
        
        // 1단계: 고막으로 소리 전달
        float tympanicOutput = ProcessTympanicMembrane(currentAudioInput);
        
        // 2단계: 이소골로 진동 전달
        float ossicleOutput = ProcessOssicleChain(tympanicOutput);
        
        // 3단계: 내이로 기계적 진동 전달
        float innerEarOutput = ProcessInnerEar(ossicleOutput);
        
        // 4단계: 청신경으로 전기 신호 전달
        float nerveOutput = ProcessAuditoryNerve(innerEarOutput);
        
        // 5단계: 혈관계 상태 업데이트
        UpdateBloodVesselSystem();
        
        // 6단계: 질병 상태 (중이염) 처리
        UpdateOtitisEffects();
        
        // 현재 소리 레벨 상태 업데이트
        if (innerEarReceiver != null)
        {
            status.currentSoundLevel = innerEarReceiver.GetCurrentLevel();
        }
    }

    /// <summary>
    /// 🥁 고막(Tympanic Membrane) 처리
    /// 
    /// 고막의 역할:
    /// - 공기 중의 소리파를 기계적 진동으로 변환
    /// - 소리의 방향성 정보 제공
    /// - 중이 압력 조절
    /// </summary>
    float ProcessTympanicMembrane(float soundInput)
    {
        if (tympanicMembrane == null)
            return soundInput * 0.8f; // 고막이 없으면 80% 효율로 근사
        
        // 고막에 소리 전달 (TympanicMembrane 스크립트의 메서드 사용)
        // 실제 물리 계산은 TympanicMembrane에서 처리됨
        
        // 고막의 현재 상태에 따른 전달 효율
        float membraneEfficiency = 1.0f;
        
        // 염증이 있으면 고막 움직임 제한
        if (otitisMedia != null && GetComponentProperty(otitisMedia, "severity", 0f) > 0.1f)
        {
            float severity = GetComponentProperty(otitisMedia, "severity", 0f);
            membraneEfficiency *= (1.0f - severity * 0.3f);
        }
        
        // 고막 천공이 있으면 추가 전달 손실
        if (tympanicMembrane != null && tympanicMembrane.HasPerforation())
        {
            float perforationLoss = tympanicMembrane.GetPerforationTransmissionLoss();
            membraneEfficiency *= perforationLoss;
            LogDebug($"🔥 고막 천공으로 인한 전달 손실: {(1f - perforationLoss) * 100f:F1}%");
        }
        
        float output = soundInput * membraneEfficiency;
        
        LogDebug($"🥁 고막 처리: 입력 {soundInput:F3} → 출력 {output:F3} (효율: {membraneEfficiency:F1}%)");
        
        return output;
    }

    /// <summary>
    /// 🦴 이소골 체인(Ossicle Chain) 처리
    /// 
    /// 이소골의 역할:
    /// - 고막의 진동을 내이로 전달
    /// - 임피던스 매칭 (공기 → 액체)
    /// - 소리 증폭 (약 20-30dB)
    /// </summary>
    float ProcessOssicleChain(float vibrationInput)
    {
        if (ossicleChain == null)
            return vibrationInput * 1.5f; // 이소골이 없으면 증폭 없이 전달
        
        // 이소골 체인의 기본 증폭 효과
        float amplification = 1.8f; // 실제로는 20-30dB 증폭
        
        // 중이염에 의한 이소골 움직임 제한
        float otitisReduction = 1.0f;
        if (otitisMedia != null)
        {
            // 고름이나 액체가 이소골 움직임을 방해
            float severity = GetComponentProperty(otitisMedia, "severity", 0f);
            float fluidLevel = GetComponentProperty(otitisMedia, "fluidLevel", 0f);
            otitisReduction = 1.0f - (severity * fluidLevel * 0.5f);
        }
        
        // 혈류 장애에 의한 영향
        float bloodFlowEffect = 1.0f;
        if (bloodVessel != null)
        {
            float bloodFlow = GetComponentProperty(bloodVessel, "bloodFlow", 1.0f);
            bloodFlowEffect = Mathf.Lerp(0.7f, 1.0f, bloodFlow);
        }
        
        float totalEfficiency = amplification * otitisReduction * bloodFlowEffect;
        float output = vibrationInput * totalEfficiency;
        
        LogDebug($"🦴 이소골 처리: 입력 {vibrationInput:F3} → 출력 {output:F3} (증폭: {totalEfficiency:F1}x)");
        
        return output;
    }

    /// <summary>
    /// 🐚 내이(Inner Ear) 처리
    /// 
    /// 내이의 역할:
    /// - 기계적 진동을 전기 신호로 변환
    /// - 주파수 분석 (음높이 인식)
    /// - 소리 크기 인식 (음량 인식)
    /// </summary>
    float ProcessInnerEar(float mechanicalInput)
    {
        if (innerEarReceiver == null)
            return mechanicalInput * 0.9f; // 내이가 없으면 90% 효율로 근사
        
        // InnerEarReceiver의 ReceiveVibration 메서드 사용
        innerEarReceiver.ReceiveVibration(mechanicalInput, currentFrequencyInput);
        
        // 내이에서 출력되는 전기 신호 강도
        float electricalOutput = innerEarReceiver.GetCurrentLevel() / 100f; // dB를 0-1 범위로 변환
        
        LogDebug($"🐚 내이 처리: 입력 {mechanicalInput:F3} → 전기신호 {electricalOutput:F3}");
        
        return electricalOutput;
    }

    /// <summary>
    /// 🧠 청신경(Auditory Nerve) 처리
    /// 
    /// 청신경의 역할:
    /// - 내이의 전기 신호를 뇌로 전송
    /// - 신호 증폭 및 필터링
    /// - 노이즈 제거
    /// </summary>
    float ProcessAuditoryNerve(float electricalInput)
    {
        if (auditoryNerve == null)
            return electricalInput * 0.95f; // 신경이 없으면 95% 효율로 근사
        
        // 청신경의 TransmitSignal 메서드 사용
        CallComponentMethod(auditoryNerve, "TransmitSignal", electricalInput);
        
        // 신경 손상에 따른 신호 약화
        float damageLevel = GetComponentProperty(auditoryNerve, "damageLevel", 0f);
        float signalStrength = GetComponentProperty(auditoryNerve, "signalStrength", 1f);
        float nerveEfficiency = 1.0f - damageLevel;
        float output = electricalInput * nerveEfficiency * signalStrength;
        
        LogDebug($"🧠 청신경 처리: 입력 {electricalInput:F3} → 뇌신호 {output:F3} (효율: {nerveEfficiency:F1}%)");
        
        return output;
    }

    /// <summary>
    /// 🔇 모든 컴포넌트에 무음 상태 전달
    /// 
    /// 소리가 없을 때 호출되어 모든 시스템을 조용한 상태로 만듭니다.
    /// </summary>
    void SendSilenceToAllComponents()
    {
        if (innerEarReceiver != null)
        {
            innerEarReceiver.ReceiveVibration(0f, 440f);
        }
        
        if (auditoryNerve != null)
        {
            CallComponentMethod(auditoryNerve, "TransmitSignal", 0f);
        }
        
        status.currentSoundLevel = 0f;
    }

    /*
     * ====================================================================
     * 🩺 건강 모니터링 메서드들 (Health Monitoring Methods)
     * ====================================================================
     * 귀의 건강 상태를 실시간으로 감시하고 관리하는 메서드들입니다.
     */

    /// <summary>
    /// 🏥 혈관계 시스템 업데이트
    /// 
    /// 혈관의 역할:
    /// - 귀 조직에 영양 공급
    /// - 염증 반응 조절
    /// - 치료 및 회복 촉진
    /// </summary>
    void UpdateBloodVesselSystem()
    {
        if (bloodVessel == null) return;
        
        // 염증 수준에 따른 혈류 변화
        float otitisMaxSeverity = GetComponentProperty(otitisMedia, "severity", 0f);
        if (otitisMedia != null && otitisMaxSeverity > 0.2f)
        {
            // 염증이 있으면 혈류 증가 (면역 반응)
            SetComponentProperty(bloodVessel, "inflammation", otitisMaxSeverity);
            SetComponentProperty(bloodVessel, "bloodFlow", Mathf.Min(1.5f, 1.0f + otitisMaxSeverity * 0.5f));
        }
        else
        {
            // 정상 상태로 회복
            float currentInflammation = GetComponentProperty(bloodVessel, "inflammation", 0f);
            float currentBloodFlow = GetComponentProperty(bloodVessel, "bloodFlow", 1f);
            SetComponentProperty(bloodVessel, "inflammation", Mathf.Lerp(currentInflammation, 0f, settings.autoHealingRate * Time.deltaTime));
            SetComponentProperty(bloodVessel, "bloodFlow", Mathf.Lerp(currentBloodFlow, 1.0f, settings.autoHealingRate * Time.deltaTime));
        }
        
        // 상태 업데이트
        float bloodFlow = GetComponentProperty(bloodVessel, "bloodFlow", 1f);
        status.bloodCirculation = bloodFlow * 100f;
    }

    /// <summary>
    /// 🦠 중이염(Otitis Media) 효과 업데이트
    /// 
    /// 중이염의 영향:
    /// - 고막 움직임 제한
    /// - 이소골 전달 효율 감소
    /// - 통증 및 불편감
    /// - 청력 일시적 감소
    /// </summary>
    void UpdateOtitisEffects()
    {
        if (otitisMedia == null) return;
        
        // 자동 치료 시스템 (시간이 지나면서 자연 회복)
        float currentSeverity = GetComponentProperty(otitisMedia, "severity", 0f);
        if (currentSeverity > 0f)
        {
            float healingRate = settings.autoHealingRate;
            
            // 혈류가 좋으면 더 빨리 회복
            float bloodFlow = GetComponentProperty(bloodVessel, "bloodFlow", 1f);
            if (bloodVessel != null && bloodFlow > 1.0f)
            {
                healingRate *= bloodFlow;
            }
            
            float currentFluidLevel = GetComponentProperty(otitisMedia, "fluidLevel", 0f);
            SetComponentProperty(otitisMedia, "severity", Mathf.Lerp(currentSeverity, 0f, healingRate * Time.deltaTime));
            SetComponentProperty(otitisMedia, "fluidLevel", Mathf.Lerp(currentFluidLevel, 0f, healingRate * 0.5f * Time.deltaTime));
        }
        
        // 전달 효율 계산
        float finalSeverity = GetComponentProperty(otitisMedia, "severity", 0f);
        cachedTransmissionEfficiency = 100f * (1.0f - finalSeverity * 0.4f);
        cachedInflammationLevel = finalSeverity * 100f;
        
        // 상태 업데이트
        status.transmissionEfficiency = cachedTransmissionEfficiency;
        status.inflammationLevel = cachedInflammationLevel;
    }

    /// <summary>
    /// 🔍 정기 건강 검진 수행
    /// 
    /// 검진 항목:
    /// 1. 소음 노출 수준 검사
    /// 2. 염증 상태 확인
    /// 3. 신경 기능 검사
    /// 4. 전체적인 청력 상태 평가
    /// 5. 필요시 경고 메시지 발생
    /// </summary>
    void PerformHealthCheck()
    {
        LogDebug("🔍 정기 건강 검진 시행 중...");
        
        // 경고 목록 초기화
        status.activeWarnings.Clear();
        
        // 1. 소음 수준 검사
        CheckNoiseExposure();
        
        // 2. 염증 상태 검사
        CheckInflammationStatus();
        
        // 3. 신경 기능 검사
        CheckNerveFunction();
        
        // 4. 전체 건강 상태 평가
        EvaluateOverallHealth();
        
        // 5. 검진 시간 업데이트
        status.lastHealthCheck = System.DateTime.Now.ToString("HH:mm:ss");
        
        LogDebug($"🏥 건강 검진 완료: {status.overallHealth} ({status.activeWarnings.Count}개 경고)");
    }

    /// <summary>
    /// 🔊 소음 노출 수준 검사
    /// 
    /// 검사 기준:
    /// - 안전 수준: 70dB 이하
    /// - 주의 수준: 70-85dB
    /// - 위험 수준: 85dB 이상
    /// </summary>
    void CheckNoiseExposure()
    {
        float currentDB = status.currentSoundLevel;
        
        if (currentDB > settings.dangerDecibelLevel)
        {
            string warning = $"위험한 소음 노출: {currentDB:F1}dB (한계: {settings.dangerDecibelLevel}dB)";
            AddWarningIfNew(warning);
        }
        else if (currentDB > settings.safeDecibelLevel)
        {
            string warning = $"소음 주의 필요: {currentDB:F1}dB";
            AddWarningIfNew(warning);
        }
    }

    /// <summary>
    /// 🔥 염증 상태 검사
    /// 
    /// 검사 기준:
    /// - 정상: 염증 0-10%
    /// - 경미: 염증 10-30%
    /// - 중등도: 염증 30-60%
    /// - 심각: 염증 60% 이상
    /// </summary>
    void CheckInflammationStatus()
    {
        float inflammation = status.inflammationLevel;
        
        if (inflammation > 60f)
        {
            AddWarningIfNew($"심각한 염증 상태: {inflammation:F1}% (즉시 치료 필요)");
        }
        else if (inflammation > 30f)
        {
            AddWarningIfNew($"중등도 염증: {inflammation:F1}% (치료 권장)");
        }
        else if (inflammation > 10f)
        {
            AddWarningIfNew($"경미한 염증: {inflammation:F1}% (관찰 필요)");
        }
    }

    /// <summary>
    /// 🧠 신경 기능 검사
    /// 
    /// 검사 항목:
    /// - 신호 강도
    /// - 신경 손상 정도
    /// - 전달 지연 시간
    /// </summary>
    void CheckNerveFunction()
    {
        float nerveStrength = status.nerveSignalStrength;
        
        if (nerveStrength < 50f)
        {
            AddWarningIfNew($"심각한 신경 기능 저하: {nerveStrength:F1}%");
        }
        else if (nerveStrength < 80f)
        {
            AddWarningIfNew($"신경 기능 저하: {nerveStrength:F1}%");
        }
        
        // 청신경 손상 검사
        float nerveDamageLevel = GetComponentProperty(auditoryNerve, "damageLevel", 0f);
        if (auditoryNerve != null && nerveDamageLevel > 0.3f)
        {
            AddWarningIfNew($"청신경 손상 감지: {nerveDamageLevel * 100f:F1}%");
        }
    }

    /// <summary>
    /// 🏥 전체 건강 상태 평가
    /// 
    /// 평가 기준:
    /// - Excellent: 모든 지표 95% 이상
    /// - Healthy: 모든 지표 80% 이상
    /// - Caution: 일부 지표 저하
    /// - Warning: 여러 지표 문제
    /// - Critical: 심각한 문제 발생
    /// </summary>
    void EvaluateOverallHealth()
    {
        int warningCount = status.activeWarnings.Count;
        float avgEfficiency = (status.transmissionEfficiency + status.nerveSignalStrength + status.bloodCirculation) / 3f;
        
        if (warningCount == 0 && avgEfficiency >= 95f)
        {
            status.overallHealth = "Excellent";
        }
        else if (warningCount <= 1 && avgEfficiency >= 80f)
        {
            status.overallHealth = "Healthy";
        }
        else if (warningCount <= 2 && avgEfficiency >= 60f)
        {
            status.overallHealth = "Caution";
        }
        else if (avgEfficiency >= 40f)
        {
            status.overallHealth = "Warning";
        }
        else
        {
            status.overallHealth = "Critical";
        }
    }

    /// <summary>
    /// ⚠️ 새로운 경고 추가 (중복 방지)
    /// 
    /// 같은 경고가 반복해서 나오지 않도록 중복을 체크합니다.
    /// </summary>
    void AddWarningIfNew(string warning)
    {
        if (!status.activeWarnings.Contains(warning) && !previousWarnings.Contains(warning))
        {
            status.activeWarnings.Add(warning);
            previousWarnings.Add(warning);
            
            // 중요한 경고는 콘솔에도 출력
            if (warning.Contains("심각") || warning.Contains("위험"))
            {
                Debug.LogWarning($"🚨 측두골 경고: {warning}");
            }
        }
    }

    /*
     * ====================================================================
     * 📊 상태 업데이트 메서드들 (Status Update Methods)
     * ====================================================================
     * 시스템의 실시간 상태를 업데이트하고 관리하는 메서드들입니다.
     */

    /// <summary>
    /// 📊 시스템 상태 실시간 업데이트
    /// 
    /// 업데이트 항목:
    /// - 현재 소리 레벨
    /// - 전달 효율
    /// - 신경 신호 강도
    /// - 혈류 상태
    /// </summary>
    void UpdateSystemStatus()
    {
        // 신경 신호 강도 업데이트
        if (auditoryNerve != null)
        {
            float signalStrength = GetComponentProperty(auditoryNerve, "signalStrength", 1f);
            float damageLevel = GetComponentProperty(auditoryNerve, "damageLevel", 0f);
            status.nerveSignalStrength = signalStrength * (1.0f - damageLevel) * 100f;
        }
        
        // 이미 캐시된 값들은 다른 메서드에서 업데이트됨
        // (performance optimization)
    }

    /*
     * ====================================================================
     * 🌐 공개 API 메서드들 (Public API Methods)
     * ====================================================================
     * 다른 스크립트에서 이 시스템과 상호작용할 때 사용하는 메서드들입니다.
     */

    /// <summary>
    /// 🎵 외부에서 소리 입력 (Public API)
    /// 
    /// 사용법:
    /// temporalBone.ReceiveExternalSound(0.5f, 1000f);
    /// 
    /// @param amplitude: 소리 크기 (0.0~1.0)
    /// @param frequency: 주파수 (20~20000 Hz)
    /// </summary>
    public void ReceiveExternalSound(float amplitude, float frequency)
    {
        currentAudioInput = Mathf.Clamp01(amplitude);
        currentFrequencyInput = Mathf.Clamp(frequency, 20f, 20000f);
        
        LogDebug($"🎵 외부 소리 입력: {amplitude:F3} @ {frequency:F0}Hz");
    }

    /// <summary>
    /// 📊 현재 시스템 상태 정보 반환 (Public API)
    /// 
    /// 사용법:
    /// TemporalBoneStatus status = temporalBone.GetSystemStatus();
    /// </summary>
    public TemporalBoneStatus GetSystemStatus()
    {
        return status;
    }

    /// <summary>
    /// 🏥 현재 건강 상태 문자열 반환 (Public API)
    /// 
    /// 반환값: "Excellent", "Healthy", "Caution", "Warning", "Critical"
    /// </summary>
    public string GetHealthStatus()
    {
        return status.overallHealth;
    }

    /// <summary>
    /// 🔊 현재 소리 크기 (dB) 반환 (Public API)
    /// </summary>
    public float GetCurrentSoundLevel()
    {
        return status.currentSoundLevel;
    }

    /// <summary>
    /// ⚠️ 활성화된 경고 목록 반환 (Public API)
    /// </summary>
    public List<string> GetActiveWarnings()
    {
        return new List<string>(status.activeWarnings); // 복사본 반환 (안전성)
    }

    /// <summary>
    /// 💊 중이염 치료 시뮬레이션 (Public API)
    /// 
    /// 사용법:
    /// temporalBone.TreatOtitis(0.8f); // 80% 효과로 치료
    /// </summary>
    public void TreatOtitis(float treatmentEffectiveness)
    {
        if (otitisMedia != null)
        {
            float reduction = treatmentEffectiveness * 0.5f * Time.deltaTime;
            float currentSeverity = GetComponentProperty(otitisMedia, "severity", 0f);
            float currentFluidLevel = GetComponentProperty(otitisMedia, "fluidLevel", 0f);
            SetComponentProperty(otitisMedia, "severity", Mathf.Max(0f, currentSeverity - reduction));
            SetComponentProperty(otitisMedia, "fluidLevel", Mathf.Max(0f, currentFluidLevel - reduction * 0.8f));
            
            LogDebug($"💊 중이염 치료 적용: 효과 {treatmentEffectiveness:F1}%");
        }
    }

    /// <summary>
    /// 🔄 시스템 재시작 (Public API)
    /// 
    /// 모든 상태를 초기값으로 재설정합니다.
    /// </summary>
    public void ResetSystem()
    {
        LogDebug("🔄 시스템 재시작 중...");
        
        systemInitialized = false;
        Start(); // 초기화 다시 실행
        
        // 모든 컴포넌트 재설정
        if (innerEarReceiver != null)
            innerEarReceiver.ResetMeasurements();
        
        if (otitisMedia != null)
        {
            SetComponentProperty(otitisMedia, "severity", 0f);
            SetComponentProperty(otitisMedia, "fluidLevel", 0f);
        }
        
        if (auditoryNerve != null)
        {
            SetComponentProperty(auditoryNerve, "damageLevel", 0f);
            SetComponentProperty(auditoryNerve, "signalStrength", 1.0f);
        }
        
        if (bloodVessel != null)
        {
            SetComponentProperty(bloodVessel, "bloodFlow", 1.0f);
            SetComponentProperty(bloodVessel, "inflammation", 0f);
        }
        
        LogDebug("✅ 시스템 재시작 완료");
    }

    /*
     * ====================================================================
     * 🐞 디버그 및 유틸리티 메서드들 (Debug & Utility Methods)
     * ====================================================================
     * 개발자를 위한 디버깅 도구들입니다.
     */

    /*
     * ====================================================================
     * 🔧 헬퍼 메서드들 (Helper Methods)
     * ====================================================================
     * 컴파일 순서 문제를 해결하기 위한 유틸리티 메서드들입니다.
     */

    /// <summary>
    /// 🔍 컴포넌트의 프로퍼티 값 가져오기 (Reflection 사용)
    /// </summary>
    float GetComponentProperty(MonoBehaviour component, string propertyName, float defaultValue = 0f)
    {
        if (component == null) return defaultValue;
        
        try
        {
            var property = component.GetType().GetProperty(propertyName);
            if (property != null && property.PropertyType == typeof(float))
            {
                return (float)property.GetValue(component);
            }
            
            var field = component.GetType().GetField(propertyName);
            if (field != null && field.FieldType == typeof(float))
            {
                return (float)field.GetValue(component);
            }
        }
        catch (System.Exception)
        {
            // 에러 발생시 기본값 반환
        }
        
        return defaultValue;
    }

    /// <summary>
    /// 🔧 컴포넌트의 프로퍼티 값 설정하기 (Reflection 사용)
    /// </summary>
    void SetComponentProperty(MonoBehaviour component, string propertyName, float value)
    {
        if (component == null) return;
        
        try
        {
            var property = component.GetType().GetProperty(propertyName);
            if (property != null && property.PropertyType == typeof(float))
            {
                property.SetValue(component, value);
                return;
            }
            
            var field = component.GetType().GetField(propertyName);
            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(component, value);
                return;
            }
        }
        catch (System.Exception)
        {
            // 에러 발생시 무시
        }
    }

    /// <summary>
    /// 📞 컴포넌트의 메서드 호출하기 (Reflection 사용)
    /// </summary>
    void CallComponentMethod(MonoBehaviour component, string methodName, params object[] parameters)
    {
        if (component == null) return;
        
        try
        {
            var method = component.GetType().GetMethod(methodName);
            if (method != null)
            {
                method.Invoke(component, parameters);
            }
        }
        catch (System.Exception)
        {
            // 에러 발생시 무시
        }
    }

    /// <summary>
    /// 🐞 조건부 디버그 로그 출력
    /// 
    /// enableDebugLogs가 true일 때만 콘솔에 메시지를 출력합니다.
    /// </summary>
    void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[TemporalBone] {message}");
        }
    }

    /// <summary>
    /// 🎨 Scene View에서 시각적 디버그 정보 표시
    /// 
    /// Unity Editor의 Scene View에서 귀의 상태를 시각적으로 보여줍니다.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !systemInitialized) return;
        
        // 전체 건강 상태에 따른 색상
        Color healthColor = Color.green;
        switch (status.overallHealth)
        {
            case "Excellent": healthColor = Color.cyan; break;
            case "Healthy": healthColor = Color.green; break;
            case "Caution": healthColor = Color.yellow; break;
            case "Warning": healthColor = new Color(1f, 0.5f, 0f); break; // 주황색
            case "Critical": healthColor = Color.red; break;
        }
        
        // 측두골 전체를 나타내는 큰 구 그리기
        Gizmos.color = healthColor;
        Gizmos.DrawWireSphere(transform.position, 0.02f);
        
        // 소리 입력 레벨 표시
        if (currentAudioInput > 0.01f)
        {
            Gizmos.color = Color.white;
            float soundSize = currentAudioInput * 0.01f;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.03f, soundSize);
        }
        
        // 경고가 있으면 빨간 구 표시
        if (status.activeWarnings.Count > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.04f, 0.002f);
        }
    }

    /// <summary>
    /// 📋 Inspector에 상세 정보 표시
    /// 
    /// Unity Inspector 하단에 현재 상태 정보를 텍스트로 표시합니다.
    /// </summary>
    void OnGUI()
    {
        if (!enableDebugLogs || !systemInitialized) return;
        
        // 화면 왼쪽 상단에 상태 정보 표시
        string statusText = $"🏥 측두골 상태: {status.overallHealth}\n";
        statusText += $"🔊 소리: {status.currentSoundLevel:F1} dB\n";
        statusText += $"📡 전달효율: {status.transmissionEfficiency:F1}%\n";
        statusText += $"🔥 염증: {status.inflammationLevel:F1}%\n";
        statusText += $"🧠 신경: {status.nerveSignalStrength:F1}%\n";
        statusText += $"❤️ 혈류: {status.bloodCirculation:F1}%\n";
        
        if (status.activeWarnings.Count > 0)
        {
            statusText += $"⚠️ 경고 {status.activeWarnings.Count}개";
        }
        
        GUI.Label(new Rect(10, 10, 300, 200), statusText);
    }
    
    /*
     * ====================================================================
     * 📊 CSV 데이터 출력 시스템 (CSV Data Export System)
     * ====================================================================
     */
    
    /// <summary>
    /// 📊 CSV 기록 시스템 초기화
    /// </summary>
    void InitializeCsvRecording()
    {
        if (!enableDataRecording) return;
        
        csvData = new System.Text.StringBuilder();
        recordedSoundLevels = new List<float>();
        recordedTransmissionEfficiency = new List<float>();
        recordedTimestamps = new List<string>();
        
        // CSV 파일 경로 생성
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{csvFilePrefix}_{timestamp}.csv";
        
        if (string.IsNullOrEmpty(customSavePath))
        {
            currentCsvPath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
        }
        else
        {
            currentCsvPath = System.IO.Path.Combine(customSavePath, fileName);
        }
        
        csvHeaderWritten = false;
        lastCsvSaveTime = Time.time;
        
        LogDebug($"📊 CSV 기록 시스템 초기화 완료: {currentCsvPath}");
    }
    
    /// <summary>
    /// 📝 실시간 데이터를 CSV에 기록
    /// </summary>
    void RecordDataToCsv()
    {
        if (!enableDataRecording || csvData == null) return;
        
        // 헤더 작성 (첫 번째 기록 시에만)
        if (!csvHeaderWritten)
        {
            WriteCSVHeader();
            csvHeaderWritten = true;
        }
        
        // 현재 상태 데이터 수집
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        float currentTime = Time.time;
        
        // 추가 데이터 수집
        float perforationSeverity = 0f;
        bool hasPerforation = false;
        if (tympanicMembrane != null)
        {
            hasPerforation = tympanicMembrane.HasPerforation();
            perforationSeverity = tympanicMembrane.GetPerforationSeverity();
        }
        
        float otitisMaxSeverity = GetComponentProperty(otitisMedia, "severity", 0f);
        float nerveDamageLevel = GetComponentProperty(auditoryNerve, "damageLevel", 0f);
        float bloodFlow = GetComponentProperty(bloodVessel, "bloodFlow", 1f);
        
        // CSV 라인 생성
        string csvLine = $"{timestamp},{currentTime:F3}," +
                        $"{status.currentSoundLevel:F2},{currentAudioInput:F4},{currentFrequencyInput:F0}," +
                        $"{status.transmissionEfficiency:F1},{status.inflammationLevel:F1}," +
                        $"{status.nerveSignalStrength:F1},{status.bloodCirculation:F1}," +
                        $"{testSoundAmplitude:F3},{testSoundFrequency:F0}," +
                        $"{hasPerforation},{perforationSeverity:F3}," +
                        $"{otitisMaxSeverity:F3},{nerveDamageLevel:F3},{bloodFlow:F3}," +
                        $"\"{status.overallHealth}\",{status.activeWarnings.Count}";
        
        csvData.AppendLine(csvLine);
        
        // 메모리에 데이터 추가 (통계용)
        recordedSoundLevels.Add(status.currentSoundLevel);
        recordedTransmissionEfficiency.Add(status.transmissionEfficiency);
        recordedTimestamps.Add(timestamp);
        
        // 주기적으로 파일에 저장 (10초마다)
        if (recordedSoundLevels.Count % 10 == 0)
        {
            SaveCsvToFile();
        }
    }
    
    /// <summary>
    /// 📋 CSV 헤더 작성
    /// </summary>
    void WriteCSVHeader()
    {
        string header = "Timestamp,GameTime," +
                       "SoundLevel_dB,InputAmplitude,InputFrequency_Hz," +
                       "TransmissionEfficiency_%,InflammationLevel_%," +
                       "NerveSignalStrength_%,BloodCirculation_%," +
                       "TestAmplitude,TestFrequency_Hz," +
                       "HasPerforation,PerforationSeverity," +
                       "OtitisSeverity,NerveDamageLevel,BloodFlow," +
                       "OverallHealth,WarningCount";
        
        csvData.AppendLine(header);
    }
    
    /// <summary>
    /// 💾 CSV 데이터를 파일에 저장
    /// </summary>
    void SaveCsvToFile()
    {
        if (csvData == null || csvData.Length == 0) return;
        
        try
        {
            // 디렉토리가 없으면 생성
            string directory = System.IO.Path.GetDirectoryName(currentCsvPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            // 파일에 저장
            System.IO.File.WriteAllText(currentCsvPath, csvData.ToString());
            
            LogDebug($"💾 CSV 데이터 저장 완료: {recordedSoundLevels.Count}개 레코드");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ CSV 저장 오류: {e.Message}");
        }
    }
    
    /// <summary>
    /// 📊 기본 통계 계산 및 반환
    /// </summary>
    public string GetRecordingStatistics()
    {
        if (recordedSoundLevels.Count == 0) return "데이터 없음";
        
        float avgSoundLevel = recordedSoundLevels.Average();
        float maxSoundLevel = recordedSoundLevels.Max();
        float minSoundLevel = recordedSoundLevels.Min();
        
        float avgTransmission = recordedTransmissionEfficiency.Average();
        float minTransmission = recordedTransmissionEfficiency.Min();
        
        return $"📊 기록 통계:\n" +
               $"• 총 레코드: {recordedSoundLevels.Count}개\n" +
               $"• 평균 소리 레벨: {avgSoundLevel:F1} dB\n" +
               $"• 최대 소리 레벨: {maxSoundLevel:F1} dB\n" +
               $"• 최소 소리 레벨: {minSoundLevel:F1} dB\n" +
               $"• 평균 전달 효율: {avgTransmission:F1}%\n" +
               $"• 최저 전달 효율: {minTransmission:F1}%\n" +
               $"• 파일 경로: {currentCsvPath}";
    }
    
    /// <summary>
    /// 🔄 CSV 기록 시작/중지
    /// </summary>
    public void ToggleDataRecording()
    {
        enableDataRecording = !enableDataRecording;
        
        if (enableDataRecording)
        {
            InitializeCsvRecording();
            LogDebug("📊 CSV 데이터 기록 시작");
        }
        else
        {
            if (csvData != null && csvData.Length > 0)
            {
                SaveCsvToFile();
            }
            LogDebug("📊 CSV 데이터 기록 중지 및 저장 완료");
        }
    }
    
    /// <summary>
    /// 💾 현재까지의 데이터 즉시 저장
    /// </summary>
    public void SaveCsvNow()
    {
        if (enableDataRecording)
        {
            SaveCsvToFile();
            LogDebug("📊 CSV 데이터 즉시 저장 완료");
        }
    }
    
    /// <summary>
    /// 🗑️ 기록된 데이터 초기화
    /// </summary>
    public void ClearRecordedData()
    {
        if (recordedSoundLevels != null) recordedSoundLevels.Clear();
        if (recordedTransmissionEfficiency != null) recordedTransmissionEfficiency.Clear();
        if (recordedTimestamps != null) recordedTimestamps.Clear();
        if (csvData != null) csvData.Clear();
        
        csvHeaderWritten = false;
        
        LogDebug("🗑️ 기록된 CSV 데이터 초기화 완료");
    }
    
    /// <summary>
    /// 📁 CSV 파일이 저장된 폴더 열기 (Windows만 지원)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR_WIN")]
    public void OpenCsvFolder()
    {
        if (!string.IsNullOrEmpty(currentCsvPath))
        {
            string folder = System.IO.Path.GetDirectoryName(currentCsvPath);
            System.Diagnostics.Process.Start("explorer.exe", folder);
        }
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        // 앱이 일시정지될 때 데이터 저장
        if (!pauseStatus && enableDataRecording)
        {
            SaveCsvToFile();
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        // 포커스를 잃을 때 데이터 저장
        if (!hasFocus && enableDataRecording)
        {
            SaveCsvToFile();
        }
    }
    
    void OnDestroy()
    {
        // 오브젝트가 파괴될 때 마지막 저장
        if (enableDataRecording && csvData != null && csvData.Length > 0)
        {
            SaveCsvToFile();
        }
    }
}