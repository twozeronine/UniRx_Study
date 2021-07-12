# UniRx 입문 2 - 메시지의 종류 / 스트림의 수명

# 이전 복습

IObserver 인터페이스

```C#

using System;

namespace UniRx
{
    public interface IObserver<T>
    {
        void OnCompleted();
        void OnError(Exception error);
        void OnNext(T value);
    }
}

```

# "OnNext", "OnError", "OnCompleted"

UniRx에서 발행되는 메시지는 모두 이 3가지 중 어느 하나가 되며, 다음과 같은 용도로 이용되고 있습니다.

-   OnNext: 통상 이벤트가 발행되었을 때 통지되는 메시지
-   OnError: 스트림 처리 중 예외가 발생했음을 통지하는 메시지
-   OnCompleted: 스트림이 종료되었음을 통지하는 메시지

## OnNext 메시지

OnNext는 UniRx에서 가장 많이 사용되는 메시지이며, 보통 이벤트 통지를 나타낸다.

### 예 1) 정수값 통지

```C#
var subject = new Subject<int>();

subject.Subscribe(x => Debug.Log(x));
subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(3);
subject.OnCompleted();

/* 실행 결과
1
2
3
*/

```

### 예 2) 의미 없는 값을 통지

```C#
var subject = new Subject<Unit>();

subject.Subscribe(x => Debug.Log(x));

// Unit 형은 그 자체는 별 의미가없다.
// 메시지의 내용에 의미가 아니라 이벤트 알림 타이밍이 중요한 순간에 사용할 수 있다.
subject.OnNext(Unit.Default);

/* 실행 결과
()
*/

```

예시 2는 Uint형 이라는 특수한 형태를 발행하고 있다.

이 형태는 메시지의 내용물에 의미는 없다라는 표현을 할 때 사용한다.

이것은 이벤트가 발생된 타이밍이 중요하며, OnNext 메시지 내용은 상관 없다라는 경우에 사용할 수 있습니다.

### 예 3) 씬의 초기화 완료 후 Uint형 통지

```C#

public class GameInitializer : MonoBehaviour
{
    // Unit형 사용
    private Subject<Unit> initializedSubject = new Subject<Unit>();

    public IObservable<Unit> OnInitializedAsync => initializedSubject;

    private void Start()
    {
        // 초기화 시작
        StartCoroutine(GameInitializeCoroutine());

        OnInitializedAsync.Subscribe(_ => { Debug.Log("초기화 완료"); });
    }

    private IEnumerator GameInitializeCoroutine()
    {
        /*
            * 초기화 처리
            *
            * WWW 통신이나 개체 인스턴스화 등
            * 시간이 걸리고 무거운 처리를 여기에서 한다고 가정
            */
        yield return null;

        // 초기화 완료 통지
        initializedSubject.OnNext(Unit.Default); // 타이밍이 중요한 통지이므로 Unit로도 충분하다.
        initializedSubject.OnCompleted();
    }
}


```

코루틴으로 게임의 초기화를 수행하고 처리가 완료되면 이벤트를 발행해 통지하는 클래스 구현 예 입니다.

이러한 이벤트는 이벤트의 내용물 값이 무엇이든 상관없는 상황에서 Uint형을 사용하는 경우가 많다.

## OnError 메시지

OnError 메시지는 이름 그대로 예외가 스트림 도중에 발생했을 때에 통지되는 메시지다.

OnError 메시지 스트림 도중에 Catch하여 처리하거나 그대로 Subscribe 메서드에 도달시켜 처리할 수 있다.  
만약 OnError 메시지가 Subscribe까지 도달 한 경우, 그 스트림 구독은 종료되고 파기된다.

### 예 4) 도중에 발생한 예외를 Subscribe로 받는다

```C#

var stringSubject = new Subject<string>();

// 문자열을 스트림 중간에서 정수로 변환
stringSubject
    .Select(str => int.Parse(str)) // 숫자를 표현하는 문자열이 아닌 경우는 예외가 나온다
    .Subscribe(
        x => Debug.Log("성공:" + x), // OnNext
        ex => Debug.Log("예외가 발생:" + ex) // OnError
    );

stringSubject.OnNext("1");
stringSubject.OnNext("2");
stringSubject.OnNext("Hello"); // 이 메시지에서 예외가 발생 한다.
stringSubject.OnNext("4");
stringSubject.OnCompleted();

/* 실행 결과
성공 : 1
성공 : 2
예외가 발생 : System.FormatException : Input string was not in the correct format
*/

```

> 위 예시는 Subscribe의 오버로드 중 Subscribe(OnNext, OnError)를 받는 메서드를 이용하고 있다.

OnNext로 보내져 온 문자열을 Select 오퍼레이터(값의 변환)에서 int로 캐스팅해서 표시하는 스트림을 사용한 예다.

이와 같이 스트림 중간에 예외가 발생했다면 OnError 메시지가 발행되고 Subscribe에게 통지가 오고 있는 것을 알 수 있다.

또한 OnError을 받은 후 OnNext("4")는 처리가 되지 않는다. 결과적으로 "OnError를 Subscribe가 감지하면 스트림 구독을 중단 한다"를 기억하자.

### 예 5) 도중에 예외가 발생하면 다시 구독하기

```C#
var stringSubject = new Subject<string>();

// 문자열을 스트림 중간에서 정수로 변환
stringSubject
    .Select(str => int.Parse(str))
    .OnErrorRetry((FormatException ex) => // 예외의 형식으로 필터링 가능
    {
        Debug.Log("예외가 발생하여 다시 구독 합니다");
    })
    .Subscribe(
        x => Debug.Log("성공:" + x), // OnNext
        ex => Debug.Log("예외가 발생:" + ex) // OnError
    );

stringSubject.OnNext("1");
stringSubject.OnNext("2");
stringSubject.OnNext("Hello");
stringSubject.OnNext("4");
stringSubject.OnNext("5");
stringSubject.OnCompleted();

/*실행 결과
성공 : 1
성공 : 2
예외가 발생하여 다시 구독합니다
성공 : 4
성공 : 5
*/

```

예 5는 도중에 예외가 발생했을 경우 OnErrorRetry로 스트림을 재 구축하고 구독을 계속 하고있다.

OnErrorRetry는 OnError가 특정 예외인 경우에 다시 Subscribe를 시도해주는 예외 핸들링 오퍼레이터 이다.

|  오퍼레이터  |                                   하고 싶은 일                                    |
| :----------: | :-------------------------------------------------------------------------------: |
|    Retry     |                      OnError가 오면 다시 Subscribe 하고 싶다                      |
|    Catch     |               OnError를 받아 에러를 하고 다른 스트림으로 전환한다.                |
| CatchIgnore  | OnError을 받아 에러 처리를 한 후, OnError을 무시하고 OnCompleted로 대체하고 싶다. |
| OnErrorRetry | OnError가 오면 에러 처리를 한 후, Subscribe를 다시 하고 싶다. ( 시간 지정 가능 )  |

## OnCompleted 메시지

OnCompleted는 스트림이 완료되었기 때문에 이후 메시지를 발행하지 않겠다라는 것을 통지하는 메시지이다.

만약 OnCompleted 메시지가 Subscribe까지 도달한 경우 OnError와 마찬가지로 그 스트림의 구독은 종료되고 파기된다.  
이 성질을 이용하여 스트림에게 OnCompleted를 적절히 발행하여 올리면 구독 종료를 실행할 수 있기 때문에 스트림 뒷정리를 할 경우에는 이 메시지를 발행하도록 한다.

또한, 한번 OnCompleted를 발행한 Subjects는 재이용이 불가능하다. Subscribe 해도 금방 OnComnplted가 돌아오게 된다.

### 예 6) OnCompleted를 감지

```C#

var subject = new Subject<int>();
subject.Subscribe(
    x => Debug.Log(x),
    () => Debug.Log("OnCompleted")
);
subject.OnNext(1);
subject.OnNext(2);
subject.OnCompleted();

/* 실행 결과
1
2
OnCompleted
*/

```

> Subscribe의 오버로드 중 Subscribe(OnNext, OnCompleted)을 받는 메서드를 이용하고 있다.

예 6과 같이 Subscribe에 OnCompleted를 받은 오버로드를 사용하여 OnCompleted를 감지할 수 있다.

## Subscribe의 오버로드

Subscribe에는 여러 오버로드가 존재한다.

-   Subscribe(IObserver observer ) 기본형
-   Subscribe() 모든 메시지를 무시
-   Subscribe(Action onNext ) OnNext만
-   Subscribe(Action onNext, Action onError ) OnNext + OnError
-   Subscribe(Action onNext, Action onCompleted ) OnNext + OnCompleted
-   Subscribe(Action onNext, Action onError, Action onCompleted ) 전부

# 스트림의 구독 종료 ( Dispose )

```C#
public interface IObservable<T>
{
    IDisponsable Subscribe(IObserver<T> observer);
}
```

IDisposable는 C#에서 제공하는 인터페이스이며, 리소스 해제를 실시할 수 있도록 하기 위한 메서드 Dispose를 단지 1개 가지고 있는 인터페이스 입니다.

Subscribe의 반환값이 IDisposable이라는 것은 즉 Subscribe 메서드가 돌려주는 IDisposable의 Dispose를 실행하면 스트림의 구독을 종료 할 수 있다.

### 예 7) Dispose() 스트림의 구독 종료

```C#
var subject = new Subject<int>();

// IDispose 저장
var disposable = subject.Subscribe(x = > Debug.Log(x), () => Debug.Log("OnCompleted"));

subject.OnNext(1);
subject.OnNext(2);

// 구독중
disposable.Dispose();

subject.OnNext(3);
subject.OnCompleted();

/*실행 결과
1
2
*/

```

Dispose를 호출하여 구독을 언제든지 중단 할 수 있다.

주의해야할 점은 Dispose()를 실행해서 구독이 중단되도 OnCompleted가 발행되는 것은 아니다 라는 점이다.  
구독 중단 처리를 OnCompleted에 사용하는 경우, Dispose에서 정지시켜 버리면 실행되지 않는다.

### 예 8) 특정 스트림만 수신 거부

```C#
var subject = new Subject<int>();

// IDispose 저장
var disposable1 = subject.Subscribe(x => Debug.Log("스트림1:" + x), () => Debug.Log("OnCompleted"));
var disposable2 = subject.Subscribe(x => Debug.Log("스트림2:" + x), () => Debug.Log("OnCompleted"));
subject.OnNext(1);
subject.OnNext(2);

// 스트림 1만 구독종료
disposeable1.Dispose();

subject.OnNext(3);
subject.OnCompleted();

/*실행 결과
스트림1 : 1
스트림2 : 1
스트림1 : 2
스트림2 : 2
스트림2 : 3
2 : OnCompleted
*/
```

OnCompleted를 실행하면 모든 스트림을 구독 종료하게 되지만 Dispose를 사용하면 일부 스트림만 종료시킬 수 있습니다.

## 스트림의 수명과 Subscribe 종료 타이밍

UniRx를 사용하는데 있어서 특히 조심하지 않으면 안되는 것이 스트림의 라이프 사이클입니다.

객체가 자주 출현과 삭제를 반복하는 Unity에서는, 특히 이것을 의식하지 않으면 퍼포먼스 저하나 에러에 의한 오동작을 일으키게 된다.

## 스트림의 실체는 구가 가지고 있는가 ?

스트림의 수명 관리를 하는데, 그 스트림은 누구의 소요인가를 의식할 필요가 있다.

기본적으로, 스트림의 실체는 Subject이며 Subject가 파기되면 스트림도 파기된다.

Subscribe란 Subject에 함수를 등록하는 과정이었다. 즉, 스트림의 실체는 Subject가 내부에 유지하는 "호출 함수목록(및 그 함수에 연관된 메소드 체인)"으로, Subject가 스트림을 관리하는 것입니다.

Subject가 파기되면 스트림도 모두 파기된다. 반대로 말하면, Subject가 남아있는 한 스트림은 계속 실행 된다.  
스트림이 참조하고 있는 객체를 스트림보다 먼저 버리고 방치해 버리면 뒤에 스트림이 계속 동작 상태가 되기 때문에 성능 저하를 일으키거나, 메모리 누수가 발생하거나, 심각한 경우 NullReferenceException을 발생시켜 게임을 정지시킬 가능성도 있다.

스트림의 수명 관리는 세심한 주의를 기울여야 한다. 사용이 끝나면 반드시 Dispose를 호출하거나 OnCompleted를 발행하여야 한다.

### 예 9) 플레이어의 좌표를 이벤트 알림으로 갱신

액션 게임을 가정하고 생각해보자

-   플레이어를 조작 할 수 있다.
-   타이머로 시간을 카운트 하고 있다.
-   타이머가 0이 되었을 때 플레이러를 초기 좌표로 되돌린다.
-   플레이어는 화면 밖으로 나오면 사망(소멸)한다.

```C#

using System;
using System.Collections;
using UniRx;
using UnityEngine;

/// <summary>
/// 카운트 다운하고 그때 값을 통지한다.
/// 3,2,1,0,(OnCompleted) 이런식으로 이벤트가 날라간다.
/// </summary>
public class TimeCounter : MonoBehaviour
{
    [SerializeField] private int TimeLeft = 3;

    // 타이머 스트림의 실체는 이 Subject
    private Subject<int> timerSubject = new Subject<int>();

    public IObservable<int> OnTimeChanged => timerSubject;

    private void Start()
    {
        StartCoroutine(TimerCoroutine());

        // 현재의 카운트를 표시
        timerSubject.Subscribe(x => Debug.Log(x));
    }

    private IEnumerator TimerCoroutine()
    {
        yield return null;

        var time = TimeLeft;
        while (time >= 0)
        {
            timerSubject.OnNext(time--);
            yield return new WaitForSeconds(1);
        }
        timerSubject.OnCompleted();
    }
}


// 플레이어 이동 처리
// 타이머가 0이되면 초기 좌표로 돌린다.
public class PlayerMover : MonoBehaviour
{
    [SerializeField] private TimeCounter _timeCounter;
    private float _moveSpeed = 10.0f;

    private void Start()
    {
        // 타이머 구독
        _timeCounter.OnTimeChanged
            .Where(x => x == 0) // 타이머가 0이 되었을 때만 실행
            .Subscribe(_ =>
            {
                // 타이머가 0이되면 초기 좌표로 돌린다
                transform.position = Vector3.zero;
            });
    }

    private void Update()
    {
        // 오른쪽 화살표를 누르고 있는 동안 이동
        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.position += new Vector3(1, 0, 0) * _moveSpeed * Time.deltaTime;
        }

        // 화면 밖으로 나오면 제거
        if (transform.position.x > 10)
        {
            Debug.Log("화면 밖에 나왔다!");
            Destroy(gameObject);
        }
    }
}


```

#### 실행 결과 1 / 타이머 0에서 플레이어가 생존 한 경우

![UniRx 예시 -1-](https://user-images.githubusercontent.com/85855054/125230709-53d12a80-e314-11eb-892e-95c92479bc04.gif)

타이머가 0이 되었을 때 플레이어의 좌표가 제대로 초기 위치로 이동하게 됩니다.

이와 같이 플레이어가 생존한 경우는 이 코드가 제대로 실행됩니다.

#### 실행 결과 2 / 타이머 0에서 플레이어가 제거 된 경우

![UniRx 예시 -2-](https://user-images.githubusercontent.com/85855054/125230713-559aee00-e314-11eb-9513-2558cfc11453.gif)

타이머 0의 시점에서 플레이어가 제거 된 경우 이 코드에서는 MissingReferenceException 예외가 발생하고 있습니다.

즉, 이 코드는 타이머 0 시점에서 플레이어가 소멸한 경우 이상이 있다는 것을 알 수 있다.

#### 실행 결과 2의 오류 원인

```C#
    private void Start()
    {
        // 타이머 구독
        _timeCounter.OnTimeChanged
            .Where(x => x == 0) // 타이머가 0이 되었을 때만 실행
            .Subscribe(_ =>
            {
                // 타이머가 0이되면 초기 좌표로 돌린다
                transform.position = Vector3.zero;
            });
    }

```

원인은 playerMover에 있다.

스트림의 실체는 Subject가 보관하고 있는데 PlayerMover가 삭제 된다해도 TimeCounter.timer.Subject가 여전히 삭제된 값인 transform.position에 접근하여 Vector3.zero값을 넣고 있기 때문입니다.

![UniRx 예시 -4-](https://user-images.githubusercontent.com/85855054/125231179-3cdf0800-e315-11eb-845b-5fe9773fb79c.jpeg)

이와 같이 스트림의 수명과 객체의 수명이 일치하지 않는 경우 ( 객체는 제거되었으나 여전히 그 객체를 참조하는 스트림이 있을때 ) 에러의 원이이 된다.

#### 대책

대책은 단순히 Player의 GameObject가 파괴되면 스트림의 구독을 중지하면 된다.

구현 방법에는 여러 가지가 있지만, 그중 AddTo의 사용예제가 있다.

```C#
private void Start()
{
    // 타이머 구독
    _timeCounter.OnTimeChanged
        .Where(x => x == 0) // 타이머가 0이 되었을 때만 실행
        .Subscribe(_ =>
        {
            // 타이머가 0이되면 초기 좌표로 돌린다
            transform.position = Vector3.zero;
        }).AddTo(gameObject); // 지정된 gameObject가 파기되면 Dispose 한다.
}
```

UniRx에는 AddTo 라는 메서드가 준비되어 있으며, Subscribe 뒤에 AddTo(gameObject)라고 적음으로써 지정된 GameObject가 Destroy되면 자동으로 Dispose를 호출하도록 설정할 수 있다.

![UniRx 예시 -3-](https://user-images.githubusercontent.com/85855054/125230714-559aee00-e314-11eb-8e3d-4cb36f734e8a.gif)

# 정리

**메시지의 종류는 3가지가 있다.**

-   OnNext: 보통 이벤트가 발생했을때 알림 메시지
-   OnError: 스트림을 처리하는 동안 예외가 발생한 경우 통지하는 메시지.
-   OnCompleted: 스트림이 종료되었음을 알리는 메시지

**스트림의 구독을 중단하는 패턴은 3가지가 있다.**

-   Subscribe 가 OnCompleted를 감지
-   Subscribe 가 OnError를 감지
-   Subscribe 가 반환한 IDisposable의 Dispose()를 호출

**스트림의 수명과 객체의 수명 관계는 항상 의식할 필요가 있다.**

-   객체를 지웠지만, 스트림은 살아 남은 상태는 절대적으로 피해야 한다.
-   스트림을 사용한 후에 Dispose를 호출하거나 OnCompleted를 발행하는 버릇을 붙이자.
