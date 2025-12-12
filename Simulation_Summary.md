# Tympanic Membrane Perforation Simulation
## 고막 천공 시뮬레이션 모델 및 결과 분석

---

## 1. 연구 배경

### 1.1 고막 천공이란?
- 고막(Tympanic Membrane)에 구멍이 생기는 질환
- 주요 원인: 중이염(COM), 외상, 압력 변화
- 결과: **전음성 난청** (Air-Bone Gap 발생)

### 1.2 연구 목적
- 고막 천공이 청력에 미치는 영향을 **물리 기반 시뮬레이션**으로 모델링
- 천공 **크기**, **위치**, **주파수**에 따른 청력 손실(ABG) 예측
- 실제 환자 데이터와 비교 검증

---

## 2. 시뮬레이션 모델

### 2.1 모델 구조

```
최종 ABG = [물리 모델] × 50% + [문헌 참고값] × 50%
```

### 2.2 물리 모델 구성요소

#### (1) 크기 효과 (Size Effect)
```
ABG_size = 20 × log₁₀(1 / (1 - 천공비율))
```
- 천공이 클수록 ABG 증가 (비선형, 로그 관계)
- 근거: 음향 임피던스 변화

#### (2) 위치 효과 (Position Effect)
| 위치 | 효과 | 이유 |
|------|------|------|
| 후방(Posterior) | +5 dB | 이소골(umbo)에 가까움 |
| 전방(Anterior) | -4 dB | 이소골에서 멂 |
| 상방(Superior) | +3 dB | 추골 연결점 근처 |
| 후상방 4분면 | +4 dB (추가) | 최대 손실 영역 |

- 총 위치 효과 범위: **-8 ~ +12 dB**
- 근거: Mehta et al. 2006, Bigelow et al. 1998

#### (3) 중이강 볼륨 효과 (Volume Effect)
- 정상 볼륨: 0.8 cm³
- 천공 시 유효 볼륨 증가 → 압력 손실
- 근거: PMC1868690

#### (4) 주파수 변조 (Frequency Modulation)
| 주파수 | 계수 | 설명 |
|--------|------|------|
| < 500 Hz | ×1.3 | 저주파 영향 큼 |
| 500-1000 Hz | ×1.15 | |
| 1000-2000 Hz | ×1.0 | 기준 |
| > 2000 Hz | ×0.85 | 고주파 영향 작음 |

#### (5) Round Window Shielding Effect
- **현상**: 큰 천공(>75%)에서 ABG가 오히려 감소
- **원리**: 소리가 정원창에 직접 도달 → 이소골 직접 자극
- **적용**: Grade IV에서 최대 20 dB 감소
- **근거**: Voss et al. 2001, Merchant et al. 1997

### 2.3 천공 등급 정의

| Grade | 천공 비율 | 직경 (mm) | 면적 (mm²) |
|-------|----------|-----------|-----------|
| 0 | 0% | 0 | 0 |
| I | < 25% | ~2.5 | ~21 |
| II | 25-50% | ~4.0 | ~32 |
| III | 50-75% | ~5.5 | ~53 |
| IV | > 75% | ~7.0 | ~64 |

- 고막 기준: 직경 9mm, 면적 85mm²

---

## 3. 환자 데이터

### 3.1 데이터 구성
- **ICW (Intact Canal Wall)**: 30명 - 고막 천공, 이소골 정상
- **CWD (Canal Wall Down)**: 63명 - 진주종, 이소골 부식

### 3.2 Grade별 환자 수
| Grade | 환자 수 | 환자 ID |
|-------|---------|---------|
| I | 1 | ICW5 |
| II | 4 | ICW1, ICW2, ICW3, ICW7 |
| III | 1 | ICW4 |
| IV | 2 | ICW6, ICW8 |

### 3.3 데이터 한계
- Grade III 환자(ICW4): **혼합성 난청** (SNHL 동반)
- 순수 천공 환자 데이터 부족
- Grade당 환자 수 적음 (통계적 한계)

---

## 4. 실험 설계

### 4.1 시뮬레이션 조건
- Grade: 1, 2, 3, 4
- 위치 (X, Y): 0.0, 0.25, 0.5, 0.75, 1.0 (5×5 = 25개)
- 주파수: 250, 500, 1000, 2000, 3000, 4000 Hz
- **총 실험 수**: 4 × 25 × 6 = **600개**

### 4.2 측정 지표
- **ABG (Air-Bone Gap)**: 기도-골도 청력 차이 (dB)
- 높을수록 전음성 난청 심함

---

## 5. 결과 분석

### 5.1 Grade별 평균 ABG 비교

| Grade | Simulation | Clinical | 오차 |
|-------|------------|----------|------|
| I | ~10 dB | 10.0 dB | ~0 dB |
| II | ~20 dB | 20.8 dB | ~-1 dB |
| III | ~28 dB | 32.5 dB | ~-4 dB |
| IV | ~19 dB | 19.2 dB | ~0 dB |

### 5.2 위치별 ABG (Heatmap)
- **후상방(1,1)**: 최대 ABG (이소골 근처)
- **전하방(0,0)**: 최소 ABG (이소골에서 멂)
- 위치 차이: 최대 **~10 dB**

### 5.3 주파수별 반응
- 저주파(250-500Hz): ABG 높음
- 고주파(2000-4000Hz): ABG 낮음
- Grade IV: 500Hz에서 ABG 급감 (Round Window 효과)

### 5.4 ICW vs CWD 비교
| 그룹 | 평균 ABG | 특징 |
|------|----------|------|
| Simulation | ~20 dB | 천공만 모델링 |
| ICW | ~11 dB | 이소골 정상 |
| CWD | ~13 dB | 이소골 부식 (+2.4 dB) |

---

## 6. 주요 발견

### 6.1 Round Window Shielding Effect
- Grade IV에서 ABG가 Grade III보다 **낮음**
- 큰 천공 → 소리가 이소골에 직접 전달
- 임상 데이터와 일치 (ICW6: 19.2 dB)

### 6.2 위치의 중요성
- 같은 크기라도 위치에 따라 **최대 10 dB 차이**
- 후상방 천공이 가장 심한 청력 손실

### 6.3 순수 천공 vs 혼합 난청
- 대부분 환자가 SNHL 동반
- 시뮬레이션은 순수 천공 기준
- Grade III 오차 원인: 환자(ICW4)가 혼합 난청

---

## 7. 모델 한계

1. **이소골 부식 미반영**: CWD 환자와 차이 존재
2. **순수 천공 데이터 부족**: 대부분 환자가 복합 질환
3. **개인차**: 중이강 볼륨, 고막 두께 등 변이
4. **동적 효과**: 시간에 따른 치유/악화 미반영

---

## 8. 결론

### 8.1 시뮬레이션 유효성
- 물리 기반 모델로 ABG 예측 가능
- 임상 데이터와 **±5 dB 이내** 일치 (대부분)
- Round Window Shielding 현상 재현 성공

### 8.2 임상적 의의
- 수술 전 청력 손실 예측 도구로 활용 가능
- 천공 위치가 수술 우선순위 결정에 도움
- 환자 교육 및 시각화에 유용

### 8.3 향후 연구
- 이소골 부식 모델 추가
- 더 많은 순수 천공 환자 데이터 수집
- 3D 고막 형태 반영

---

## 참고 문헌

1. Voss SE, et al. (2001). "Middle-ear function with tympanic-membrane perforations." *JASA*
2. Mehta RP, et al. (2006). "Middle ear mechanics: the dynamic behavior of the incudo-stapedial joint." *Otology & Neurotology*
3. Merchant SN, et al. (1997). "Acoustic input impedance of the stapes and cochlea." *Hearing Research*
4. Ahmad SW, Ramani GV. (1979). "Hearing loss in perforations of the tympanic membrane." *J Laryngol Otol*
5. Bigelow DC, et al. (1998). "Otoscopic findings and audiometric measurements in perforated ears." *Am J Otol*

---

## 부록: 생성된 그래프

1. **Fig1**: Grade별 ABG 비교 (Simulation vs Clinical)
2. **Fig2**: 위치별 ABG Heatmap
3. **Fig3**: 최적 위치 매칭 (오차 최소화)
4. **Fig4**: ABG 분포 (Sim vs ICW vs CWD)
5. **Fig5**: 주파수별 반응 (모든 Grade)
6. **Fig6**: Round Window Shielding Effect
7. **Fig7**: 종합 Dashboard
8. **Fig8**: ICW vs CWD 비교

---

*문서 생성일: 2025-12-11*
*시뮬레이션 버전: Unity 기반 Middle Ear Simulator*
