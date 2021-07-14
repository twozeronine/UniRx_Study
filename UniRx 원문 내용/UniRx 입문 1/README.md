# 소개 UniRx란 ?

## Reactive Extensions (이하 Rx)는 요점만 말하면 다음과 같은 라이브러리 입니다.

MiscrosoftResearch가 개발하고 있던 C#용 비동기 처리를 위한 라이브러리
디자인 패턴 중 하나인 Observer 패턴을 기반으로 설계되어 있다.  
시간에 관련된 처리 및 실행 타이밍이 중요한 곳에서 처리를 쉽게 작성할 수 있도록 되어 있다.  
완성도가 높고 Java, JavaScript, Swift 등 다양한 언어로 포팅되어 있다.  
UniRx는 Rx를 기반으로 Unity에 이식된 라이브러리이며, 본가 .NET Rx에 비해 다음과 같은 차이가 있습니다.

## Unity C#에 최적화되어 만들어져있다.

Unity 개발에 유용한 기능이나 오퍼레이터가 추가적으로 구현되어 있다.  
ReactiveProperty 등이 추가되어 있다.  
철저하게 성능 튜닝이 진행되어, 원래 .NET Rx보다 메모리 퍼포먼스가 더 좋다.

# event와 UniRx

## event

C# 표준 기능의 하나인 event는 어떤 타이밍에서 메세지를 확인하고 다른 위치에 쓴 처리를 실행시킬 수 있는 기능이다.

```C#
// event를 발행하는 측

using System.Collections;
using UnityEngine;

/// <summary>
/// 100에서 카운트 다운 값을 보고하는 샘플
/// </summary>
public class TimeCounter : MonoBehaviour
{
    /// <summary>
    /// 이벤드 핸들러 (이벤트 메시지의 형식 정의)
    /// </summary>
    public delegate void TimerEventHandler(int time);

    /// <summary>
    /// 이벤트
    /// </summary>
    public event TimerEventHandler OnTimeChanged;

    private void Start()
    {
        // 타이머 시작
        StartCoroutine(TimerCoroutine());
    }

    private IEnumerator TimerCoroutine()
    {
        // 100에서 카운트 다운
        var time = 100;
        while (time > 0)
        {
            time--;

            // 이벤트 알림
            OnTimeChanged(time);

            // 1초 기다리는
            yield return new WaitForSeconds(1);
        }
    }
}

//event listener
using UnityEngine;
using UnityEngine.UI;

public class TimerView : MonoBehaviour
{
    // 각 인스턴스는 인스펙터에서 설정
    [SerializeField] private TimeCounter timeCounter;
    [SerializeField] private Text counterText; // UGUI의 Text

    private void Start()
    {
        // 타이머 카운터가 변화한 이벤트를 받고 UGUI Text를 업데이트
        timeCounter.OnTimeChanged += time => // "=>" 는 람다식이라는 익명 함수 표기법
        {
            // 현재 타이머 값을 UI에 반영
            counterText.text = time.ToString();
        };
    }
}

```

## UniRx

UniRx는 event의 완전한 상위 호환이며, event에 비해 보다 유연한 기술을 사용할 수 있다.

```C#
//이벤트 게시자
using System;
using System.Collections;
using UniRx;
using UnityEngine;

/// <summary>
/// 100에서 카운트 다운하고 Debug.Log에 그 값을 표시하는 샘플
/// </summary>
public class TimeCounterRx : MonoBehaviour
{
    // 이벤트를 발행하는 핵심 인스턴스
    private Subject<int> timerSubject = new Subject<int>();

    // 이벤트의 구독자만 공개
    public IObservable<int> OnTimeChanged => timerSubject;

    private void Start() => StartCoroutine(TimerCoroutine());

    private IEnumerator TimerCoroutine()
    {
        // 100에서 카운트 다운
        var time = 100;
        while (time > 0)
        {
            time--;

            // 이벤트를 발행
            timerSubject.OnNext(time);

            // 1초 기다리는
            yield return new WaitForSeconds(1);
        }
    }
}

//이벤트 구독자

using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class TimerViewRx : MonoBehaviour
{
    // 각 인스턴스는 인스펙터에서 설정
    [SerializeField] private TimeCounterRx timeCounter;
    [SerializeField] private Text counterText; // UGUI의 Text

    private void Start() =>
        // 타이머 카운터가 변화한 이벤트를 받고 UGUI Text를 업데이트
        timeCounter.OnTimeChanged.Subscribe(time =>
        {
            // 현재 타이머 값을 ui에 반영
            counterText.text = time.ToString();
        });
}

```

위의 event로 구현하고 있던 코드를 UniRx로 변환한 코드이다.

event 대신 Subject라는 클래스가 이벤트 핸들러를 등록할 때 Subscribe 하는 방법이 등장한다.

즉, Subject가 이벤트 구조의 핵심이 되고, Subject에 값을 전달(OnNext)하고 Subject에 구독(Subscribe)해서 메시지를 전달 될 수 있는 시스템으로 되어있다.

## OnNext와 Subscribe

OnNext와 Subscribe는 모두 Subject에 구현된 메서드이며, 각각 다음과 같은 동작을 하고있다.

- Subscribe: 메시지 수신시 실행을 함수에 등록한다.
- OnNext: Subscribe에 등록 된 함수에 메시지를 전달하고 실행한다.

```C#

//OnNext와 Subscribe

private void Start()
{
    // Subject 작성
    Subject<string> subject = new Subject<string>();

    // 3회 Subscribe
    subject.Subscribe(msg => Debug.Log("Subscribe1 : " + msg));
    subject.Subscribe(msg => Debug.Log("Subscribe2 : " + msg));
    subject.Subscribe(msg => Debug.Log("Subscribe3 : " + msg));

    // 이벤트 메시지 발행
    subject.OnNext("helloWorld!");
    subject.OnNext("hello!");

    // 실행결과
    /*
    Subscribe1 : helloWorld!
    Subscribe2 : helloWorld!
    Subscribe3 : helloWorld!
    Subscribe1 : hello!
    Subscribe2 : hello!
    Subscribe3 : hello!
    */

}

```

## IObserver와 IObservable

이전에 Subject에는 OnNext와 Subscribe라는 2개의 메소드가 구현되어있다고 했으나 대략적인 설명이며, 정확한 설명이 아니다.

정확히 설명하자면 Subject는 IObserver 인터페이스와 IObservable 인터페이스 2개를 구현하고 있다.

### IObserver 인터페이스

IObserver 인터페이스는 Rx에서 "이벤트 메시지를 발행 할 수 있다" 라는 행동을 정의한 인터페이스 이다.

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

IObserver는 보시는 것 처럼, “OnNext”, “OnError”, “OnCompleted”의 3개의 메서드 정의만 있는 굉장히 간단한 인터페이스로 되어 있다.  
방금 전까지 이용했던 OnNext 메서드는 사실 이 IObserver에 정의된 메서드다.

OnError은 “발생한 오류(Exception)을 알리는 메시지를 발행하는 메서드”이며, OnCompleted는 “메시지의 발행이 완료되었음을 알리는 메서드” 이다.

### IObservable 인터페이스

IObservable 인터페이스는 Rx에서 "이벤트 메시지를 구독 할 수 있다"라는 행동을 정의한 인터페이스 이다.

```C#
using System;

namespace UniRx
{
    public interface IObservable<T>
    {
        IDisposable Subscribe(IObserver<T> observer);
    }
}
```

### (보충) Subscribe의 생략 호출

```C#
subject.Subscribe(msg => Debug.Log("Subscribe1:" +msg ));
```

분명 위 인터페이스 IObservable의 Subscribe 메소드에서는 인수로 IObserver를 받고 있는데, 지금은 무명 메서드를 받고있다.  
UniRx에서는 OnNext, OnError, OnCompleted의 3개이 메시지 중에 필요한 메시지만을 이용할 수 있는 Subscribe의 생략 호출용 메서드가 IObservable에 준비되어 있다.

실제로 UniRx를 이용할 때는 이 생략 호출을 사용하는 경우가 대부분이며, Subscribe(IObserver\<T> observer) 호출은 거의 없다.

```C#

// Subscribe 생략 호출 예

// OnNext만
subject.Subscribe(msg => Debug.Log("Subscribe1:" + msg));

// OnNext & OnError
subject.Subscribe(
    msg => Debug.Log("Subscribe1:" + msg),
    error => Debug.LogError("Error" + error));

// OnNext & OnCompleted
subject.Subscribe(
    msg => Debug.Log("Subscribe1:" + msg),
    () => Debug.Log("Completed"));

// OnNext & OnError & OnCompleted
subject.Subscribe(
    msg => Debug.Log("Subscribe1:" + msg),
    error => Debug.LogError("Error" + error),
    () => Debug.Log("Completed"));

```

# 오퍼레이터

## Where

UniRx에는 다양한 오퍼레이터가 준비되어 있으며 그중 메시지를 필터링 하는 Where 오퍼레이터를 사용해보자

```C#

// 문자열을 발행하는 Subject
Subject<string> subject = new Subject<string>();

// Enemy만 필터링
subject
    .Where(x => x == "Enemy") // 필터링 오퍼레이터
    .Subscribe(x => Debug.Log($"플레이어가 {x}에 충돌했습니다."));

// 이벤트 메시지 발급
// 플레이어가 언급한 개체의 Tag가 발행되었다는 가정
subject.OnNext("Wall");
subject.OnNext("Wall");
subject.OnNext("Enemy");
subject.OnNext("Enemy");

/* 실행결과
플레이어가 Enemy에 충돌했습니다.
플레이어가 Enemy에 충돌했습니다.
*/

```

## 다양한 오퍼레이터

- 필터링 Where
- 메시지 변환 Select
- 중복을 제거하는 Distinct
- 일정 개수가 올때까지 대기하는 Buffer
- 단시간에 함께 온 경우 처음만 사용하는 ThrottleFirst

이 외에도 [UniRx가 제공하는 모든 오퍼레이터 목록 ](https://qiita.com/toRisouP/items/3cf1c9be3c37e7609a2f)을 참고하자

# 스트림

UniRx에서 "스트림"은 메시지가 발행된 후 Susbscribe에 도달 할때까지의 일련의 처리 흐름을 표현하는 단어이다 ( 일반적인 Rx에서도 이와같은 기능을 하는것을 스트림이라고 한다.)

오퍼레이터를 결합하여 스트림을 구축, Subscribe하여 스트림을 실행한다. OnCompleted를 발행하여 스트림을 정지시킨다.

### 나만의 Where 오퍼레이터 MyFilter를 직접 구현해 보기

```C#

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 필터링 오퍼레이터
/// </summary>
public class MyFilter<T> : IObservable<T>
{
    /// <summary>
    /// 상류가 되는 Observable
    /// </summary>
    private IObservable<T> _source;

    /// <summary>
    /// 판정식
    /// </summary>
    private Func<T, bool> _conditionalFunc;

    public MyFilter(IObservable<T> source, Func<T, bool> conditionalFunc)
    {
        _source = source;
        _conditionalFunc = conditionalFunc;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        // Subscribe되면 MyFilterOperator 본체를 만들어 반환한다.
        return new MyFilterInternal(this, observer).Run();
    }

    // Observer로 MyFilterInternal이 실제로 작동하는 곳
    private class MyFilterInternal : IObserver<T>
    {
        private MyFilter<T> _parent;
        private IObserver<T> _observer;
        private object _lockObject = new object();

        public MyFilterInternal(MyFilter<T> parent, IObserver<T> observer)
        {
            _parent = parent;
            _observer = observer;
        }

        public IDisposable Run()
        {
            return _parent._source.Subscribe(this);
        }

        public void OnNext(T value)
        {
            lock (_lockObject)
            {
                if (_observer == null)
                {
                    return;
                }

                try
                {
                    // 같은 경우에만 OnNext를 통과
                    if (_parent._conditionalFunc(value))
                    {
                        _observer.OnNext(value);
                    }
                }
                catch (Exception e)
                {
                    // 도중에 에러가 발생하면 에러를 전송
                    _observer.OnError(e);
                    _observer = null;
                }
            }
        }

        public void OnError(Exception error)
        {
            lock (_lockObject)
            {
                // 오류를 전파하고 정지
                _observer.OnError(error);
                _observer = null;
            }
        }

        public void OnCompleted()
        {
            lock (_lockObject)
            {
                // 정지
                _observer.OnCompleted();
                _observer = null;
            }
        }
    }
}

```

### 나만의 Where 오퍼레이터 MyFilter를 사용해보기

```C#
using System;

// 인스턴스화 시키지 않기 위하여 오퍼레이터 체인으로 Filter를 사용할 수 있도록 확장 메서드를 만듬.
public static class ObservableOperators
{
    public static IObservable<T> FilterOperator<T>(this IObservable<T> source, Func<T, bool> conditionalFunc)
        => new MyFilter<T>(source, conditionalFunc);
}

private void Start()
{
    // 문자열을 발행하는 Subject
    Subject<string> subject = new Subject<string>();

    // filter를 끼고 Subscribe 보면
    subject
        .FilterOperator(x => x == "Enemy")
        .Subscribe(x => Debug.Log(string.Format("플레이어가 {0}에 충돌했습니다", x)));

    // 이벤트 메시지 발급
    // 플레이어가 언급 한 개체의 Tag가 발행되어, 같은 가정
    subject.OnNext("Wall");
    subject.OnNext("Wall");
    subject.OnNext("Enemy");
    subject.OnNext("Enemy");
}

/* 실행 결과
플레이어가 Enemy에 충돌했습니다
플레이어가 Enemy에 충돌했습니다
*/

```
