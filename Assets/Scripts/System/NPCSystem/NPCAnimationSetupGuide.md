# NPC Animation Controller 설정 가이드

## 1. Animator Controller 생성

1. **Assets > Create > Animator Controller** 생성 (`NPCAnimatorController`)
2. NPC GameObject의 **Animator** 컴포넌트에 할당

## 2. 필수 Parameters 추가

Animator Controller에 다음 파라미터들을 추가해야 합니다:

- **Speed** (Float): 이동 속도 (0.0 ~ 1.0)
- **IsMoving** (Bool): 이동 중인지 여부
- **Behavior** (Int): 행동 타입 (NPCBehaviorType enum 값)

## 3. 기본 애니메이션 상태 생성

### 필수 상태들:
- **Idle**: 대기 상태
- **Walking**: 걷기 상태  
- **Working**: 작업 상태
- **Talking**: 대화 상태
- **Sitting**: 앉기 상태

### 선택적 상태들:
- **Eating**: 식사 상태
- **Shopping**: 쇼핑 상태
- **Sleeping**: 수면 상태
- **Reading**: 독서 상태
- **LookLeft**: 좌측 둘러보기
- **LookRight**: 우측 둘러보기

## 4. 상태 전환(Transition) 설정

### 자동 Walking 지원을 위한 설정:

#### A. 파라미터 기반 전환 (권장)
```
Any State -> Walking
조건: IsMoving == true

Walking -> Idle  
조건: IsMoving == false

Idle -> Walking
조건: IsMoving == true
```

#### B. 또는 Speed 기반 전환
```
Any State -> Walking
조건: Speed > 0.1

Walking -> Idle
조건: Speed < 0.1
```

### 행동별 전환:
```
Any State -> Working
조건: Behavior == 2 (Working)

Any State -> Talking  
조건: Behavior == 3 (Talking)

Any State -> Sitting
조건: Behavior == 4 (Sitting)
```

## 5. NPCAnimationController 컴포넌트 설정

### Inspector 설정:
```
Animation Settings:
✓ Enable Animations: true
- Animation Transition Time: 0.1

Auto Walking Detection:
✓ Auto Detect Walking: true  
- Walking Speed Threshold: 0.1
- Walking Update Interval: 0.1

Animation Parameters:
- Speed Parameter Name: "Speed"
- Behavior Parameter Name: "Behavior" 
- Is Moving Parameter Name: "IsMoving"
- Trigger Parameter Name: "Trigger"

Debug:
✓ Show Debug Logs: true
```

## 6. 문제 해결

### 걷기 애니메이션이 안 나올 때:

1. **파라미터 확인**:
   - Animator에 `Speed`, `IsMoving`, `Behavior` 파라미터가 있는지 확인
   - 파라미터 이름이 NPCAnimationController 설정과 정확히 일치하는지 확인

2. **상태 확인**:
   - `Walking` 상태가 Animator에 있는지 확인
   - Walking 애니메이션 클립이 할당되어 있는지 확인

3. **전환 확인**:
   - `IsMoving == true` 조건으로 Walking 상태로 전환되는지 확인
   - `HasExitTime`이 체크되어 있다면 해제 (즉시 전환을 위해)

4. **디버그 방법**:
   ```csharp
   // NPCAnimationController 컴포넌트에서 우클릭 > Debug Animation Info
   // 또는 코드에서:
   GetComponent<NPCAnimationController>().DebugAnimationInfo();
   ```

## 7. 간단한 테스트 설정

### 최소한의 Animator 설정:
1. **상태**: Idle, Walking 2개만 생성
2. **파라미터**: IsMoving (Bool) 1개만 추가
3. **전환**: 
   - Any State -> Walking (IsMoving == true)
   - Walking -> Idle (IsMoving == false)

### 테스트 확인:
```csharp
// 이동 시작할 때 콘솔에 출력되어야 함:
[NPCAnimationController] (TestNPC) 이동 상태: True, 속도: 1.00
[NPCAnimationController] (TestNPC) 자동 Walking 시작 (이전 행동: Idle)
[NPCAnimationController] (TestNPC) 행동 변경: Walking -> 애니메이션: Walking

// 이동 완료 시:
[NPCAnimationController] (TestNPC) 이동 상태: False, 속도: 0.00  
[NPCAnimationController] (TestNPC) 자동 Walking 종료 (복귀 행동: Idle)
[NPCAnimationController] (TestNPC) 행동 변경: Idle -> 애니메이션: Idle
```

## 8. 고급 설정

### 블렌드 트리 사용:
- Speed 파라미터를 활용한 블렌드 트리 생성
- Idle(0.0) ~ Walk(0.5) ~ Run(1.0) 애니메이션 블렌딩

### 레이어 분리:
- Base Layer: 기본 이동/행동
- Override Layer: 특수 애니메이션 (대화, 제스처 등)

### 애니메이션 이벤트:
- Walking 애니메이션에 발소리 이벤트 추가
- 행동 완료 시점에 콜백 이벤트 추가 