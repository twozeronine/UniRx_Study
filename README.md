# UniRx_Study

> [Csharp_Study](https://github.com/twozeronine/Csharp_Study) C# 공부내용 링크
> [Unity_Study](https://github.com/twozeronine/Unity_Study) Unity 공부내용 링크

Today I Learnred (TIL)

> UniRX Functional Reactive Programming(함수 반응형 프로그래밍)에 대해 공부한 내용을 정리하는 저장소.
> Microsoft에서 이미 C#용으로 만든 Rx.NET가 있지만 무겁고 기술적인 이슈로 Unity 전용으로 최적화된 UniRx를 사용한다고 한다.

Rx는 하나의 프로그래밍 방법론이고 어떤 언어라도 Rx의 개념은 똑같다 !

## 참고 사이트

[ReactiveX 공식 사이트](http://reactivex.io/)  
[UniRx 공식 깃허브](https://github.com/neuecc/UniRx)  
[UniRx 소개 slideshare](https://www.slideshare.net/agebreak/160402-unirx)  
[UniRx 시작하기 slideshare](https://www.slideshare.net/agebreak/160409-unirx?from_action=save)  
[Rx와 Functional Reactive Programming으로 고성능 서버 만들기 slideshare](https://www.slideshare.net/jongwookkim/ndc14-rx-functional-reactive-programming)  
[UniRx에 대해 정리한 블로그](https://rito15.github.io/posts/unity-study-unirx/)

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

- 기본적으로 Subscribe하지 않는 한 스트림은 동작하지 않는다
- Subscribe 타이밍에 의해서 결과가 바뀔 가능성이 있다.

OnError, OnComplete가 오면 Subscribe는 종료 된다

### Subscribe와 메시지

Subscribe는 오버로드로 여러 개 정의되어 있어서, 용도에 따라 사용하는게 좋다
