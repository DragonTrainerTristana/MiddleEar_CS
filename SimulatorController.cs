using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 고막 천공 시뮬레이터 메인 컨트롤러
/// 모든 컴포넌트를 통합하고 UI와 연결
/// </summary>
public class SimulatorController : MonoBehaviour
{
    [Header("Core Components")]
    public PerforationSimulator perforationSimulator;
    public SoundPropagationModel soundPropagationModel;

    [Header("3D Objects")]
    public Transform tympanicMembrane;  // 고막
    public Transform malleus;            // 추골
    public Transform incus;              // 침골
    public Transform stapes;             // 등골
    public Transform stapesFootplate;    // 등골 족판 (Observer)

    [Header("Audio Visualization")]
    public AudioSource audioSource;
    public LineRenderer soundPathLine;
    public ParticleSystem soundWaveParticles;

    [Header("Current Settings")]
    [Range(0, 4)]
    public int currentPerforationGrade = 0;

    [Range(125f, 8000f)]
    public float currentFrequency = 1000f;

    [Header("Results Display")]
    [SerializeField] private float currentABG_dB;
    [SerializeField] private string severityLevel;

    [Header("Patient Data (Optional)")]
    public PatientData currentPatient;

    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool autoUpdateVisualization = true;

    // 주파수별 결과 캐시
    private float[] cachedABG = new float[8];
    private float[] frequencies = { 125f, 250f, 500f, 1000f, 2000f, 3000f, 4000f, 8000f };

    // Tone generation
    private float tonePhase = 0f;
    private bool isPlayingTone = false;

    void Start()
    {
        InitializeComponents();
        UpdateSimulation();
    }

    void InitializeComponents()
    {
        // 자동으로 컴포넌트 찾기
        if (perforationSimulator == null)
            perforationSimulator = FindObjectOfType<PerforationSimulator>();

        if (soundPropagationModel == null)
            soundPropagationModel = FindObjectOfType<SoundPropagationModel>();

        // AudioSource 설정
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
        }

        Debug.Log("[SimulatorController] 초기화 완료");
    }

    void Update()
    {
        if (autoUpdateVisualization)
        {
            UpdateSimulation();
            UpdateVisualization();
        }
    }

    /// <summary>
    /// 시뮬레이션 업데이트
    /// </summary>
    public void UpdateSimulation()
    {
        if (perforationSimulator == null || soundPropagationModel == null) return;

        // 천공 등급 적용
        if (perforationSimulator.perforationGrade != currentPerforationGrade)
        {
            perforationSimulator.SetPerforationGrade(currentPerforationGrade);
        }

        // 현재 주파수의 Air-Bone Gap 계산
        currentABG_dB = soundPropagationModel.GetAirBoneGap(currentFrequency);

        // 청력 손실 등급 판정
        severityLevel = GetSeverityLevel(currentABG_dB);

        // 모든 주파수 계산
        cachedABG = soundPropagationModel.GetAllAirBoneGaps();
    }

    /// <summary>
    /// 시각화 업데이트
    /// </summary>
    void UpdateVisualization()
    {
        // 소리 경로 시각화
        if (soundPathLine != null && tympanicMembrane != null && stapesFootplate != null)
        {
            soundPathLine.positionCount = 5;
            Vector3[] positions = new Vector3[5];

            positions[0] = tympanicMembrane.position - tympanicMembrane.forward * 0.02f; // 외이도
            positions[1] = tympanicMembrane.position;
            positions[2] = malleus != null ? malleus.position : tympanicMembrane.position;
            positions[3] = incus != null ? incus.position : positions[2];
            positions[4] = stapesFootplate != null ? stapesFootplate.position : stapes.position;

            soundPathLine.SetPositions(positions);

            // 색상: Air-Bone Gap에 따라 변경
            float normalizedABG = Mathf.Clamp01(currentABG_dB / 60f);
            Color pathColor = Color.Lerp(Color.green, Color.red, normalizedABG);
            soundPathLine.startColor = pathColor;
            soundPathLine.endColor = pathColor;
        }

        // 파티클 시스템
        if (soundWaveParticles != null && isPlayingTone)
        {
            var emission = soundWaveParticles.emission;
            float rate = Mathf.Lerp(50f, 10f, currentABG_dB / 60f);
            emission.rateOverTime = rate;
        }
    }

    /// <summary>
    /// Air-Bone Gap에 따른 청력 손실 등급
    /// </summary>
    string GetSeverityLevel(float abg)
    {
        if (abg < 15) return "정상 (Normal)";
        if (abg < 25) return "경도 (Mild)";
        if (abg < 40) return "중등도 (Moderate)";
        if (abg < 55) return "중등고도 (Moderately Severe)";
        if (abg < 70) return "고도 (Severe)";
        return "심도 (Profound)";
    }

    // ===================== Public API =====================

    /// <summary>
    /// 천공 등급 설정 (0~4)
    /// </summary>
    public void SetPerforationGrade(int grade)
    {
        currentPerforationGrade = Mathf.Clamp(grade, 0, 4);
        UpdateSimulation();
    }

    /// <summary>
    /// 테스트 주파수 설정
    /// </summary>
    public void SetFrequency(float freq)
    {
        currentFrequency = Mathf.Clamp(freq, 125f, 8000f);
        UpdateSimulation();
    }

    /// <summary>
    /// 순음 재생 시작
    /// </summary>
    public void PlayTone()
    {
        if (audioSource == null) return;

        AudioClip toneClip = AudioClip.Create("TestTone", 44100, 1, 44100, true, OnAudioRead);
        audioSource.clip = toneClip;
        audioSource.Play();
        isPlayingTone = true;

        Debug.Log($"[SimulatorController] {currentFrequency}Hz 순음 재생 시작");
    }

    /// <summary>
    /// 순음 재생 중지
    /// </summary>
    public void StopTone()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        isPlayingTone = false;
    }

    void OnAudioRead(float[] data)
    {
        float sampleRate = 44100f;
        float amplitude = 0.5f;

        // Air-Bone Gap에 따른 볼륨 감소
        float attenuatedAmplitude = amplitude * Mathf.Pow(10f, -currentABG_dB / 20f);

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = Mathf.Sin(2f * Mathf.PI * currentFrequency * tonePhase / sampleRate) * attenuatedAmplitude;
            tonePhase++;
            if (tonePhase >= sampleRate) tonePhase = 0f;
        }
    }

    /// <summary>
    /// 전체 주파수 결과 출력
    /// </summary>
    [ContextMenu("Print Full Report")]
    public void PrintFullReport()
    {
        string report = "\n";
        report += "╔══════════════════════════════════════════════════════════════╗\n";
        report += "║           고막 천공 시뮬레이션 결과 보고서                    ║\n";
        report += "╠══════════════════════════════════════════════════════════════╣\n";
        report += $"║ 천공 등급: Grade {currentPerforationGrade}";
        report += $" ({perforationSimulator?.GetPerforationRatio():P0})".PadRight(40) + "║\n";
        report += $"║ 천공 면적: {perforationSimulator?.GetPerforationAreaMM2():F1} mm²".PadRight(52) + "║\n";
        report += "╠══════════════════════════════════════════════════════════════╣\n";
        report += "║ 주파수(Hz)    예측 Air-Bone Gap (dB)    청력손실 등급        ║\n";
        report += "╠══════════════════════════════════════════════════════════════╣\n";

        for (int i = 0; i < frequencies.Length; i++)
        {
            float abg = soundPropagationModel.GetAirBoneGap(frequencies[i]);
            string severity = GetSeverityLevel(abg);
            string line = $"║ {frequencies[i],8:F0}        {abg,8:F1}                 {severity,-18}║\n";
            report += line;
        }

        report += "╠══════════════════════════════════════════════════════════════╣\n";

        float avgLow = soundPropagationModel.GetLowFrequencyAverage();
        float avgMid = soundPropagationModel.GetMidFrequencyAverage();
        float avgHigh = soundPropagationModel.GetHighFrequencyAverage();

        report += $"║ 저주파 평균 (125-500Hz):  {avgLow:F1} dB".PadRight(53) + "║\n";
        report += $"║ 중주파 평균 (1k-2kHz):    {avgMid:F1} dB".PadRight(53) + "║\n";
        report += $"║ 고주파 평균 (3k-8kHz):    {avgHigh:F1} dB".PadRight(53) + "║\n";
        report += "╚══════════════════════════════════════════════════════════════╝\n";

        Debug.Log(report);
    }

    /// <summary>
    /// 결과를 CSV 형식으로 반환 (논문용)
    /// </summary>
    public string GetResultsAsCSV()
    {
        string csv = "Frequency_Hz,Predicted_ABG_dB,Severity\n";
        for (int i = 0; i < frequencies.Length; i++)
        {
            float abg = soundPropagationModel.GetAirBoneGap(frequencies[i]);
            csv += $"{frequencies[i]},{abg:F2},{GetSeverityLevel(abg)}\n";
        }
        return csv;
    }

    // ===================== Inspector GUI =====================

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 350, 400));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== 고막 천공 시뮬레이터 ===");
        GUILayout.Space(10);

        // 천공 등급 조절
        GUILayout.Label($"천공 등급: {currentPerforationGrade} ({GetGradeDescription(currentPerforationGrade)})");
        int newGrade = (int)GUILayout.HorizontalSlider(currentPerforationGrade, 0, 4);
        if (newGrade != currentPerforationGrade)
        {
            SetPerforationGrade(newGrade);
        }

        GUILayout.Space(10);

        // 주파수 조절
        GUILayout.Label($"테스트 주파수: {currentFrequency:F0} Hz");
        float newFreq = GUILayout.HorizontalSlider(currentFrequency, 125f, 8000f);
        if (Mathf.Abs(newFreq - currentFrequency) > 10f)
        {
            SetFrequency(newFreq);
        }

        GUILayout.Space(10);

        // 결과 표시
        GUILayout.Label($"예측 Air-Bone Gap: {currentABG_dB:F1} dB");
        GUILayout.Label($"청력 손실 등급: {severityLevel}");

        GUILayout.Space(10);

        // 주파수별 결과
        GUILayout.Label("--- 주파수별 Air-Bone Gap ---");
        for (int i = 0; i < frequencies.Length; i++)
        {
            float abg = cachedABG[i];
            GUILayout.Label($"{frequencies[i]:F0} Hz: {abg:F1} dB");
        }

        GUILayout.Space(10);

        // 버튼
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(isPlayingTone ? "Stop" : "Play Tone"))
        {
            if (isPlayingTone) StopTone();
            else PlayTone();
        }
        if (GUILayout.Button("Report"))
        {
            PrintFullReport();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    string GetGradeDescription(int grade)
    {
        switch (grade)
        {
            case 0: return "정상";
            case 1: return "<25%";
            case 2: return "25-50%";
            case 3: return "50-75%";
            case 4: return ">75%";
            default: return "Unknown";
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateSimulation();
        }
    }
}

/// <summary>
/// 환자 데이터 구조체 (CSV에서 로드용)
/// </summary>
[System.Serializable]
public class PatientData
{
    public string patientID;
    public int age;
    public int sex;  // 1: 남, 2: 여
    public string diagnosis;
    public int perforationGrade;  // 0~4

    [Header("Pre-Op Air Conduction (dB)")]
    public float preOp_125Hz;
    public float preOp_250Hz;
    public float preOp_500Hz;
    public float preOp_1000Hz;
    public float preOp_2000Hz;
    public float preOp_3000Hz;
    public float preOp_4000Hz;
    public float preOp_8000Hz;

    [Header("Pre-Op Bone Conduction (dB)")]
    public float preOp_BC_500Hz;
    public float preOp_BC_1000Hz;
    public float preOp_BC_2000Hz;
    public float preOp_BC_4000Hz;

    /// <summary>
    /// 특정 주파수의 Air-Bone Gap 계산
    /// </summary>
    public float GetAirBoneGap(float freqHz)
    {
        // 간단히 500Hz, 1kHz, 2kHz, 4kHz만 계산
        if (freqHz <= 500f)
            return preOp_500Hz - preOp_BC_500Hz;
        else if (freqHz <= 1000f)
            return preOp_1000Hz - preOp_BC_1000Hz;
        else if (freqHz <= 2000f)
            return preOp_2000Hz - preOp_BC_2000Hz;
        else
            return preOp_4000Hz - preOp_BC_4000Hz;
    }
}
