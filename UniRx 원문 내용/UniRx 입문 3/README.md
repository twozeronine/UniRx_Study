# UniRx 입문 3 - 스트림 소스를 만드는 방법

# 스트림의 소스 (메시지 게시자)는 ?

UniRx의 스트림은 다음 3가지로 구성되어 있습니다

1. 메시지를 발행하는 소스가 되는 것 ( Subject 등 )
2. 메시지 전파하는 오퍼레이터 ( Where, Select 등 )
3. 메시지를 구독하는 것 ( Subscribe )

# 스트림 소스가 될 수 있는 목록

스트림 소스를 준비하는 방법은 여러가지가 있다. UniRx에서 제공해주는 스트림 소스를 이용해도 좋으며, 직접 스트림 소스를 만들 수도 있다.

-   Subject 시리즈를 사용
-   ReactiveProperty 시리즈를 사용
-   팩토리 메서드 시리즈를 사용
-   UniRx.Triggers 시리즈를 사용
-   코루틴을 변환하여 사용
-   UGUI 이벤트를 변환하여 사용
-   기타 UniRx에서 준비되어 있는 것을 사용

## Subject 시리즈

| Subject         | 기능                                                                                                                                    |
| :-------------- | :-------------------------------------------------------------------------------------------------------------------------------------- |
| Subject         | OnNext가 실행되면 값을 발행한다.                                                                                                        |
| BehaviorSubject | 마지막으로 발행 된 값을 캐쉬하고 나중에 Subscribe 될 때 그 캐시를 반환해 준다. 초기 값을 설정 할 수도 있다.                             |
| ReplaySubject   | 과거 모든 발행된 값을 캐쉬하고 나중에 Subscribe 될 때 캐시를 모두 정리해 발행한다.                                                      |
| AsyncSubject    | OnNext를 즉시 발행하지 않고 내부에 캐쉬하고 OnCompleted가 실행 된 시간에 마지막 OnNext 하나만 발행한다. 다른 언어에서 Future 및 Promise |

AsyncSubject는 다른 언어에서 Future나 Promise 같은 기능. 비동기 처리를 하고 싶을때 이용할 수 있다.

## ReactiveProperty 시리즈

### ReactiveProperty

ReactiveProperty\<T>는 변수에 Subject의 기능을 붙인 것이다.

```C#
// int 형의 ReactiveProperty
var rp = new ReactiveProperty<int>(10); // 초기값 지정 가능

// 일반적으로 대입하거나 값을 읽을 수 있다.
rp.Value = 20;
var currentValue = rp.Value;

// Subscribe 할 수 있다. ( Subscribe시 현재 값도 발행된다 )
rp.Subscribe(x => Debug.Log(x));

// 값을 다시 설정할때 OnNext가 발행 된다.
rp.Value = 30;

/* 실행 결과
20
30
*/
```

또한 ReactiveProperty는 인스펙터 뷰에 표시하여 이용할 수 있습니다.  
이 경우 제네릭 버전이 아닌 각각의 형태의 전용 ReactivePropertyProperty를 사용해야 합니다.

```C#
using UniRx;
using UnityEngine;

public class TestReactiveProperty : MonoBehaviour
{
    // int형의 ReactiveProperty ( 인스펙터 뷰에 나오는 버전 )
    [SerializeField]
    private IntReactiveProperty playerHealth = new IntReactiveProperty(100);

    private void Start() => playerHealth.Subscribe(x => Debug.Log(x));
}

```

## ReactiveCollection

ReactiveCollection\<T>는 ReactiveProperty와 같은 것이며, 상태의 변화를 알리는 기능이 내장된 List이다.

ReactiveCollection은 보통의 List처럼 쓸 수 있는 데다 상태의 변화를 Subscribe 할 수 있도록 되어 있다.

준비되어 있는 이벤트는 다음과 같다.

-   요소의 추가
-   요소의 제거
-   요소 수의 변화
-   요소 재정의
-   요소의 이동
-   목록 지우기

```C#
var collection = new ReactiveCollection<string>();

collection
    .ObserveAdd()
    .Subscribe(x =>
    {
        Debug.Log($"Add [{x.Index}] = {x.Value}");
    });

collection
    .ObserveRemove()
    .Subscribe(x =>
    {
        Debug.Log($"Remove [{x.Index}] = {x.Value}");
    })

collection.Add("Apple");
collection.Add("Baseball");
collection.Add("Cherry");
collection.Remove("Apple");

Add [0] = Apple
Add [1] = Baseball
Add [2] = Cherry
Remove [0] = Apple

```

> ReactiveDictionary\<T1, T2> : ReactiveCollection의 Dictionary 버전도 있다.

## 팩토리 메서드 시리즈

팩토리 메서드는 UniRx가 제공하는 스트림 소스 구축 메서드 군입니다.

Subject만으로는 표현할 수 없는 복잡한 스트림을 쉽게 만들 수 있는 경우가 있다.

[ReactiveX 공식 홈페이지](http://reactivex.io/documentation/operators.html#creating)에서 여러가지 팩토리 메서드 방법을 참고 할 수 있다.

### Observable.Create

Observable.Create\<T>는 자유롭게 값을 발행하는 스트림을 만들 수 있는 팩토리 메서드다.  
예를 들어, 일정한 절차에 의해 처리 호출 규칙을 이 팩토리 메서드 내부에 은폐시켜, 결과만들 스트림에서 추출 할 수 있다.

Observable.Create는 인수 Func\<IObserver<T>,IDsiposable>를 인수로 취한다.

```C#
Observable.Create<int>(observer =>
{
    Debug.Log("Start");

    for (int i = 0; i <= 100; i += 10)
    {
        observer.OnNext(i);
    }

    Debug.Log("Finished");
    observer.OnCompleted();

    return Disposable.Create(() =>
    {
        // 종료 시 처리
        Debug.Log("Dispose");
    });
}).Subscribe( x => Debug.Log(x));

/*실행 결과
start
0
10
20
30
40
50
60
70
80
90
100
Finished
Disposable
*/

```

### Observable.Start

Observable.Start는 주어진 블록을 다른 스레드에서 실행하여 결과를 1개만 발급하는 팩토리 메서드이다.  
비동기로 무엇인가를 처리를 하고 결과가 나오면 통지를 원할때 사용할 수 있다.

```C#
// 주어진 블록 내부를 다른 스레드에서 실행
Observable.Start(()=>
{
    // google의 메인 페이지를 http를 통해 get 한다.
    var req = (HttpWebRequest) WebRequest.Create("http://google.com");
    var res = (HttpWebResponse) req.GetResponse();
    using (var reader = new StreamReader(res.GetResponseStream()))
    {
        return reader.ReadToEnd();
    }
})
.ObserveOnMainThread() // 메시지를 다른 스레드에서 Unity 메인 스레드로 전환
.Subscribe(x => Debug.Log(x));

```

주의할 점으론 Observable.Start 처리를 다른 스레드에서 실행하고 그 쓰레드에서 그대로 Subscribe 내 함수를 실행한다.  
이것은 스레드로부터 안전하지 않은 Unity에서 문제를 일으킬 수 있으므로 주의해야 한다.

만약 메시지를 다른 스레드에서 메인 스레드로 전환하고자 하는 경우 ObserveOnMainThread의 오퍼레이터를 이용하자.  
이 오퍼레이터 사용하면, 이후 Unity 메인 스레드에서 실행되도록 변환된다.

### Observable.Timer/TimerFrame

Observable.Timer는 일정 시간 후에 메시지를 발행하는 간단한 팩토리 메서드 이다.

실제 시간을 지정하는 경우 Timer를 사용하고 Unity의 프레임 수로 지정하는 경우 TimerFrame을 이용한다.

Timer,TimerFrame은 인수에 따라 행동이 달라진다.  
1개 밖에 지정하지 않으면 OnShot 동작으로 종료하고 2개 지정한 경우 주기적으로 메시지를 발행한다.
또한 스케줄러를 지정하여 실행하는 스레드를 지정할 수도 있다.

또한 비슷한 팩토리 메서드인 Observable.Interval/IntervalFrame도 존재한다.  
이것은 Timer/TimerFrame의 2개의 인수를 지정하는 경우의 생략 버전 같은 것으로 생각하면 된다.
Interval / IntervalFrame은 타이머를 시작할 때까지의 시간 ( 첫번째 인수 )를 지정할 수 없게 되어 있다.

```C#
// 5초 후에 메시지 발행하고 종료
Observable.Timer(TimeSpan.FromSeconds(5))
    .Subscribe(_ => Debug.Log("5초 경과했습니다."));

// 5초 후 메시지 발행 후 1초 간격으로 계속 발행
// 스스로 정지시키지 않는 한 계속 움직인다.
Observable.Timer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1))
    .Subscribe(_ => Debug.Log("주기적으로 수행되고 있습니다."))
    .AddTo(gameObject); // Timer 뒤에 AddTo 메서드를 붙여, 게임 오브젝트가 제거 될때 자동으로 Dispose가 호출되게 된다.
```

Timer, TimerFrame을 정기적으로 실행을 하고자 할때는 Dispose의 작동을 기억해야한다.  
멈추는 것을 잊지 않고 방치하면 메모리 누수와 NullReferenceException의 원인이 된다.

## UniRx.Triggers

UniRx.Triggers는 using UniRx.Triggers; 를 사용하는 스트림 소스다.  
Unity에서 제공하는 콜백 이벤트를 UniRx의 IObservable로 변환하여 제공한다.

[UniRx.Triggers-github](https://github.com/neuecc/UniRx/wiki/UniRx.Triggers)를 참고하자.

Unity가 제공하는 대부분의 콜백 이벤트를 스트림으로써 취득 가능하게 되어 있으며 GameObject가 Destroy 될때 자동으로 OnCompleted를 발급 해주는 구조로 되어있다.

```C#
using UniRx;
using UnityEngine;
using UniRx.Triggers; // 필수 추가

// <summary>
// WarpZone (라는 이름의 IsTrigger인 Collider가 붙은 영역)에
// 들어왔을때 부유하는 스크림트 (임의)
// </summary>
public class TriggersSample : MonoBehaviour
{
    private void Start()
    {
        bool isForceEnabled = true;
        var rb = GetComponent<Rigidbody>();

                // 플래그가 유요한 동안 위쪽에 힘을 가한다.
        this.FixedUpdateAsObservable()
            .Where(_ => isForceEnabled)
            .Subscribe(_ => rb.AddForce(Vector3.up * 20));

                // WarpZone에 침입하면 플래그를 활성화 한다.
        this.OnTriggerEnterAsObservable()
            .Where(x => x.gameObject.CompareTag("WarpZone"))
            .Subscribe(_ => isForceEnabled = true);

                // WarpZone에 나오면 플래그를 해제 한다.
        this.OnTriggerExitAsObservable()
            .Where(x => x.gameObject.CompareTag("WarpZone"))
            .Subscribe(_ => isForceEnabled = false);
    }
}
```

Triggers를 사용하여 Unity 콜백을 스트림으로 변환하면 모든 것을 Awake/Start에 작성할 수 있다.

## 코루틴에서 변환

코루틴에서 IObservable의 변환은 Observable.FromCoroutine을 이용하여 수행 할 수 있다.  
오퍼레이터 체인으로 복잡한 스트림을 구축하는 것보다 코루틴을 사용해 절차적으로 쓰는 경우가 더 심플하고 알기 쉬운 경우도 존재한다.

```C#
using System;
using System.Collections;
using UniRx;
using UnityEngine;

public class Example23_Timer : MonoBehaviour
{
    // <summary>
    // 일시 정지 플래그
    // </summary>
    public bool IsPaused { get; private set; }

    private void Start()
    {
        // 60초 카운트하는 스트림을 코루틴에서 만든다.
        Observable.FromCoroutine<int>(observer => TimerCoroutine(observer, 60))
            .Subscribe(t => Debug.Log(끝));
    }

    // <summary>
    // 초기 값에서 0까지 카운트하는 코루틴
    // 그러나 IsPaused 플래그가 유요한 경우는 카운트 중지
    // </summary>
    IEnumerator TimerCoroutine(IObserver<int> observer, int initializeTime)
    {
        var current = initializeTime;
        while (current > 0)
        {
            if (!IsPaused)
            {
                observer.OnNext(current--);
            }
            yield return new WaitForSeconds(1);
        }
                observer.OnNext(0);
        observer.OnCompleted();
    }
}

```

## UGUI 이벤트에서 변환

UniRx는 uGUI와 같이 사용하기 쉽다, ReactiveProperty와 결합하여 View와 Model의 관계를 굉장히 명확하게 구현할 수 있다. ( 이는 MVP패턴이라고 불린다. )

```C#
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class Example23_uGUI : MonoBehaviour
{
    // 인스펙터에서 설정
    [SerializeField] private Button button;
    [SerializeField] private InputField inputField;
    [SerializeField] private Slider slider;

    private void Start()
    {
        // uGUI의 기본 Unity 이벤트의 이름을 한 Observable이 준비되어 있다.
        button.OnClickAsObservable().Subscribe(_ =>
        {
            Debug.Log("button OnClick!");
        });

        inputField.OnValueChangedAsObservable().Subscribe(str =>
        {
            Debug.Log("inputField OnValueChanged : " + str);
        });
        inputField.OnEndEditAsObservable().Subscribe(str =>
        {
            Debug.Log("inputField OnEndEdit : " + str);
        });

        slider.OnValueChangedAsObservable().Subscribe(val =>
        {
            Debug.Log("slider value changed : " + val);
        });

        // ----------

        // 또한 이러한 방법도 있다.
        inputField.onValueChanged.AsObservable().Subscribe();

        // 이 두 기법의 차이는 Subscribe시 현재 값의 초기 값의 발행 여부이다
        // Subscribe시 초기 값이 필요한 경우는 전자를 사용하면 된다.
        inputField.OnValueChangedAsObservable(); // 초기값이 있다.
        inputField.onValueChanged.AsObservable(); // 초기값이 없다.
    }
}

```

## 기타

UniRx는 이외에도 편리한 스트림 소스를 제공해주고 있다.

### ObservableWWW

ObservableWWW는 Unity의 WWW를 스트림으로 처리 할 수 있도록 래핑 해 준 것이다. 호출하는 것으로 UniRx는 코루틴을 실행해 WWW를 처리하고 결과만 알려준다

```C#
ObservableWWW.Get("https://google.com")
             .Subscribe(x => Debug.Log(x));
```

> UniRx는 내부에 코루틴을 가지고 있다. 그 실체가 되는 GameObject는 MainThreadDispacher라는 이름으로 씬에 존재한다.  
> 이 MainTreadDispatcher를 멈추면 UniRx가 올바르게 동작하지 않게 되므로 이 GameObject를 손보는 것은 피해야 한다.

> Unity 2018.3 이상 버전부터는 ObservableWWW의 사용을 권장하지 않는다. 그 대신에 유니티에 새로 추가된 UnityWebRequest를 권장한다.  
> 실제 편안한 사용을 위해서는 UniTask 라이브러리와 같이 사용해 C# 의 Task 처럼 사용을 하거나 Task를 Observable로 변환하여 사용하면 될 것으로 보인다.

### Observable.NextFrame

다음 프레임으로 메시지를 발행 해주는 스트림을 만들 수 있다. **메시지의 발행 타이밍은 Update 타이밍이 아닌 코루틴 타이밍 이므로** 실행 타이밍이 매우 중요한 경우에는 주의해서 사용해야 한다.

> [Unity 이벤트 함수의 실행 순서 ](https://docs.unity3d.com/kr/current/Manual/ExecutionOrder.html)를 참고하자.

```C#
Observable.NextFrame()
        .Subscribe(_ => Debug.Log("다음 프레임에서 실행됩니다."));
```

업데이트 타이밍은 다음 3가지로 조절 가능하며, 기본 값은 Update이다.

-   Update (yield return null)
-   FixedUpdate (yield return new WaitForFixedUpdate())
-   EndOfFrame (yield return new WaitForEndOfFrame())

### Observable.EveryUpdate

Observable.EveryUpdate는 매 Update 타이밍을 알려주는 스트림 소스이다.  
 UniRx.Triggers의 UpdateAsObservable과 비슷하지만 이쪽은 GameObject에 붙어 Destroy시 OnCompleted가 실행되는 반면,  
 Observable.EveryUpdate는 스스로 중지하지 않는 한 씬을 거쳐도 계속 움직이는 스트림이다.

FPS 카운터와 같은 어떤 신에서도 계속 같은 스트림을 구축해야 할때 유용하다.

참고: [UniRx에서 FPS 카운터를 만들어 보기](https://tech.lonpeach.com/2019/10/23/UniRx-FPS-Counter/)

### ObservableEveryValueChanged

ObservableEveryValueChanged는 모든 객체의 파라미터를 매 프레임 모니터링하고 변화가 있었을 때에 통지하는 스트림을 생성 할 수 있다.

```C#
var characterController = GetComponent<CharacterController>();

// CharacterController의 isGrounded를 감시
// false -> true가 되면 로그 출력
characterController
    .ObserveEveryValueChanged(c => c.isGrounded)
    .Where(x => x)
    .Subscribe(_ => Debug.Log("착지!"))
    .AddTo(gameObject);

// ↑ 코드는 ↓와 거의 동의어
Observable.EveryUpdate()
    .Select(_ => characterController.isGrounded)
    .DistinctUntilChanged()
    .Where(x => x)
    .Subscribe(_ => Debug.Log("착지!"))
    .AddTo(gameObject);

// ObserveEveryValueChanged는
// EveryUpdate + Select + DistinctUntilChanged
// 의 축약 버전에 속한다.
```

# 정리

스트림 소스를 만드는 방법은 여러가지 있다.

1. Subject
2. ReactiveProperty
3. 팩토리 메서드
4. UniRx.Triggers
5. 코루틴을 변환하여 사용
6. UGUI 이벤트를 변환하여 사용
7. 기타 UniRx에 준비되어 있는 것을 사용

> Unity에서는 주로 Subject, UniRx.Triggers , ReactiveProperty, uGUI변환을 사용한다.
