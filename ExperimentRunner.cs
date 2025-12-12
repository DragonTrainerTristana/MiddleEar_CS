using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// 자동 실험 실행기
/// 모든 천공 등급, 위치 조합에 대해 ABG 계산 후 CSV 저장
/// </summary>
public class ExperimentRunner : MonoBehaviour
{
    [Header("=== EarSimulator 참조 ===")]
    public EarSimulator earSimulator;

    [Header("=== 실험 설정 ===")]
    [Tooltip("위치 그리드 해상도 (3이면 3x3=9개 위치)")]
    [Range(2, 10)]
    public int positionGridSize = 5;

    [Tooltip("중이강 볼륨 테스트 (여러 값)")]
    public float[] middleEarVolumes = { 0.5f, 0.8f, 1.2f };

    [Header("=== 실제 크기 설정 (mm) ===")]
    [Tooltip("실제 고막 직경 (mm) - 평균 9mm")]
    public float realTMDiameterMM = 9f;

    [Header("=== 시각화 설정 ===")]
    [Tooltip("시각화 모드 (천공 변화를 눈으로 확인)")]
    public bool visualizationMode = true;

    [Tooltip("각 실험 사이 대기 시간 (초)")]
    [Range(0.1f, 3f)]
    public float delayBetweenExperiments = 0.5f;

    [Tooltip("Grade 변경 시 추가 대기 시간 (초)")]
    [Range(0.5f, 5f)]
    public float delayOnGradeChange = 1.5f;

    [Header("=== 결과 ===")]
    public int totalExperiments;
    public int completedExperiments;
    public float progressPercent;
    public string currentStatus = "";
    public string lastExportPath;

    // 실행 중 여부
    public bool isRunning = false;
    private Coroutine runningCoroutine;

    // 표준 주파수
    private readonly float[] frequencies = { 125f, 250f, 500f, 1000f, 2000f, 3000f, 4000f, 8000f };

    // 천공 등급
    private readonly int[] grades = { 0, 1, 2, 3, 4 };

    // Grade별 천공 면적 비율 (PerforationSimulator와 동일)
    private readonly float[] gradeToRatio = { 0f, 0.05f, 0.25f, 0.50f, 0.75f };

    // 결과 저장
    private List<ExperimentResult> results = new List<ExperimentResult>();

    /// <summary>
    /// 모든 실험 실행 (Inspector 버튼)
    /// </summary>
    [ContextMenu("Run All Experiments")]
    public void RunAllExperiments()
    {
        if (earSimulator == null)
        {
            Debug.LogError("[ExperimentRunner] EarSimulator가 연결되지 않았습니다!");
            return;
        }

        if (isRunning)
        {
            Debug.LogWarning("[ExperimentRunner] 이미 실행 중입니다!");
            return;
        }

        if (visualizationMode && Application.isPlaying)
        {
            // 시각화 모드: Coroutine으로 천천히 실행
            runningCoroutine = StartCoroutine(RunExperimentsCoroutine());
        }
        else
        {
            // 즉시 실행 모드
            RunExperimentsImmediate();
        }
    }

    /// <summary>
    /// 실험 중지
    /// </summary>
    [ContextMenu("Stop Experiments")]
    public void StopExperiments()
    {
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }
        isRunning = false;
        currentStatus = "중지됨";
        Debug.Log("[ExperimentRunner] 실험 중지됨");
    }

    /// <summary>
    /// 시각화 모드 - Coroutine으로 천천히 실행
    /// </summary>
    IEnumerator RunExperimentsCoroutine()
    {
        isRunning = true;
        results.Clear();
        completedExperiments = 0;

        int positionCount = positionGridSize * positionGridSize;
        // Grade 0: 1번, Grade 1-4: 각각 positionCount번
        totalExperiments = (1 + 4 * positionCount) * middleEarVolumes.Length;

        Debug.Log($"[ExperimentRunner] 시각화 모드 시작 - 총 {totalExperiments}개 케이스 (Grade0: {middleEarVolumes.Length}개, Grade1-4: {4 * positionCount * middleEarVolumes.Length}개)");
        currentStatus = "실험 시작...";

        int lastGrade = -1;

        foreach (float volume in middleEarVolumes)
        {
            earSimulator.middleEarVolumeCm3 = volume;

            foreach (int grade in grades)
            {
                // Grade 변경 시 추가 대기
                if (grade != lastGrade)
                {
                    earSimulator.SetPerforationGrade(grade);
                    currentStatus = $"Grade {grade} 시작 (Volume: {volume}cm³)";
                    Debug.Log($"[ExperimentRunner] === Grade {grade} 시작 ===");
                    lastGrade = grade;
                    yield return new WaitForSeconds(delayOnGradeChange);
                }

                // Grade 0은 천공 없음 - 위치 무관하게 1번만 테스트
                if (grade == 0)
                {
                    earSimulator.SetPerforationPosition(0.5f, 0.5f);
                    currentStatus = $"Grade 0 (천공 없음)";

                    yield return new WaitForSeconds(delayBetweenExperiments);

                    ExperimentResult result = CreateExperimentResult(grade, 0.5f, 0.5f, volume);
                    results.Add(result);
                    completedExperiments++;
                    progressPercent = (float)completedExperiments / totalExperiments * 100f;
                    continue;
                }

                // Grade 1-4: 위치별 테스트
                for (int xi = 0; xi < positionGridSize; xi++)
                {
                    for (int yi = 0; yi < positionGridSize; yi++)
                    {
                        float posX = (float)xi / (positionGridSize - 1);
                        float posY = (float)yi / (positionGridSize - 1);

                        earSimulator.SetPerforationPosition(posX, posY);
                        currentStatus = $"Grade {grade}, 위치 ({posX:F2}, {posY:F2})";

                        // 시각화를 위해 대기
                        yield return new WaitForSeconds(delayBetweenExperiments);

                        // 결과 계산
                        ExperimentResult result = CreateExperimentResult(grade, posX, posY, volume);
                        results.Add(result);
                        completedExperiments++;
                        progressPercent = (float)completedExperiments / totalExperiments * 100f;
                    }
                }
            }
        }

        Debug.Log($"[ExperimentRunner] 실험 완료 - {results.Count}개 결과 생성");
        currentStatus = "완료! CSV 저장 중...";

        ExportToCSV();
        ExportSummaryByGrade();

        currentStatus = "완료!";
        isRunning = false;
    }

    /// <summary>
    /// 즉시 실행 모드 (시각화 없음)
    /// </summary>
    void RunExperimentsImmediate()
    {
        results.Clear();
        completedExperiments = 0;

        int positionCount = positionGridSize * positionGridSize;
        // Grade 0: 1번, Grade 1-4: 각각 positionCount번
        totalExperiments = (1 + 4 * positionCount) * middleEarVolumes.Length;

        Debug.Log($"[ExperimentRunner] 즉시 실행 시작 - 총 {totalExperiments}개 케이스");

        foreach (float volume in middleEarVolumes)
        {
            earSimulator.middleEarVolumeCm3 = volume;

            foreach (int grade in grades)
            {
                earSimulator.SetPerforationGrade(grade);

                // Grade 0은 천공 없음 - 1번만 테스트
                if (grade == 0)
                {
                    earSimulator.SetPerforationPosition(0.5f, 0.5f);
                    ExperimentResult result = CreateExperimentResult(grade, 0.5f, 0.5f, volume);
                    results.Add(result);
                    completedExperiments++;
                    continue;
                }

                // Grade 1-4: 위치별 테스트
                for (int xi = 0; xi < positionGridSize; xi++)
                {
                    for (int yi = 0; yi < positionGridSize; yi++)
                    {
                        float posX = (float)xi / (positionGridSize - 1);
                        float posY = (float)yi / (positionGridSize - 1);

                        earSimulator.SetPerforationPosition(posX, posY);

                        ExperimentResult result = CreateExperimentResult(grade, posX, posY, volume);
                        results.Add(result);
                        completedExperiments++;
                    }
                }
            }
        }

        Debug.Log($"[ExperimentRunner] 실험 완료 - {results.Count}개 결과 생성");

        ExportToCSV();
        ExportSummaryByGrade();
    }

    /// <summary>
    /// 실험 결과 생성
    /// </summary>
    ExperimentResult CreateExperimentResult(int grade, float posX, float posY, float volume)
    {
        // 실제 크기 계산 (mm)
        float tmRadiusMM = realTMDiameterMM / 2f;
        float tmAreaMM2 = Mathf.PI * tmRadiusMM * tmRadiusMM;

        float perforationRatio = gradeToRatio[Mathf.Clamp(grade, 0, 4)];
        float perforationAreaMM2 = tmAreaMM2 * perforationRatio;
        float perforationRadiusMM = Mathf.Sqrt(perforationAreaMM2 / Mathf.PI);
        float perforationDiameterMM = perforationRadiusMM * 2f;

        ExperimentResult result = new ExperimentResult
        {
            grade = grade,
            posX = posX,
            posY = posY,
            middleEarVolume = volume,
            quadrant = GetQuadrant(posX, posY),
            abgByFrequency = new float[frequencies.Length],

            // 실제 크기 (mm)
            tmDiameterMM = realTMDiameterMM,
            perforationDiameterMM = perforationDiameterMM,
            perforationAreaMM2 = perforationAreaMM2
        };

        for (int f = 0; f < frequencies.Length; f++)
        {
            result.abgByFrequency[f] = earSimulator.CalculateAirBoneGap(frequencies[f]);
        }

        result.avgLow = (result.abgByFrequency[0] + result.abgByFrequency[1] + result.abgByFrequency[2]) / 3f;
        result.avgMid = (result.abgByFrequency[3] + result.abgByFrequency[4]) / 2f;
        result.avgHigh = (result.abgByFrequency[5] + result.abgByFrequency[6] + result.abgByFrequency[7]) / 3f;
        result.avgTotal = 0f;
        foreach (var abg in result.abgByFrequency) result.avgTotal += abg;
        result.avgTotal /= frequencies.Length;

        return result;
    }

    /// <summary>
    /// 환자 데이터 비교용 실험 (Grade별 평균)
    /// </summary>
    [ContextMenu("Run Patient Comparison")]
    public void RunPatientComparison()
    {
        if (earSimulator == null)
        {
            Debug.LogError("[ExperimentRunner] EarSimulator가 연결되지 않았습니다!");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("\n================ Patient Data Comparison ================");
        sb.AppendLine("시뮬레이션 결과 vs 실제 환자 데이터 (ICW 환자)");
        sb.AppendLine("=========================================================\n");

        // 실제 환자 데이터 (ICW 환자 평균)
        float[,] patientABG = new float[5, 8]
        {
            // 125Hz, 250Hz, 500Hz, 1kHz, 2kHz, 3kHz, 4kHz, 8kHz
            { 0, 0, 0, 0, 0, 0, 0, 0 },           // Grade 0
            { 20, 35, 5, 10, 0, 0, 10, 5 },       // Grade 1 (ICW5)
            { 25, 28, 20, 20, 10, 28, 30, 23 },   // Grade 2 (ICW1,7 평균)
            { 40, 60, 35, 35, 15, 30, 20, 18 },   // Grade 3 (ICW4)
            { 38, 38, 20, 28, 20, 35, 43, 38 }    // Grade 4 (ICW6,8 평균)
        };

        earSimulator.middleEarVolumeCm3 = 0.8f;
        earSimulator.SetPerforationPosition(0.5f, 0.5f); // 중앙

        sb.AppendLine("Grade\tFreq\tPatient\tSimulation\tDiff");
        sb.AppendLine("---------------------------------------------------------");

        float totalError = 0f;
        int count = 0;

        for (int grade = 0; grade <= 4; grade++)
        {
            earSimulator.SetPerforationGrade(grade);

            for (int f = 0; f < frequencies.Length; f++)
            {
                float simABG = earSimulator.CalculateAirBoneGap(frequencies[f]);
                float patABG = patientABG[grade, f];
                float diff = simABG - patABG;

                sb.AppendLine($"{grade}\t{frequencies[f]}Hz\t{patABG:F1}\t{simABG:F1}\t\t{diff:+0.0;-0.0}");

                totalError += Mathf.Abs(diff);
                count++;
            }
            sb.AppendLine();
        }

        float mae = totalError / count;
        sb.AppendLine($"=========================================================");
        sb.AppendLine($"Mean Absolute Error (MAE): {mae:F2} dB");
        sb.AppendLine($"=========================================================");

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// 전체 결과 CSV 저장
    /// </summary>
    void ExportToCSV()
    {
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"Experiment_Full_{timestamp}.csv";
        string filePath = Path.Combine(Application.dataPath, fileName);

        StringBuilder csv = new StringBuilder();

        // 헤더
        csv.Append("Grade,PosX,PosY,Quadrant,MiddleEarVolume_cm3");
        csv.Append(",TM_Diameter_mm,Perforation_Diameter_mm,Perforation_Area_mm2");
        foreach (var freq in frequencies)
        {
            csv.Append($",ABG_{freq}Hz");
        }
        csv.AppendLine(",AvgLow,AvgMid,AvgHigh,AvgTotal");

        // 데이터
        foreach (var result in results)
        {
            csv.Append($"{result.grade},{result.posX:F2},{result.posY:F2},{result.quadrant},{result.middleEarVolume:F2}");
            csv.Append($",{result.tmDiameterMM:F2},{result.perforationDiameterMM:F2},{result.perforationAreaMM2:F2}");
            foreach (var abg in result.abgByFrequency)
            {
                csv.Append($",{abg:F2}");
            }
            csv.AppendLine($",{result.avgLow:F2},{result.avgMid:F2},{result.avgHigh:F2},{result.avgTotal:F2}");
        }

        File.WriteAllText(filePath, csv.ToString());
        lastExportPath = filePath;

        Debug.Log($"[ExperimentRunner] 전체 결과 저장: {filePath}");
    }

    /// <summary>
    /// Grade별 요약 CSV 저장
    /// </summary>
    void ExportSummaryByGrade()
    {
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"Experiment_Summary_{timestamp}.csv";
        string filePath = Path.Combine(Application.dataPath, fileName);

        StringBuilder csv = new StringBuilder();

        // 헤더
        csv.Append("Grade,MiddleEarVolume_cm3,TM_Diameter_mm,Perforation_Diameter_mm,Perforation_Area_mm2");
        foreach (var freq in frequencies)
        {
            csv.Append($",ABG_{freq}Hz");
        }
        csv.AppendLine(",AvgLow,AvgMid,AvgHigh,AvgTotal");

        // Grade별, Volume별 평균 계산
        foreach (float volume in middleEarVolumes)
        {
            foreach (int grade in grades)
            {
                float[] avgABG = new float[frequencies.Length];
                float avgLow = 0f, avgMid = 0f, avgHigh = 0f, avgTotal = 0f;
                float tmDiameter = 0f, perfDiameter = 0f, perfArea = 0f;
                int count = 0;

                foreach (var result in results)
                {
                    if (result.grade == grade && Mathf.Approximately(result.middleEarVolume, volume))
                    {
                        for (int f = 0; f < frequencies.Length; f++)
                        {
                            avgABG[f] += result.abgByFrequency[f];
                        }
                        avgLow += result.avgLow;
                        avgMid += result.avgMid;
                        avgHigh += result.avgHigh;
                        avgTotal += result.avgTotal;
                        tmDiameter = result.tmDiameterMM;  // 동일 값
                        perfDiameter = result.perforationDiameterMM;  // 동일 값
                        perfArea = result.perforationAreaMM2;  // 동일 값
                        count++;
                    }
                }

                if (count > 0)
                {
                    csv.Append($"{grade},{volume:F2},{tmDiameter:F2},{perfDiameter:F2},{perfArea:F2}");
                    for (int f = 0; f < frequencies.Length; f++)
                    {
                        csv.Append($",{avgABG[f] / count:F2}");
                    }
                    csv.AppendLine($",{avgLow / count:F2},{avgMid / count:F2},{avgHigh / count:F2},{avgTotal / count:F2}");
                }
            }
        }

        File.WriteAllText(filePath, csv.ToString());

        Debug.Log($"[ExperimentRunner] 요약 결과 저장: {filePath}");
    }

    int GetQuadrant(float posX, float posY)
    {
        if (posX >= 0.5f && posY >= 0.5f) return 1; // 후상방
        if (posX < 0.5f && posY >= 0.5f) return 2;  // 전상방
        if (posX < 0.5f && posY < 0.5f) return 3;   // 전하방
        return 4; // 후하방
    }

    // 결과 클래스
    [System.Serializable]
    public class ExperimentResult
    {
        public int grade;
        public float posX;
        public float posY;
        public int quadrant;
        public float middleEarVolume;
        public float[] abgByFrequency;
        public float avgLow;
        public float avgMid;
        public float avgHigh;
        public float avgTotal;

        // 실제 크기 (mm)
        public float tmDiameterMM;           // 고막 직경
        public float perforationDiameterMM;  // 천공 직경
        public float perforationAreaMM2;     // 천공 면적
    }
}
