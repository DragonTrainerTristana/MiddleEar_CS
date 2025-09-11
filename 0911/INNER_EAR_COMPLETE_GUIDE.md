# 🎧 INNER EAR RECEIVER - COMPLETE USER GUIDE
# 완전한 사용자 가이드 (고등학생도 바로 이해 가능)

---

## 📚 목차 (Table of Contents)
1. [🧠 기본 개념 이해](#기본-개념-이해)
2. [🚀 빠른 시작 가이드](#빠른-시작-가이드)  
3. [⚙️ 설정 완전 가이드](#설정-완전-가이드)
4. [📊 데이터 읽는 방법](#데이터-읽는-방법)
5. [🎨 시각화 설정](#시각화-설정)
6. [🐞 문제 해결](#문제-해결)
7. [🔬 고급 활용법](#고급-활용법)

---

## 🧠 기본 개념 이해

### 내이(달팽이관)가 뭐야?
- **달팽이관**: 귀 안쪽에 있는 달팽이 모양 기관
- **역할**: 소리를 전기 신호로 바꿔서 뇌로 전달
- **특징**: 20Hz~20,000Hz 소리를 24개 구역으로 나누어 처리

### dB SPL이 뭐야?
- **dB**: 데시벨, 소리 크기 단위
- **SPL**: Sound Pressure Level (음압 레벨)
- **예시**:
  - 0 dB SPL = 들을 수 없는 소리 (기준점)
  - 20 dB SPL = 나뭇잎 바스락거리는 소리
  - 60 dB SPL = 일반 대화 소리
  - 90 dB SPL = 지하철 소음 (위험 시작)
  - 120 dB SPL = 제트엔진 소리 (고통 레벨)

### 주파수가 뭐야?
- **주파수**: 소리의 높낮이 (Hz 단위)
- **낮은 주파수**: 20-200Hz (저음, 베이스)
- **중간 주파수**: 200-4000Hz (목소리, 대화)
- **높은 주파수**: 4000-20000Hz (고음, 새소리)

---

## 🚀 빠른 시작 가이드

### STEP 1: Unity 프로젝트 준비
```
1. Unity 새 프로젝트 생성 (3D Template)
2. 씬에 빈 GameObject 생성 (Hierarchy 우클릭 → Create Empty)
3. GameObject 이름을 "InnerEarReceiver"로 변경
```

### STEP 2: 스크립트 추가
```
1. InnerEarReceiver GameObject 선택
2. Inspector에서 "Add Component" 클릭
3. "Inner Ear Receiver" 검색 후 추가
```

### STEP 3: 기본 실행
```
1. Unity Play 버튼 (▶️) 클릭
2. Inspector에서 "Measurement Data" 섹션 확인
3. 실시간으로 변하는 숫자들 관찰
```

### 🎉 완료! 
이제 가상 달팽이관이 작동합니다!

---

## ⚙️ 설정 완전 가이드

### 🎵 달팽이관 응답 (Cochlear Response) 설정

#### Hearing Threshold (청각 임계값)
```
- 기본값: 20 dB SPL
- 의미: 이보다 작은 소리는 못 들음
- 조정 가이드:
  * 10-15: 매우 좋은 청력
  * 20-25: 정상 청력
  * 30-40: 약간의 청력 저하
```

#### Pain Threshold (고통 임계값)  
```
- 기본값: 120 dB SPL
- 의미: 이 정도면 귀가 아픔
- 조정 가이드:
  * 110-115: 민감한 귀
  * 120-125: 보통 귀
  * 130-135: 둔감한 귀
```

#### Damage Threshold (손상 임계값)
```
- 기본값: 90 dB SPL  
- 의미: 장시간 들으면 청력 손상
- 조정 가이드:
  * 85: 매우 보수적 (안전 중시)
  * 90: 표준 (의학적 권장)
  * 95: 관대함 (위험 허용)
```

#### Adaptation Rate (적응 속도)
```
- 기본값: 0.1 (10초에 걸쳐 적응)
- 의미: 큰 소리에 얼마나 빨리 익숙해지는가
- 조정 가이드:
  * 0.01-0.05: 천천히 적응 (100-20초)
  * 0.1-0.2: 보통 속도 (10-5초)
  * 0.5-1.0: 매우 빠르게 적응 (2-1초)
```

### ⚙️ 설정 (Settings) 조정

#### Measurement Interval (측정 간격)
```
- 기본값: 0.1초 (초당 10번 측정)
- 성능 vs 정확도 트레이드오프:

🐌 느리지만 가벼움:
- 0.5초: 초당 2번 → 낮은 CPU 사용
- 1.0초: 초당 1번 → 매우 가벼움

⚡ 빠르지만 무거움:
- 0.05초: 초당 20번 → 높은 정확도
- 0.01초: 초당 100번 → 실시간 분석
```

#### Averaging Window (평균 윈도우)
```
- 기본값: 5초
- 의미: 몇 초간의 평균을 계산할 것인가

📊 데이터 안정성:
- 1-2초: 빠른 반응, 불안정
- 5-10초: 안정된 평균값
- 30-60초: 매우 안정, 느린 반응
```

---

## 📊 데이터 읽는 방법

### 실시간 측정값 섹션

#### Current SPL (현재 dB)
```
📈 실시간 소리 크기
- 0-20 dB: 거의 무음
- 20-40 dB: 조용함 (도서관)
- 40-60 dB: 보통 (사무실)
- 60-80 dB: 시끄러움 (식당)
- 80+ dB: 매우 시끄러움 (주의!)
```

#### Peak SPL (최대 dB)
```
🔴 지금까지 가장 큰 소리
- 일종의 "최고 기록"
- 갑작스러운 큰 소리 감지용
- Reset 버튼으로 초기화 가능
```

#### Average SPL (평균 dB)
```
📊 최근 5초간 평균 소리 크기
- Current SPL보다 안정적
- 전체적인 소음 수준 파악용
- 청력 손상 위험도 계산에 사용
```

### 누적 데이터 섹션

#### Total Exposure Time (총 노출 시간)
```
⏱️ 소리에 노출된 총 시간
- 단위: 초
- 3600초 = 1시간
- 28800초 = 8시간 (위험 기준)
```

#### Hearing Damage Risk (청력 손상 위험도)
```
⚠️ 0~1 사이의 위험도 점수

🟢 안전 구간:
- 0.0-0.1: 완전 안전
- 0.1-0.3: 안전함

🟡 주의 구간:  
- 0.3-0.5: 조심 필요
- 0.5-0.7: 주의 요망

🔴 위험 구간:
- 0.7-0.9: 위험함
- 0.9-1.0: 매우 위험
```

### 상태 섹션

#### Current Hearing Status (청력 상태)
```
📋 한눈에 보는 상태 요약

"Normal": 😊 정상 (0-0.1 위험도)
"Caution": 😐 주의 (0.1-0.3 위험도) 
"Warning": 😰 경고 (0.3-0.7 위험도)
"Danger": 🚨 위험 (0.7-1.0 위험도)
```

---

## 🎨 시각화 설정

### 파티클 시스템 (Particle System)
```
1. 빈 GameObject 생성
2. Particle System 컴포넌트 추가
3. InnerEarReceiver의 "Sound Visualization"에 드래그
4. 설정:
   - Start Lifetime: 2-5초
   - Start Speed: 1-3
   - Max Particles: 100-500
```

### 라인 렌더러 (Line Renderer)  
```
1. 빈 GameObject 생성
2. Line Renderer 컴포넌트 추가
3. InnerEarReceiver의 "Frequency Response"에 드래그
4. 설정:
   - Width: 0.01-0.05
   - Color: Gradient (파란색→빨간색)
   - Material: Default Line Material
```

### 색상 설정
```
Normal Color: 초록색 (0, 255, 0)
Warning Color: 노란색 (255, 255, 0)  
Danger Color: 빨간색 (255, 0, 0)
```

---

## 🐞 문제 해결

### Q1: 아무 반응이 없어요
```
✅ 해결책:
1. enableRealTimeAnalysis가 체크되어 있는지 확인
2. 다른 스크립트에서 ReceiveVibration() 호출 확인
3. Unity Console에서 에러 메시지 확인
```

### Q2: 숫자가 이상해요 (NaN, Infinity)
```
✅ 해결책:
1. 이미 안전장치가 내장되어 있음
2. Unity Console 확인
3. 설정값들이 유효한 범위인지 확인
```

### Q3: 너무 느려요 / 너무 빨라요
```
✅ 해결책:
느릴 때:
- Measurement Interval을 0.05로 줄이기
- Averaging Window를 2-3초로 줄이기

빠를 때:
- Measurement Interval을 0.2-0.5로 늘리기
- Averaging Window를 10-20초로 늘리기
```

### Q4: 시각화가 안 보여요
```
✅ 해결책:
1. Particle System과 Line Renderer가 제대로 연결됐는지 확인
2. 카메라 위치 조정
3. Scale 조정 (너무 작거나 클 수 있음)
```

---

## 🔬 고급 활용법

### 1. 마이크 입력 연결
```csharp
// 다른 스크립트에서:
InnerEarReceiver innerEar = GetComponent<InnerEarReceiver>();
innerEar.ReceiveVibration(microphoneAmplitude, dominantFrequency);
```

### 2. 오디오 파일 분석
```csharp  
// AudioSource와 연결:
AudioSource audioSource = GetComponent<AudioSource>();
float[] samples = new float[1024];
audioSource.GetOutputData(samples, 0);
// 샘플 데이터를 InnerEarReceiver로 전달
```

### 3. 실시간 모니터링
```csharp
// Update()에서:
if (innerEarReceiver.GetHearingDamageRisk() > 0.7f)
{
    Debug.LogWarning("청력 위험! 볼륨을 낮춰주세요!");
}
```

### 4. 데이터 저장
```csharp
// CSV 파일로 저장:
string csvData = $"{Time.time},{innerEarReceiver.GetCurrentLevel()},{innerEarReceiver.GetHearingDamageRisk()}";
File.AppendAllText("hearing_data.csv", csvData + "\n");
```

---

## 🎯 실제 활용 예시

### 학교 과학 프로젝트
```
1. 교실 소음 측정 실험
2. 이어폰 볼륨 안전성 연구  
3. 소음 공해 조사 프로젝트
4. 청력 보호 교육 자료
```

### 개인 건강 관리
```
1. 일일 소음 노출량 추적
2. 이어폰 사용 시간 모니터링
3. 직장 소음 환경 평가
4. 청력 보호 의식 향상
```

### 게임 개발
```
1. 실감나는 청각 시뮬레이션
2. VR/AR 청각 체험 콘텐츠
3. 교육용 시뮬레이션 게임
4. 헬스케어 앱 개발
```

---

## 📞 마지막 팁

### 🎯 꼭 기억하세요!
1. **안전 첫째**: 실제 청력 보호용으로 사용하지 마세요 (교육/시뮬레이션 목적만)
2. **데이터 해석**: 수치는 참고용이며, 실제 의학적 진단 대신 사용하지 마세요
3. **설정 백업**: Inspector 설정을 Prefab으로 저장해두세요
4. **성능 최적화**: 모바일에서는 Measurement Interval을 0.2초 이상으로 설정

### 🚀 더 배우고 싶다면?
- Unity 공식 문서: Audio 섹션
- DSP (Digital Signal Processing) 기초
- 음향학 기초 이론
- C# 프로그래밍 심화

---

**🎧 이제 당신도 가상 달팽이관 전문가입니다! 🎉**

*이 가이드로 고등학생도 바로 이해하고 활용할 수 있습니다.*