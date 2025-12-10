using UnityEngine;
using System;

/// <summary>
/// 음파 전파 모델
/// 고막 천공 시 주파수별 감쇄(Air-Bone Gap)를 계산
///
/// 물리 모델:
/// - 정상 고막: 음파 → 고막 진동 → 이소골 → 등골 족판 (효율적 전달)
/// - 천공 고막:
///   1) 직접 경로: 구멍으로 에너지 누출 (저주파에서 더 큼)
///   2) 진동 경로: 남은 고막의 진동 전달 (면적 감소로 효율 저하)
/// </summary>
public class SoundPropagationModel : MonoBehaviour
{
    [Header("References")]
    public PerforationSimulator perforationSimulator;
    public Transform soundSource;           // 외이도 입구 (음원)
    public Transform stapesFootplate;       // 등골 족판 (Observer)

    [Header("Test Frequency")]
    [Range(125f, 8000f)]
    public float testFrequencyHz = 1000f;

    [Header("Physical Parameters")]
    [Tooltip("고막-등골 간 전달 효율 (정상 상태)")]
    [Range(0.5f, 1f)]
    public float normalTransmissionEfficiency = 0.85f;

    [Tooltip("저주파 누출 계수 (낮을수록 저주파 손실 큼)")]
    [Range(0.1f, 1f)]
    public float lowFrequencyLeakFactor = 0.3f;

    [Header("Calculated Results (Read Only)")]
    [SerializeField] private float predictedAirBoneGap_dB;
    [SerializeField] private float transmissionLoss_dB;
    [SerializeField] private float leakageLoss_dB;
    [SerializeField] private float totalEfficiency;

    // 주파수별 Air-Bone Gap 결과 저장
    private float[] frequencyList = { 125f, 250f, 500f, 1000f, 2000f, 3000f, 4000f, 8000f };
    private float[] predictedABG = new float[8];

    // 물리 상수
    private const float SPEED_OF_SOUND = 343f;  // m/s (공기 중)
    private const float REFERENCE_PRESSURE = 20e-6f;  // 20 μPa (0 dB SPL 기준)

    void Start()
    {
        if (perforationSimulator == null)
            perforationSimulator = FindObjectOfType<PerforationSimulator>();

        CalculateAllFrequencies();
    }

    void Update()
    {
        // 실시간으로 현재 주파수의 Air-Bone Gap 계산
        predictedAirBoneGap_dB = CalculateAirBoneGap(testFrequencyHz);
    }

    /// <summary>
    /// 특정 주파수에서의 Air-Bone Gap 계산 (dB)
    /// </summary>
    public float CalculateAirBoneGap(float frequencyHz)
    {
        if (perforationSimulator == null) return 0f;

        float perforationRatio = perforationSimulator.GetPerforationRatio();

        // 천공이 없으면 Air-Bone Gap = 0
        if (perforationRatio <= 0f) return 0f;

        float remainingRatio = 1f - perforationRatio;

        // ========== 1. 누출 손실 (Leakage Loss) ==========
        // 저주파는 파장이 길어서 구멍으로 더 잘 빠져나감
        // 고주파는 파장이 짧아서 상대적으로 덜 빠져나감
        float wavelength = SPEED_OF_SOUND / frequencyHz;
        float effectiveHoleSize = Mathf.Sqrt(perforationSimulator.GetPerforationAreaMM2()) / 1000f; // m

        // Rayleigh 산란 기반 누출 모델
        // 구멍 크기 << 파장: 누출 많음 / 구멍 크기 >= 파장: 누출 적음
        float sizeToWavelengthRatio = effectiveHoleSize / wavelength;
        float leakageCoefficient;

        if (sizeToWavelengthRatio < 0.1f)
        {
            // 저주파 (구멍 << 파장): 높은 누출
            leakageCoefficient = 1f - Mathf.Pow(sizeToWavelengthRatio / 0.1f, 2) * 0.5f;
        }
        else if (sizeToWavelengthRatio < 1f)
        {
            // 중주파: 중간 누출
            leakageCoefficient = 0.5f * (1f - sizeToWavelengthRatio);
        }
        else
        {
            // 고주파 (구멍 >= 파장): 낮은 누출
            leakageCoefficient = 0.1f / sizeToWavelengthRatio;
        }

        // 천공 비율에 따른 누출 손실
        leakageLoss_dB = -10f * Mathf.Log10(1f - perforationRatio * leakageCoefficient * lowFrequencyLeakFactor);
        leakageLoss_dB = Mathf.Clamp(leakageLoss_dB, 0f, 30f);

        // ========== 2. 전달 손실 (Transmission Loss) ==========
        // 남은 고막 면적 감소로 인한 진동 전달 효율 저하
        // 면적 비율의 제곱근에 비례 (진동 진폭 ∝ √면적)
        float areaEfficiency = Mathf.Sqrt(remainingRatio);

        // 주파수에 따른 고막 진동 효율 (공명 주파수 ~1kHz 근처에서 최대)
        float resonanceFreq = 1000f;
        float Q = 2f;
        float freqRatio = frequencyHz / resonanceFreq;
        float resonanceEfficiency = 1f / Mathf.Sqrt(Mathf.Pow(1 - freqRatio * freqRatio, 2) + Mathf.Pow(freqRatio / Q, 2));
        resonanceEfficiency = Mathf.Clamp(resonanceEfficiency, 0.3f, 1.5f);

        // 총 전달 효율
        totalEfficiency = areaEfficiency * normalTransmissionEfficiency * resonanceEfficiency;
        totalEfficiency = Mathf.Clamp01(totalEfficiency);

        // 전달 손실 (dB)
        transmissionLoss_dB = -20f * Mathf.Log10(Mathf.Max(totalEfficiency, 0.001f));
        transmissionLoss_dB = Mathf.Clamp(transmissionLoss_dB, 0f, 60f);

        // ========== 3. 총 Air-Bone Gap ==========
        // 두 손실의 조합 (에너지 기준 합산)
        float totalLoss = leakageLoss_dB + transmissionLoss_dB * 0.7f; // 가중 합산

        // 천공 등급에 따른 경험적 보정 (임상 데이터 기반)
        int grade = Mathf.RoundToInt(perforationRatio * 4f);
        float empiricalCorrection = GetEmpiricalCorrection(grade, frequencyHz);

        float finalABG = totalLoss * 0.6f + empiricalCorrection * 0.4f;

        return Mathf.Clamp(finalABG, 0f, 60f);
    }

    /// <summary>
    /// 임상 데이터 기반 경험적 Air-Bone Gap 값
    /// (논문 데이터를 바탕으로 보정)
    /// </summary>
    float GetEmpiricalCorrection(int grade, float frequencyHz)
    {
        // Grade별, 주파수별 평균 Air-Bone Gap (dB)
        // 실제 환자 데이터로 calibration 필요
        float[,] empiricalABG = new float[5, 8]
        {
            // 125Hz, 250Hz, 500Hz, 1kHz, 2kHz, 3kHz, 4kHz, 8kHz
            { 0, 0, 0, 0, 0, 0, 0, 0 },           // Grade 0: 정상
            { 15, 12, 10, 8, 6, 5, 5, 4 },        // Grade 1: <25%
            { 25, 22, 18, 15, 12, 10, 9, 7 },     // Grade 2: 25-50%
            { 35, 32, 28, 24, 20, 17, 15, 12 },   // Grade 3: 50-75%
            { 45, 42, 38, 35, 30, 27, 25, 20 }    // Grade 4: >75%
        };

        // 주파수 인덱스 찾기
        int freqIndex = 0;
        float minDiff = float.MaxValue;
        for (int i = 0; i < frequencyList.Length; i++)
        {
            float diff = Mathf.Abs(frequencyHz - frequencyList[i]);
            if (diff < minDiff)
            {
                minDiff = diff;
                freqIndex = i;
            }
        }

        return empiricalABG[Mathf.Clamp(grade, 0, 4), freqIndex];
    }

    /// <summary>
    /// 모든 표준 주파수에서 Air-Bone Gap 계산
    /// </summary>
    [ContextMenu("Calculate All Frequencies")]
    public void CalculateAllFrequencies()
    {
        string report = "\n========== Air-Bone Gap Prediction ==========\n";
        report += $"천공 등급: {perforationSimulator?.GetPerforationRatio():P1}\n\n";
        report += "Freq(Hz)\tPredicted ABG(dB)\n";
        report += "--------\t-----------------\n";

        for (int i = 0; i < frequencyList.Length; i++)
        {
            predictedABG[i] = CalculateAirBoneGap(frequencyList[i]);
            report += $"{frequencyList[i]:F0}\t\t{predictedABG[i]:F1}\n";
        }

        // 평균 계산
        float avgLow = (predictedABG[0] + predictedABG[1] + predictedABG[2]) / 3f;
        float avgMid = (predictedABG[3] + predictedABG[4]) / 2f;
        float avgHigh = (predictedABG[5] + predictedABG[6] + predictedABG[7]) / 3f;
        float avgTotal = 0f;
        foreach (var abg in predictedABG) avgTotal += abg;
        avgTotal /= predictedABG.Length;

        report += $"\n저주파 평균 (125-500Hz): {avgLow:F1} dB";
        report += $"\n중주파 평균 (1k-2kHz): {avgMid:F1} dB";
        report += $"\n고주파 평균 (3k-8kHz): {avgHigh:F1} dB";
        report += $"\n전체 평균: {avgTotal:F1} dB";
        report += "\n=============================================\n";

        Debug.Log(report);
    }

    // ===================== Public API =====================

    /// <summary>
    /// 특정 주파수의 Air-Bone Gap 반환
    /// </summary>
    public float GetAirBoneGap(float frequencyHz)
    {
        return CalculateAirBoneGap(frequencyHz);
    }

    /// <summary>
    /// 모든 주파수의 Air-Bone Gap 배열 반환
    /// </summary>
    public float[] GetAllAirBoneGaps()
    {
        for (int i = 0; i < frequencyList.Length; i++)
        {
            predictedABG[i] = CalculateAirBoneGap(frequencyList[i]);
        }
        return (float[])predictedABG.Clone();
    }

    /// <summary>
    /// 주파수 목록 반환
    /// </summary>
    public float[] GetFrequencyList()
    {
        return (float[])frequencyList.Clone();
    }

    /// <summary>
    /// 저주파 평균 ABG (125, 250, 500 Hz)
    /// </summary>
    public float GetLowFrequencyAverage()
    {
        return (CalculateAirBoneGap(125) + CalculateAirBoneGap(250) + CalculateAirBoneGap(500)) / 3f;
    }

    /// <summary>
    /// 중주파 평균 ABG (1k, 2k Hz)
    /// </summary>
    public float GetMidFrequencyAverage()
    {
        return (CalculateAirBoneGap(1000) + CalculateAirBoneGap(2000)) / 2f;
    }

    /// <summary>
    /// 고주파 평균 ABG (3k, 4k, 8k Hz)
    /// </summary>
    public float GetHighFrequencyAverage()
    {
        return (CalculateAirBoneGap(3000) + CalculateAirBoneGap(4000) + CalculateAirBoneGap(8000)) / 3f;
    }

    void OnValidate()
    {
        if (Application.isPlaying && perforationSimulator != null)
        {
            predictedAirBoneGap_dB = CalculateAirBoneGap(testFrequencyHz);
        }
    }
}
