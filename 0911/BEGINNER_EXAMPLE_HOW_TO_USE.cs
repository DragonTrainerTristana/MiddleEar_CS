using UnityEngine;
using UnityEngine.UI;

/*
 * =====================================================================
 * 📚 BEGINNER'S COMPLETE EXAMPLE - 초보자를 위한 완전한 사용 예시
 * =====================================================================
 * 
 * 🎯 이 스크립트의 목적:
 * - InnerEarReceiver를 어떻게 사용하는지 실제 예시로 보여줌
 * - 고등학생도 바로 이해하고 따라할 수 있는 수준
 * - 복사-붙여넣기로 바로 사용 가능한 완전한 코드
 * - 마이크 입력부터 UI 표시까지 모든 것 포함
 * 
 * 🚀 사용 방법:
 * 1. 새로운 GameObject 생성
 * 2. 이 스크립트를 컴포넌트로 추가  
 * 3. InnerEarReceiver도 같은 GameObject에 추가
 * 4. UI 요소들을 Inspector에서 연결
 * 5. Play 버튼 눌러서 실행!
 * 
 * 💡 배울 수 있는 것들:
 * - InnerEarReceiver API 모든 메서드 사용법
 * - 마이크 입력 처리 방법
 * - UI 연동 방법
 * - 실시간 데이터 시각화
 * - 청력 보호 시스템 구현
 */

public class BeginnerExampleHowToUse : MonoBehaviour
{
    /*
     * ====================================================================
     * 🎤 AUDIO INPUT SETTINGS (오디오 입력 설정)
     * ====================================================================
     */
    [Header("🎤 오디오 입력 (Audio Input)")]
    [Tooltip("마이크 사용할지 여부 - true면 실제 마이크, false면 가상 소리")]
    public bool useMicrophone = true;
    
    [Tooltip("마이크 감도 (0.1 = 둔감, 10 = 매우 민감)")]
    [Range(0.1f, 10f)]
    public float microphoneSensitivity = 1f;
    
    [Tooltip("가상 소리 주파수 (Hz) - 마이크 안 쓸 때 테스트용")]
    [Range(20f, 20000f)]  
    public float testFrequency = 440f; // 라 음
    
    [Tooltip("가상 소리 크기 (0~1) - 마이크 안 쓸 때 테스트용")]
    [Range(0f, 2f)]
    public float testAmplitude = 0.5f;

    /*
     * ====================================================================
     * 🖥️ UI ELEMENTS (UI 요소들) - Inspector에서 연결하세요!
     * ====================================================================
     */
    [Header("🖥️ UI 요소들 (UI Elements) - 드래그해서 연결하세요!")]
    [Tooltip("현재 dB 값을 표시할 텍스트")]
    public Text currentLevelText;
    
    [Tooltip("평균 dB 값을 표시할 텍스트")]
    public Text averageLevelText;
    
    [Tooltip("청력 상태를 표시할 텍스트 (Normal, Warning, Danger)")]
    public Text hearingStatusText;
    
    [Tooltip("위험도를 표시할 진행바 (0~1 값)")]
    public Slider riskProgressBar;
    
    [Tooltip("경고 메시지를 표시할 텍스트")]
    public Text warningMessageText;
    
    [Tooltip("마이크 ON/OFF 토글 버튼")]
    public Toggle microphoneToggle;
    
    [Tooltip("측정값 리셋 버튼")]
    public Button resetButton;

    /*
     * ====================================================================
     * 🔧 PRIVATE VARIABLES (내부 변수들) - 건드리지 마세요!
     * ====================================================================
     */
    private InnerEarReceiver innerEarReceiver; // InnerEarReceiver 컴포넌트 참조
    private AudioSource microphoneAudioSource; // 마이크 오디오 소스
    private string microphoneName;              // 사용 중인 마이크 이름
    private float[] microphoneBuffer;           // 마이크 데이터 버퍼
    
    // 가상 사인파 생성용 (마이크 안 쓸 때)
    private float sineWavePhase = 0f;           // 사인파 위상
    private float lastUpdateTime;               // 마지막 업데이트 시간

    /*
     * ====================================================================
     * 🚀 UNITY LIFECYCLE METHODS (유니티 메서드들)
     * ====================================================================
     */

    /// <summary>
    /// 🎬 게임 시작시 한번만 실행 - 모든 초기화 작업
    /// </summary>
    void Start()
    {
        // 1. InnerEarReceiver 컴포넌트 찾기
        InitializeInnerEarReceiver();
        
        // 2. 마이크 시스템 준비  
        InitializeMicrophoneSystem();
        
        // 3. UI 이벤트 연결
        SetupUIEvents();
        
        // 4. 초기 UI 표시
        UpdateUI();
        
        Debug.Log("🎉 BeginnerExample 초기화 완료! 이제 소리를 감지합니다!");
    }

    /// <summary>
    /// 🔄 매 프레임마다 실행 - 실시간 데이터 처리
    /// </summary>
    void Update()
    {
        // 1. 오디오 데이터 수집 (마이크 또는 가상 소리)
        ProcessAudioInput();
        
        // 2. UI 업데이트 (매 프레임은 너무 자주이므로 0.1초마다)
        if (Time.time - lastUpdateTime > 0.1f)
        {
            UpdateUI();
            CheckForWarnings();
            lastUpdateTime = Time.time;
        }
    }

    /*
     * ====================================================================
     * 🔧 INITIALIZATION METHODS (초기화 메서드들)  
     * ====================================================================
     */

    /// <summary>
    /// 🎧 InnerEarReceiver 컴포넌트 초기화
    /// </summary>
    void InitializeInnerEarReceiver()
    {
        // 같은 GameObject에서 InnerEarReceiver 찾기
        innerEarReceiver = GetComponent<InnerEarReceiver>();
        
        if (innerEarReceiver == null)
        {
            // 없으면 자동으로 추가
            innerEarReceiver = gameObject.AddComponent<InnerEarReceiver>();
            Debug.Log("✅ InnerEarReceiver 컴포넌트를 자동으로 추가했습니다!");
        }
        
        Debug.Log("🎧 InnerEarReceiver 준비 완료!");
    }

    /// <summary>
    /// 🎤 마이크 시스템 초기화
    /// </summary>
    void InitializeMicrophoneSystem()
    {
        if (!useMicrophone)
        {
            Debug.Log("🎵 가상 소리 모드로 실행합니다 (마이크 사용 안함)");
            return;
        }

        // 1. 사용 가능한 마이크 확인
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("⚠️ 마이크가 없어서 가상 소리 모드로 전환합니다");
            useMicrophone = false;
            return;
        }

        // 2. 첫 번째 마이크 사용
        microphoneName = Microphone.devices[0];
        Debug.Log($"🎤 마이크 '{microphoneName}' 사용 중");

        // 3. 오디오 소스 생성
        microphoneAudioSource = gameObject.AddComponent<AudioSource>();
        microphoneAudioSource.playOnAwake = false;
        
        // 4. 마이크 녹음 시작 (1초 길이, 44100Hz, 루프)
        microphoneAudioSource.clip = Microphone.Start(microphoneName, true, 1, 44100);
        microphoneAudioSource.loop = true;
        
        // 5. 데이터 버퍼 준비
        microphoneBuffer = new float[1024];
        
        // 6. 마이크 재생 시작 (스피커로는 안 들리게 volume=0)
        microphoneAudioSource.volume = 0f;
        microphoneAudioSource.Play();
        
        Debug.Log("🎤 마이크 녹음 시작!");
    }

    /// <summary>
    /// 🖥️ UI 이벤트 연결
    /// </summary>
    void SetupUIEvents()
    {
        // 마이크 토글 이벤트
        if (microphoneToggle != null)
        {
            microphoneToggle.isOn = useMicrophone;
            microphoneToggle.onValueChanged.AddListener(OnMicrophoneToggle);
        }

        // 리셋 버튼 이벤트
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetButtonClicked);
        }
    }

    /*
     * ====================================================================
     * 🎵 AUDIO PROCESSING METHODS (오디오 처리 메서드들)
     * ====================================================================
     */

    /// <summary>
    /// 🎵 오디오 입력 처리 - 마이크 또는 가상 소리
    /// </summary>
    void ProcessAudioInput()
    {
        float amplitude = 0f;
        float frequency = testFrequency;

        if (useMicrophone && microphoneAudioSource != null && microphoneAudioSource.isPlaying)
        {
            // 📡 실제 마이크에서 데이터 가져오기
            ProcessMicrophoneInput(out amplitude, out frequency);
        }
        else
        {
            // 🎵 가상 사인파 생성 (테스트용)
            ProcessVirtualSound(out amplitude, out frequency);
        }

        // 🧠 InnerEarReceiver에 데이터 전달 - 이것이 핵심!
        innerEarReceiver.ReceiveVibration(amplitude, frequency);
    }

    /// <summary>
    /// 🎤 실제 마이크 입력 처리
    /// </summary>
    void ProcessMicrophoneInput(out float amplitude, out float frequency)
    {
        // 1. 마이크에서 오디오 샘플 가져오기
        microphoneAudioSource.GetOutputData(microphoneBuffer, 0);
        
        // 2. RMS (Root Mean Square) 계산 - 음량 측정 방법
        float sum = 0f;
        for (int i = 0; i < microphoneBuffer.Length; i++)
        {
            sum += microphoneBuffer[i] * microphoneBuffer[i];
        }
        
        amplitude = Mathf.Sqrt(sum / microphoneBuffer.Length) * microphoneSensitivity;
        
        // 3. 주파수 분석 (간단한 방법 - 실제로는 FFT가 더 정확)
        frequency = EstimateFrequency(microphoneBuffer);
        
        // 4. 값 검증
        amplitude = Mathf.Clamp(amplitude, 0f, 10f); // 최대값 제한
        frequency = Mathf.Clamp(frequency, 20f, 20000f); // 가청 주파수 범위
    }

    /// <summary>
    /// 🎵 가상 사인파 소리 생성 (테스트용)
    /// </summary>
    void ProcessVirtualSound(out float amplitude, out float frequency)
    {
        // 시간에 따른 사인파 생성
        sineWavePhase += Time.deltaTime * testFrequency * 2 * Mathf.PI;
        
        // 진폭 계산 (사인파의 절댓값)
        amplitude = Mathf.Abs(Mathf.Sin(sineWavePhase)) * testAmplitude;
        frequency = testFrequency;
        
        // 위상이 너무 커지면 리셋 (오버플로우 방지)
        if (sineWavePhase > 2 * Mathf.PI * 1000)
        {
            sineWavePhase = 0f;
        }
    }

    /// <summary>
    /// 🔍 간단한 주파수 추정 (실제로는 FFT 사용 권장)
    /// </summary>
    float EstimateFrequency(float[] samples)
    {
        // 매우 간단한 방법: 제로크로싱 카운트
        int zeroCrossings = 0;
        for (int i = 1; i < samples.Length; i++)
        {
            if ((samples[i-1] >= 0 && samples[i] < 0) || (samples[i-1] < 0 && samples[i] >= 0))
            {
                zeroCrossings++;
            }
        }
        
        // 샘플링 레이트 44100Hz, 버퍼 크기 1024
        float frequency = (float)zeroCrossings * 44100f / (2f * samples.Length);
        
        return Mathf.Clamp(frequency, 20f, 20000f);
    }

    /*
     * ====================================================================  
     * 🖥️ UI UPDATE METHODS (UI 업데이트 메서드들)
     * ====================================================================
     */

    /// <summary>
    /// 🖥️ UI 전체 업데이트
    /// </summary>
    void UpdateUI()
    {
        if (innerEarReceiver == null) return;

        // 🔢 숫자 데이터 업데이트
        UpdateNumberDisplays();
        
        // 🎨 상태 및 색상 업데이트  
        UpdateStatusDisplay();
        
        // 📊 위험도 진행바 업데이트
        UpdateRiskProgressBar();
    }

    /// <summary>
    /// 🔢 숫자 표시 업데이트
    /// </summary>
    void UpdateNumberDisplays()
    {
        // 현재 dB 레벨 표시
        if (currentLevelText != null)
        {
            float currentDB = innerEarReceiver.GetCurrentLevel();
            currentLevelText.text = $"현재 소리: {currentDB:F1} dB";
        }

        // 평균 dB 레벨 표시
        if (averageLevelText != null)
        {
            float averageDB = innerEarReceiver.GetAverageLevel();
            averageLevelText.text = $"평균 소리: {averageDB:F1} dB";
        }
    }

    /// <summary>
    /// 🎨 상태 표시 업데이트 (색상 포함)
    /// </summary>
    void UpdateStatusDisplay()
    {
        if (hearingStatusText == null) return;

        // 상태 문자열 가져오기
        string status = innerEarReceiver.GetHearingStatus();
        
        // 한국어로 번역
        string koreanStatus = TranslateStatusToKorean(status);
        hearingStatusText.text = $"청력 상태: {koreanStatus}";
        
        // 상태에 따른 색상 변경
        switch (status)
        {
            case "Normal":
                hearingStatusText.color = Color.green;
                break;
            case "Caution":
                hearingStatusText.color = Color.yellow;
                break;
            case "Warning":
                hearingStatusText.color = new Color(1f, 0.5f, 0f, 1f);
                break;
            case "Danger":
                hearingStatusText.color = Color.red;
                break;
            default:
                hearingStatusText.color = Color.white;
                break;
        }
    }

    /// <summary>
    /// 📊 위험도 진행바 업데이트
    /// </summary>
    void UpdateRiskProgressBar()
    {
        if (riskProgressBar == null) return;

        // 위험도 값 가져오기 (0~1)
        float risk = innerEarReceiver.GetHearingDamageRisk();
        riskProgressBar.value = risk;
        
        // 진행바 색상도 위험도에 따라 변경
        Image fillImage = riskProgressBar.fillRect?.GetComponent<Image>();
        if (fillImage != null)
        {
            if (risk < 0.3f)
                fillImage.color = Color.green;      // 안전
            else if (risk < 0.7f)
                fillImage.color = Color.yellow;     // 주의
            else
                fillImage.color = Color.red;        // 위험
        }
    }

    /// <summary>
    /// ⚠️ 경고 메시지 확인 및 표시
    /// </summary>
    void CheckForWarnings()
    {
        if (warningMessageText == null) return;

        float risk = innerEarReceiver.GetHearingDamageRisk();
        float currentDB = innerEarReceiver.GetCurrentLevel();

        // 위험도에 따른 경고 메시지
        if (risk > 0.8f)
        {
            warningMessageText.text = "🚨 매우 위험! 즉시 볼륨을 낮추세요!";
            warningMessageText.color = Color.red;
        }
        else if (risk > 0.5f)
        {
            warningMessageText.text = "⚠️ 위험 수준입니다. 조심하세요!";
            warningMessageText.color = new Color(1f, 0.5f, 0f, 1f);
        }
        else if (currentDB > 85f)
        {
            warningMessageText.text = "🔊 소음이 큽니다. 주의하세요!";
            warningMessageText.color = Color.yellow;
        }
        else
        {
            warningMessageText.text = "😊 안전한 수준입니다";
            warningMessageText.color = Color.green;
        }
    }

    /*
     * ====================================================================
     * 🎛️ UI EVENT HANDLERS (UI 이벤트 핸들러들)
     * ====================================================================
     */

    /// <summary>
    /// 🎤 마이크 토글 이벤트
    /// </summary>
    void OnMicrophoneToggle(bool isOn)
    {
        useMicrophone = isOn;
        
        if (isOn)
        {
            // 마이크 켜기
            InitializeMicrophoneSystem();
            Debug.Log("🎤 마이크 활성화");
        }
        else
        {
            // 마이크 끄기
            if (microphoneAudioSource != null)
            {
                microphoneAudioSource.Stop();
                Microphone.End(microphoneName);
            }
            Debug.Log("🎵 가상 소리 모드로 전환");
        }
    }

    /// <summary>
    /// 🔄 리셋 버튼 이벤트
    /// </summary>
    void OnResetButtonClicked()
    {
        if (innerEarReceiver != null)
        {
            innerEarReceiver.ResetMeasurements();
            Debug.Log("📊 모든 측정값이 리셋되었습니다");
        }
    }

    /*
     * ====================================================================
     * 🛠️ UTILITY METHODS (유틸리티 메서드들)
     * ====================================================================
     */

    /// <summary>
    /// 🌐 영어 상태를 한국어로 번역
    /// </summary>
    string TranslateStatusToKorean(string englishStatus)
    {
        switch (englishStatus)
        {
            case "Normal": return "정상 😊";
            case "Caution": return "주의 😐";
            case "Warning": return "경고 😰";
            case "Danger": return "위험 🚨";
            default: return "알 수 없음 ❓";
        }
    }

    /// <summary>
    /// 🧹 게임 종료시 정리 작업
    /// </summary>
    void OnDestroy()
    {
        // 마이크 정리
        if (useMicrophone && !string.IsNullOrEmpty(microphoneName))
        {
            Microphone.End(microphoneName);
        }
    }

    /*
     * ====================================================================
     * 🎯 INSPECTOR BUTTONS (Inspector 버튼들) - 테스트용
     * ====================================================================
     */

    /// <summary>
    /// 🧪 Inspector에서 테스트용 - 큰 소리 시뮬레이션
    /// </summary>
    [ContextMenu("테스트: 큰 소리 (90dB)")]
    public void TestLoudSound()
    {
        if (innerEarReceiver != null)
        {
            innerEarReceiver.ReceiveVibration(2f, 1000f); // 매우 큰 소리
            Debug.Log("🔊 큰 소리 테스트!");
        }
    }

    /// <summary>
    /// 🧪 Inspector에서 테스트용 - 조용한 소리 시뮬레이션
    /// </summary>
    [ContextMenu("테스트: 조용한 소리 (40dB)")]
    public void TestQuietSound()
    {
        if (innerEarReceiver != null)
        {
            innerEarReceiver.ReceiveVibration(0.1f, 440f); // 조용한 소리
            Debug.Log("🔉 조용한 소리 테스트!");
        }
    }
}

/*
 * =====================================================================
 * 🎓 학습 정리 (LEARNING SUMMARY)
 * =====================================================================
 * 
 * 🏆 이 예시에서 배운 것들:
 * 
 * 1️⃣ InnerEarReceiver 핵심 사용법:
 *    - ReceiveVibration() 호출로 소리 데이터 전달
 *    - GetCurrentLevel(), GetAverageLevel() 등으로 결과 받기
 *    - UI와 연동하여 실시간 표시
 * 
 * 2️⃣ 마이크 처리:
 *    - Microphone.Start()로 녹음 시작
 *    - GetOutputData()로 실시간 샘플 가져오기  
 *    - RMS 계산으로 음량 측정
 * 
 * 3️⃣ UI 연동:
 *    - 실시간 업데이트 (매 0.1초)
 *    - 색상 변화로 상태 표시
 *    - 진행바로 위험도 시각화
 * 
 * 4️⃣ 안전장치:
 *    - 값 범위 제한 (Clamp)
 *    - null 체크
 *    - 예외 상황 처리
 * 
 * 🎯 다음 단계로 발전시킬 방법:
 * - FFT 사용한 정확한 주파수 분석
 * - 데이터 저장 및 분석 기능
 * - 더 정교한 UI 디자인
 * - 알림/경고 시스템 고도화
 * 
 * 🎉 축하합니다! 이제 InnerEarReceiver 전문가입니다!
 */