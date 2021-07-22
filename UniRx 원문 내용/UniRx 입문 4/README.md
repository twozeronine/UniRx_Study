# UniRx 입문 4 - Update를 스트림으로 변환하는 방법 및 장점

# Update()를 스트림으로 변환하는 방법

Unity의 Update() 호출을 스트림으로 변환하는 방법은 두 가지가 있다.

-   UniRx.Triggers의 UpdateAsObservable을 이용하는 방법
-   Observable.EveryUpdate를 이용하는 방법

## UniRx.Triggers의 UpdateAsObservable를 이용하는 방법

```C#
using UnityEngine;
using UniRx;
using UniRx.Triggers; // 이 using문이 필요

public class UpdateSample : MonoBehaviour
{
    private void Start() =>
        // UpdateAsObservable는 Component에 대한
        // 확장 메서드로 정의되어 있기 때문에 호출시
        // "this"가 필요
        this.UpdateAsObservable()
            .Subscribe(_ => Debug.Log("Update!")); // 이때 발행되는 형태는 Unit이다.
}

```

UpdateAsObservable이 GameObject가 파괴되었을때 자동으로 OnCompleted가 발행되기 때문에 스트림의 수명 관리도 쉽다.

### 구조

UpdateAsObservable은 ObservableUpdateTrigger 컴포넌트의 실체를 갖는 스트림이다.

UpdateAsObservable을 호출 하는 타이밍에 해당 GameObject에 ObservableUpdateTrigger 컴포넌트를 UniRx가 자동으로 연결하고 이 ObservableUpdateTrigger가 발행하는 이벤트를 사용하는 구조로 되어 있다.

UpdateAsObservable은 ObservableUpdateTrigger를 초기화 하는 확장 메서드

```C#
public static IObservable<Unit> UpdateAsObservable(this Component component)
{
    if (component == null || component.gameObject == null) return Observable.Empty<Unit>();
    return GetOrAddComponent<ObservableUpdateTrigger>(component.gameObject).UpdateAsObservable();
}
```

UpdateAsObservable의 본체

```C#
using System; // require keep for Windows Universal App
using UnityEngine;

namespace UniRx.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableUpdateTrigger : ObservableTriggerBase
    {
        Subject<Unit> update;

        /// <summary>Update is called every frame, if the MonoBehaviour is enabled.</summary>
        void Update()
        {
            if (update != null) update.OnNext(Unit.Default);
        }

        /// <summary>Update is called every frame, if the MonoBehaviour is enabled.</summary>
        public IObservable<Unit> UpdateAsObservable()
        {
            return update ?? (update = new Subject<Unit>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (update != null)
            {
                update.OnCompleted();
            }
        }
    }
}
```

![2020-02-23-16](https://user-images.githubusercontent.com/85855054/125711782-5cb1cf4e-bd61-4303-a80e-96aaa40dee25.png)

이처럼 UpdateAsObservable을 호출하는 것으로 ObservableUpdateTrigger라는 컴포넌트를 해당 GameObject에 붙이고,  
ObservableUpdateTrigger에서 실행되는 Update()를 내부에 가지는 Subject를 사용하여 단지 이벤트를 발행하는 구조고 되어있다.

주의 할점은 두 가지가 있다.

1. ObservableUpdateTrigger라는 컴포넌트가 갑자기 증가하더라도 그것은 정상 동작이므로 삭제하지 말자
2. 1개의 GameObject마다 1개의 ObservableUpdateTrigger를 공유하고 있기 때문에 UpdateAsObservable 자체를 무수히 Subscribe해도 그다지 비용 증가는 없다.

## Observable.EveryUPdate를 이용하는 방법

```C#
using UniRx;
using UnityEngine;

public class UpdateSample : MonoBehaviour
{
    private void Start() =>
        Observable.EveryUpdate()
            .Subscribe(_ => Debug.Log("Update!")); // 발행되는 형태 long(Subscribe 후 경과한 프레임 )
}

```

기본적으로 사용법은 이전의 UpdateAsObservable와 같다. 하지만 가장 큰 차이점은 Observable.EveryUpdate()는 스스로 OnCompleted를 발행하지 않는다.  
즉 반드시 직접 스트림의 수명 관리를 해야 한다.

### 구조

Observable.EveryUpdate()는 UniRx의 기능 중 하나인 "마이크로코루틴"을 이용하여 동작하며, 구조는 UpdateAsObservable에 비해 다소 복잡하다.  
Observable.EveryUpdate()는 호출 될 때마다 싱글톤 상에서 코루틴을 시작한다.  
이 코루틴은 수동으로 멈추지 않는 한 계속 실행되기 때문에 스트림의 수명 관리를 제대로 해야한다.

장점은 2가지 있다.

-   싱글톤에서 작동하기 때문에 게임 진행 내내 존재하는 스트림을 생성할 수 있다.
-   대량의 Subscribe해도 성능이 저하되지 않는다. ([마이크로 코루틴의 성질](http://neue.cc/2016/05/14_529.html))

또한 UniRx가 관리하는 싱글톤은 "MainThreadDispatcher"라는 GameObject이다. ( 절대로 임의대로 삭제하지 말자. )

## UpdateAsObservable()와 Observable.EveryUpdate()의 구분

-   UpdateAsObservable(): GameObject가 파기되면 자동으로 멈춘다
-   Observable.EveryUpdate(): 성능상으로 이점이 있지만 Dispose를 수동으로 호출할 필요가 있다.

UpdateAsObservable를 사용하면 좋을 것 같은 장소

-   GameObject에 연관된 스트림을 이용한다.
    -   OnDestroy시 OnCompleted가 발행되므로 수명 관리가 편하다

Observable.EveryUpdate()를 사용하면 좋을 것 같은 장소

-   GameObject를 이용하지 않는 Pure한 Class에서 Update 이벤트를 이용하고 싶을 때
    -   싱글톤을 통해 Update 이벤트를 가져올 수 있으므로 MonoBehaviour를 상속하지 않아도 Update 이벤트를 사용할 수 있다.
-   게임 중에 항상 존재하고 작동하는 스트림을 준비하고 싶을 때
    -   싱글톤을 사용하고 있기 때문에 OnCompleted가 자동으로 발동하지 않는다
    -   예시) [UniRx로 화면 상단에 FPS 카운터 만들기](https://tech.lonpeach.com/2019/10/23/UniRx-FPS-Counter/)
-   대량의 Update() 호출이 필요할 때
    -   소량의 Update() 호출보다 압도적으로 성능이 나온다.

Observable.EveryUpdate() 쪽이 성능은 좋지만, Dispose를 해야 되는 단점이 있다.  
에러가 나서 스트림이 멈추면 좋지만, 만약 계속 작동하는 경우이다. ( 이 경우 쓰레기 스트림이 대량으로 뒤에서 작동하고 있는다고 생각하면 된다. )

# Update를 스트림으로 변환하는 이유

UniRx를 이용해야 하는 이유중 1개는 "Update를 스트림으로 변환 할 수 있다"는 점이다.

-   UniRx 오퍼레이터를 이용하여 로직을 작성 할 수 있게 된다.
-   로직의 처리 단위가 명확해진다.

## 오퍼레이터를 이용한 로직의 작성

UniRx는 시간에 관련된 오퍼레이터가 다수 준비되어 있기 때문에 UniRx 스트림에서 논리를 작성하고 나면 시간의 관계 논리를 간결하게 기술 할 수 있다.

예를 들어, 버튼을 누르고 있는 동안 일정 간격으로 공격하는 처리를 한다고 할때 즉, 슈팅 게임의 총알 발사등으로 예를 들자면

버튼을 누르고 있는 동안 n초마다 총알을 발사한다라는 상황이다.

이를 UniRx를 이용하지 않고 구현하는 경우 마지막으로 실행한 시간을 기록하여 매 프레임 비교하는 복잡한 구현이 필요하다.

이를 UniRx를 이용하여 구현하게 되면

```C#
using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class UpdateSample3 : MonoBehaviour
{
    // 실행 간격
    [SerializeField]
    private float intervalSeconds = 0.25f;

    private void Start() =>
        // ThrottleFirst는 마지막으로 실행하고
        // 일정 시간 OnNext를 차단하는 오퍼레이터
        this.UpdateAsObservable()
            .Where(_ => Input.GetKey(KeyCode.Z))
            .ThrottleFirst(TimeSpan.FromSeconds(intervalSeconds))
            .Subscribe(_ => Attack());

    private void Attack() => Debug.Log("Attack");
}

```

이렇게 UniRx를 사용하면 게임 로직을 선언적으로 간결하게 작성 할 수 있다.

## 논리가 명확해진다

Unity에서 개발을 진행 하면, Update() 내에서 게임 로직이 담겨 엉망이 되어가는 경우가 대부분이다.

UniRx를 사용하면 이와 같은 로직도 수정 가능하다.

### 이동, 점프, 착지시 효과음의 재생을 하는 로직의 예

UniRx없이 작성

```C#
using System;
using UnityEngine;

public class Sample : MonoBehaviour
{
    private CharacterController characterController;

    // 점프 중 플래그
    private bool isJumping;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (!isJumping)
        {
            var inputVector = new Vector3(
                Input.GetAxis("Horizontal"),
                0,
                Input.GetAxis("Vertical")
            );

            if (inputVector.magnitude > 0.1f)
            {
                var dir = inputVector.normalized;
                Move(dir);
            }
            if (Input.GetKeyDown(KeyCode.Space) && characterController.isGrounded)
            {
                Jump();
                isJumping = true;
            }
        }
        else
        {
            if (characterController.isGrounded)
            {
                isJumping = false;
                PlaySoundEffect();
            }
        }
    }
    void Jump()
    {
        // Jump 처리
    }
    void PlaySoundEffect()
    {
        // 효과음 재생
    }
    void Move(Vector3 direction)
    {
        // 이동 처리
    }
}

```

UniRx를 사용하여 작성

```C#
using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class Sample : MonoBehaviour
{
    private CharacterController characterController;

    // 점프 중 플래그
    private BoolReactiveProperty isJumping = new BoolReactiveProperty();

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // 점프 중이 아니면 이동
        this.UpdateAsObservable()
            .Where(_ => !isJumping.Value)
            .Select(_ => new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")))
            .Where(x => x.magnitude > 0.1f)
            .Subscribe(x => Move(x.normalized));

        // 점프 중이 아니라면 점프
        this.UpdateAsObservable()
            .Where(_ => Input.GetKeyDown(KeyCode.Space) && !isJumping.Value && characterController.isGrounded)
            .Subscribe(_ =>
            {
                Jump();
                isJumping.Value = true;
            });

        // 착지 플래그가 변화 할때 점프 중 플래그를 리셋
        characterController
            .ObserveEveryValueChanged(x => x.isGrounded)
            .Where(x => x && isJumping.Value)
            .Subscribe(_ => isJumping.Value = false)
            .AddTo(gameObject);

        // 점프 중 플래그가 false가 되면 효과음을 재생
        isJumping.Where(x => !x)
            .Subscribe(_ => PlaySoundEffect());
    }

    void Jump()
    {
        // Jump 처리
    }

    void PlaySoundEffect()
    {
        // 효과음 재생
    }

    void Move(Vector3 direction)
    {
        // 이동 처리
    }
}

```

UniRx를 사용하지 않는 경우 Update 내에서 여러 작업을 함께 작성해야 하기 때문에 if문에 의해 중첩이 발생하거나 변수의 범위가 모호해진다.

하지만, UniRx를 사용하여 Update를 스트림화 하는 경우 로직 단위로 처리를 분할하고 나열해 기술할 수 있게 되었고, 변수의 범위도 스트림 내에 닫힌 구현이 되었다.

이와 같이 Update를 스트림화 하는 것으로 처리를 적절한 단위로 구분하여 기술할 수 있게 되며, 변수의 범위도 명확히 할 수 있다.

# 정리

Update()를 스트림으로 변환하는 방법은 2가지

-   일반적인 용도로 사용하는 경우 UpdateAsObservable()를 하면 된다.
-   특수 용도의 경우 Observable.EveryUpdate()를 사용 하면 된다.

Update()를 스트림으로 변환하면 로직을 설명하기 쉬워진다.

-   UniRx 오퍼레이터를 게임로직에 그대로 사용할 수 있다.
-   선언적으로, 간결하고 읽기 쉽게 작성할 수 있게 된다.

# 추가

## ObserveEveryValueChanged에 대해

```C#
var charcterController = GetComponent<CharacterController>();

// CharacterController의 IsGrounded을 감시
// false → true가 되면 로그출력
charcterController
    .ObserveEveryValueChanged(c => c.isGrounded)
    .Where(x => x)
    .Subscribe(_ => Debug.Log("착지!"))
    .AddTo(gameObject);

// ↑ 코드는 ↓와 거의 동일
Observable.EveryUpdate()
    .Select(_=>charcterController.isGrounded)
    .DistinctUntilChanged()
    .Where(x=>x)
    .Subscribe(_ => Debug.Log("착지!"))
    .AddTo(gameObject);
```

ObserveEveryValueChange는 감시 대상의 오브젝트를 [약 참조(WeakReference)](https://docs.microsoft.com/ko-kr/dotnet/api/system.weakreference?view=netframework-4.8)에서 참조 한다.

즉, ObservableEveryValueChanged의 모니터링은 GC의 참조 카운트에 포함되지 않습니다. 또한 ObserveEveryValueChanged는 감시 대상의 객체가 GC에 회수되면 OnCompelted를 자동으로 발행한다.

이 점에 유의하여 ObserveEveryValueChanged를 사용하면 된다.
