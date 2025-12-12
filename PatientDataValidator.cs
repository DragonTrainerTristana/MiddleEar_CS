using UnityEngine;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 환자 데이터 기반 시뮬레이션 검증
/// 실제 청력검사 데이터와 시뮬레이션 결과 비교
/// </summary>
public class PatientDataValidator : MonoBehaviour
{
    [Header("=== EarSimulator 참조 ===")]
    public EarSimulator earSimulator;

    [Header("=== 검증 결과 (읽기 전용) ===")]
    [SerializeField] private float meanAbsoluteError;
    [SerializeField] private float correlationCoeff;
    [SerializeField] private string validationStatus;

    // 실제 환자 데이터 (ICW 환자 - 수술 전 측정)
    // ABG = Air Conduction - Bone Conduction
    private List<PatientRecord> patientRecords = new List<PatientRecord>();

    // 표준 주파수
    private readonly float[] frequencies = { 250f, 500f, 1000f, 2000f, 3000f, 4000f };

    void Start()
    {
        if (earSimulator == null)
            earSimulator = FindObjectOfType<EarSimulator>();

        LoadPatientData();
    }

    /// <summary>
    /// 실제 환자 데이터 로드
    /// 환자_청력검사_주파수별_상세_전체.txt 에서 추출
    /// </summary>
    void LoadPatientData()
    {
        patientRecords.Clear();

        // ICW1 - Grade 2, Left ear (수술전)
        patientRecords.Add(new PatientRecord
        {
            id = "ICW1",
            perforationGrade = 2,
            ear = "Left",
            // ABG = Air - Bone for each frequency
            // L_A: 0.25=40, 0.5=40, 1=30, 2=40, 3=70, 4=65
            // L_B: 0.25=25, 0.5=25, 1=15, 2=40, 3=40, 4=35
            abgByFrequency = new Dictionary<float, float>
            {
                { 250f, 40f - 25f },   // 15 dB
                { 500f, 40f - 25f },   // 15 dB
                { 1000f, 30f - 15f },  // 15 dB
                { 2000f, 40f - 40f },  // 0 dB
                { 3000f, 70f - 40f },  // 30 dB
                { 4000f, 65f - 35f }   // 30 dB
            }
        });

        // ICW4 - Grade 3, Left ear (수술전)
        patientRecords.Add(new PatientRecord
        {
            id = "ICW4",
            perforationGrade = 3,
            ear = "Left",
            // L_A: 0.25=65, 0.5=60, 1=60, 2=70, 3=85, 4=80
            // L_B: 0.25=5, 0.5=25, 1=25, 2=55, 3=55, 4=60
            abgByFrequency = new Dictionary<float, float>
            {
                { 250f, 65f - 5f },    // 60 dB
                { 500f, 60f - 25f },   // 35 dB
                { 1000f, 60f - 25f },  // 35 dB
                { 2000f, 70f - 55f },  // 15 dB
                { 3000f, 85f - 55f },  // 30 dB
                { 4000f, 80f - 60f }   // 20 dB
            }
        });

        // ICW5 - Grade 1, Right ear (수술전)
        patientRecords.Add(new PatientRecord
        {
            id = "ICW5",
            perforationGrade = 1,
            ear = "Right",
            // R_A: 0.25=50, 0.5=35, 1=35, 2=40, 3=40, 4=45
            // R_B: 0.25=15, 0.5=30, 1=25, 2=40, 3=40, 4=35
            abgByFrequency = new Dictionary<float, float>
            {
                { 250f, 50f - 15f },   // 35 dB
                { 500f, 35f - 30f },   // 5 dB
                { 1000f, 35f - 25f },  // 10 dB
                { 2000f, 40f - 40f },  // 0 dB
                { 3000f, 40f - 40f },  // 0 dB
                { 4000f, 45f - 35f }   // 10 dB
            }
        });

        // ICW6 - Grade 4, Right ear (수술전)
        patientRecords.Add(new PatientRecord
        {
            id = "ICW6",
            perforationGrade = 4,
            ear = "Right",
            // R_A: 0.25=35, 0.5=20, 1=35, 2=35, 3=55, 4=60
            // R_B: 0.25=5, 0.5=15, 1=20, 2=30, 3=30, 4=25
            abgByFrequency = new Dictionary<float, float>
            {
                { 250f, 35f - 5f },    // 30 dB
                { 500f, 20f - 15f },   // 5 dB
                { 1000f, 35f - 20f },  // 15 dB
                { 2000f, 35f - 30f },  // 5 dB
                { 3000f, 55f - 30f },  // 25 dB
                { 4000f, 60f - 25f }   // 35 dB
            }
        });

        // ICW7 - Grade 2, Right ear (수술전)
        patientRecords.Add(new PatientRecord
        {
            id = "ICW7",
            perforationGrade = 2,
            ear = "Right",
            // R_A: 0.25=45, 0.5=40, 1=40, 2=40, 3=55, 4=65
            // R_B: 0.25=5, 0.5=15, 1=15, 2=20, 3=30, 4=35
            abgByFrequency = new Dictionary<float, float>
            {
                { 250f, 45f - 5f },    // 40 dB
                { 500f, 40f - 15f },   // 25 dB
                { 1000f, 40f - 15f },  // 25 dB
                { 2000f, 40f - 20f },  // 20 dB
                { 3000f, 55f - 30f },  // 25 dB
                { 4000f, 65f - 35f }   // 30 dB
            }
        });

        // ICW8 - Grade 4, Right ear (수술전)
        patientRecords.Add(new PatientRecord
        {
            id = "ICW8",
            perforationGrade = 4,
            ear = "Right",
            // R_A: 0.25=50, 0.5=45, 1=50, 2=45, 3=55, 4=60
            // R_B: 0.25=5, 0.5=10, 1=10, 2=10, 3=10, 4=10
            abgByFrequency = new Dictionary<float, float>
            {
                { 250f, 50f - 5f },    // 45 dB
                { 500f, 45f - 10f },   // 35 dB
                { 1000f, 50f - 10f },  // 40 dB
                { 2000f, 45f - 10f },  // 35 dB
                { 3000f, 55f - 10f },  // 45 dB
                { 4000f, 60f - 10f }   // 50 dB
            }
        });

        Debug.Log($"[PatientDataValidator] {patientRecords.Count}명의 환자 데이터 로드 완료");
    }

    /// <summary>
    /// 시뮬레이션 결과와 실제 데이터 비교
    /// </summary>
    [ContextMenu("Run Validation")]
    public void RunValidation()
    {
        if (earSimulator == null)
        {
            Debug.LogError("[PatientDataValidator] EarSimulator가 연결되지 않았습니다!");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("\n============ Simulation Validation Results ============");

        float totalError = 0f;
        int totalCount = 0;
        List<float> simValues = new List<float>();
        List<float> realValues = new List<float>();

        foreach (var patient in patientRecords)
        {
            // 시뮬레이터 설정
            earSimulator.SetPerforationGrade(patient.perforationGrade);
            // 위치는 중앙으로 (실제 위치 데이터 없음)
            earSimulator.SetPerforationPosition(0.5f, 0.5f);

            sb.AppendLine($"\n--- Patient {patient.id} (Grade {patient.perforationGrade}, {patient.ear}) ---");
            sb.AppendLine("Freq(Hz)\tReal ABG\tSim ABG\t\tError");

            float patientError = 0f;
            int patientCount = 0;

            foreach (var kvp in patient.abgByFrequency)
            {
                float freq = kvp.Key;
                float realABG = kvp.Value;
                float simABG = earSimulator.CalculateAirBoneGap(freq);
                float error = Mathf.Abs(simABG - realABG);

                sb.AppendLine($"{freq:F0}\t\t{realABG:F1} dB\t\t{simABG:F1} dB\t\t{error:F1} dB");

                patientError += error;
                patientCount++;
                totalError += error;
                totalCount++;

                simValues.Add(simABG);
                realValues.Add(realABG);
            }

            float patientMAE = patientError / patientCount;
            sb.AppendLine($"Patient MAE: {patientMAE:F2} dB");
        }

        // 전체 통계
        meanAbsoluteError = totalError / totalCount;
        correlationCoeff = CalculateCorrelation(simValues, realValues);

        sb.AppendLine("\n============ Summary ============");
        sb.AppendLine($"Total Samples: {totalCount}");
        sb.AppendLine($"Mean Absolute Error (MAE): {meanAbsoluteError:F2} dB");
        sb.AppendLine($"Correlation Coefficient: {correlationCoeff:F3}");

        // 검증 상태 판정
        if (meanAbsoluteError < 10f && correlationCoeff > 0.7f)
            validationStatus = "EXCELLENT";
        else if (meanAbsoluteError < 15f && correlationCoeff > 0.5f)
            validationStatus = "GOOD";
        else if (meanAbsoluteError < 20f && correlationCoeff > 0.3f)
            validationStatus = "ACCEPTABLE";
        else
            validationStatus = "NEEDS IMPROVEMENT";

        sb.AppendLine($"Validation Status: {validationStatus}");
        sb.AppendLine("=================================");

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// 등급별 ABG 평균 비교
    /// </summary>
    [ContextMenu("Compare By Grade")]
    public void CompareByGrade()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("\n============ ABG by Perforation Grade ============");
        sb.AppendLine("Grade\tReal Avg\tSim Avg\t\tDiff");

        for (int grade = 1; grade <= 4; grade++)
        {
            // 해당 등급 환자들의 평균 ABG
            float realSum = 0f;
            int realCount = 0;

            foreach (var patient in patientRecords)
            {
                if (patient.perforationGrade == grade)
                {
                    foreach (var abg in patient.abgByFrequency.Values)
                    {
                        realSum += abg;
                        realCount++;
                    }
                }
            }

            float realAvg = realCount > 0 ? realSum / realCount : 0f;

            // 시뮬레이션 평균
            earSimulator.SetPerforationGrade(grade);
            earSimulator.SetPerforationPosition(0.5f, 0.5f);

            float simSum = 0f;
            foreach (float freq in frequencies)
            {
                simSum += earSimulator.CalculateAirBoneGap(freq);
            }
            float simAvg = simSum / frequencies.Length;

            float diff = simAvg - realAvg;
            sb.AppendLine($"Grade {grade}\t{realAvg:F1} dB\t\t{simAvg:F1} dB\t\t{diff:+0.0;-0.0} dB");
        }

        sb.AppendLine("==================================================");
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Pearson 상관계수 계산
    /// </summary>
    float CalculateCorrelation(List<float> x, List<float> y)
    {
        if (x.Count != y.Count || x.Count == 0) return 0f;

        float n = x.Count;
        float sumX = 0f, sumY = 0f, sumXY = 0f, sumX2 = 0f, sumY2 = 0f;

        for (int i = 0; i < x.Count; i++)
        {
            sumX += x[i];
            sumY += y[i];
            sumXY += x[i] * y[i];
            sumX2 += x[i] * x[i];
            sumY2 += y[i] * y[i];
        }

        float numerator = n * sumXY - sumX * sumY;
        float denominator = Mathf.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));

        return denominator > 0 ? numerator / denominator : 0f;
    }

    // 환자 기록 클래스
    [System.Serializable]
    public class PatientRecord
    {
        public string id;
        public int perforationGrade;
        public string ear;
        public Dictionary<float, float> abgByFrequency;
    }
}
