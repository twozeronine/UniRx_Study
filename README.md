# UniRx_Study

> [Csharp_Study](https://github.com/twozeronine/Csharp_Study) C# 공부내용 링크
> [Unity_Study](https://github.com/twozeronine/Unity_Study) Unity 공부내용 링크

Today I Learnred (TIL)

> UniRX Functional Reactive Programming(함수 반응형 프로그래밍)에 대해 공부한 내용을 정리하는 저장소.
> Microsoft에서 이미 C#용으로 만든 Rx.NET가 있지만 무겁고 기술적인 이슈로 Unity 전용으로 최적화된 UniRx를 사용한다고 한다.

Rx는 하나의 프로그래밍 방법론이고 어떤 언어라도 Rx의 개념은 똑같다 !

[UniRx를 이용한 예제 목록으로 이동](https://github.com/twozeronine/UniRx_Study/tree/main/Assets/UniRx_Practice_Scripts)

## 참고 사이트

- [ReactiveX 공식 사이트](http://reactivex.io/)
- [UniRx 공식 깃허브](https://github.com/neuecc/UniRx)
- [UniRx 소개 slideshare](https://www.slideshare.net/agebreak/160402-unirx)
- [UniRx 시작하기 slideshare](https://www.slideshare.net/agebreak/160409-unirx?from_action=save)
- [Rx와 Functional Reactive Programming으로 고성능 서버 만들기 slideshare](https://www.slideshare.net/jongwookkim/ndc14-rx-functional-reactive-programming)
- [UniRx에 대해 정리한 블로그](https://rito15.github.io/posts/unity-study-unirx/)

## Functional Reactive Programming ? 함수형 반응형 프로그래밍이란 ?

비동기적인 데이터 처리를 간단한 함수를 통해 수행하는 프로그래밍 방법론이다. Functional Reactive Porgramming의 원리를 활용해서 옵저버 패턴을 이용해 비동기적인 이벤트를 손쉽게 처리하기 위해 만들어진 API가 바로 ReactiveX이다.  
모든것을 Data Stream이라고 생각하고 흘러 들어오는 이벤트을 (Observable)이 언제, 그리고 무엇을 하는지 알기위해 구독 Subscribe하여 해당 이벤트를 처리한다.

## UniRx 활용 방법

- 이벤트의 기다림
  -- 마우스 클릭이나 버튼의 입력 타이밍에 무언가를 처리 한다
- 비동기 처리
  -- 다른 스레드에서 통신을 하거나, 데이터를 로드할 때
- 시간 측정이 판정에 필요한 처리
  -- 홀드, 더블클릭의 판정
- 시간 변화하는 값의 감시
  -- False->True가 되는 순간에 1회만 처리하고 싶을 때
- UI의 변화에 따른 동작 구현
- Update()의 로직을 모두 스트림화하여 Update() 없애기
- 코루틴과의 결합
- MVP 패턴 구현
  -- M(Model) : 내부 처리를 위한 스트림 보유
  -- V(View) : 입력 또는 UI의 변화를 감지하는 스트림 보유
  -- P(Presenter) : M, V 양측의 스트림을 구독하고, V의 이벤트를 감지하여 M에 전달하고 그 결과를 다시 V에 전달

## 스트림이란 ?

### 이벤트가 흐르는 파이프 같은 이미지

- 어렵게 말하자면, \[타임라인에 배열되어 있는 이벤트의 시퀀스 ]
- 분기 되거나 합쳐지는게 가능하다

- 코드 안에서는 IObservable\<T> 로 취급된다
  -- LINQ에서 IEnumerable\<T>에 해당

### 스트림에 흐르는 이벤트 <메시지>

- OnNext
  -- 일반적으로 사용되는 메시지
  -- 보통은 이것을 사용함.
- OnError
  -- 에러 발생시에 예외를 통지하는 메시지
- OnCompleted
  -- 스트림이 완료되었음을 통지하는 메시지

### Subscribe (스트림의 구독)

스트림의 말단에서 메시지가 올때 무엇을 할 것인지를 정의한다.  
스트림은 Subscribe 된 순간에 생성 된다

> JavaScript의 map함수 C#의 LINQ의 Select와 비슷하다.

- 기본적으로 Subscribe하지 않는 한 스트림은 동작하지 않는다
- Subscribe 타이밍에 의해서 결과가 바뀔 가능성이 있다.

OnError, OnComplete가 오면 Subscribe는 종료 된다

### Subscribe와 메시지

Subscribe는 오버로드로 여러 개 정의되어 있어서, 용도에 따라 사용하는게 좋다

## 스트림이라는 개념의 메리트

이벤트의 투영, 필터링, 합성등이 가능하다.

ex ) Button이 3회 눌리면 Text에 표시해라!

일반적인 경우라면 카운트용의 변수를 필드에 정의하고 버튼이 눌렀을시의 이벤트를 받아서 변수를 증가 시켜야한다.

하지만 UniRx는 아래와 같이 구현 가능하다.

Buffer(3)을 추가하면된다.

- 굳이 필드 변수 추가 필요 없음
- 혹은 Skip(2)로도 똑같은 동작을 한다
  > n회후에 동작하는 경우에는 Skip 쪽이 더 적절하다.

```C#
button
    .OnClickAsObservable()
    .Buffer(3)
    .SubscribeToText(text, _=> "clicked");
```

Rx는

1. 스트림을 준비해서
2. 스트림을 오퍼레이토로 가공 해서
3. 최후에 Subscribe 한다.

## 오퍼레이터

스트림을 조작하는 메소드

종류는 엄청나게 많다.

Select, Where, Skip, SkipUntil, SkipWhile, Take, TakeUntil, TakeWhile, Throttle, Zip,
Merge, CombineLatest, Distinct, DistinctUntilChanged, Delay, DelayFrame, First,
FirstOfDefault, Last, LastOfDefault, StartWith, Concat, Buffer, Cast, Catch,
CatchIgnore, ObserveOn, Do, Sample, Scan, Single, SingleOrDefault, Retry, Repeat,
Time, TimeStamp, TimeInterval... 등등

### 자주 사용 하는 오퍼레이터

1. Where

조건을 만족하는 메세지만 통과 시키는 오퍼레이터

> 다른 언어에서는 \[filter]라고도 한다.

![Rx where](https://user-images.githubusercontent.com/67315288/121776437-c80a9800-cbc7-11eb-8ed7-dc4c3634d648.png)

2. Selet

요소의 값을 변경한다

> 다른 언어에서는 \[map]이라고 한다.

![Rx Select](https://user-images.githubusercontent.com/67315288/121776427-c640d480-cbc7-11eb-9cc5-78c771ccd6d7.png)

3. SelectMany

새로운 스트림을 생성하고, 그 스트림이 흐르는 메세지를 본래의 스트림의 메세지로 취급

> 스트림을 다른 스트림으로 교체하는 이미지 ( 정밀히 말하면 다름 )
> 다른 언어에서는 \[flatMap]이라고도 한다.

![Rx SelectMany](https://user-images.githubusercontent.com/67315288/121776429-c640d480-cbc7-11eb-8b1a-7097d499f238.png)

4. Throttle / ThrottleFrame

도착한 때에 최후의 메세지를 보낸다

> 메시지가 집중해서 들어 올때에 마지막 이외를 무시한다
> 다른 언어에서는 [debounce]라고도 한다
> 자주 사용됨

![Rx Throttle_ThrottleFrame](https://user-images.githubusercontent.com/67315288/121776434-c7720180-cbc7-11eb-9169-8817db8448b3.png)

5. ThrottleFirst / ThrottleFirstFrame

최초의 메시지가 올때부터 일정 시간 무시 한다

> 하나의 메시지가 온때부터 잠시 메세지를 무시 한다
> 대용량으로 들어오는 데이터의 첫번째만 사용하고 싶을 때 유효

![Rx ThrottleFirst_ThrottleFirstFrame](https://user-images.githubusercontent.com/67315288/121776435-c80a9800-cbc7-11eb-910a-fca20c1941bc.png)

6. Delay / DelayFrame

메시지의 전달을 연기 한다

![Rx Delay_DelayFrame](https://user-images.githubusercontent.com/67315288/121776438-c8a32e80-cbc7-11eb-8e1e-97b9c1befbea.png)

7. DistinctUntilChanged

메세지가 변화한 순간에만 통지한다

> 같은 값이 연속되는 경우에는 무시한다

![Rx DistinctUntilChanged](https://user-images.githubusercontent.com/67315288/121776423-c50fa780-cbc7-11eb-9761-d433dc9fb2e6.png)

8. SkipUntil

지정한 스트림에 메시지가 올때까지 메시지를 Skip 한다

> 같은 값이 연속되는 경우에 무시한다

![Rx SkipUntil](https://user-images.githubusercontent.com/67315288/121776430-c6d96b00-cbc7-11eb-9a3b-94fd58e82309.png)

9. TakeUntil

지정한 스트림에 메시지가 오면, 자신의 스트림에 OnCompleted를 보내서 종료 한다

![Rx TakeUntil](https://user-images.githubusercontent.com/67315288/121776433-c7720180-cbc7-11eb-8d13-43b1cc762fa1.png)

10. Repeat

스트림이 OnCompleted로 종료될 때에 다시 한번 Subscribe를 한다

11. First

스트림에 최초로 받은 메시지만 보낸다

> OnNext 직후에 OnComplete도 보낸다

![Rx First](https://user-images.githubusercontent.com/67315288/121776426-c5a83e00-cbc7-11eb-9c10-094c29358af2.png)

### 그외에 자주 사용되는 조합

1. SkipUntil + TakeUntil + Repeat

이벤트 A가 올때부터 이벤트 B가 올때까지 처리를 하고 싶을때 사용

> ex) 드래그로 오브젝트를 회전 시키기
> MouseDown이 올 때부터 Mouse Up이 올때까지 처리할 때

![Rx SkipUntil+TakeUntil+Repeat](https://user-images.githubusercontent.com/67315288/121776432-c7720180-cbc7-11eb-9194-7bb9b9bb8b28.png)
